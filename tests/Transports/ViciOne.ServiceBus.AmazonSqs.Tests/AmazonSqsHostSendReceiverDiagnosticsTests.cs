using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsHostSendReceiverDiagnosticsTests
{
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "host-own-debug-preserves-connected-endpoint-start")]
    public async Task HostConnection_OptionalDiagnosticPreservesActualEndpointStartAsync(bool hostile)
    {
        var previous = LogContext.Current;
        var logger = new SelectedLogger("Connect receive endpoint: {InputAddress}", hostile);
        Fixture? fixture = null;
        IHostReceiveEndpointHandle? handle = null;
        var stops = new List<Task>();
        await RunAndRetireAsync(async () =>
        {
            LogContext.ConfigureCurrentLogContext(logger);
            fixture = new Fixture(false);
            var host = new AmazonSqsHost(fixture.Host, fixture.Host.Topology);
            Uri? configuredAddress = null;
            Exception? invocationFailure = Record.Exception(() =>
            {
                handle = host.ConnectReceiveEndpoint(Fixture.QueueName, (IAmazonSqsReceiveEndpointConfigurator configuration) =>
                {
                    configuration.AutoStart = false;
                    configuredAddress = configuration.InputAddress;
                });
            });
            logger.AssertSelected(hostile, "InputAddress", configuredAddress);
            if (invocationFailure is not null)
                Assert.Same(logger.Failure, invocationFailure);
            Assert.Null(invocationFailure);
            Assert.NotNull(handle);
            ReceiveEndpointReady ready = await handle.Ready.WaitAsync(Bound, CancellationToken.None);
            Assert.False(ready.IsStarted);
            Assert.Equal(configuredAddress, ready.InputAddress);
            Assert.Empty(fixture.Calls);
            Task stop = handle.StopAsync(CancellationToken.None);
            stops.Add(stop);
            await stop.WaitAsync(Bound, CancellationToken.None);
        }, async failures =>
        {
            logger.Armed = false;
            if (handle is not null)
                await CaptureAsync(() =>
                {
                    stops.Add(handle.StopAsync(CancellationToken.None));
                    return Task.CompletedTask;
                }, failures);
            foreach (Task stop in stops)
                await CaptureAsync(() => ObserveAsync(stop), failures);
            if (fixture is not null)
                await fixture.RetireAsync(failures);
        }, () => LogContext.Current = previous);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "send-creation-own-debug-preserves-real-child-and-transport-admissions")]
    public async Task SendCreation_OptionalDiagnosticPreservesBothRealOwnerAdmissionsAsync(bool topic, bool hostile)
    {
        var previous = LogContext.Current;
        var logger = new SelectedLogger("Create send transport: {DestinationAddress}", hostile);
        Fixture? fixture = null;
        Task<ISendTransport>? operation = null;
        var admitted = new List<IAgent>();
        await RunAndRetireAsync(async () =>
        {
            LogContext.ConfigureCurrentLogContext(logger);
            fixture = new Fixture(true);
            IClientContextSupervisor parent = fixture.Parent!;
            var recordingParent = InterfaceProxy<IClientContextSupervisor>.Create((method, arguments) =>
            {
                if (method.Name == nameof(IClientContextSupervisor.AddSendAgent))
                    admitted.Add((IAgent)(arguments?[0] ?? throw new InvalidOperationException("Missing admitted agent.")));
                try { return method.Invoke(parent, arguments); }
                catch (TargetInvocationException exception) when (exception.InnerException is not null)
                {
                    ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                    throw;
                }
            });
            var address = new Uri(topic ? "topic:diagnostic-destination" : "queue:diagnostic-destination");
            Uri normalized = fixture.Supervisor.NormalizeAddress(address);
            Exception? invocationFailure = Record.Exception(() =>
            {
                operation = fixture.Supervisor.CreateSendTransportAsync(fixture.Context!, recordingParent, address, CancellationToken.None);
            });
            Exception? taskFailure = operation is null ? null
                : await Record.ExceptionAsync(() => operation.WaitAsync(Bound, CancellationToken.None));
            logger.AssertSelected(hostile, "DestinationAddress", normalized);
            if (invocationFailure is not null)
                Assert.Same(logger.Failure, invocationFailure);
            if (taskFailure is not null)
                Assert.Same(logger.Failure, taskFailure);
            Assert.Null(invocationFailure);
            Assert.Null(taskFailure);
            Assert.NotNull(operation);
            Assert.True(operation.IsCompletedSuccessfully);
            var returned = Assert.IsType<SendTransport<ClientContext>>(await operation.WaitAsync(Bound, CancellationToken.None));
            Assert.Equal(2, admitted.Count);
            Assert.IsType<ClientContextSupervisor>(admitted[0]);
            Assert.Same(returned, admitted[1]);
            Assert.NotSame(admitted[0], admitted[1]);
            Assert.Equal(1, returned.TotalCount);
            Assert.Empty(fixture.Calls);
            await returned.StopAsync(new FixtureStopContext(), CancellationToken.None).WaitAsync(Bound, CancellationToken.None);
            await returned.Completed.WaitAsync(Bound, CancellationToken.None);
            await admitted[0].Completed.WaitAsync(Bound, CancellationToken.None);
        }, async failures =>
        {
            logger.Armed = false;
            if (operation is not null)
                await CaptureAsync(() => ObserveAsync(operation, logger.Failure), failures);
            foreach (IAgent agent in admitted)
            {
                await CaptureAsync(() => agent.StopAsync(new FixtureStopContext(), CancellationToken.None).WaitAsync(Bound, CancellationToken.None), failures);
                await CaptureAsync(() => agent.Completed.WaitAsync(Bound, CancellationToken.None), failures);
            }
            if (fixture is not null)
                await fixture.RetireAsync(failures);
        }, () => LogContext.Current = previous);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "visibility-own-debug-preserves-real-poll-admission")]
    public async Task ReceiverVisibility_OptionalDiagnosticPreservesActualReceiveAdmissionAsync(bool hostile)
    {
        var previous = LogContext.Current;
        var logger = new SelectedLogger("Using queue visibility timeout of {VisibilityTimeout}", hostile);
        Fixture? fixture = null;
        AmazonSqsMessageReceiver? receiver = null;
        var publicTasks = new List<Task>();
        await RunAndRetireAsync(async () =>
        {
            LogContext.ConfigureCurrentLogContext(logger);
            fixture = new Fixture(true);
            fixture.CreateClient();
            fixture.Client!.GetOrAddPayload<ReceiveSettings>(() => fixture.Settings!);
            Exception? invocationFailure = Record.Exception(() =>
            {
                receiver = new AmazonSqsMessageReceiver(fixture.Client!, fixture.Context!);
            });
            Assert.Null(invocationFailure);
            Assert.NotNull(receiver);
            await fixture.AttributesEntered.Task.WaitAsync(Bound, CancellationToken.None);
            Assert.False(fixture.AttributesRaw.IsCompleted);
            Assert.Empty(logger.Selected);
            Assert.Equal(0, fixture.ReceiveCalls);
            SdkCall lookup = Assert.Single(fixture.Calls, call => call.Name == nameof(IAmazonSQS.GetQueueUrlAsync));
            Assert.Equal(Fixture.QueueName, lookup.Arguments[0]);
            Assert.True(lookup.Raw.IsCompletedSuccessfully);
            SdkCall attributes = Assert.Single(fixture.Calls, call => call.Name == nameof(IAmazonSQS.GetQueueAttributesAsync));
            Assert.Equal(Fixture.QueueUrl, attributes.Arguments[0]);
            Assert.Equal("All", Assert.Single(Assert.IsAssignableFrom<IEnumerable<string>>(attributes.Arguments[1])));
            Assert.True(attributes.Token.CanBeCanceled);
            fixture.ReleaseAttributes();
            await Task.WhenAny(receiver.Completed, fixture.ReceiveEntered.Task).WaitAsync(Bound, CancellationToken.None);
            logger.AssertSelected(hostile, "VisibilityTimeout", "45s");
            Assert.Equal(Fixture.QueueUrl, fixture.Settings!.QueueUrl);
            Assert.True(fixture.AttributesRaw.IsCompletedSuccessfully);
            if (receiver.Completed.IsCompleted && fixture.ReceiveCalls == 0)
            {
                await receiver.Completed.WaitAsync(Bound, CancellationToken.None);
                Emission warning = Assert.Single(logger.Warnings);
                Assert.Equal("Consumer stop faulted: {InputAddress}", warning.Fields["{OriginalFormat}"]);
                Assert.Equal(fixture.Context!.InputAddress, warning.Fields["InputAddress"]);
                Assert.Same(logger.Failure, warning.Cause);
                // Ready may still be pending; Completed does not prove the private observer task was joined.
            }
            Assert.Equal(1, fixture.ReceiveCalls);
            await receiver.Ready.WaitAsync(Bound, CancellationToken.None);
            Assert.Equal(45, fixture.Settings.VisibilityTimeout);
            SdkCall receive = Assert.Single(fixture.Calls, call => call.Name == nameof(IAmazonSQS.ReceiveMessageAsync));
            var request = Assert.IsType<ReceiveMessageRequest>(receive.Arguments[0]);
            Assert.Equal(Fixture.QueueUrl, request.QueueUrl);
            Assert.Equal(2, request.MaxNumberOfMessages);
            Assert.Equal(fixture.Settings.WaitTimeSeconds, request.WaitTimeSeconds);
            Assert.Equal("All", Assert.Single(request.MessageSystemAttributeNames));
            Assert.Equal("All", Assert.Single(request.MessageAttributeNames));
            Assert.False(receive.Raw.IsCompleted);
            Task stop = receiver.StopAsync(new FixtureStopContext(), CancellationToken.None);
            publicTasks.Add(stop);
            fixture.CancelReceives();
            await stop.WaitAsync(Bound, CancellationToken.None);
            await receiver.Completed.WaitAsync(Bound, CancellationToken.None);
        }, async failures =>
        {
            logger.Armed = false;
            fixture?.ReleaseAttributes();
            if (receiver is not null)
            {
                await CaptureAsync(() =>
                {
                    publicTasks.Add(receiver.StopAsync(new FixtureStopContext(), CancellationToken.None));
                    return Task.CompletedTask;
                }, failures);
                fixture?.CancelReceives();
                foreach (Task task in publicTasks)
                    await CaptureAsync(() => ObserveAsync(task, logger.Failure), failures);
                await CaptureAsync(() => ObserveAsync(receiver.Completed, logger.Failure), failures);
            }
            if (fixture is not null)
                await fixture.RetireAsync(failures);
        }, () => LogContext.Current = previous);
    }

    static async Task RunAndRetireAsync(Func<Task> body, Func<List<Exception>, Task> retire, Action restore)
    {
        Exception? primary = null;
        var failures = new List<Exception>();
        try { await body(); }
        catch (Exception exception) { primary = exception; }
        try { await retire(failures); }
        catch (Exception exception) { failures.Add(exception); }
        finally { restore(); }
        if (failures.Count != 0)
        {
            if (primary is not null)
                failures.Insert(0, primary);
            throw new AggregateException("Public diagnostic control and fixture retirement failed.", failures);
        }
        if (primary is not null)
            ExceptionDispatchInfo.Capture(primary).Throw();
    }

    static async Task CaptureAsync(Func<Task> action, List<Exception> failures)
    {
        try { await action(); }
        catch (Exception exception) { failures.Add(exception); }
    }

    static async Task ObserveAsync(Task task, Exception? known = null)
    {
        try { await task.WaitAsync(Bound, CancellationToken.None); }
        catch (Exception exception) when (task.IsFaulted && ReferenceEquals(exception, known)) { }
    }

    sealed class FixtureStopContext() : ViciOne.ServiceBus.Middleware.BasePipeContext(CancellationToken.None), StopContext
    {
        public string Reason => "SQS public diagnostics fixture retirement";
    }

    sealed class NameFormatter : IEntityNameFormatter
    {
        public string FormatEntityName<T>() => typeof(T).Name;
    }

    sealed record Emission(Dictionary<string, object?> Fields, Exception? Cause);
    sealed class SelectedLogger(string template, bool hostile) : ILogger
    {
        public readonly IOException Failure = new("unique host/send/receiver selected diagnostic failure");
        public readonly ConcurrentQueue<Emission> Selected = new();
        public readonly ConcurrentQueue<Emission> Warnings = new();
        public volatile bool Armed = true;
        int _throws;
        public bool IsEnabled(LogLevel level) => level is LogLevel.Debug or LogLevel.Warning;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new EmptyScope();
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (state is not IEnumerable<KeyValuePair<string, object?>> fields)
                return;
            var values = fields.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (!values.TryGetValue("{OriginalFormat}", out var actual))
                return;
            if (level == LogLevel.Warning && Equals(actual, "Consumer stop faulted: {InputAddress}"))
                Warnings.Enqueue(new Emission(values, exception));
            if (level != LogLevel.Debug || !Equals(actual, template))
                return;
            Selected.Enqueue(new Emission(values, exception));
            if (Armed && hostile)
            {
                Interlocked.Increment(ref _throws);
                throw Failure;
            }
        }
        public void AssertSelected(bool selectedHostile, string argument, object? expected)
        {
            Emission emission = Assert.Single(Selected);
            Assert.Equal(template, emission.Fields["{OriginalFormat}"]);
            Assert.Equal(expected, emission.Fields[argument]);
            Assert.Null(emission.Cause);
            Assert.Equal(selectedHostile ? 1 : 0, Volatile.Read(ref _throws));
        }
        sealed class EmptyScope : IDisposable { public void Dispose() { } }
    }

    sealed record SdkCall(string Name, object?[] Arguments, CancellationToken Token, Task Raw);
    sealed record PendingReceive(TaskCompletionSource<ReceiveMessageResponse> Source, CancellationToken Token);
    sealed class Fixture
    {
        public const string QueueName = "diagnostic-endpoint";
        public const string QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/diagnostic-endpoint";
        const string QueueArn = "arn:aws:sqs:us-east-1:123456789012:diagnostic-endpoint";
        readonly TaskCompletionSource<GetQueueAttributesResponse> _attributes = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly ConcurrentQueue<PendingReceive> _receives = new();
        readonly CancellationTokenSource _lifetime = new();
        public readonly ConcurrentQueue<SdkCall> Calls = new();
        public readonly TaskCompletionSource AttributesEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource ReceiveEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly IAmazonSQS Sqs;
        public readonly IAmazonSimpleNotificationService Sns;
        public readonly IAmazonSqsHostConfiguration Host;
        public readonly IConnectionContextSupervisor Supervisor;
        public readonly SqsReceiveEndpointContext? Context;
        public readonly IClientContextSupervisor? Parent;
        public readonly ReceiveSettings? Settings;
        public AmazonSqsConnectionContext? Connection;
        public ClientContext? Client;
        int _receiveCalls;
        int _cancelReceives;
        int _sqsDisposals;
        int _snsDisposals;
        public int ReceiveCalls => Volatile.Read(ref _receiveCalls);
        public Task AttributesRaw => _attributes.Task;

        public Fixture(bool buildContext)
        {
            Sqs = InterfaceProxy<IAmazonSQS>.Create(HandleSqs);
            Sns = InterfaceProxy<IAmazonSimpleNotificationService>.Create((method, _) =>
            {
                if (method.Name != nameof(IDisposable.Dispose))
                    throw new NotSupportedException("Unexpected SNS operation: " + method);
                Interlocked.Increment(ref _snsDisposals);
                return null;
            });
            var host = new AmazonSqsHostConfigurator(new Uri("amazonsqs://us-east-1/"));
            host.ClientFactories(() => Sqs, () => Sns);
            var topology = new AmazonSqsTopologyConfiguration(new ViciOne.ServiceBus.Topology.MessageTopology(new NameFormatter()));
            var bus = new AmazonSqsBusConfiguration(topology);
            Host = bus.HostConfiguration;
            Host.Settings = host.Settings;
            Host.LogContext = LogContext.Current;
            Supervisor = Host.ConnectionContextSupervisor;
            if (buildContext)
            {
                var configuration = Host.CreateReceiveEndpointConfiguration(QueueName, configure =>
                {
                    configure.PrefetchCount = 2;
                    configure.ConcurrentMessageLimit = 2;
                });
                Settings = configuration.Settings;
                Context = Assert.IsAssignableFrom<SqsReceiveEndpointContext>(configuration.CreateReceiveEndpointContext());
                Parent = Context.ClientContextSupervisor;
            }
        }

        public void CreateClient()
        {
            Connection = new AmazonSqsConnectionContext(Host.Settings.CreateConnection(), Host, _lifetime.Token);
            Client = Connection.CreateClientContext(_lifetime.Token);
        }

        object? HandleSqs(MethodInfo method, object?[]? arguments)
        {
            if (method.Name == nameof(IDisposable.Dispose))
            {
                Interlocked.Increment(ref _sqsDisposals);
                return null;
            }
            object?[] args = arguments ?? throw new InvalidOperationException("SDK arguments missing.");
            if (args.Length == 0 || args[^1] is not CancellationToken token)
                throw new NotSupportedException("Unexpected SDK overload: " + method);
            Task raw;
            switch (method.Name)
            {
                case nameof(IAmazonSQS.GetQueueUrlAsync):
                    raw = Task.FromResult(new GetQueueUrlResponse { QueueUrl = QueueUrl, HttpStatusCode = HttpStatusCode.OK });
                    break;
                case nameof(IAmazonSQS.GetQueueAttributesAsync):
                    raw = _attributes.Task;
                    break;
                case nameof(IAmazonSQS.ReceiveMessageAsync):
                    var source = new TaskCompletionSource<ReceiveMessageResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _receives.Enqueue(new PendingReceive(source, token));
                    if (Volatile.Read(ref _cancelReceives) != 0)
                        source.TrySetCanceled(token);
                    raw = source.Task;
                    Interlocked.Increment(ref _receiveCalls);
                    break;
                default:
                    throw new NotSupportedException("Unexpected SQS operation: " + method);
            }
            Calls.Enqueue(new SdkCall(method.Name, args.ToArray(), token, raw));
            if (method.Name == nameof(IAmazonSQS.GetQueueAttributesAsync))
                AttributesEntered.TrySetResult();
            if (method.Name == nameof(IAmazonSQS.ReceiveMessageAsync))
                ReceiveEntered.TrySetResult();
            return raw;
        }

        public void ReleaseAttributes() => _attributes.TrySetResult(new GetQueueAttributesResponse
        {
            HttpStatusCode = HttpStatusCode.OK,
            Attributes = new Dictionary<string, string>
            {
                [QueueAttributeName.QueueArn] = QueueArn,
                [QueueAttributeName.VisibilityTimeout] = "45"
            }
        });

        public void CancelReceives()
        {
            Interlocked.Exchange(ref _cancelReceives, 1);
            foreach (PendingReceive receive in _receives.ToArray())
                receive.Source.TrySetCanceled(receive.Token);
        }

        public async Task RetireAsync(List<Exception> failures)
        {
            ReleaseAttributes();
            CancelReceives();
            if (Parent is not null)
            {
                await CaptureAsync(() => Parent.StopAsync(new FixtureStopContext(), CancellationToken.None).WaitAsync(Bound, CancellationToken.None), failures);
                await CaptureAsync(() => Parent.Completed.WaitAsync(Bound, CancellationToken.None), failures);
            }
            await CaptureAsync(() => Supervisor.StopAsync(new FixtureStopContext(), CancellationToken.None).WaitAsync(Bound, CancellationToken.None), failures);
            await CaptureAsync(() => Supervisor.Completed.WaitAsync(Bound, CancellationToken.None), failures);
            if (Context is not null)
                await CaptureAsync(() => Context.ResetAsync(CancellationToken.None).AsTask().WaitAsync(Bound, CancellationToken.None), failures);
            // Every public operation is retired before the final actual SDK task snapshot.
            foreach (SdkCall call in Calls.ToArray())
                await CaptureAsync(async () =>
                {
                    try { await call.Raw.WaitAsync(Bound, CancellationToken.None); }
                    catch (OperationCanceledException exception) when (call.Raw.IsCanceled
                        && call.Name == nameof(IAmazonSQS.ReceiveMessageAsync) && exception.CancellationToken == call.Token) { }
                }, failures);
            if (Connection is not null)
                await CaptureAsync(() => Connection.DisposeAsync().AsTask().WaitAsync(Bound, CancellationToken.None), failures);
            await CaptureAsync(() => { _lifetime.Dispose(); return Task.CompletedTask; }, failures);
            if (Volatile.Read(ref _sqsDisposals) == 0)
                await CaptureAsync(() => { Sqs.Dispose(); return Task.CompletedTask; }, failures);
            if (Volatile.Read(ref _snsDisposals) == 0)
                await CaptureAsync(() => { Sns.Dispose(); return Task.CompletedTask; }, failures);
        }
    }
}
