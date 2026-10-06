using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.ActiveMq.Middleware;
using ViciOne.ServiceBus.ActiveMq.Tests.TestDoubles;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.ActiveMqTransport;

public sealed class ActiveMqPublicContinuationDiagnosticsTests
{
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);
    static readonly Uri HostAddress = new("activemq://broker.internal:61616");
    const string Entity = "public-diagnostic-continuation";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "connect-endpoint-own-debug-preserves-real-start")]
    public async Task ConnectReceiveEndpoint_OwnDebugPreservesRegisteredEndpointStartAsync(bool hostile)
    {
        var previous = LogContext.Current;
        var logger = new SelectedLogger("Connect receive endpoint: {InputAddress}", hostile);
        IHostReceiveEndpointHandle? handle = null;
        Task? readyNotification = null;
        Task? stop = null;
        var fixture = new EndpointFixture();
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            fixture.LogContext = LogContext.Current!;
            var host = new ActiveMqHost(fixture.HostConfiguration, fixture.BusTopology);
            Exception? error = Record.Exception(() =>
            {
                handle = host.ConnectReceiveEndpoint(Entity,
                    (Action<IActiveMqReceiveEndpointConfigurator>)(_ => fixture.CallbackCalls++));
            });
            Assert.Equal(1, fixture.CreateCalls);
            Assert.Equal(1, fixture.CallbackCalls);
            Assert.Equal(1, fixture.ValidateCalls);
            AssertEmission(logger, "InputAddress", fixture.InputAddress, hostile, error);
            Assert.Null(error);

            Assert.NotNull(handle);
            Assert.Equal(1, fixture.BuildCalls);
            Assert.Equal(1, fixture.StartCalls);
            Assert.Same(fixture.Endpoint, handle.ReceiveEndpoint);
            Assert.Equal(fixture.InputAddress, handle.ReceiveEndpoint.InputAddress);
            readyNotification = fixture.NotifyReadyAsync();
            await readyNotification.WaitAsync(Bound, CancellationToken.None);
            ReceiveEndpointReady ready = await handle.Ready.WaitAsync(Bound, CancellationToken.None);
            Assert.Same(fixture.Endpoint, ready.ReceiveEndpoint);
            Assert.Equal(fixture.InputAddress, ready.InputAddress);
            Assert.Equal(ReceiveEndpoint.State.Ready, fixture.Endpoint!.CurrentState);
            stop = handle.StopAsync(CancellationToken.None);
            await stop.WaitAsync(Bound, CancellationToken.None);
            Assert.Equal(1, fixture.StopCalls);
            Assert.Equal(1, fixture.ResetCalls);
            Assert.Equal(ReceiveEndpoint.State.Stopped, fixture.Endpoint.CurrentState);
        }
        finally
        {
            logger.DisableFailure();
            var failures = new List<Exception>();
            try
            {
                // Ready is a real endpoint operation, not merely the notification's completion.
                if (handle is not null)
                {
                    if (readyNotification is null)
                        await CaptureAsync(() => fixture.NotifyReadyAsync().WaitAsync(Bound, CancellationToken.None), failures);
                    await CaptureAsync(() => ObserveAsync(readyNotification), failures);
                    await CaptureAsync(() => handle.Ready.WaitAsync(Bound, CancellationToken.None), failures);
                    await CaptureAsync(() => ObserveAsync(stop), failures);
                    await CaptureAsync(() => handle.StopAsync(CancellationToken.None).WaitAsync(Bound, CancellationToken.None), failures);
                }
                foreach (Task task in fixture.ResetTasks)
                    await CaptureAsync(() => task.WaitAsync(Bound, CancellationToken.None), failures);
                foreach (ConnectHandle registration in fixture.Registrations)
                    await CaptureAsync(() => { registration.Dispose(); return Task.CompletedTask; }, failures);
                ThrowCleanup(failures);
            }
            finally { LogContext.Current = previous; }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "create-send-own-debug-preserves-parent-adoption")]
    public async Task CreateSendTransport_OwnDebugPreservesBothActualParentAdmissionsAsync(bool hostile)
    {
        var previous = LogContext.Current;
        var logger = new SelectedLogger("Create send transport: {DestinationAddress}", hostile);
        var parent = new Supervisor();
        var admitted = new List<IAgent>();
        ConnectionContextSupervisor? supervisor = null;
        Task<ISendTransport>? operation = null;
        var stops = new List<Task>();
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            var topology = new ActiveMqTopologyConfiguration(ActiveMqBusFactory.CreateMessageTopology());
            var configuration = new ActiveMqBusConfiguration(topology);
            configuration.HostConfiguration.Settings = new OpenWireHostSettings(HostAddress);
            supervisor = new ConnectionContextSupervisor(configuration.HostConfiguration, topology);
            var parentFacade = InterfaceProxy<ISessionContextSupervisor>.Create((method, args) =>
            {
                if (method.Name == "AddSendAgent")
                {
                    var agent = (IAgent)args![0]!;
                    parent.Add(agent);
                    admitted.Add(agent);
                    return null;
                }
                throw new NotSupportedException(method.ToString());
            });
            var serialization = InterfaceProxy<ISerialization>.Create((method, _) => throw new NotSupportedException(method.ToString()));
            var endpoint = InterfaceProxy<ActiveMqReceiveEndpointContext>.Create((method, _) =>
                method.Name == "get_Serialization" ? serialization : throw new NotSupportedException(method.ToString()));
            Uri address = new("queue:" + Entity);
            using var precanceled = new CancellationTokenSource();
            precanceled.Cancel();
            Task<ISendTransport> canceled = supervisor.CreateSendTransportAsync(endpoint, parentFacade, address, precanceled.Token);
            var cancellation = Assert.IsAssignableFrom<OperationCanceledException>(
                await Record.ExceptionAsync(() => canceled.WaitAsync(Bound, CancellationToken.None)));
            Assert.Equal(precanceled.Token, cancellation.CancellationToken);
            Assert.True(canceled.IsCanceled);
            Assert.Empty(admitted);
            Assert.Empty(logger.Emissions);

            Exception? error = Record.Exception(() =>
            {
                operation = supervisor.CreateSendTransportAsync(endpoint, parentFacade, address, CancellationToken.None);
            });
            if (error is null && operation is not null)
                error = await Record.ExceptionAsync(() => operation.WaitAsync(Bound, CancellationToken.None));
            AssertEmission(logger, "DestinationAddress", supervisor.NormalizeAddress(address), hostile, error);
            Assert.Null(error);

            Assert.NotNull(operation);
            ISendTransport transport = await operation.WaitAsync(Bound, CancellationToken.None);
            Assert.Equal(2, admitted.Count);
            Assert.IsType<SessionContextSupervisor>(admitted[0]);
            Assert.Same(transport, admitted[1]);
            Assert.NotSame(admitted[0], admitted[1]);
            Assert.Equal(2, parent.TotalCount);
            // Creating the transport acquires agents, without creating a native connection or sending a message.
            foreach (IAgent agent in admitted)
            {
                Task actualStop = agent.StopAsync(new FixtureStopContext(), CancellationToken.None);
                stops.Add(actualStop);
                await actualStop.WaitAsync(Bound, CancellationToken.None);
                await agent.Completed.WaitAsync(Bound, CancellationToken.None);
            }
        }
        finally
        {
            logger.DisableFailure();
            var failures = new List<Exception>();
            try
            {
                await CaptureAsync(() => ObserveAsync(operation, logger.Failure), failures);
                foreach (Task task in stops)
                    await CaptureAsync(() => ObserveAsync(task), failures);
                foreach (IAgent agent in admitted)
                {
                    await CaptureAsync(() => agent.StopAsync(new FixtureStopContext(), CancellationToken.None).WaitAsync(Bound, CancellationToken.None), failures);
                    await CaptureAsync(() => agent.Completed.WaitAsync(Bound, CancellationToken.None), failures);
                }
                await CaptureAsync(() => parent.StopAsync(new FixtureStopContext(), CancellationToken.None).WaitAsync(Bound, CancellationToken.None), failures);
                await CaptureAsync(() => parent.Completed.WaitAsync(Bound, CancellationToken.None), failures);
                if (supervisor is not null)
                {
                    await CaptureAsync(() => supervisor.StopAsync(new FixtureStopContext(), CancellationToken.None).WaitAsync(Bound, CancellationToken.None), failures);
                    await CaptureAsync(() => supervisor.Completed.WaitAsync(Bound, CancellationToken.None), failures);
                }
                ThrowCleanup(failures);
            }
            finally { LogContext.Current = previous; }
        }
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "topology-own-debug-preserves-native-resolution-and-next")]
    public async Task Topology_OwnDebugPreservesNativeResolutionAndDownstreamAsync(bool topic, bool hostile)
    {
        var previous = LogContext.Current;
        var logger = new SelectedLogger(topic ? "Declare topic {Topic}" : "Get queue {Queue}", hostile);
        var fixture = new NativeFixture();
        ActiveMqConnectionContext? connection = null;
        ActiveMqSessionContext? session = null;
        Task<IMessageConsumer>? barrierOperation = null;
        Task? operation = null;
        Task? repeat = null;
        var next = new RecordingPipe();
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            var topology = new ActiveMqTopologyConfiguration(ActiveMqBusFactory.CreateMessageTopology());
            var configuration = new ActiveMqBusConfiguration(topology);
            configuration.HostConfiguration.Settings = new OpenWireHostSettings(HostAddress);
            connection = new ActiveMqConnectionContext(fixture.Connection, configuration.HostConfiguration, CancellationToken.None);
            session = new ActiveMqSessionContext(connection, fixture.Session, CancellationToken.None);
            barrierOperation = session.CreateMessageConsumerAsync(fixture.BarrierQueue, null, false, cancellationToken: CancellationToken.None);
            await fixture.ConsumerEntered.Task.WaitAsync(Bound, CancellationToken.None);
            Assert.Equal(1, fixture.ConsumerCalls);
            Assert.Same(fixture.BarrierQueue, fixture.ConsumerDestination);
            Assert.Null(fixture.ConsumerSelector);
            Assert.False(fixture.ConsumerNoLocal);
            Assert.False(fixture.RawConsumerTask.IsCompleted);
            Assert.False(barrierOperation.IsCompleted);

            SendSettings settings = topic
                ? new ActiveMqTopicSendSettings(new ActiveMqEndpointAddress(HostAddress, new Uri("topic:" + Entity))) { Durable = true, AutoDelete = false }
                : new ActiveMqQueueSendSettings(new ActiveMqEndpointAddress(HostAddress, new Uri("queue:" + Entity))) { Durable = true, AutoDelete = false };
            BrokerTopology broker = settings.GetBrokerTopology();
            SendSettings retained = new ActiveMqQueueSendSettings(new ActiveMqEndpointAddress(HostAddress, new Uri("queue:retained-settings")));
            // Existing-payload and absent-payload cases both run on the real session payload cache.
            if (topic)
                session.GetOrAddPayload<SendSettings>(() => retained);
            var endpoint = InterfaceProxy<ActiveMqReceiveEndpointContext>.Create((method, _) => throw new NotSupportedException(method.ToString()));
            var filter = new ConfigureActiveMqTopologyFilter<SendSettings>(settings, broker, endpoint);
            operation = topic
                ? filter.ConfigureAsync(session, CancellationToken.None)
                : filter.SendAsync(session, next);
            Assert.Empty(fixture.Resolutions);
            Assert.Equal(0, fixture.ProducerCalls);
            fixture.ReleaseConsumer();
            Assert.Same(fixture.Consumer, await barrierOperation.WaitAsync(Bound, CancellationToken.None));
            await fixture.RawConsumerTask.WaitAsync(Bound, CancellationToken.None);
            Exception? error = await Record.ExceptionAsync(() => operation.WaitAsync(Bound, CancellationToken.None));
            object declaration = topic ? Assert.Single(broker.Topics) : Assert.Single(broker.Queues);
            AssertEmission(logger, topic ? "Topic" : "Queue", declaration, hostile, error);
            Assert.Null(error);

            Assert.True(operation.IsCompletedSuccessfully);
            Assert.Equal(Entity, Assert.Single(fixture.Resolutions));
            Assert.True(session.TryGetPayload<SendSettings>(out var payload));
            Assert.Same(topic ? retained : settings, payload);
            if (topic)
            {
                Assert.Equal(1, fixture.ProducerCalls);
                Assert.Same(fixture.TargetTopic, fixture.ProducerDestination);
                Assert.Equal(new[] { "close", "dispose" }, fixture.ProducerStages.ToArray());
                Assert.Equal(0, next.Calls);
            }
            else
            {
                Assert.Equal(0, fixture.ProducerCalls);
                Assert.Equal(1, next.Calls);
                Assert.Same(session, next.Context);
                repeat = filter.SendAsync(session, next);
                await repeat.WaitAsync(Bound, CancellationToken.None);
                Assert.Equal(2, next.Calls);
                Assert.Equal(Entity, Assert.Single(fixture.Resolutions));
                Assert.Single(logger.Emissions);
            }
        }
        finally
        {
            logger.DisableFailure();
            fixture.ReleaseConsumer();
            var failures = new List<Exception>();
            try
            {
                await CaptureAsync(() => ObserveAsync(fixture.RawConsumerTask), failures);
                await CaptureAsync(() => ObserveAsync(barrierOperation), failures);
                await CaptureAsync(() => ObserveAsync(operation, logger.Failure), failures);
                await CaptureAsync(() => ObserveAsync(repeat), failures);
                foreach (Task task in next.Tasks)
                    await CaptureAsync(() => ObserveAsync(task), failures);
                if (session is not null)
                    await CaptureAsync(() => session.DisposeAsync().AsTask().WaitAsync(Bound, CancellationToken.None), failures);
                await CaptureAsync(() => { fixture.Consumer.Dispose(); return Task.CompletedTask; }, failures);
                if (connection is not null)
                    await CaptureAsync(() => connection.DisposeAsync().AsTask().WaitAsync(Bound, CancellationToken.None), failures);
                ThrowCleanup(failures);
            }
            finally { LogContext.Current = previous; }
        }
    }

    static void AssertEmission(SelectedLogger logger, string argument, object expected, bool hostile, Exception? error)
    {
        var emission = Assert.Single(logger.Emissions);
        Assert.Equal(logger.Template, emission["{OriginalFormat}"]);
        Assert.Equal(expected, emission[argument]);
        Assert.Equal(hostile ? 1 : 0, logger.ThrowCount);
        if (error is not null)
            Assert.Same(logger.Failure, error);
    }

    static async Task ObserveAsync(Task? task, Exception? expected = null)
    {
        if (task is null)
            return;
        try { await task.WaitAsync(Bound, CancellationToken.None); }
        catch (Exception error) when (expected is not null && ReferenceEquals(error, expected) && task.IsFaulted) { }
    }

    static async Task CaptureAsync(Func<Task> action, List<Exception> failures)
    {
        try { await action(); }
        catch (Exception error) { failures.Add(error); }
    }

    static void ThrowCleanup(List<Exception> failures)
    {
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException("Public ActiveMQ fixture cleanup failed.", failures);
    }

    sealed class FixtureStopContext : BasePipeContext, StopContext
    {
        public string Reason => "public ActiveMQ fixture cleanup";
    }

    sealed class SelectedLogger(string template, bool hostile) : ILogger
    {
        int _enabled = hostile ? 1 : 0;
        int _throwCount;
        public string Template => template;
        public IOException Failure { get; } = new("unique owning ActiveMQ continuation diagnostic: " + template);
        public ConcurrentQueue<IReadOnlyDictionary<string, object?>> Emissions { get; } = new();
        public int ThrowCount => Volatile.Read(ref _throwCount);
        public bool IsEnabled(LogLevel level) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void DisableFailure() => Volatile.Write(ref _enabled, 0);
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? error, Func<TState, Exception?, string> formatter)
        {
            if (state is not IEnumerable<KeyValuePair<string, object?>> pairs)
                return;
            var data = pairs.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (!data.TryGetValue("{OriginalFormat}", out object? value) || !Equals(value, template))
                return;
            Emissions.Enqueue(data);
            if (Volatile.Read(ref _enabled) != 0)
            {
                Interlocked.Increment(ref _throwCount);
                throw Failure;
            }
        }
    }

    sealed class EndpointFixture
    {
        readonly ReceiveEndpointObservable _observers = new();
        IReceiveTransportObserver? _transportObserver;
        public Uri InputAddress { get; } = new(HostAddress, Entity);
        public ILogContext LogContext { get; set; } = null!;
        public IActiveMqHostConfiguration HostConfiguration { get; }
        public IActiveMqBusTopology BusTopology { get; }
        public ReceiveEndpoint? Endpoint { get; private set; }
        public List<ConnectHandle> Registrations { get; } = new();
        public List<Task> ResetTasks { get; } = new();
        public int CreateCalls;
        public int CallbackCalls;
        public int ValidateCalls;
        public int BuildCalls;
        public int StartCalls;
        public int StopCalls;
        public int ResetCalls;

        public EndpointFixture()
        {
            BusTopology = InterfaceProxy<IActiveMqBusTopology>.Create((method, _) => throw new NotSupportedException(method.ToString()));
            var context = InterfaceProxy<ReceiveEndpointContext>.Create((method, args) =>
            {
                switch (method.Name)
                {
                    case "get_InputAddress": return InputAddress;
                    case "get_IsBusEndpoint": return false;
                    case "get_LogContext": return LogContext;
                    case "get_EndpointObservers": return _observers;
                    case "get_DependentsCompleted": return Task.CompletedTask;
                    case "ConnectReceiveEndpointObserver":
                        ConnectHandle registration = _observers.Connect((IReceiveEndpointObserver)args![0]!);
                        Registrations.Add(registration);
                        return registration;
                    case "ResetAsync":
                        ResetCalls++;
                        Task reset = Task.CompletedTask;
                        ResetTasks.Add(reset);
                        return new ValueTask(reset);
                    default: throw new NotSupportedException(method.ToString());
                }
            });
            var lifecycle = InterfaceProxy<ReceiveTransportHandle>.Create((method, _) =>
            {
                if (method.Name != "StopAsync") throw new NotSupportedException(method.ToString());
                StopCalls++;
                return Task.CompletedTask;
            });
            var transport = InterfaceProxy<IReceiveTransport>.Create((method, args) =>
            {
                if (method.Name == "ConnectReceiveTransportObserver")
                {
                    _transportObserver = (IReceiveTransportObserver)args![0]!;
                    var registration = new NoopHandle();
                    Registrations.Add(registration);
                    return registration;
                }
                if (method.Name == "Start") { StartCalls++; return lifecycle; }
                throw new NotSupportedException(method.ToString());
            });
            var configuration = InterfaceProxy<IActiveMqReceiveEndpointConfiguration>.Create((method, args) =>
            {
                if (method.Name == "get_InputAddress") return InputAddress;
                if (method.Name == "Validate") { ValidateCalls++; return Array.Empty<ValidationResult>(); }
                if (method.Name == "Build")
                {
                    BuildCalls++;
                    Endpoint = new ReceiveEndpoint(transport, context);
                    ((IHost)args![0]!).AddReceiveEndpoint(Entity, Endpoint);
                    return null;
                }
                throw new NotSupportedException(method.ToString());
            });
            var configurator = InterfaceProxy<IActiveMqReceiveEndpointConfigurator>.Create((method, _) => throw new NotSupportedException(method.ToString()));
            HostConfiguration = InterfaceProxy<IActiveMqHostConfiguration>.Create((method, args) =>
            {
                if (method.Name == "get_LogContext") return LogContext;
                if (method.Name == "get_HostAddress") return HostAddress;
                if (method.Name == "CreateReceiveEndpointConfiguration" && args is { Length: 2 } && args[0] is string name)
                {
                    Assert.Equal(Entity, name);
                    CreateCalls++;
                    ((Action<IActiveMqReceiveEndpointConfigurator>?)args[1])?.Invoke(configurator);
                    return configuration;
                }
                throw new NotSupportedException(method.ToString());
            });
        }

        public Task NotifyReadyAsync() => (_transportObserver ?? throw new InvalidOperationException("No actual transport observer was registered."))
            .ReadyAsync(new PublicReady(InputAddress));
    }

    sealed class PublicReady(Uri inputAddress) : ReceiveTransportReady
    {
        public Uri InputAddress => inputAddress;
        public bool IsStarted => true;
    }

    sealed class NoopHandle : ConnectHandle
    {
        public void Disconnect() { }
        public void Dispose() => Disconnect();
    }

    sealed class RecordingPipe : IPipe<SessionContext>
    {
        public int Calls { get; private set; }
        public SessionContext? Context { get; private set; }
        public List<Task> Tasks { get; } = new();
        public Task SendAsync(SessionContext context)
        {
            Calls++;
            Context = context;
            Task task = Task.CompletedTask;
            Tasks.Add(task);
            return task;
        }
        public void Probe(ProbeContext context) { }
    }

    sealed class NativeFixture
    {
        readonly TaskCompletionSource<IMessageConsumer> _consumer = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ConsumerEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IMessageConsumer> RawConsumerTask => _consumer.Task;
        public IMessageConsumer Consumer { get; }
        public IQueue BarrierQueue { get; }
        public ITopic TargetTopic { get; }
        public IQueue TargetQueue { get; }
        public IConnection Connection { get; }
        public ISession Session { get; }
        public ConcurrentQueue<string> Resolutions { get; } = new();
        public ConcurrentQueue<string> ProducerStages { get; } = new();
        public int ConsumerCalls;
        public int ProducerCalls;
        public IDestination? ConsumerDestination;
        public string? ConsumerSelector;
        public bool ConsumerNoLocal;
        public IDestination? ProducerDestination;

        public NativeFixture()
        {
            BarrierQueue = Queue("public-continuation-executor-barrier");
            TargetQueue = Queue(Entity);
            TargetTopic = InterfaceProxy<ITopic>.Create((method, _) => method.Name switch
            {
                "get_IsTopic" => true, "get_IsQueue" => false, "get_TopicName" => Entity,
                _ => throw new NotSupportedException(method.ToString())
            });
            Consumer = InterfaceProxy<IMessageConsumer>.Create((method, _) => method.Name == "Dispose"
                ? null : throw new NotSupportedException(method.ToString()));
            var producer = InterfaceProxy<IMessageProducer>.Create((method, _) =>
            {
                if (method.Name == "Close") { ProducerStages.Enqueue("close"); return null; }
                if (method.Name == "Dispose") { ProducerStages.Enqueue("dispose"); return null; }
                throw new NotSupportedException(method.ToString());
            });
            Connection = InterfaceProxy<IConnection>.Create((method, _) => method.Name switch
            {
                "CloseAsync" => Task.CompletedTask, "Dispose" => null,
                _ => throw new NotSupportedException(method.ToString())
            });
            Session = InterfaceProxy<ISession>.Create((method, args) =>
            {
                if (method.Name == "CreateConsumerAsync" && args is { Length: 3 })
                {
                    ConsumerCalls++;
                    ConsumerDestination = (IDestination)args[0]!;
                    ConsumerSelector = (string?)args[1];
                    ConsumerNoLocal = (bool)args[2]!;
                    ConsumerEntered.TrySetResult();
                    return RawConsumerTask;
                }
                if (method.Name == "GetTopic") { Resolutions.Enqueue((string)args![0]!); return TargetTopic; }
                if (method.Name == "GetQueue") { Resolutions.Enqueue((string)args![0]!); return TargetQueue; }
                if (method.Name == "CreateProducer")
                {
                    ProducerCalls++;
                    ProducerDestination = (IDestination)args![0]!;
                    return producer;
                }
                if (method.Name == "CloseAsync") return Task.CompletedTask;
                if (method.Name == "Dispose") return null;
                throw new NotSupportedException(method.ToString());
            });
        }

        static IQueue Queue(string name) => InterfaceProxy<IQueue>.Create((method, _) => method.Name switch
        {
            "get_IsQueue" => true, "get_IsTopic" => false, "get_QueueName" => name,
            _ => throw new NotSupportedException(method.ToString())
        });
        public void ReleaseConsumer() => _consumer.TrySetResult(Consumer);
    }
}
