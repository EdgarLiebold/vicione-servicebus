using System.Collections.Concurrent;
using System.Reflection;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMqTransport.Configuration;
using ViciOne.ServiceBus.ActiveMqTransport.Middleware;
using ViciOne.ServiceBus.ActiveMqTransport.Topology;
using ViciOne.ServiceBus.ActiveMqTransport.Tests.TestDoubles;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.Tests.ActiveMqTransport;

public sealed class ActiveMqLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "retained-send-session-retires-with-connection")]
    public async Task SharedSendSession_RetiresWhenItsConnectionFaults()
    {
        ExceptionListener? exceptionListener = null;
        var listenerRemoved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        IConnection connection = InterfaceProxy<IConnection>.Create((method, args) => method.Name switch
        {
            "add_ExceptionListener" => Record(() => exceptionListener += Assert.IsType<ExceptionListener>(args![0])),
            "remove_ExceptionListener" => Record(() =>
            {
                exceptionListener -= Assert.IsType<ExceptionListener>(args![0]);
                listenerRemoved.TrySetResult(true);
            }),
            _ => Default(method.ReturnType),
        });
        ConnectionContext connectionContext = InterfaceProxy<ConnectionContext>.Create((method, _) => method.Name switch
        {
            "get_Connection" => connection,
            _ => Default(method.ReturnType),
        });
        SessionContext sessionContext = InterfaceProxy<SessionContext>.Create((method, _) => method.Name switch
        {
            "get_ConnectionContext" => connectionContext,
            _ => Default(method.ReturnType),
        });
        var parent = new FixedSessionContextSupervisor(sessionContext);
        var sendSupervisor = new SessionContextSupervisor(parent);

        try
        {
            SessionContext firstUse = await CaptureSession(sendSupervisor);
            SessionContext secondUse = await CaptureSession(sendSupervisor);
            SessionContext firstCachedSession = UnwrapSharedSession(firstUse);
            Assert.Same(firstCachedSession, UnwrapSharedSession(secondUse));
            ExceptionListener activeListener = Assert.IsType<ExceptionListener>(exceptionListener);

            activeListener(new NMSException("connection lost"));
            Assert.True(await listenerRemoved.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

            SessionContext recoveredUse = await CaptureSession(sendSupervisor);
            Assert.NotSame(firstCachedSession, UnwrapSharedSession(recoveredUse));
            Assert.NotNull(exceptionListener);
        }
        finally
        {
            await sendSupervisor.Stop("test complete", CancellationToken.None);
            await parent.Stop("test complete", CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "auto-delete-cleanup-acquires-live-session-at-stop")]
    public async Task AutoDeleteCleanup_AcquiresALiveSessionWhenTheEndpointStops()
    {
        const string queueName = "temporary-orders";
        bool sessionCreated = false;
        bool sessionClosed = false;
        bool sessionDisposed = false;
        string? deletedQueue = null;
        IQueue queueDestination = InterfaceProxy<IQueue>.Create((method, _) => method.Name switch
        {
            "get_QueueName" => queueName,
            _ => Default(method.ReturnType),
        });
        ISession stopSession = InterfaceProxy<ISession>.Create((method, args) => method.Name switch
        {
            nameof(ISession.GetQueue) => queueDestination,
            nameof(ISession.DeleteDestination) => Record(() =>
            {
                Assert.Same(queueDestination, args![0]);
                deletedQueue = queueName;
            }),
            nameof(ISession.CloseAsync) => Record(() => sessionClosed = true, Task.CompletedTask),
            nameof(IDisposable.Dispose) => Record(() => sessionDisposed = true),
            _ => Default(method.ReturnType),
        });
        ConnectionContext connection = InterfaceProxy<ConnectionContext>.Create((method, _) => method.Name switch
        {
            nameof(ConnectionContext.CreateSession) => Record(
                () => sessionCreated = true,
                Task.FromResult(stopSession)),
            nameof(ConnectionContext.TryRemoveTemporaryEntity) => false,
            _ => Default(method.ReturnType),
        });
        ViciOne.ServiceBus.ActiveMqTransport.Topology.Queue queue =
            InterfaceProxy<ViciOne.ServiceBus.ActiveMqTransport.Topology.Queue>.Create((method, _) => method.Name switch
            {
                "get_EntityName" => queueName,
                "get_AutoDelete" => true,
                "get_Durable" => false,
                _ => Default(method.ReturnType),
            });
        var topology = new ActiveMqBrokerTopology([], [queue], []);
        var agent = new RemoveAutoDeleteAgent(connection, topology);

        await agent.Stop("endpoint stopping", TestContext.Current.CancellationToken);

        Assert.True(sessionCreated);
        Assert.Equal(queueName, deletedQueue);
        Assert.True(sessionClosed);
        Assert.True(sessionDisposed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "connection-disposed-after-close-failure")]
    public async Task ConnectionDispose_AttemptsEveryCleanupStageAfterCloseFails()
    {
        bool disposed = false;
        IConnection connection = InterfaceProxy<IConnection>.Create((method, _) => method.Name switch
        {
            nameof(IConnection.CloseAsync) => Task.FromException(new NMSException("close failed")),
            nameof(IDisposable.Dispose) => Record(() => disposed = true),
            _ => Default(method.ReturnType),
        });
        ActiveMqConnectionContext context = CreateConnectionContext(connection);

        await context.DisposeAsync();

        Assert.True(disposed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "session-disposed-after-close-failure")]
    public async Task SessionDispose_AttemptsDisposeAfterCloseFails()
    {
        bool disposed = false;
        ISession session = InterfaceProxy<ISession>.Create((method, _) => method.Name switch
        {
            nameof(ISession.CloseAsync) => Task.FromException(new NMSException("close failed")),
            nameof(IDisposable.Dispose) => Record(() => disposed = true),
            _ => Default(method.ReturnType),
        });
        await using ActiveMqConnectionContext connectionContext = CreateConnectionContext(
            InterfaceProxy<IConnection>.Create((method, _) => method.Name switch
            {
                nameof(IConnection.CloseAsync) => Task.CompletedTask,
                _ => Default(method.ReturnType),
            }));
        var context = new ActiveMqSessionContext(connectionContext, session, TestContext.Current.CancellationToken);

        await context.DisposeAsync();

        Assert.True(disposed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "temporary-entity-removed-after-delete")]
    public async Task TemporaryEntityDelete_RemovesTheSuccessfulRegistration()
    {
        await using ActiveMqConnectionContext context = CreateConnectionContext(
            InterfaceProxy<IConnection>.Create((method, _) => method.Name switch
            {
                nameof(IConnection.CloseAsync) => Task.CompletedTask,
                _ => Default(method.ReturnType),
            }));
        IDestination destination = InterfaceProxy<IDestination>.Create((method, _) => Default(method.ReturnType));
        TemporaryEntities(context)["temp-orders"] = destination;
        IDestination? deleted = null;
        ISession session = InterfaceProxy<ISession>.Create((method, args) => method.Name switch
        {
            nameof(ISession.DeleteDestination) => Record(() => deleted = Assert.IsAssignableFrom<IDestination>(args![0])),
            _ => Default(method.ReturnType),
        });

        bool removed = context.TryRemoveTemporaryEntity(session, "temp-orders");

        Assert.True(removed);
        Assert.Same(destination, deleted);
        Assert.False(context.TryGetTemporaryEntity("temp-orders", out _));
        Assert.False(context.TryRemoveTemporaryEntity(session, "temp-orders"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "failed-temporary-delete-remains-retryable")]
    public async Task TemporaryEntityDelete_RestoresTheRegistrationWhenTheBrokerDeleteFails()
    {
        await using ActiveMqConnectionContext context = CreateConnectionContext(
            InterfaceProxy<IConnection>.Create((method, _) => method.Name switch
            {
                nameof(IConnection.CloseAsync) => Task.CompletedTask,
                _ => Default(method.ReturnType),
            }));
        IDestination destination = InterfaceProxy<IDestination>.Create((method, _) => Default(method.ReturnType));
        TemporaryEntities(context)["temp-orders"] = destination;
        ISession session = InterfaceProxy<ISession>.Create((method, _) => method.Name switch
        {
            nameof(ISession.DeleteDestination) => throw new NMSException("delete failed"),
            _ => Default(method.ReturnType),
        });

        NMSException exception = Assert.Throws<NMSException>(
            () => context.TryRemoveTemporaryEntity(session, "temp-orders"));

        Assert.Equal("delete failed", exception.Message);
        Assert.True(context.TryGetTemporaryEntity("temp-orders", out IDestination restored));
        Assert.Same(destination, restored);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "temporary-topic-registration-reused-by-publisher")]
    public async Task AmqpTemporaryTopicRegistration_IsReusedOnlyForTopicDestinations()
    {
        await using ActiveMqConnectionContext connectionContext = CreateConnectionContext(
            InterfaceProxy<IConnection>.Create((method, _) => method.Name switch
            {
                nameof(IConnection.CloseAsync) => Task.CompletedTask,
                _ => Default(method.ReturnType),
            }),
            ActiveMqHostAddress.AmqpScheme);
        ITopic registeredTopic = InterfaceProxy<ITopic>.Create((method, _) => method.Name switch
        {
            "get_IsTopic" => true,
            "get_IsQueue" => false,
            "get_TopicName" => "provider-topic-id",
            _ => Default(method.ReturnType),
        });
        IQueue requestedQueue = InterfaceProxy<IQueue>.Create((method, _) => method.Name switch
        {
            "get_IsTopic" => false,
            "get_IsQueue" => true,
            "get_QueueName" => "logical-name",
            _ => Default(method.ReturnType),
        });
        int queueLookups = 0;
        ISession session = InterfaceProxy<ISession>.Create((method, _) => method.Name switch
        {
            nameof(ISession.GetQueue) => Record(() => queueLookups++, requestedQueue),
            nameof(ISession.GetQueueAsync) => Task.FromResult(requestedQueue),
            nameof(ISession.CloseAsync) => Task.CompletedTask,
            _ => Default(method.ReturnType),
        });
        TemporaryEntities(connectionContext)["logical-name"] = registeredTopic;
        await using var sessionContext = new ActiveMqSessionContext(
            connectionContext,
            session,
            TestContext.Current.CancellationToken);

        IDestination topic = await sessionContext.GetDestination("logical-name", DestinationType.Topic);
        IDestination temporaryTopic = await sessionContext.GetDestination("logical-name", DestinationType.TemporaryTopic);
        IDestination queue = await sessionContext.GetDestination("logical-name", DestinationType.Queue);

        Assert.Same(registeredTopic, topic);
        Assert.Same(registeredTopic, temporaryTopic);
        Assert.Same(requestedQueue, queue);
        Assert.Equal(1, queueLookups);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "openwire-virtual-topic-keeps-canonical-publish-name")]
    public async Task OpenWireVirtualTopicPublication_DoesNotUseATemporaryTopicRegistration()
    {
        await using ActiveMqConnectionContext connectionContext = CreateConnectionContext(
            InterfaceProxy<IConnection>.Create((method, _) => method.Name switch
            {
                nameof(IConnection.CloseAsync) => Task.CompletedTask,
                _ => Default(method.ReturnType),
            }));
        ITopic registeredTopic = InterfaceProxy<ITopic>.Create((method, _) => method.Name switch
        {
            "get_IsTopic" => true,
            "get_IsQueue" => false,
            "get_TopicName" => "provider-topic-id",
            _ => Default(method.ReturnType),
        });
        ITopic canonicalTopic = InterfaceProxy<ITopic>.Create((method, _) => method.Name switch
        {
            "get_IsTopic" => true,
            "get_IsQueue" => false,
            "get_TopicName" => "VirtualTopic.logical-name",
            _ => Default(method.ReturnType),
        });
        int topicLookups = 0;
        ISession session = InterfaceProxy<ISession>.Create((method, _) => method.Name switch
        {
            nameof(ISession.GetTopic) => Record(() => topicLookups++, canonicalTopic),
            nameof(ISession.CloseAsync) => Task.CompletedTask,
            _ => Default(method.ReturnType),
        });
        TemporaryEntities(connectionContext)["VirtualTopic.logical-name"] = registeredTopic;
        await using var sessionContext = new ActiveMqSessionContext(
            connectionContext,
            session,
            TestContext.Current.CancellationToken);

        IDestination publishTopic = await sessionContext.GetDestination(
            "VirtualTopic.logical-name",
            DestinationType.Topic);
        IDestination explicitTemporaryTopic = await sessionContext.GetDestination(
            "VirtualTopic.logical-name",
            DestinationType.TemporaryTopic);

        Assert.Same(canonicalTopic, publishTopic);
        Assert.Same(registeredTopic, explicitTemporaryTopic);
        Assert.Equal(1, topicLookups);
    }

    private static ActiveMqConnectionContext CreateConnectionContext(
        IConnection connection,
        string scheme = ActiveMqHostAddress.ActiveMqScheme)
    {
        var topology = new ActiveMqTopologyConfiguration(ActiveMqBusFactory.CreateMessageTopology());
        var busConfiguration = new ActiveMqBusConfiguration(topology);
        busConfiguration.HostConfiguration.Settings = scheme == ActiveMqHostAddress.AmqpScheme
            ? new AmqpHostSettings(new Uri("amqp://broker.internal:5672"))
            : new OpenWireHostSettings(new Uri("activemq://broker.internal:61616"));
        return new ActiveMqConnectionContext(connection, busConfiguration.HostConfiguration, CancellationToken.None);
    }

    private static ConcurrentDictionary<string, IDestination> TemporaryEntities(ActiveMqConnectionContext context) =>
        Assert.IsType<ConcurrentDictionary<string, IDestination>>(
            typeof(ActiveMqConnectionContext)
                .GetField("_temporaryEntities", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(context));

    private static async Task<SessionContext> CaptureSession(ISessionContextSupervisor supervisor)
    {
        var pipe = new CaptureSessionPipe();
        await supervisor.Send(pipe, TestContext.Current.CancellationToken);
        return Assert.IsAssignableFrom<SessionContext>(pipe.Context);
    }

    private static SessionContext UnwrapSharedSession(SessionContext context) =>
        Assert.IsAssignableFrom<SessionContext>(
            typeof(SharedSessionContext)
                .GetField("_context", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(Assert.IsType<SharedSessionContext>(context)));

    private static object? Record(Action action)
    {
        action();
        return null;
    }

    private static object Record(Action action, object result)
    {
        action();
        return result;
    }

    private static object? Default(Type returnType)
    {
        if (returnType == typeof(Task))
            return Task.CompletedTask;

        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }

    private sealed class CaptureSessionPipe : IPipe<SessionContext>
    {
        public SessionContext? Context { get; private set; }

        public Task Send(SessionContext context)
        {
            Context = context;
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class FixedSessionContextSupervisor :
        TransportPipeContextSupervisor<SessionContext>,
        ISessionContextSupervisor
    {
        public FixedSessionContextSupervisor(SessionContext context)
            : base(new FixedSessionContextFactory(context))
        {
        }
    }

    private sealed class FixedSessionContextFactory(SessionContext context) : IPipeContextFactory<SessionContext>
    {
        public IPipeContextAgent<SessionContext> CreateContext(ISupervisor supervisor) =>
            supervisor.AddContext(context);

        public IActivePipeContextAgent<SessionContext> CreateActiveContext(
            ISupervisor supervisor,
            PipeContextHandle<SessionContext> contextHandle,
            CancellationToken cancellationToken = default) =>
            supervisor.AddActiveContext(contextHandle, contextHandle.Context);
    }
}
