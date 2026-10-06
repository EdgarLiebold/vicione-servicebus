using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using SnsTopic = Amazon.SimpleNotificationService.Model.Topic;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsConnectionAndTopologyDiagnosticsTests
{
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "connection-own-diagnostics-preserve-client-admission-and-primary-failure")]
    public async Task ConnectionCreation_OptionalDiagnosticPreservesTheFactoryOutcomeAsync(bool providerFails, bool hostile)
    {
        var fixture = new ConnectionFixture(providerFails, hostile);
        var previous = LogContext.Current;
        IPipeContextAgent<ConnectionContext>? handle = null;
        await RunAndRetireAsync(async () =>
        {
            LogContext.ConfigureCurrentLogContext(fixture.Logger);
            handle = new ConnectionContextFactory(fixture.Host).CreateContext(fixture.Owner);
            Exception? observed = await Record.ExceptionAsync(() => handle.Context.WaitAsync(Bound, CancellationToken.None));
            Assert.True(handle.Context.IsCompleted);
            Emission emission = Assert.Single(fixture.Logger.Emissions);
            Assert.Equal(fixture.Logger.Template, emission.Fields["{OriginalFormat}"]);
            Assert.Equal(hostile ? 1 : 0, fixture.Logger.Throws);
            if (providerFails)
            {
                Assert.Equal(1, fixture.SqsFactoryCalls);
                Assert.Equal(0, fixture.SnsFactoryCalls);
                Assert.Equal(fixture.Host.HostAddress, emission.Fields["InputAddress"]);
                Assert.Same(fixture.BusinessFailure, emission.Cause);
                if (hostile && observed is IOException)
                    Assert.Same(fixture.Logger.Failure, observed);
                // The intended connection wrapper must retain the real client-factory cause.
                var wrapper = Assert.IsType<AmazonSqsConnectionException>(observed);
                Assert.Same(fixture.BusinessFailure, wrapper.InnerException);
                Assert.True(handle.Context.IsFaulted);
            }
            else
            {
                Assert.Equal(fixture.Settings.ToString(), emission.Fields["Host"]);
                if (observed is not null)
                    Assert.Same(fixture.Logger.Failure, Assert.IsType<AmazonSqsConnectionException>(observed).InnerException);
                // This count is checked before fallback disposal or later success assertions.
                Assert.Equal(1, fixture.SqsFactoryCalls);
                Assert.Null(observed);
                var context = Assert.IsType<AmazonSqsConnectionContext>(await handle.Context);
                Assert.Same(fixture.Sqs, context.Connection.SqsClient);
                Assert.Same(fixture.Sns, context.Connection.SnsClient);
                Assert.Equal(1, fixture.SnsFactoryCalls);
                await handle.Ready.WaitAsync(Bound, CancellationToken.None);
                await fixture.Owner.StopAsync("connection control completed", CancellationToken.None).WaitAsync(Bound, CancellationToken.None);
                await handle.Completed.WaitAsync(Bound, CancellationToken.None);
                Assert.Equal(1, fixture.SqsDisposals);
                Assert.Equal(1, fixture.SnsDisposals);
            }
        }, async failures =>
        {
            fixture.Logger.Armed = false;
            if (handle is not null)
            {
                await CaptureAsync(() => ObserveKnownAsync(handle.Context, fixture.IsKnownFailure), failures);
                await CaptureAsync(() => ObserveKnownAsync(handle.Ready, fixture.IsKnownFailure), failures);
                await CaptureAsync(() => handle.DisposeAsync().AsTask().WaitAsync(Bound, CancellationToken.None), failures);
                await CaptureAsync(() => handle.StopAsync(new FixtureStopContext(), CancellationToken.None).WaitAsync(Bound, CancellationToken.None), failures);
                await CaptureAsync(() => handle.Completed.WaitAsync(Bound, CancellationToken.None), failures);
            }
            await CaptureAsync(() => ObserveKnownAsync(fixture.Owner.Ready, fixture.IsKnownFailure), failures);
            await CaptureAsync(() => fixture.Owner.StopAsync("connection fixture retirement", CancellationToken.None).WaitAsync(Bound, CancellationToken.None), failures);
            await CaptureAsync(() => fixture.Owner.Completed.WaitAsync(Bound, CancellationToken.None), failures);
            // Proxies not transferred to a published context remain test-owned.
            if (fixture.SqsDisposals == 0)
                await CaptureAsync(() => { fixture.Sqs.Dispose(); return Task.CompletedTask; }, failures);
            if (fixture.SnsDisposals == 0)
                await CaptureAsync(() => { fixture.Sns.Dispose(); return Task.CompletedTask; }, failures);
        }, () => LogContext.Current = previous);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "topology-own-diagnostics-preserve-real-declarations-and-next")]
    public async Task TopologyDeclaration_OptionalDiagnosticPreservesTheContinuationAsync(int route, bool hostile)
    {
        var fixture = new TopologyFixture(route, hostile);
        var previous = LogContext.Current;
        var operations = new List<Task>();
        await RunAndRetireAsync(async () =>
        {
            LogContext.ConfigureCurrentLogContext(fixture.Logger);
            Task operation = fixture.Filter.SendAsync(fixture.Client, fixture.Next);
            operations.Add(operation);
            await fixture.FinalEntered.Task.WaitAsync(Bound, CancellationToken.None);
            Assert.False(fixture.FinalRaw.IsCompleted);
            Assert.False(operation.IsCompleted);
            Assert.Equal(0, fixture.Next.Calls);
            Assert.Empty(fixture.Logger.Emissions);
            fixture.AssertRequests();
            fixture.ReleaseFinal();
            Exception? observed = await Record.ExceptionAsync(() => operation.WaitAsync(Bound, CancellationToken.None));
            Assert.True(operation.IsCompleted);
            Assert.True(fixture.FinalRaw.IsCompletedSuccessfully);
            foreach (SdkCall call in fixture.Calls.ToArray())
            {
                if (ReferenceEquals(call.Raw, fixture.MissingQueueTask))
                {
                    Assert.True(call.Raw.IsFaulted);
                    Assert.Same(fixture.MissingQueue, Assert.Single(call.Raw.Exception!.InnerExceptions));
                }
                else
                    Assert.True(call.Raw.IsCompletedSuccessfully);
            }
            fixture.AssertEmission(Assert.Single(fixture.Logger.Emissions));
            Assert.Equal(hostile ? 1 : 0, fixture.Logger.Throws);
            if (observed is not null)
                Assert.Same(fixture.Logger.Failure, observed);
            Assert.Null(observed);
            Assert.Equal(1, fixture.Next.Calls);
            Assert.Same(fixture.Client, fixture.Next.Context);
            Assert.True(fixture.Client.TryGetPayload<FixtureSettings>(out var payload));
            Assert.Same(fixture.Settings, payload);
            fixture.AssertRequests();
            int calls = fixture.Calls.Count;
            Task reuse = fixture.Filter.SendAsync(fixture.Client, fixture.Next);
            operations.Add(reuse);
            await reuse.WaitAsync(Bound, CancellationToken.None);
            Assert.Equal(2, fixture.Next.Calls);
            Assert.Equal(calls, fixture.Calls.Count);
            Assert.Single(fixture.Logger.Emissions);
        }, async failures =>
        {
            fixture.Logger.Armed = false;
            fixture.ReleaseFinal();
            // Public operations are joined before the final raw-task snapshot.
            foreach (Task operation in operations)
                await CaptureAsync(() => ObserveKnownAsync(operation, e => ReferenceEquals(e, fixture.Logger.Failure)), failures);
            foreach (SdkCall call in fixture.Calls.ToArray())
                await CaptureAsync(() => ObserveKnownAsync(call.Raw,
                    e => ReferenceEquals(call.Raw, fixture.MissingQueueTask) && ReferenceEquals(e, fixture.MissingQueue)), failures);
            await CaptureAsync(() => fixture.Connection.DisposeAsync().AsTask().WaitAsync(Bound, CancellationToken.None), failures);
            await CaptureAsync(() => { fixture.Lifetime.Dispose(); return Task.CompletedTask; }, failures);
            if (fixture.SqsDisposals == 0)
                await CaptureAsync(() => { fixture.Sqs.Dispose(); return Task.CompletedTask; }, failures);
            if (fixture.SnsDisposals == 0)
                await CaptureAsync(() => { fixture.Sns.Dispose(); return Task.CompletedTask; }, failures);
        }, () => LogContext.Current = previous);
    }

    static async Task RunAndRetireAsync(Func<Task> body, Func<List<Exception>, Task> retire, Action restore)
    {
        Exception? primary = null;
        var cleanup = new List<Exception>();
        try { await body(); }
        catch (Exception exception) { primary = exception; }
        try { await retire(cleanup); }
        catch (Exception exception) { cleanup.Add(exception); }
        finally { restore(); }
        if (cleanup.Count != 0)
        {
            if (primary is not null)
                cleanup.Insert(0, primary);
            throw new AggregateException("Public control and fixture retirement failed.", cleanup);
        }
        if (primary is not null)
            ExceptionDispatchInfo.Capture(primary).Throw();
    }

    static async Task CaptureAsync(Func<Task> action, List<Exception> failures)
    {
        try { await action(); }
        catch (Exception exception) { failures.Add(exception); }
    }

    static async Task ObserveKnownAsync(Task task, Func<Exception, bool> known)
    {
        try { await task.WaitAsync(Bound, CancellationToken.None); }
        catch (Exception exception) when (task.IsFaulted && known(exception)) { }
    }

    sealed class FixtureStopContext() : ViciOne.ServiceBus.Middleware.BasePipeContext(CancellationToken.None), StopContext
    {
        public string Reason => "connection fixture retirement";
    }

    sealed class FixtureSettings { }
    sealed class FixtureNameFormatter : IEntityNameFormatter
    {
        public string FormatEntityName<T>() => typeof(T).Name;
    }

    static IAmazonSqsHostConfiguration HostView(AmazonSqsHostSettings settings)
    {
        var topology = new AmazonSqsTopologyConfiguration(new ViciOne.ServiceBus.Topology.MessageTopology(new FixtureNameFormatter()));
        var configuration = new AmazonSqsBusConfiguration(topology);
        configuration.HostConfiguration.Settings = settings;
        return InterfaceProxy<IAmazonSqsHostConfiguration>.Create((method, _) => method.Name switch
        {
            "get_Settings" => settings,
            "get_HostAddress" => settings.HostAddress,
            "get_Topology" => configuration.HostConfiguration.Topology,
            "get_ReceiveTransportRetryPolicy" => Retry.None,
            _ => throw new NotSupportedException("Unexpected public host boundary: " + method.Name)
        });
    }

    sealed class ConnectionFixture
    {
        public readonly Supervisor Owner = new();
        public readonly IOException BusinessFailure = new("unique SQS client factory failure");
        public readonly SelectedLogger Logger;
        public readonly IAmazonSQS Sqs;
        public readonly IAmazonSimpleNotificationService Sns;
        public readonly AmazonSqsHostSettings Settings;
        public readonly IAmazonSqsHostConfiguration Host;
        public int SqsFactoryCalls;
        public int SnsFactoryCalls;
        public int SqsDisposals;
        public int SnsDisposals;

        public ConnectionFixture(bool providerFails, bool hostile)
        {
            Logger = new SelectedLogger(providerFails ? "Connection Failed: {InputAddress}" : "Connect: {Host}",
                providerFails ? LogLevel.Warning : LogLevel.Debug, hostile);
            Sqs = InterfaceProxy<IAmazonSQS>.Create((method, _) =>
            {
                if (method.Name != nameof(IDisposable.Dispose))
                    throw new NotSupportedException("Unexpected SQS request: " + method.Name);
                Interlocked.Increment(ref SqsDisposals);
                return null;
            });
            Sns = InterfaceProxy<IAmazonSimpleNotificationService>.Create((method, _) =>
            {
                if (method.Name != nameof(IDisposable.Dispose))
                    throw new NotSupportedException("Unexpected SNS request: " + method.Name);
                Interlocked.Increment(ref SnsDisposals);
                return null;
            });
            var host = new AmazonSqsHostConfigurator(new Uri("amazonsqs://us-east-1/"));
            host.ClientFactories(() =>
            {
                Interlocked.Increment(ref SqsFactoryCalls);
                if (providerFails)
                    throw BusinessFailure;
                return Sqs;
            }, () => { Interlocked.Increment(ref SnsFactoryCalls); return Sns; });
            Settings = host.Settings;
            Host = HostView(Settings);
        }

        public bool IsKnownFailure(Exception exception) => ReferenceEquals(exception, BusinessFailure)
            || ReferenceEquals(exception, Logger.Failure)
            || exception is AmazonSqsConnectionException { InnerException: { } inner } && IsKnownFailure(inner)
            || exception is AggregateException aggregate && aggregate.InnerExceptions.All(IsKnownFailure);
    }

    sealed record Emission(Dictionary<string, object?> Fields, Exception? Cause);
    sealed class SelectedLogger(string template, LogLevel level, bool hostile) : ILogger
    {
        public string Template => template;
        public readonly IOException Failure = new("unique selected SQS owning diagnostic failure");
        public readonly ConcurrentQueue<Emission> Emissions = new();
        public volatile bool Armed = true;
        int _throws;
        public int Throws => Volatile.Read(ref _throws);
        public bool IsEnabled(LogLevel logLevel) => logLevel is LogLevel.Debug or LogLevel.Warning;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new EmptyScope();
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel != level || state is not IEnumerable<KeyValuePair<string, object?>> fields)
                return;
            var values = fields.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (!values.TryGetValue("{OriginalFormat}", out var actual) || !Equals(actual, template))
                return;
            Emissions.Enqueue(new Emission(values, exception));
            if (!Armed || !hostile)
                return;
            Interlocked.Increment(ref _throws);
            throw Failure;
        }
        sealed class EmptyScope : IDisposable { public void Dispose() { } }
    }

    sealed record SdkCall(string Name, object?[] Arguments, CancellationToken Token, Task Raw);
    sealed class CountingNext : IPipe<ClientContext>
    {
        public int Calls;
        public ClientContext? Context;
        public Task SendAsync(ClientContext context)
        {
            Context = context;
            Interlocked.Increment(ref Calls);
            return Task.CompletedTask;
        }
        public void Probe(ProbeContext context) { }
    }

    sealed class TopologyFixture
    {
        public const string TopicName = "topology-diagnostics-topic";
        public const string QueueName = "topology-diagnostics-queue";
        public const string TopicArn = "arn:aws:sns:us-east-1:123456789012:topology-diagnostics-topic";
        public const string QueueArn = "arn:aws:sqs:us-east-1:123456789012:topology-diagnostics-queue";
        public const string QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/topology-diagnostics-queue";
        const string SubscriptionArn = TopicArn + ":subscription";
        readonly int _route;
        readonly TaskCompletionSource<ListTopicsResponse> _list = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<GetTopicAttributesResponse> _topicAttributes = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<GetQueueAttributesResponse> _queueAttributes = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<SubscribeResponse> _subscribe = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<SetQueueAttributesResponse> _policy = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly CancellationTokenSource Lifetime = new();
        public readonly TaskCompletionSource FinalEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly ConcurrentQueue<SdkCall> Calls = new();
        public readonly QueueDoesNotExistException MissingQueue = new("unique missing queue lookup");
        public readonly Task<GetQueueUrlResponse>? MissingQueueTask;
        public readonly FixtureSettings Settings = new();
        public readonly CountingNext Next = new();
        public readonly SelectedLogger Logger;
        public readonly IAmazonSQS Sqs;
        public readonly IAmazonSimpleNotificationService Sns;
        public readonly AmazonSqsConnectionContext Connection;
        public readonly ClientContext Client;
        public readonly ConfigureAmazonSqsTopologyFilter<FixtureSettings> Filter;
        public readonly TopicEntity Topic = new(1, TopicName, true, false);
        public readonly QueueEntity Queue = new(2, QueueName, true, false);
        public int SqsDisposals;
        public int SnsDisposals;
        public Task FinalRaw => _route switch
        {
            0 => _list.Task,
            1 => _topicAttributes.Task,
            2 or 3 => _queueAttributes.Task,
            4 => _policy.Task,
            5 => _subscribe.Task,
            _ => throw new ArgumentOutOfRangeException(nameof(_route))
        };

        public TopologyFixture(int route, bool hostile)
        {
            _route = route;
            if (route == 3)
                MissingQueueTask = Task.FromException<GetQueueUrlResponse>(MissingQueue);
            Logger = new SelectedLogger(route switch
            {
                0 => "Existing topic {Topic} {TopicArn}",
                1 => "Created topic {Topic} {TopicArn}",
                2 => "Existing queue {Queue} {QueueArn} {QueueUrl}",
                3 => "Created queue {Queue} {QueueArn} {QueueUrl}",
                4 => "Created subscription {Topic} to {Queue}",
                5 => "Existing subscription {Topic} to {Queue}",
                _ => throw new ArgumentOutOfRangeException(nameof(route))
            }, LogLevel.Debug, hostile);
            Sqs = InterfaceProxy<IAmazonSQS>.Create(HandleSqs);
            Sns = InterfaceProxy<IAmazonSimpleNotificationService>.Create(HandleSns);
            var host = new AmazonSqsHostConfigurator(new Uri("amazonsqs://us-east-1/"));
            host.ClientFactories(() => Sqs, () => Sns);
            AmazonSqsHostSettings settings = host.Settings;
            Connection = new AmazonSqsConnectionContext(settings.CreateConnection(), HostView(settings), Lifetime.Token);
            Client = Connection.CreateClientContext(Lifetime.Token);
            var broker = new AmazonSqsBrokerTopology(route is 0 or 1 || route >= 4 ? [Topic] : [],
                route >= 2 ? [Queue] : [], route >= 4 ? [new QueueSubscriptionEntity(3, Topic, Queue)] : []);
            Filter = new ConfigureAmazonSqsTopologyFilter<FixtureSettings>(Settings, broker);
        }

        object? HandleSns(MethodInfo method, object?[]? arguments)
        {
            if (method.Name == nameof(IDisposable.Dispose))
            {
                Interlocked.Increment(ref SnsDisposals);
                return null;
            }
            return method.Name switch
            {
                nameof(IAmazonSimpleNotificationService.ListTopicsAsync) => Track(method, arguments, _route == 0
                    ? _list.Task : Task.FromResult(ListResponse(_route != 1)), _route == 0),
                nameof(IAmazonSimpleNotificationService.CreateTopicAsync) => Track(method, arguments,
                    Task.FromResult(new CreateTopicResponse { TopicArn = TopicArn, HttpStatusCode = HttpStatusCode.OK })),
                nameof(IAmazonSimpleNotificationService.GetTopicAttributesAsync) => Track(method, arguments, _topicAttributes.Task, true),
                nameof(IAmazonSimpleNotificationService.SubscribeAsync) => Track(method, arguments, _route == 5
                    ? _subscribe.Task : Task.FromResult(new SubscribeResponse { SubscriptionArn = SubscriptionArn, HttpStatusCode = HttpStatusCode.OK }), _route == 5),
                _ => throw new NotSupportedException("Unexpected SNS SDK call: " + method)
            };
        }

        object? HandleSqs(MethodInfo method, object?[]? arguments)
        {
            if (method.Name == nameof(IDisposable.Dispose))
            {
                Interlocked.Increment(ref SqsDisposals);
                return null;
            }
            return method.Name switch
            {
                nameof(IAmazonSQS.GetQueueUrlAsync) => Track(method, arguments, _route == 3 ? MissingQueueTask!
                    : Task.FromResult(new GetQueueUrlResponse { QueueUrl = QueueUrl, HttpStatusCode = HttpStatusCode.OK })),
                nameof(IAmazonSQS.CreateQueueAsync) => Track(method, arguments,
                    Task.FromResult(new CreateQueueResponse { QueueUrl = QueueUrl, HttpStatusCode = HttpStatusCode.OK })),
                nameof(IAmazonSQS.GetQueueAttributesAsync) => Track(method, arguments, _route is 2 or 3
                    ? _queueAttributes.Task : Task.FromResult(QueueAttributes()), _route is 2 or 3),
                nameof(IAmazonSQS.SetQueueAttributesAsync) => Track(method, arguments, _policy.Task, true),
                _ => throw new NotSupportedException("Unexpected SQS SDK call: " + method)
            };
        }

        object Track(MethodInfo method, object?[]? arguments, Task raw, bool final = false)
        {
            object?[] args = arguments ?? throw new InvalidOperationException("SDK arguments missing");
            if (args.Length == 0 || args[^1] is not CancellationToken token)
                throw new NotSupportedException("Unexpected SDK overload: " + method);
            Calls.Enqueue(new SdkCall(method.Name, args.ToArray(), token, raw));
            if (final)
                FinalEntered.TrySetResult();
            return raw;
        }

        static ListTopicsResponse ListResponse(bool existing) => new()
        {
            HttpStatusCode = HttpStatusCode.OK,
            Topics = existing ? [new SnsTopic { TopicArn = TopicArn }] : [],
            NextToken = null
        };

        GetQueueAttributesResponse QueueAttributes()
        {
            var attributes = new Dictionary<string, string> { [QueueAttributeName.QueueArn] = QueueArn };
            if (_route == 5)
                attributes[QueueAttributeName.Policy] = "{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\",\"Principal\":{\"Service\":\"sns.amazonaws.com\"},\"Action\":\"sqs:SendMessage\",\"Resource\":\"" + QueueArn
                    + "\",\"Condition\":{\"ArnEquals\":{\"aws:SourceArn\":\"" + TopicArn + "\"}}}]}";
            return new GetQueueAttributesResponse { HttpStatusCode = HttpStatusCode.OK, Attributes = attributes };
        }

        public void ReleaseFinal()
        {
            _list.TrySetResult(ListResponse(true));
            _topicAttributes.TrySetResult(new GetTopicAttributesResponse { HttpStatusCode = HttpStatusCode.OK, Attributes = new Dictionary<string, string>() });
            _queueAttributes.TrySetResult(QueueAttributes());
            _subscribe.TrySetResult(new SubscribeResponse { HttpStatusCode = HttpStatusCode.OK, SubscriptionArn = SubscriptionArn });
            _policy.TrySetResult(new SetQueueAttributesResponse { HttpStatusCode = HttpStatusCode.OK });
        }

        public void AssertRequests()
        {
            SdkCall[] calls = Calls.ToArray();
            Assert.All(calls, call => { Assert.True(call.Token.CanBeCanceled); Assert.False(call.Token.IsCancellationRequested); });
            if (_route is 0 or 1 or 4 or 5)
            {
                var list = Assert.IsType<ListTopicsRequest>(Assert.Single(calls, c => c.Name == "ListTopicsAsync").Arguments[0]);
                Assert.Null(list.NextToken);
            }
            if (_route == 1)
            {
                var create = Assert.IsType<CreateTopicRequest>(Assert.Single(calls, c => c.Name == "CreateTopicAsync").Arguments[0]);
                Assert.Equal(TopicName, create.Name);
                Assert.Equal(TopicArn, Assert.Single(calls, c => c.Name == "GetTopicAttributesAsync").Arguments[0]);
            }
            if (_route >= 2)
            {
                Assert.Equal(QueueName, Assert.Single(calls, c => c.Name == "GetQueueUrlAsync").Arguments[0]);
                var attributes = Assert.Single(calls, c => c.Name == "GetQueueAttributesAsync");
                Assert.Equal(QueueUrl, attributes.Arguments[0]);
                Assert.Equal("All", Assert.Single(Assert.IsAssignableFrom<IEnumerable<string>>(attributes.Arguments[1])));
                if (_route == 3)
                    Assert.Equal(QueueName, Assert.IsType<CreateQueueRequest>(Assert.Single(calls, c => c.Name == "CreateQueueAsync").Arguments[0]).QueueName);
            }
            if (_route >= 4)
            {
                var subscribe = Assert.IsType<SubscribeRequest>(Assert.Single(calls, c => c.Name == "SubscribeAsync").Arguments[0]);
                Assert.Equal(TopicArn, subscribe.TopicArn);
                Assert.Equal(QueueArn, subscribe.Endpoint);
                Assert.Equal("sqs", subscribe.Protocol);
                Assert.Equal("true", subscribe.Attributes["RawMessageDelivery"]);
                if (_route == 4)
                {
                    var policy = Assert.Single(calls, c => c.Name == "SetQueueAttributesAsync");
                    Assert.Equal(QueueUrl, policy.Arguments[0]);
                    string text = Assert.IsAssignableFrom<IDictionary<string, string>>(policy.Arguments[1])[QueueAttributeName.Policy];
                    Assert.Contains(TopicArn, text, StringComparison.Ordinal);
                    Assert.Contains(QueueArn, text, StringComparison.Ordinal);
                }
            }
            string[] expected = _route switch
            {
                0 => ["ListTopicsAsync"],
                1 => ["ListTopicsAsync", "CreateTopicAsync", "GetTopicAttributesAsync"],
                2 => ["GetQueueUrlAsync", "GetQueueAttributesAsync"],
                3 => ["GetQueueUrlAsync", "CreateQueueAsync", "GetQueueAttributesAsync"],
                4 => ["ListTopicsAsync", "GetQueueUrlAsync", "GetQueueAttributesAsync", "SubscribeAsync", "SetQueueAttributesAsync"],
                5 => ["ListTopicsAsync", "GetQueueUrlAsync", "GetQueueAttributesAsync", "SubscribeAsync"],
                _ => throw new ArgumentOutOfRangeException(nameof(_route))
            };
            Assert.Equal(expected.OrderBy(name => name), calls.Select(call => call.Name).OrderBy(name => name));
        }

        public void AssertEmission(Emission emission)
        {
            Assert.Equal(Logger.Template, emission.Fields["{OriginalFormat}"]);
            Assert.Null(emission.Cause);
            if (_route is 0 or 1)
            {
                Assert.Equal(TopicName, emission.Fields["Topic"]);
                Assert.Equal(TopicArn, emission.Fields["TopicArn"]);
            }
            else if (_route is 2 or 3)
            {
                Assert.Equal(QueueName, emission.Fields["Queue"]);
                Assert.Equal(QueueArn, emission.Fields["QueueArn"]);
                Assert.Equal(QueueUrl, emission.Fields["QueueUrl"]);
            }
            else
            {
                Assert.Same(Topic, emission.Fields["Topic"]);
                Assert.Same(Queue, emission.Fields["Queue"]);
            }
        }
    }
}
