using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Configuration;

public sealed partial class RabbitMqConfiguredConnectionWiringTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONFIGURED-CONNECTION-WIRING",
        "configured-connection-middleware-precedes-public-endpoint-channel-creation")]
    public async Task ConfigureConnection_WrapsPublicEndpointChannelCreationAsync(bool configureConnection)
    {
        ILogContext? previous = LogContext.Current;
        var filter = new HoldingConnectionFilter();
        ConnectionFixtureSupervisor? supervisor = null;
        RecordingConnectionContext? connection = null;
        IReceiveEndpointHandle? endpointHandle = null;
        Task? endpointStop = null;
        Exception? operationFailure = null;
        using var testBound = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        testBound.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            LogContext.ConfigureCurrentLogContext(NullLogger.Instance);
            var bus = new RabbitMqBusConfiguration(new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology()));
            IRabbitMqHostConfiguration actualHost = bus.HostConfiguration;
            actualHost.LogContext = LogContext.Current;
            connection = new RecordingConnectionContext(actualHost);
            supervisor = new ConnectionFixtureSupervisor(new ConnectionFixtureFactory(connection, actualHost));

            IRabbitMqHostConfiguration configuredHost = DispatchProxy.Create<IRabbitMqHostConfiguration, HostConfigurationProxy>();
            var hostProxy = (HostConfigurationProxy)(object)configuredHost;
            hostProxy.Inner = actualHost;
            hostProxy.Supervisor = supervisor;
            IRabbitMqEndpointConfiguration child = bus.CreateEndpointConfiguration(false);
            child.AutoStart = false;
            const string queue = "connection-wiring-original";
            var settings = new RabbitMqReceiveSettings(child, queue, ExchangeType.Fanout, true, false);
            var endpoint = new RabbitMqReceiveEndpointConfiguration(configuredHost, settings, child);
            int configurationCalls = 0;
            if (configureConnection)
            {
                endpoint.ConfigureConnection(configurator =>
                {
                    configurationCalls++;
                    configurator.UseFilter(filter);
                });
            }

            IHost registrationHost = DispatchProxy.Create<IHost, RegistrationHostProxy>();
            var registration = (RegistrationHostProxy)(object)registrationHost;
            endpoint.Build(registrationHost);
            Assert.Equal(1, registration.AddCalls);
            Assert.Equal(queue, registration.Name);
            ReceiveEndpoint builtEndpoint = Assert.IsType<ReceiveEndpoint>(registration.Endpoint);
            Assert.Same(builtEndpoint, endpoint.ReceiveEndpoint);
            Assert.Equal(configureConnection ? 1 : 0, configurationCalls);
            Assert.Equal(0, filter.Calls);
            Assert.Equal(0, connection.CreateCalls);

            endpointHandle = builtEndpoint.Start(TestContext.Current.CancellationToken);
            if (configureConnection)
            {
                // An actual creation admission also ends this wait if the configured filter is omitted.
                await Task.WhenAny(filter.Entered.Task, connection.CreateEntered.Task).WaitAsync(testBound.Token);
                Assert.Equal(1, filter.Calls);
                Assert.Equal(0, connection.CreateCalls);
                Assert.False(endpointHandle.Ready.IsCompleted);
                filter.Release.TrySetResult();
            }

            await connection.CreateEntered.Task.WaitAsync(testBound.Token);
            Assert.Equal(1, connection.CreateCalls);
            Assert.NotNull(connection.CreationAgent);
            Assert.True(connection.CreateToken.CanBeCanceled);
            Assert.False(connection.CreateToken.IsCancellationRequested);
            Assert.False(connection.CreationTask!.IsCompleted);
            Assert.False(endpointHandle.Ready.IsCompleted);
            connection.CreateRelease.TrySetResult();

            var ready = await endpointHandle.Ready.WaitAsync(testBound.Token);
            Assert.False(ready.IsStarted);
            Assert.Equal(endpoint.InputAddress, ready.InputAddress);
            Assert.Same(builtEndpoint, ready.ReceiveEndpoint);
            await connection.ChannelEvents.Added.Task.WaitAsync(testBound.Token);
            await connection.ConnectionEvents.Added.Task.WaitAsync(testBound.Token);
            Assert.Equal(1, connection.ChannelEvents.AddCalls);
            Assert.Equal(1, connection.ConnectionEvents.AddCalls);
            Assert.Equal(0, connection.ChannelDisposal.DisposeCalls);
            Assert.Equal(0, connection.DisposeCalls);
            Assert.Equal(configureConnection ? 1 : 0, filter.Calls);
            Assert.True(hostProxy.SupervisorReads > 0);

            endpointStop = endpointHandle.StopAsync(CancellationToken.None);
            await connection.ChannelDisposal.Entered.Task.WaitAsync(testBound.Token);
            Assert.False(endpointStop.IsCompleted);
            Assert.Equal(1, connection.ChannelDisposal.DisposeCalls);
            Assert.Equal(0, connection.DisposeCalls);
            connection.ChannelDisposal.Release.TrySetResult();
            await endpointStop.WaitAsync(testBound.Token);
            await connection.CreationAgent!.Completed.WaitAsync(testBound.Token);
            await connection.ChannelEvents.Removed.Task.WaitAsync(testBound.Token);
            await connection.ConnectionEvents.Removed.Task.WaitAsync(testBound.Token);
            Assert.Equal(1, connection.ChannelEvents.RemoveCalls);
            Assert.Equal(1, connection.ConnectionEvents.RemoveCalls);
            Assert.False(connection.ChannelEvents.HasHandler);
            Assert.False(connection.ConnectionEvents.HasHandler);
            Assert.Equal(0, connection.DisposeCalls);

            await supervisor.StopAsync("fixture connection owner completed", CancellationToken.None).WaitAsync(testBound.Token);
            await supervisor.Completed.WaitAsync(testBound.Token);
            Assert.Equal(1, connection.DisposeCalls);
            Assert.Equal(1, connection.CreateCalls);
            Assert.Equal(1, connection.ChannelDisposal.DisposeCalls);
        }
        catch (Exception failure)
        {
            operationFailure = failure;
            throw;
        }
        finally
        {
            filter.Release.TrySetResult();
            connection?.CreateRelease.TrySetResult();
            connection?.ChannelDisposal.Release.TrySetResult();
            var cleanupFailures = new List<Exception>();

            async Task ObserveCleanupAsync(Func<Task> operation)
            {
                using var observationBound = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                try
                {
                    await operation().WaitAsync(observationBound.Token);
                }
                catch (Exception failure)
                {
                    cleanupFailures.Add(failure);
                }
            }

            try
            {
                if (endpointHandle is not null)
                    await ObserveCleanupAsync(() => endpointStop ?? endpointHandle.StopAsync(CancellationToken.None));
                if (supervisor is not null)
                {
                    await ObserveCleanupAsync(() => supervisor.StopAsync("fixture final drain", CancellationToken.None));
                    await ObserveCleanupAsync(() => supervisor.Completed);
                }
                if (connection?.CreationTask is { } creation)
                    await ObserveCleanupAsync(() => creation);
                if (connection?.CreationAgent is { } agent)
                    await ObserveCleanupAsync(() => agent.Completed);
            }
            finally
            {
                LogContext.Current = previous;
            }

            if (cleanupFailures.Count > 0)
            {
                if (operationFailure is not null)
                    cleanupFailures.Insert(0, operationFailure);
                if (cleanupFailures.Count == 1)
                    ExceptionDispatchInfo.Capture(cleanupFailures[0]).Throw();
                throw new AggregateException("Endpoint test operation or owned cleanup failed.", cleanupFailures);
            }
        }
    }

    private sealed class HoldingConnectionFilter : IFilter<ConnectionContext>
    {
        private int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public TaskCompletionSource Entered { get; } = Signal();
        public TaskCompletionSource Release { get; } = Signal();

        public async Task SendAsync(ConnectionContext context, IPipe<ConnectionContext> next)
        {
            Interlocked.Increment(ref _calls);
            Entered.TrySetResult();
            await Release.Task.ConfigureAwait(false);
            await next.SendAsync(context).ConfigureAwait(false);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("heldConnectionOriginal");
    }

    private sealed class ConnectionFixtureFactory(RecordingConnectionContext context, IRabbitMqHostConfiguration actualHost)
        : IPipeContextFactory<ConnectionContext>
    {
        private readonly ConnectionContextFactory _sharedFactory = new(actualHost);

        public IPipeContextAgent<ConnectionContext> CreateContext(ISupervisor supervisor)
            => supervisor.AddContext<ConnectionContext>(context);

        public IActivePipeContextAgent<ConnectionContext> CreateActiveContext(ISupervisor supervisor,
            IPipeContextHandle<ConnectionContext> owner, CancellationToken cancellationToken)
            => _sharedFactory.CreateActiveContext(supervisor, owner, cancellationToken);
    }

    private sealed class ConnectionFixtureSupervisor(IPipeContextFactory<ConnectionContext> factory)
        : TransportPipeContextSupervisor<ConnectionContext>(factory), IConnectionContextSupervisor
    {
        public Uri NormalizeAddress(Uri address) => throw Unexpected(nameof(NormalizeAddress));

        public Task<ISendTransport> CreateSendTransportAsync(RabbitMqReceiveEndpointContext context,
            IChannelContextSupervisor channels, Uri address, CancellationToken cancellationToken = default)
            => throw Unexpected(nameof(CreateSendTransportAsync));

        public Task<ISendTransport> CreatePublishTransportAsync<T>(RabbitMqReceiveEndpointContext context,
            IChannelContextSupervisor channels, CancellationToken cancellationToken = default) where T : class
            => throw Unexpected(nameof(CreatePublishTransportAsync));
    }

    private sealed class RecordingConnectionContext : BasePipeContext, ConnectionContext, IAsyncDisposable
    {
        private readonly IRabbitMqHostConfiguration _host;
        private readonly ChannelContext _channelContext;
        private int _createCalls;
        private int _disposeCalls;

        public RecordingConnectionContext(IRabbitMqHostConfiguration host)
        {
            _host = host;
            Connection = DispatchProxy.Create<IConnection, ConnectionProxy>();
            ((ConnectionProxy)(object)Connection).Events = ConnectionEvents;
            IChannel channel = DispatchProxy.Create<IChannel, ChannelProxy>();
            ((ChannelProxy)(object)channel).Events = ChannelEvents;
            _channelContext = DispatchProxy.Create<ChannelContext, ChannelContextProxy>();
            var proxy = (ChannelContextProxy)(object)_channelContext;
            proxy.Channel = channel;
            proxy.ConnectionContext = this;
            proxy.Disposal = ChannelDisposal;
        }

        public int CreateCalls => Volatile.Read(ref _createCalls);
        public int DisposeCalls => Volatile.Read(ref _disposeCalls);
        public TaskCompletionSource CreateEntered { get; } = Signal();
        public TaskCompletionSource CreateRelease { get; } = Signal();
        public Task<ChannelContext>? CreationTask { get; private set; }
        public IAgent? CreationAgent { get; private set; }
        public CancellationToken CreateToken { get; private set; }
        public EventRecord ChannelEvents { get; } = new();
        public EventRecord ConnectionEvents { get; } = new();
        public DisposalRecord ChannelDisposal { get; } = new();
        public IConnection Connection { get; }
        public string Description => "public endpoint connection fixture";
        public Uri HostAddress => _host.HostAddress;
        public bool PublisherConfirmation => _host.PublisherConfirmation;
        public BatchSettings BatchSettings => _host.BatchSettings;
        public TimeSpan ContinuationTimeout => _host.Settings.ContinuationTimeout;
        public TimeSpan StopTimeout => TimeSpan.FromSeconds(10);
        public IRabbitMqBusTopology Topology => _host.Topology;
        public RabbitMqTopologyEntityCache TopologyEntityCache => throw Unexpected(nameof(TopologyEntityCache));

        public Task<IChannel> CreateChannelAsync(ushort? concurrentMessageLimit, CancellationToken cancellationToken)
            => throw Unexpected(nameof(CreateChannelAsync));

        public Task<ChannelContext> CreateChannelContextAsync(IAgent agent, ushort? concurrentMessageLimit,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _createCalls);
            CreationAgent = agent;
            CreateToken = cancellationToken;
            CreationTask = CreateHeldChannelContextAsync();
            CreateEntered.TrySetResult();
            return CreationTask;
        }

        private async Task<ChannelContext> CreateHeldChannelContextAsync()
        {
            await CreateRelease.Task.ConfigureAwait(false);
            return _channelContext;
        }

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCalls);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class DisposalRecord
    {
        private int _disposeCalls;
        public int DisposeCalls => Volatile.Read(ref _disposeCalls);
        public TaskCompletionSource Entered { get; } = Signal();
        public TaskCompletionSource Release { get; } = Signal();
        public Func<ValueTask>? Completion { get; set; }

        public async ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCalls);
            Entered.TrySetResult();
            await Release.Task.ConfigureAwait(false);
            if (Completion is { } complete)
                await complete().ConfigureAwait(false);
        }
    }

    private sealed class EventRecord
    {
        private AsyncEventHandler<ShutdownEventArgs>? _handler;
        private int _addCalls;
        private int _removeCalls;
        public int AddCalls => Volatile.Read(ref _addCalls);
        public int RemoveCalls => Volatile.Read(ref _removeCalls);
        public bool HasHandler => _handler is not null;
        public TaskCompletionSource Added { get; } = Signal();
        public TaskCompletionSource Removed { get; } = Signal();

        public object? Add(object?[]? args)
        {
            _handler += Assert.IsType<AsyncEventHandler<ShutdownEventArgs>>(Assert.Single(args!));
            Interlocked.Increment(ref _addCalls);
            Added.TrySetResult();
            return null;
        }

        public object? Remove(object?[]? args)
        {
            _handler -= Assert.IsType<AsyncEventHandler<ShutdownEventArgs>>(Assert.Single(args!));
            Interlocked.Increment(ref _removeCalls);
            Removed.TrySetResult();
            return null;
        }
    }

    private class ConnectionProxy : DispatchProxy
    {
        public EventRecord Events { get; set; } = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method?.Name switch
        {
            "get_IsOpen" => true,
            "add_ConnectionShutdownAsync" => Events.Add(args),
            "remove_ConnectionShutdownAsync" => Events.Remove(args),
            _ => throw Unexpected(method?.Name),
        };
    }

    private class ChannelProxy : DispatchProxy
    {
        public EventRecord Events { get; set; } = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method?.Name switch
        {
            "get_IsClosed" => false,
            "add_ChannelShutdownAsync" => Events.Add(args),
            "remove_ChannelShutdownAsync" => Events.Remove(args),
            _ => throw Unexpected(method?.Name),
        };
    }

    private class ChannelContextProxy : DispatchProxy, IAsyncDisposable
    {
        public IChannel Channel { get; set; } = null!;
        public ConnectionContext ConnectionContext { get; set; } = null!;
        public DisposalRecord Disposal { get; set; } = null!;
        public ValueTask DisposeAsync() => Disposal.DisposeAsync();

        protected override object? Invoke(MethodInfo? method, object?[]? args) => method?.Name switch
        {
            "get_Channel" => Channel,
            "get_ConnectionContext" => ConnectionContext,
            "get_CancellationToken" => CancellationToken.None,
            _ => throw Unexpected(method?.Name),
        };
    }

    private class HostConfigurationProxy : DispatchProxy
    {
        private int _supervisorReads;
        public IRabbitMqHostConfiguration Inner { get; set; } = null!;
        public IConnectionContextSupervisor Supervisor { get; set; } = null!;
        public int SupervisorReads => Volatile.Read(ref _supervisorReads);

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(method);
            if (method.Name == "get_ConnectionContextSupervisor")
            {
                Interlocked.Increment(ref _supervisorReads);
                return Supervisor;
            }

            try
            {
                return method.Invoke(Inner, args);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is { } inner)
            {
                ExceptionDispatchInfo.Capture(inner).Throw();
                throw;
            }
        }
    }

    private class RegistrationHostProxy : DispatchProxy
    {
        public int AddCalls { get; private set; }
        public string? Name { get; private set; }
        public ReceiveEndpoint? Endpoint { get; private set; }

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name != nameof(IHost.AddReceiveEndpoint))
                throw Unexpected(method?.Name);
            Assert.Equal(2, args!.Length);
            Name = Assert.IsType<string>(args[0]);
            Endpoint = Assert.IsType<ReceiveEndpoint>(args[1]);
            AddCalls++;
            return null;
        }
    }

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static NotSupportedException Unexpected(string? operation) => new($"Unexpected fixture/broker operation: {operation}");
}
