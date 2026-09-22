using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class ConnectionContextFactoryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-CREATION", "required-factory-dependencies")]
    public void Constructor_RejectsMissingHostConfigurationAndConnector()
    {
        IRabbitMqHostConfiguration host = CreateHost(new RecordingHostSettings());

        Assert.Equal(
            "hostConfiguration",
            Assert.Throws<ArgumentNullException>(() => new ConnectionContextFactory(null!)).ParamName);
        Assert.Equal(
            "connect",
            Assert.Throws<ArgumentNullException>(() => new ConnectionContextFactory(host, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-CREATION", "pre-stopped-supervisor-has-no-connection-side-effects")]
    public async Task CreateContext_PreCanceledSupervisorSkipsRefreshAndConnectionAsync()
    {
        using var stopping = new CancellationTokenSource();
        stopping.Cancel();
        var settings = new RecordingHostSettings();
        var supervisor = new TestSupervisor(stopping.Token);
        var connectCalls = 0;
        var factory = new ConnectionContextFactory(
            CreateHost(settings),
            (_, _, _) =>
            {
                Interlocked.Increment(ref connectCalls);
                return Task.FromResult<IConnection>(CreateConnection());
            });

        RabbitMqConnectionException failure = await AssertContextFailureAsync<RabbitMqConnectionException>(factory, supervisor);

        Assert.True(failure.IsTransient);
        Assert.Contains("stopping", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, settings.RefreshCalls);
        Assert.Equal(0, connectCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-CREATION", "stopping-token-reaches-refresh-and-connect")]
    public async Task CreateContext_ForwardsTheStoppingTokenThroughRefreshAndConnectAsync()
    {
        ILogContext? previous = LogContext.Current;
        var logger = new CaptureLogger();
        using var stopping = new CancellationTokenSource();
        var settings = new RecordingHostSettings
        {
            Refresh = connectionFactory => connectionFactory.UserName = "rotated-user",
        };
        var supervisor = new TestSupervisor(stopping.Token);
        CancellationToken connectToken = default;
        RabbitMqHostSettings? connectedSettings = null;
        uint maxInboundMessageBodySize = 0;
        var connection = CreateConnection();
        var factory = new ConnectionContextFactory(
            CreateHost(settings, 123_456),
            (connectionFactory, snapshot, cancellationToken) =>
            {
                connectedSettings = snapshot;
                connectToken = cancellationToken;
                maxInboundMessageBodySize = connectionFactory.MaxInboundMessageBodySize;
                return Task.FromResult(connection);
            });

        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            IPipeContextAgent<ConnectionContext> agent = factory.CreateContext(supervisor);

            RabbitMqConnectionContext context = Assert.IsType<RabbitMqConnectionContext>(await agent.Context);

            Assert.Equal(stopping.Token, settings.RefreshToken);
            Assert.Equal(stopping.Token, connectToken);
            Assert.Same(settings, connectedSettings);
            Assert.Equal(123_456U, maxInboundMessageBodySize);
            Assert.Equal("rotated-user@broker.example.test:5672/", context.Description);
            Assert.True(context.TryGetPayload(out RabbitMqHostSettings? payload));
            Assert.Same(settings, payload);
            Assert.Contains(logger.Entries, entry =>
                entry.Level == LogLevel.Debug && entry.Message.Contains(context.Description, StringComparison.Ordinal));

            await agent.DisposeAsync();
            Assert.Equal(1, GetConnectionProxy(connection).AsyncDisposeCalls);
        }
        finally
        {
            LogContext.Current = previous;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-CREATION", "stop-during-refresh-prevents-connect")]
    public async Task CreateContext_StopDuringRefreshPreventsConnectionAsync()
    {
        using var stopping = new CancellationTokenSource();
        var settings = new RecordingHostSettings { Refresh = _ => stopping.Cancel() };
        var connectCalls = 0;
        var factory = new ConnectionContextFactory(
            CreateHost(settings),
            (_, _, _) =>
            {
                Interlocked.Increment(ref connectCalls);
                return Task.FromResult<IConnection>(CreateConnection());
            });

        RabbitMqConnectionException failure = await AssertContextFailureAsync<RabbitMqConnectionException>(
            factory,
            new TestSupervisor(stopping.Token));

        Assert.True(failure.IsTransient);
        Assert.Contains("stopping", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, settings.RefreshCalls);
        Assert.Equal(0, connectCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-LIFECYCLE", "owner-is-registered-before-connection-creation")]
    public async Task CreateContext_StopDuringConnectCannotLeaveAnUnownedConnectionAsync()
    {
        var supervisor = new Supervisor();
        IConnection connection = CreateConnection();
        Task? stopTask = null;
        var factory = new ConnectionContextFactory(
            CreateHost(new RecordingHostSettings()),
            (_, _, _) =>
            {
                stopTask = supervisor.StopAsync("stop raced connection creation");
                return Task.FromResult(connection);
            });

        IPipeContextAgent<ConnectionContext> agent = factory.CreateContext(supervisor);

        Assert.NotNull(stopTask);
        await stopTask;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => agent.Context);
        await agent.Completed.WaitAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, supervisor.TotalCount);
        Assert.Equal(1, GetConnectionProxy(connection).AsyncDisposeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-FAILURE", "configuration-failure-remains-terminal")]
    public async Task CreateContext_PreservesConfigurationFailureWithoutAConnectionWrapperAsync()
    {
        var expected = new ConfigurationException("invalid host configuration");
        var factory = new ConnectionContextFactory(
            CreateHost(new RecordingHostSettings()),
            (_, _, _) => Task.FromException<IConnection>(expected));

        ConfigurationException actual = await AssertContextFailureAsync<ConfigurationException>(factory, new TestSupervisor());

        Assert.Same(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-CREATION", "default-connector-requires-host-or-resolver")]
    public async Task CreateContext_DefaultConnectorRejectsMissingHostAsync()
    {
        var settings = new RecordingHostSettings { Host = null };
        var factory = new ConnectionContextFactory(CreateHost(settings));

        ConfigurationException actual = await AssertContextFailureAsync<ConfigurationException>(factory, new TestSupervisor());

        Assert.Contains("host name is required", actual.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, settings.RefreshCalls);
    }

    [Theory]
    [InlineData(DefaultConnectionRoute.Host)]
    [InlineData(DefaultConnectionRoute.EndpointResolver)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-CREATION", "default-connector-routes-host-and-resolver-without-losing-socket-cause")]
    public async Task CreateContext_DefaultConnectorUsesTheConfiguredRouteAndPreservesSocketFailureAsync(DefaultConnectionRoute route)
    {
        var expected = new SocketCreationFailureException();
        var tcpClient = new TokenRecordingTcpClient(expected);
        var settings = new RecordingHostSettings
        {
            Host = "127.0.0.1",
            Refresh = connectionFactory => connectionFactory.SocketFactory = _ => tcpClient,
        };
        if (route == DefaultConnectionRoute.EndpointResolver)
        {
            settings.EndpointResolver = new SequentialEndpointResolver(
                [ClusterNode.Parse("127.0.0.1:5672")],
                settings);
        }
        var supervisor = new TestSupervisor();
        var factory = new ConnectionContextFactory(CreateHost(settings));

        RabbitMqConnectionException actual = await AssertContextFailureAsync<RabbitMqConnectionException>(factory, supervisor);

        Assert.True(ContainsException(actual, expected), actual.ToString());
        Assert.Equal(1, tcpClient.ConnectCalls);
        if (route == DefaultConnectionRoute.EndpointResolver)
            Assert.Equal("127.0.0.1", settings.EndpointResolver!.LastHost.HostName);
    }

    [Theory]
    [InlineData(DefaultConnectionRoute.Host)]
    [InlineData(DefaultConnectionRoute.EndpointResolver)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-CREATION", "default-connector-propagates-cancellation-through-both-routes")]
    public async Task CreateContext_DefaultConnectorPropagatesCancellationThroughBothRoutesAsync(DefaultConnectionRoute route)
    {
        using var stopping = new CancellationTokenSource();
        using var tcpClient = new CancellationRecordingTcpClient();
        var settings = new RecordingHostSettings
        {
            Host = "127.0.0.1",
            Refresh = connectionFactory => connectionFactory.SocketFactory = _ => tcpClient,
        };
        if (route == DefaultConnectionRoute.EndpointResolver)
        {
            settings.EndpointResolver = new SequentialEndpointResolver(
                [ClusterNode.Parse("127.0.0.1:5672")],
                settings);
        }
        var factory = new ConnectionContextFactory(CreateHost(settings));
        IPipeContextAgent<ConnectionContext> agent = factory.CreateContext(new TestSupervisor(stopping.Token));
        await tcpClient.Entered.WaitAsync(TestContext.Current.CancellationToken);

        stopping.Cancel();
        bool cancellationPropagated = tcpClient.ConnectToken.IsCancellationRequested;
        tcpClient.ReleaseIfNeeded();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => agent.Context);

        Assert.True(cancellationPropagated);
        Assert.Equal(1, tcpClient.ConnectCalls);
        await agent.Completed.WaitAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-FAILURE", "warning-log-failure-cannot-replace-primary-cause")]
    public async Task CreateContext_PreservesConnectionFailureWhenWarningLoggingAlsoFailsAsync()
    {
        ILogContext? previous = LogContext.Current;
        var expected = new InvalidOperationException("connection failed");
        var factory = new ConnectionContextFactory(
            CreateHost(new RecordingHostSettings()),
            (_, _, _) => Task.FromException<IConnection>(expected));

        try
        {
            LogContext.ConfigureCurrentLogContext(new LevelThrowingLogger(LogLevel.Warning));

            RabbitMqConnectionException actual = await AssertContextFailureAsync<RabbitMqConnectionException>(factory, new TestSupervisor());

            Assert.Same(expected, actual.InnerException);
        }
        finally
        {
            LogContext.Current = previous;
        }
    }

    [Theory]
    [InlineData(ConnectionFailureKind.ConnectFailure)]
    [InlineData(ConnectionFailureKind.BrokerUnreachable)]
    [InlineData(ConnectionFailureKind.OperationInterrupted)]
    [InlineData(ConnectionFailureKind.Unexpected)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-FAILURE", "typed-client-failures-map-to-stable-transport-errors")]
    public async Task CreateContext_MapsEachClientFailureWithoutLosingItsCauseAsync(ConnectionFailureKind kind)
    {
        ILogContext? previous = LogContext.Current;
        var logger = new CaptureLogger();
        Exception expected = CreateFailure(kind);
        var factory = new ConnectionContextFactory(
            CreateHost(new RecordingHostSettings()),
            (_, _, _) => Task.FromException<IConnection>(expected));

        try
        {
            LogContext.ConfigureCurrentLogContext(logger);

            RabbitMqConnectionException actual = await AssertContextFailureAsync<RabbitMqConnectionException>(factory, new TestSupervisor());

            Assert.Same(expected, actual.InnerException);
            Assert.StartsWith(ExpectedPrefix(kind), actual.Message, StringComparison.Ordinal);
            Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning && ReferenceEquals(expected, entry.Exception));
        }
        finally
        {
            LogContext.Current = previous;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-CREATION", "unrelated-cancellation-is-preserved")]
    public async Task CreateContext_PreservesAnUnrelatedCancellationTokenAsync()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var factory = new ConnectionContextFactory(
            CreateHost(new RecordingHostSettings()),
            (_, _, _) => Task.FromCanceled<IConnection>(cancellation.Token));

        OperationCanceledException actual = await AssertContextFailureAnyAsync<OperationCanceledException>(factory, new TestSupervisor());

        Assert.Equal(cancellation.Token, actual.CancellationToken);
    }

    [Theory]
    [InlineData(ContextFailureKind.Configuration)]
    [InlineData(ContextFailureKind.Unexpected)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-FAILURE", "constructed-connection-is-disposed-on-context-failure")]
    public async Task CreateContext_DisposesAConnectedClientWhenContextConstructionFailsAsync(ContextFailureKind kind)
    {
        Exception expected = kind == ContextFailureKind.Configuration
            ? new ConfigurationException("invalid batch settings")
            : new InvalidOperationException("batch settings unavailable");
        var settings = new RecordingHostSettings { BatchSettingsFailure = expected };
        var connection = CreateConnection();
        var factory = new ConnectionContextFactory(
            CreateHost(settings),
            (_, _, _) => Task.FromResult(connection));

        Exception actual = kind == ContextFailureKind.Configuration
            ? await AssertContextFailureAsync<ConfigurationException>(factory, new TestSupervisor())
            : await AssertContextFailureAsync<RabbitMqConnectionException>(factory, new TestSupervisor());

        Assert.Same(expected, kind == ContextFailureKind.Configuration ? actual : actual.InnerException);
        Assert.Equal(1, GetConnectionProxy(connection).DisposeCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-FAILURE", "cleanup-failure-cannot-replace-primary-cause")]
    public async Task CreateContext_PreservesThePrimaryFailureWhenConnectionDisposalAlsoFailsAsync(bool loggerThrows)
    {
        ILogContext? previous = LogContext.Current;
        var primary = new InvalidOperationException("context construction failed");
        var cleanup = new ConnectionCleanupFailureException();
        var settings = new RecordingHostSettings { BatchSettingsFailure = primary };
        IConnection connection = CreateConnection(cleanup);
        var factory = new ConnectionContextFactory(
            CreateHost(settings),
            (_, _, _) => Task.FromResult(connection));

        try
        {
            LogContext.Current = null;
            if (loggerThrows)
                LogContext.ConfigureCurrentLogContext(new LevelThrowingLogger(LogLevel.Error));

            RabbitMqConnectionException actual = await AssertContextFailureAsync<RabbitMqConnectionException>(factory, new TestSupervisor());

            Assert.Same(primary, actual.InnerException);
            Assert.Equal(1, GetConnectionProxy(connection).DisposeCalls);
        }
        finally
        {
            LogContext.Current = previous;
        }
    }

    [Theory]
    [InlineData(ShutdownInitiator.Application, false)]
    [InlineData(ShutdownInitiator.Peer, true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-LIFECYCLE", "shutdown-invalidates-state-and-only-broker-stop-owns-context")]
    public async Task ConnectionShutdown_InvalidatesConnectionStateAndUsesInitiatorOwnershipAsync(
        ShutdownInitiator initiator,
        bool stopsContext)
    {
        IConnection connection = CreateConnection();
        ConnectionProxy connectionProxy = GetConnectionProxy(connection);
        var factory = new ConnectionContextFactory(
            CreateHost(new RecordingHostSettings()),
            (_, _, _) => Task.FromResult(connection));
        IPipeContextAgent<ConnectionContext> agent = factory.CreateContext(new TestSupervisor());
        RabbitMqConnectionContext context = Assert.IsType<RabbitMqConnectionContext>(await agent.Context);
        await connectionProxy.ShutdownHandlerAdded.WaitAsync(TestContext.Current.CancellationToken);
        var exchange = new TestExchange(
            "orders",
            ExchangeType.Direct,
            true,
            false,
            new Dictionary<string, object?>());
        var declarations = 0;
        Task DeclareAsync(CancellationToken _) => Task.FromResult(Interlocked.Increment(ref declarations));
        await context.TopologyEntityCache.DeclareExchangeAsync(exchange, DeclareAsync, TestContext.Current.CancellationToken);
        await context.TopologyEntityCache.DeclareExchangeAsync(exchange, DeclareAsync, TestContext.Current.CancellationToken);
        var reason = new ShutdownEventArgs(initiator, 320, "connection forced");

        await connectionProxy.RaiseShutdownAsync(reason);

        OperationInterruptedException unavailable = await Assert.ThrowsAsync<OperationInterruptedException>(
            () => context.CreateChannelAsync(null, TestContext.Current.CancellationToken));
        Assert.Same(reason, unavailable.ShutdownReason);
        await context.TopologyEntityCache.DeclareExchangeAsync(exchange, DeclareAsync, TestContext.Current.CancellationToken);
        Assert.Equal(2, declarations);
        Assert.Equal(stopsContext, agent.Completed.IsCompleted);

        if (!stopsContext)
            await agent.DisposeAsync();

        await agent.Completed.WaitAsync(TestContext.Current.CancellationToken);
        await connectionProxy.ShutdownHandlerRemovalAttempted.WaitAsync(TestContext.Current.CancellationToken);
        Assert.False(connectionProxy.HasShutdownHandler);
        Assert.Equal(1, connectionProxy.AsyncDisposeCalls);
        Assert.Equal(1, connectionProxy.ShutdownHandlerRemoveCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-LIFECYCLE", "disposed-client-cannot-break-handler-removal")]
    public async Task CompletedContext_IgnoresAnAlreadyDisposedClientDuringHandlerRemovalAsync()
    {
        IConnection connection = CreateConnection();
        ConnectionProxy connectionProxy = GetConnectionProxy(connection);
        var factory = new ConnectionContextFactory(
            CreateHost(new RecordingHostSettings()),
            (_, _, _) => Task.FromResult(connection));
        IPipeContextAgent<ConnectionContext> agent = factory.CreateContext(new TestSupervisor());
        _ = await agent.Context;
        await connectionProxy.ShutdownHandlerAdded.WaitAsync(TestContext.Current.CancellationToken);
        connectionProxy.ShutdownHandlerRemovalFailure = new ObjectDisposedException("connection");

        await agent.DisposeAsync();

        await agent.Completed.WaitAsync(TestContext.Current.CancellationToken);
        await connectionProxy.ShutdownHandlerRemovalAttempted.WaitAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, connectionProxy.AsyncDisposeCalls);
    }

    [Theory]
    [InlineData(HandlerRemovalLogging.None)]
    [InlineData(HandlerRemovalLogging.Records)]
    [InlineData(HandlerRemovalLogging.Throws)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-LIFECYCLE", "unexpected-handler-removal-failure-cannot-escape-completion")]
    public async Task CompletedContext_ContainsUnexpectedHandlerRemovalAndLoggingFailuresAsync(HandlerRemovalLogging logging)
    {
        ILogContext? previous = LogContext.Current;
        IConnection connection = CreateConnection();
        ConnectionProxy connectionProxy = GetConnectionProxy(connection);
        var expected = new InvalidOperationException("handler removal failed");
        connectionProxy.ShutdownHandlerRemovalFailure = expected;
        SignalingLogger? logger = logging == HandlerRemovalLogging.None
            ? null
            : new SignalingLogger(logging == HandlerRemovalLogging.Throws);
        var factory = new ConnectionContextFactory(
            CreateHost(new RecordingHostSettings()),
            (_, _, _) => Task.FromResult(connection));

        try
        {
            LogContext.Current = null;
            if (logger is not null)
                LogContext.ConfigureCurrentLogContext(logger);
            IPipeContextAgent<ConnectionContext> agent = factory.CreateContext(new TestSupervisor());
            _ = await agent.Context;
            await connectionProxy.ShutdownHandlerAdded.WaitAsync(TestContext.Current.CancellationToken);

            await agent.DisposeAsync();

            await agent.Completed.WaitAsync(TestContext.Current.CancellationToken);
            await connectionProxy.ShutdownHandlerRemovalAttempted.WaitAsync(TestContext.Current.CancellationToken);
            if (logger is not null)
            {
                await logger.Called.WaitAsync(TestContext.Current.CancellationToken);
                Assert.Same(expected, logger.Exception);
            }
            else
                await Task.Yield();
            Assert.Equal(1, connectionProxy.AsyncDisposeCalls);
            Assert.Equal(1, connectionProxy.ShutdownHandlerRemoveCalls);
        }
        finally
        {
            LogContext.Current = previous;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-LIFECYCLE", "closed-connection-before-handler-subscription-is-not-published")]
    public async Task CreateContext_DoesNotPublishAConnectionThatClosedBeforeShutdownSubscriptionAsync(bool hasBrokerReason)
    {
        IConnection connection = CreateConnection();
        ConnectionProxy connectionProxy = GetConnectionProxy(connection);
        connectionProxy.IsOpen = false;
        connectionProxy.CloseReason = hasBrokerReason
            ? new ShutdownEventArgs(ShutdownInitiator.Peer, 320, "connection forced")
            : null;
        var factory = new ConnectionContextFactory(
            CreateHost(new RecordingHostSettings()),
            (_, _, _) => Task.FromResult(connection));

        IPipeContextAgent<ConnectionContext> agent = factory.CreateContext(new TestSupervisor());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => agent.Context);
        await agent.Completed.WaitAsync(TestContext.Current.CancellationToken);
        await connectionProxy.AsyncDisposed.WaitAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, connectionProxy.AsyncDisposeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-LIFECYCLE", "connection-closed-during-handler-subscription-is-not-published")]
    public async Task CreateContext_DoesNotPublishAConnectionThatClosesDuringShutdownSubscriptionAsync()
    {
        IConnection connection = CreateConnection();
        ConnectionProxy connectionProxy = GetConnectionProxy(connection);
        connectionProxy.CloseDuringShutdownHandlerAddition = true;
        connectionProxy.CloseReason = new ShutdownEventArgs(ShutdownInitiator.Peer, 320, "connection forced");
        var factory = new ConnectionContextFactory(
            CreateHost(new RecordingHostSettings()),
            (_, _, _) => Task.FromResult(connection));

        IPipeContextAgent<ConnectionContext> agent = factory.CreateContext(new TestSupervisor());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => agent.Context);
        await agent.Completed.WaitAsync(TestContext.Current.CancellationToken);
        await connectionProxy.ShutdownHandlerRemovalAttempted.WaitAsync(TestContext.Current.CancellationToken);
        await connectionProxy.AsyncDisposed.WaitAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, connectionProxy.AsyncDisposeCalls);
        Assert.False(connectionProxy.HasShutdownHandler);
    }

    [Theory]
    [InlineData(SubscriptionFailureKind.Cancellation)]
    [InlineData(SubscriptionFailureKind.Unexpected)]
    [InlineData(SubscriptionFailureKind.UnexpectedWithCleanupFailure)]
    [InlineData(SubscriptionFailureKind.UnexpectedWithCleanupAndLoggingFailure)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-LIFECYCLE", "shutdown-subscription-failure-disposes-unpublished-context")]
    public async Task CreateContext_ShutdownSubscriptionFailureDisposesTheUnpublishedConnectionAsync(
        SubscriptionFailureKind kind)
    {
        ILogContext? previous = LogContext.Current;
        Exception expected = kind == SubscriptionFailureKind.Cancellation
            ? new OperationCanceledException("subscription canceled")
            : new InvalidOperationException("subscription failed");
        IConnection connection = CreateConnection();
        ConnectionProxy connectionProxy = GetConnectionProxy(connection);
        connectionProxy.ShutdownHandlerAdditionFailure = expected;
        if (kind is SubscriptionFailureKind.UnexpectedWithCleanupFailure
            or SubscriptionFailureKind.UnexpectedWithCleanupAndLoggingFailure)
        {
            connectionProxy.AsyncDisposeFailure = new ConnectionCleanupFailureException();
        }
        var factory = new ConnectionContextFactory(
            CreateHost(new RecordingHostSettings()),
            (_, _, _) => Task.FromResult(connection));

        try
        {
            LogContext.Current = null;
            if (kind == SubscriptionFailureKind.UnexpectedWithCleanupAndLoggingFailure)
                LogContext.ConfigureCurrentLogContext(new LevelThrowingLogger(LogLevel.Error));

            IPipeContextAgent<ConnectionContext> agent = factory.CreateContext(new TestSupervisor());
            Exception actual = kind == SubscriptionFailureKind.Cancellation
                ? await Assert.ThrowsAnyAsync<OperationCanceledException>(() => agent.Context)
                : await Assert.ThrowsAsync<InvalidOperationException>(() => agent.Context);

            if (kind != SubscriptionFailureKind.Cancellation)
                Assert.Same(expected, actual);
            await agent.Completed.WaitAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, connectionProxy.AsyncDisposeCalls);
            Assert.False(connectionProxy.HasShutdownHandler);
        }
        finally
        {
            LogContext.Current = previous;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-LIFECYCLE", "active-context-borrows-open-owner-with-caller-token")]
    public async Task CreateActiveContext_BorrowsAnOpenConnectionWithoutOwningItAsync()
    {
        IConnection connection = CreateConnection();
        var factory = new ConnectionContextFactory(
            CreateHost(new RecordingHostSettings()),
            (_, _, _) => Task.FromResult(connection));
        IPipeContextAgent<ConnectionContext> owner = factory.CreateContext(new TestSupervisor());
        _ = await owner.Context;
        using var caller = new CancellationTokenSource();
        var activeSupervisor = new TestSupervisor();

        IActivePipeContextAgent<ConnectionContext> active = factory.CreateActiveContext(activeSupervisor, owner, caller.Token);
        SharedConnectionContext shared = Assert.IsType<SharedConnectionContext>(await active.Context);

        Assert.Same(connection, shared.Connection);
        Assert.Equal(caller.Token, shared.CancellationToken);
        Assert.Equal(1, activeSupervisor.TotalCount);
        await active.DisposeAsync();
        Assert.Equal(0, GetConnectionProxy(connection).AsyncDisposeCalls);

        await owner.DisposeAsync();
        Assert.Equal(1, GetConnectionProxy(connection).AsyncDisposeCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-LIFECYCLE", "closed-connection-preserves-or-synthesizes-broker-reason")]
    public async Task CreateActiveContext_RejectsClosedConnectionWithAnActionableReasonAsync(bool hasBrokerReason)
    {
        IConnection connection = CreateConnection();
        ConnectionProxy proxy = GetConnectionProxy(connection);
        proxy.IsOpen = false;
        var brokerReason = new ShutdownEventArgs(ShutdownInitiator.Peer, 541, "internal error");
        proxy.CloseReason = hasBrokerReason ? brokerReason : null;
        var context = new RabbitMqConnectionContext(
            connection,
            CreateHost(new RecordingHostSettings()),
            "broker.example.test",
            default);
        var owner = new TestContextHandle(Task.FromResult<ConnectionContext>(context));
        var factory = new ConnectionContextFactory(CreateHost(new RecordingHostSettings()));

        IActivePipeContextAgent<ConnectionContext> active = factory.CreateActiveContext(
            new TestSupervisor(),
            owner,
            TestContext.Current.CancellationToken);
        OperationInterruptedException actual = await Assert.ThrowsAsync<OperationInterruptedException>(() => active.Context);

        if (hasBrokerReason)
            Assert.Same(brokerReason, actual.ShutdownReason);
        else
        {
            var synthesized = Assert.IsType<ShutdownEventArgs>(actual.ShutdownReason);
            Assert.Equal(ShutdownInitiator.Library, synthesized.Initiator);
            Assert.Equal(491, synthesized.ReplyCode);
            Assert.Contains("no longer available", synthesized.ReplyText, StringComparison.OrdinalIgnoreCase);
        }

        await context.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-LIFECYCLE", "active-context-wait-honors-caller-cancellation")]
    public async Task CreateActiveContext_CancelsAWaitForTheSharedConnectionAsync()
    {
        var pending = new TaskCompletionSource<ConnectionContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new TestContextHandle(pending.Task);
        var factory = new ConnectionContextFactory(CreateHost(new RecordingHostSettings()));
        using var caller = new CancellationTokenSource();
        IActivePipeContextAgent<ConnectionContext> active = factory.CreateActiveContext(
            new TestSupervisor(),
            owner,
            caller.Token);

        caller.Cancel();
        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => active.Context);

        Assert.Equal(caller.Token, actual.CancellationToken);
        Assert.False(owner.IsDisposed);
    }

    private static IRabbitMqHostConfiguration CreateHost(RabbitMqHostSettings settings, int? maxEnvelopeBytes = null)
    {
        var topology = new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());
        var bus = new RabbitMqBusConfiguration(topology);
        bus.HostConfiguration.Settings = settings;
        if (maxEnvelopeBytes is { } limit)
        {
            new RabbitMqBusFactoryConfigurator(bus).Limits(new MessageLimits
            {
                MaxBodyBytes = limit / 2,
                MaxEnvelopeBytes = limit,
                MaxJsonDepth = 32,
            });
        }
        return bus.HostConfiguration;
    }

    private static bool ContainsException(Exception candidate, Exception expected)
    {
        if (ReferenceEquals(candidate, expected))
            return true;
        if (candidate is AggregateException aggregate && aggregate.InnerExceptions.Any(exception => ContainsException(exception, expected)))
            return true;
        return candidate.InnerException is { } inner && ContainsException(inner, expected);
    }

    private static async Task<TException> AssertContextFailureAsync<TException>(
        ConnectionContextFactory factory,
        ISupervisor supervisor)
        where TException : Exception
    {
        IPipeContextAgent<ConnectionContext> agent = factory.CreateContext(supervisor);
        try
        {
            return await Assert.ThrowsAsync<TException>(() => agent.Context);
        }
        finally
        {
            await agent.DisposeAsync();
        }
    }

    private static async Task<TException> AssertContextFailureAnyAsync<TException>(
        ConnectionContextFactory factory,
        ISupervisor supervisor)
        where TException : Exception
    {
        IPipeContextAgent<ConnectionContext> agent = factory.CreateContext(supervisor);
        try
        {
            return await Assert.ThrowsAnyAsync<TException>(() => agent.Context);
        }
        finally
        {
            await agent.DisposeAsync();
        }
    }

    private static Exception CreateFailure(ConnectionFailureKind kind) => kind switch
    {
        ConnectionFailureKind.ConnectFailure => new ConnectFailureException("connect failed", new IOException("socket failed")),
        ConnectionFailureKind.BrokerUnreachable => new BrokerUnreachableException(new IOException("all endpoints failed")),
        ConnectionFailureKind.OperationInterrupted => new OperationInterruptedException(
            new ShutdownEventArgs(ShutdownInitiator.Peer, 320, "connection forced")),
        _ => new InvalidOperationException("unexpected client failure"),
    };

    private static string ExpectedPrefix(ConnectionFailureKind kind) => kind switch
    {
        ConnectionFailureKind.ConnectFailure => "Connect failed: ",
        ConnectionFailureKind.BrokerUnreachable => "Broker unreachable: ",
        ConnectionFailureKind.OperationInterrupted => "Operation interrupted: ",
        _ => "Create Connection Faulted: ",
    };

    private static IConnection CreateConnection(Exception? disposeFailure = null)
    {
        IConnection connection = DispatchProxy.Create<IConnection, ConnectionProxy>();
        GetConnectionProxy(connection).DisposeFailure = disposeFailure;
        return connection;
    }

    private static ConnectionProxy GetConnectionProxy(IConnection connection) => (ConnectionProxy)(object)connection;

    public enum ConnectionFailureKind
    {
        ConnectFailure,
        BrokerUnreachable,
        OperationInterrupted,
        Unexpected,
    }

    public enum ContextFailureKind
    {
        Configuration,
        Unexpected,
    }

    public enum DefaultConnectionRoute
    {
        Host,
        EndpointResolver,
    }

    public enum SubscriptionFailureKind
    {
        Cancellation,
        Unexpected,
        UnexpectedWithCleanupFailure,
        UnexpectedWithCleanupAndLoggingFailure,
    }

    public enum HandlerRemovalLogging
    {
        None,
        Records,
        Throws,
    }

    private class ConnectionProxy : DispatchProxy
    {
        private AsyncEventHandler<ShutdownEventArgs>? _shutdownHandlers;
        private readonly TaskCompletionSource _shutdownHandlerAdded = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _shutdownHandlerRemovalAttempted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _asyncDisposed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int AsyncDisposeCalls { get; private set; }
        public Task AsyncDisposed => _asyncDisposed.Task;
        public Exception? AsyncDisposeFailure { get; set; }
        public bool CloseDuringShutdownHandlerAddition { get; set; }
        public ShutdownEventArgs? CloseReason { get; set; }
        public int DisposeCalls { get; private set; }
        public Exception? DisposeFailure { get; set; }
        public bool HasShutdownHandler => _shutdownHandlers is not null;
        public bool IsOpen { get; set; } = true;
        public Task ShutdownHandlerAdded => _shutdownHandlerAdded.Task;
        public Task ShutdownHandlerRemovalAttempted => _shutdownHandlerRemovalAttempted.Task;
        public Exception? ShutdownHandlerAdditionFailure { get; set; }
        public int ShutdownHandlerRemoveCalls { get; private set; }
        public Exception? ShutdownHandlerRemovalFailure { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            string name = targetMethod?.Name ?? throw new InvalidOperationException("The invoked method is unavailable.");
            return name switch
            {
                "get_Endpoint" => new AmqpTcpEndpoint("broker.example.test", 5672),
                "get_LocalPort" => 12345,
                "get_IsOpen" => IsOpen,
                "get_CloseReason" => CloseReason,
                "add_ConnectionShutdownAsync" => AddShutdownHandler(args),
                "remove_ConnectionShutdownAsync" => RemoveShutdownHandler(args),
                "Dispose" => Dispose(),
                "DisposeAsync" => DisposeAsync(),
                _ when name.StartsWith("add_", StringComparison.Ordinal) => null,
                _ when name.StartsWith("remove_", StringComparison.Ordinal) => null,
                _ => Default(targetMethod!),
            };
        }

        public async Task RaiseShutdownAsync(ShutdownEventArgs reason)
        {
            AsyncEventHandler<ShutdownEventArgs>? handlers = _shutdownHandlers;
            Assert.NotNull(handlers);

            foreach (AsyncEventHandler<ShutdownEventArgs> handler in handlers.GetInvocationList())
                await handler(this, reason);
        }

        private object? AddShutdownHandler(object?[]? args)
        {
            if (ShutdownHandlerAdditionFailure is not null)
                throw ShutdownHandlerAdditionFailure;
            _shutdownHandlers += Assert.IsType<AsyncEventHandler<ShutdownEventArgs>>(Assert.Single(args!));
            if (CloseDuringShutdownHandlerAddition)
                IsOpen = false;
            _shutdownHandlerAdded.TrySetResult();
            return null;
        }

        private object? RemoveShutdownHandler(object?[]? args)
        {
            ShutdownHandlerRemoveCalls++;
            _shutdownHandlerRemovalAttempted.TrySetResult();
            if (ShutdownHandlerRemovalFailure is not null)
                throw ShutdownHandlerRemovalFailure;
            _shutdownHandlers -= Assert.IsType<AsyncEventHandler<ShutdownEventArgs>>(Assert.Single(args!));
            return null;
        }

        private object? Dispose()
        {
            DisposeCalls++;
            if (DisposeFailure is not null)
                throw DisposeFailure;
            return null;
        }

        private ValueTask DisposeAsync()
        {
            AsyncDisposeCalls++;
            _asyncDisposed.TrySetResult();
            return AsyncDisposeFailure is null
                ? ValueTask.CompletedTask
                : ValueTask.FromException(AsyncDisposeFailure);
        }

        private static object? Default(MethodInfo method)
        {
            if (method.ReturnType == typeof(void))
                return null;
            if (method.ReturnType == typeof(Task))
                return Task.CompletedTask;
            if (method.ReturnType == typeof(ValueTask))
                return ValueTask.CompletedTask;
            return method.ReturnType.IsValueType ? Activator.CreateInstance(method.ReturnType) : null;
        }
    }

    private sealed class TestSupervisor(CancellationToken stopping = default) : ISupervisor
    {
        private long _totalCount;

        public Task Ready => Task.CompletedTask;
        public Task Completed => Task.CompletedTask;
        public CancellationToken Stopping { get; } = stopping;
        public CancellationToken Stopped => default;
        public int PeakActiveCount => _totalCount > 0 ? 1 : 0;
        public long TotalCount => _totalCount;

        public void Add(IAgent agent)
        {
            ArgumentNullException.ThrowIfNull(agent);
            Interlocked.Increment(ref _totalCount);
        }

        public Task StopAsync(StopContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestContextHandle(Task<ConnectionContext> context) : IPipeContextHandle<ConnectionContext>
    {
        public bool IsDisposed { get; private set; }
        public Task<ConnectionContext> Context { get; } = context;

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return default;
        }
    }

    private sealed class RecordingHostSettings : RabbitMqHostSettings
    {
        private readonly TestBatchSettings _batchSettings = new();

        public Action<ConnectionFactory>? Refresh { get; init; }
        public Exception? BatchSettingsFailure { get; init; }
        public int RefreshCalls { get; private set; }
        public CancellationToken RefreshToken { get; private set; }
        public string? Host { get; init; } = "broker.example.test";
        public int Port { get; init; } = 5672;
        public string? VirtualHost { get; init; } = "/";
        public string? Username { get; init; } = "initial-user";
        public string? Password { get; init; } = "secret";
        public TimeSpan Heartbeat { get; init; }
        public bool Ssl { get; init; }
        public SslProtocols SslProtocol { get; init; } = SslProtocols.None;
        public string? SslServerName { get; init; }
        public SslPolicyErrors AcceptablePolicyErrors { get; init; }
        public string? ClientCertificatePath { get; init; }
        public string? ClientCertificatePassphrase { get; init; }
        public X509Certificate? ClientCertificate { get; init; }
        public bool UseClientCertificateAsAuthenticationIdentity { get; init; }
        public LocalCertificateSelectionCallback? CertificateSelectionCallback { get; set; }
        public RemoteCertificateValidationCallback? CertificateValidationCallback { get; set; }
        public IRabbitMqEndpointResolver? EndpointResolver { get; set; }
        public string? ClientProvidedName { get; init; } = "connection-tests";
        public Uri HostAddress => new RabbitMqHostAddress(Host, Port, VirtualHost, Ssl);
        public bool PublisherConfirmation { get; init; } = true;
        public ushort RequestedChannelMax { get; init; }
        public TimeSpan RequestedConnectionTimeout { get; init; } = TimeSpan.FromSeconds(1);
        public BatchSettings BatchSettings => BatchSettingsFailure is null ? _batchSettings : throw BatchSettingsFailure;
        public TimeSpan ContinuationTimeout { get; init; } = TimeSpan.FromSeconds(1);
        public uint? MaxMessageSize { get; init; }
        public ICredentialsProvider? CredentialsProvider { get; init; }
        public uint? RequestedFrameMax { get; init; }

        public Task RefreshAsync(ConnectionFactory connectionFactory, CancellationToken cancellationToken = default)
        {
            RefreshCalls++;
            RefreshToken = cancellationToken;
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            Refresh?.Invoke(connectionFactory);
            return Task.CompletedTask;
        }
    }

    private sealed class TestBatchSettings : BatchSettings
    {
        public bool Enabled => false;
        public int MessageLimit => 100;
        public int SizeLimit => 64 * 1024;
        public TimeSpan Timeout => TimeSpan.FromMilliseconds(1);
    }

    private sealed class ConnectionCleanupFailureException : Exception;

    private sealed class SocketCreationFailureException : Exception;

    private sealed record TestExchange(
        string ExchangeName,
        string ExchangeType,
        bool Durable,
        bool AutoDelete,
        IDictionary<string, object?> ExchangeArguments) : Exchange;

    private sealed class LevelThrowingLogger(LogLevel throwingLevel) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel == throwingLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == throwingLevel)
                throw new LoggingFailureException();
        }
    }

    private sealed class TokenRecordingTcpClient(Exception failure) : ITcpClient
    {
        private readonly Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public bool Connected => false;
        public TimeSpan ReceiveTimeout { get; set; }
        public Socket Client => _socket;
        public int ConnectCalls { get; private set; }
        public CancellationToken ConnectToken { get; private set; }

        public Task ConnectAsync(IPAddress host, int port, CancellationToken cancellationToken = default)
        {
            ConnectCalls++;
            ConnectToken = cancellationToken;
            return Task.FromException(failure);
        }

        public NetworkStream GetStream() => throw new InvalidOperationException("The failed test client has no stream.");

        public void Close() => _socket.Close();

        public void Dispose() => _socket.Dispose();
    }

    private sealed class CancellationRecordingTcpClient : ITcpClient
    {
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private CancellationTokenRegistration _registration;

        public bool Connected => false;
        public TimeSpan ReceiveTimeout { get; set; }
        public Socket Client => _socket;
        public int ConnectCalls { get; private set; }
        public CancellationToken ConnectToken { get; private set; }
        public Task Entered => _entered.Task;

        public Task ConnectAsync(IPAddress host, int port, CancellationToken cancellationToken = default)
        {
            ConnectCalls++;
            ConnectToken = cancellationToken;
            _registration = cancellationToken.Register(() => _completion.TrySetCanceled(cancellationToken));
            _entered.TrySetResult();
            return _completion.Task;
        }

        public void ReleaseIfNeeded() => _completion.TrySetCanceled();

        public NetworkStream GetStream() => throw new InvalidOperationException("The canceled test client has no stream.");

        public void Close() => _socket.Close();

        public void Dispose()
        {
            _registration.Dispose();
            _socket.Dispose();
        }

    }

    private sealed class CaptureLogger : ILogger
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }

    private sealed class SignalingLogger(bool throws) : ILogger
    {
        private readonly TaskCompletionSource _called = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Called => _called.Task;
        public Exception? Exception { get; private set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Error;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Exception = exception;
            _called.TrySetResult();
            if (throws)
                throw new LoggingFailureException();
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class LoggingFailureException : Exception;
}
