using System.Collections.Concurrent;
using System.Net;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsConfiguredPipelinesAndSendTopologyTests
{
    static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(10);
    static CancellationToken TestToken => TestContext.Current.CancellationToken;
    const string QueueName = "configured-orders";
    const string QueueUrl = "https://sqs.eu-central-1.amazonaws.com/123456789012/configured-orders";
    const string QueueArn = "arn:aws:sqs:eu-central-1:123456789012:configured-orders";

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-CONFIGURED-CONNECTION-PIPE", "endpoint-declared-context-filter-precedes-actual-provider-topology")]
    public async Task ConfiguredContextFilters_ExecuteBeforeProviderTopologyAsync(int route)
    {
        var sdk = new QueueSdkProbe();
        var clientFilter = new HeldFilter<ClientContext>();
        var connectionFilter = new HeldFilter<ConnectionContext>();
        var topology = new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology());
        var configuration = new AmazonSqsBusConfiguration(topology);
        var hostSettings = new AmazonSqsHostConfigurator(new Uri("amazonsqs://eu-central-1/"));
        hostSettings.ClientFactories(() => sdk.Sqs, () => sdk.Sns);
        configuration.HostConfiguration.Settings = hostSettings.Settings;
        configuration.HostConfiguration.DeployTopologyOnly = true;
        var endpoint = Assert.IsType<AmazonSqsReceiveEndpointConfiguration>(
            configuration.HostConfiguration.CreateReceiveEndpointConfiguration(QueueName, configure =>
            {
                if (route == 0)
                    configure.ConfigureClient(pipe => pipe.UseFilter(clientFilter));
                else if (route == 1)
                    configure.ConfigureConnection(pipe => pipe.UseFilter(connectionFilter));
                else if (route == 3)
                {
                    configure.ConfigureConnection(null);
                    configure.ConfigureClient(null);
                }
            }));
        if (route == 2)
            endpoint.ConfigureConnection(pipe => pipe.UseFilter(connectionFilter));
        Assert.DoesNotContain(endpoint.Validate(), result => result.Disposition == ValidationResultDisposition.Failure);
        ILogContext? previous = LogContext.Current;
        IHostHandle? handle = null;
        Task<HostReady>? readiness = null;
        try
        {
            LogContext.ConfigureCurrentLogContext();
            configuration.HostConfiguration.LogContext = LogContext.Current;
            IHost host = configuration.HostConfiguration.Build();
            handle = host.Start(TestToken);
            readiness = handle.Ready;
            if (route != 3)
            {
                Task entered = route == 0 ? clientFilter.Entered.Task : connectionFilter.Entered.Task;
                // The first actual public pipeline boundary wins. Missing wiring fails a finite call-count oracle.
                await Task.WhenAny(entered, sdk.CreateEntered.Task).WaitAsync(Watchdog, TestToken);
                Assert.Equal(1, route == 0 ? clientFilter.Calls : connectionFilter.Calls);
                Assert.Equal(0, sdk.CreateCalls);
                Assert.False(readiness.IsCompleted);
                Assert.Equal(0, sdk.SqsDisposals);
                Assert.Equal(0, sdk.SnsDisposals);
                clientFilter.Release.TrySetResult();
                connectionFilter.Release.TrySetResult();
            }
            await sdk.CreateEntered.Task.WaitAsync(Watchdog, TestToken);
            Assert.Equal(QueueName, sdk.Submitted!.QueueName);
            Assert.Equal(1, sdk.CreateCalls);
            Assert.False(sdk.ActualCreateTask.IsCompleted);
            Assert.False(readiness.IsCompleted);
            sdk.Release.TrySetResult();
            await readiness.WaitAsync(Watchdog, TestToken);
            Assert.True(sdk.ActualCreateTask.IsCompletedSuccessfully);
            Assert.Equal(1, sdk.LookupCalls);
            Assert.Equal(1, sdk.AttributeCalls);
            Assert.Equal(route == 0 ? 1 : 0, clientFilter.Calls);
            Assert.Equal(route is 1 or 2 ? 1 : 0, connectionFilter.Calls);
            await handle.StopAsync(CancellationToken.None).WaitAsync(Watchdog, TestToken);
            Assert.Equal(1, sdk.SqsDisposals);
            Assert.Equal(1, sdk.SnsDisposals);
        }
        finally
        {
            clientFilter.Release.TrySetResult();
            connectionFilter.Release.TrySetResult();
            sdk.Release.TrySetResult();
            try
            {
                if (readiness is not null)
                    await ObserveJoinAsync(readiness);
            }
            finally
            {
                try
                {
                    if (handle is not null)
                        await handle.StopAsync(CancellationToken.None).WaitAsync(Watchdog, CancellationToken.None);
                }
                finally
                {
                    try { await sdk.JoinActualTasksAsync(); }
                    finally { LogContext.Current = previous; }
                }
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-SEND-TOPOLOGY-METADATA", "send-settings-preserve-declared-queue-attributes-subscriptions-and-tags")]
    public async Task QueueSendTopology_PreservesConfiguredMetadataToProviderAsync(int metadataKind)
    {
        var settings = new QueueSendSettings(new AmazonSqsEndpointAddress(
            new Uri("amazonsqs://eu-central-1/"), new Uri("queue:" + QueueName)));
        if (metadataKind == 1)
        {
            settings.QueueAttributes[QueueAttributeName.VisibilityTimeout] = 47;
            settings.QueueAttributes[QueueAttributeName.MessageRetentionPeriod] = 3600;
        }
        else if (metadataKind == 2)
        {
            settings.QueueTags["tenant"] = "blue";
            settings.QueueTags["owner"] = "ops";
        }
        else if (metadataKind == 3)
        {
            settings.QueueSubscriptionAttributes["RawMessageDelivery"] = "false";
            settings.QueueSubscriptionAttributes["FilterPolicy"] = "{\"tenant\":[\"blue\"]}";
        }
        BrokerTopology topology = settings.GetBrokerTopology();
        Queue declaration = Assert.Single(topology.Queues);
        Assert.Equal(QueueName, declaration.EntityName);
        Assert.True(declaration.Durable);
        Assert.False(declaration.AutoDelete);
        Assert.Empty(topology.Topics);
        Assert.Empty(topology.QueueSubscriptions);
        var sdk = new QueueSdkProbe();
        await using var cache = new QueueCache(sdk.Sqs, new AmazonSqsClientContextCacheOptions(), TestToken);
        Task<QueueInfo>? operation = null;
        try
        {
            operation = cache.GetAsync(declaration, TestToken);
            await sdk.CreateEntered.Task.WaitAsync(Watchdog, TestToken);
            Assert.False(operation.IsCompleted);
            Assert.False(sdk.ActualCreateTask.IsCompleted);
            Assert.True(sdk.CreateToken.CanBeCanceled);
            Assert.False(sdk.CreateToken.IsCancellationRequested);
            Assert.Equal(QueueName, sdk.Submitted!.QueueName);
            var attributes = settings.QueueAttributes.ToDictionary(pair => pair.Key,
                pair => Convert.ToString(pair.Value, System.Globalization.CultureInfo.InvariantCulture)!);
            Assert.Equal(attributes.OrderBy(pair => pair.Key), sdk.Submitted.Attributes.OrderBy(pair => pair.Key));
            Assert.Equal(settings.QueueTags.OrderBy(pair => pair.Key), sdk.Submitted.Tags.OrderBy(pair => pair.Key));
            Assert.DoesNotContain("RawMessageDelivery", sdk.Submitted.Attributes.Keys);
            Assert.DoesNotContain("FilterPolicy", sdk.Submitted.Attributes.Keys);
            Assert.Equal(settings.QueueAttributes.OrderBy(pair => pair.Key), declaration.QueueAttributes.OrderBy(pair => pair.Key));
            Assert.Equal(settings.QueueTags.OrderBy(pair => pair.Key), declaration.QueueTags.OrderBy(pair => pair.Key));
            Assert.Equal(settings.QueueSubscriptionAttributes.OrderBy(pair => pair.Key), declaration.QueueSubscriptionAttributes.OrderBy(pair => pair.Key));
            sdk.Release.TrySetResult();
            QueueInfo result = await operation.WaitAsync(Watchdog, TestToken);
            Assert.False(result.Existing);
            Assert.Equal(QueueUrl, result.Url);
            Assert.Equal(QueueArn, result.Arn);
            Assert.Same(result, await cache.GetAsync(declaration, TestToken));
            Assert.Equal(1, sdk.LookupCalls);
            Assert.Equal(1, sdk.CreateCalls);
            Assert.Equal(1, sdk.AttributeCalls);
        }
        finally
        {
            sdk.Release.TrySetResult();
            try
            {
                if (operation is not null)
                    await ObserveJoinAsync(operation);
            }
            finally { await sdk.JoinActualTasksAsync(); }
        }
    }

    static async Task ObserveJoinAsync(Task task)
    {
        Exception? failure = await Record.ExceptionAsync(() => task.WaitAsync(Watchdog, CancellationToken.None));
        Assert.IsNotType<TimeoutException>(failure);
    }

    sealed class HeldFilter<T> : IFilter<T> where T : class, PipeContext
    {
        int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task SendAsync(T context, IPipe<T> next)
        {
            Interlocked.Increment(ref _calls);
            Entered.TrySetResult();
            await Release.Task.ConfigureAwait(false);
            await next.SendAsync(context).ConfigureAwait(false);
        }
        public void Probe(ProbeContext context) { }
    }

    sealed class QueueSdkProbe
    {
        int _lookups;
        int _creates;
        int _attributes;
        int _sqsDisposals;
        int _snsDisposals;
        readonly ConcurrentQueue<Task> _actualTasks = new();
        public TaskCompletionSource CreateEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CreateQueueRequest? Submitted { get; private set; }
        public CancellationToken CreateToken { get; private set; }
        public Task<CreateQueueResponse> ActualCreateTask { get; private set; } = null!;
        public int LookupCalls => Volatile.Read(ref _lookups);
        public int CreateCalls => Volatile.Read(ref _creates);
        public int AttributeCalls => Volatile.Read(ref _attributes);
        public int SqsDisposals => Volatile.Read(ref _sqsDisposals);
        public int SnsDisposals => Volatile.Read(ref _snsDisposals);
        public IAmazonSQS Sqs { get; }
        public IAmazonSimpleNotificationService Sns { get; }
        public QueueSdkProbe()
        {
            Sqs = InterfaceProxy<IAmazonSQS>.Create((method, args) => method.Name switch
            {
                nameof(IAmazonSQS.GetQueueUrlAsync) => LookupAsync(args!),
                nameof(IAmazonSQS.CreateQueueAsync) => CreateAsync(args!),
                nameof(IAmazonSQS.GetQueueAttributesAsync) => AttributesAsync(args!),
                nameof(IDisposable.Dispose) => DisposeSqs(),
                _ => throw new NotSupportedException("Unexpected SQS boundary: " + method.Name),
            });
            Sns = InterfaceProxy<IAmazonSimpleNotificationService>.Create((method, _) => method.Name switch
            {
                nameof(IDisposable.Dispose) => DisposeSns(),
                _ => throw new NotSupportedException("Unexpected SNS boundary: " + method.Name),
            });
        }
        Task<GetQueueUrlResponse> LookupAsync(object?[] args)
        {
            Assert.Equal(QueueName, Assert.IsType<string>(args[0]));
            Assert.IsType<CancellationToken>(args[^1]);
            Interlocked.Increment(ref _lookups);
            Task<GetQueueUrlResponse> task = Task.FromException<GetQueueUrlResponse>(new QueueDoesNotExistException("missing"));
            _actualTasks.Enqueue(task);
            return task;
        }
        Task<CreateQueueResponse> CreateAsync(object?[] args)
        {
            Submitted = Assert.IsType<CreateQueueRequest>(args[0]);
            CreateToken = Assert.IsType<CancellationToken>(args[^1]);
            Interlocked.Increment(ref _creates);
            ActualCreateTask = CompleteCreateAsync();
            _actualTasks.Enqueue(ActualCreateTask);
            CreateEntered.TrySetResult();
            return ActualCreateTask;
        }
        async Task<CreateQueueResponse> CompleteCreateAsync()
        {
            await Release.Task.ConfigureAwait(false);
            return new CreateQueueResponse { QueueUrl = QueueUrl, HttpStatusCode = HttpStatusCode.OK };
        }
        Task<GetQueueAttributesResponse> AttributesAsync(object?[] args)
        {
            Assert.Equal(QueueUrl, Assert.IsType<string>(args[0]));
            Assert.Equal([QueueAttributeName.All], Assert.IsAssignableFrom<IEnumerable<string>>(args[1]));
            Assert.IsType<CancellationToken>(args[^1]);
            Interlocked.Increment(ref _attributes);
            Task<GetQueueAttributesResponse> task = Task.FromResult(new GetQueueAttributesResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Attributes = new Dictionary<string, string> { [QueueAttributeName.QueueArn] = QueueArn,
                    [QueueAttributeName.VisibilityTimeout] = "30" },
            });
            _actualTasks.Enqueue(task);
            return task;
        }
        object? DisposeSqs() { Interlocked.Increment(ref _sqsDisposals); return null; }
        object? DisposeSns() { Interlocked.Increment(ref _snsDisposals); return null; }
        public async Task JoinActualTasksAsync()
        {
            Task[] actual = _actualTasks.ToArray();
            foreach (Task task in actual)
                await ObserveJoinAsync(task);
        }
    }
}
