using System.Collections.Concurrent;
using System.Reflection;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMqTransport.Configuration;
using ViciOne.ServiceBus.ActiveMqTransport.Middleware;
using ViciOne.ServiceBus.ActiveMqTransport.Tests.TestDoubles;
using ViciOne.ServiceBus.ActiveMqTransport.Topology;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.Tests.ActiveMqTransport;

public sealed class ActiveMqLifecycleTests
{
    [Theory]
    [InlineData("activemq://broker:61616/", true)]
    [InlineData("amqp://broker:5672/", false)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "manual-auto-delete-is-provider-capability")]
    public void ManualAutoDelete_IsInstalledOnlyForTheProviderThatSupportsDestinationDeletion(string address, bool expected)
    {
        bool actual = ConfigureActiveMqTopologyFilter<ReceiveSettings>.RequiresManualAutoDelete(new Uri(address));

        Assert.Equal(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "retained-send-session-retires-with-connection")]
    public async Task SharedSendSession_RetiresWhenItsConnectionFaults()
    {
        ExceptionListener? exceptionListener = null;
        var recoveredListener = new TaskCompletionSource<ExceptionListener>(TaskCreationOptions.RunContinuationsAsynchronously);
        var listenerGeneration = 0;
        var listenerRemoved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        IConnection connection = InterfaceProxy<IConnection>.Create((method, args) => method.Name switch
        {
            "add_ExceptionListener" => Record(() =>
            {
                ExceptionListener listener = Assert.IsType<ExceptionListener>(args![0]);
                exceptionListener += listener;
                if (Interlocked.Increment(ref listenerGeneration) == 2)
                    recoveredListener.TrySetResult(listener);
            }),
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
            Assert.Same(
                await recoveredListener.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken),
                exceptionListener);
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
        var agent = new RemoveAutoDeleteAgent(FixedConnectionSupervisor(connection), topology);

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
        var closeFailure = new NMSException("close failed");
        bool disposed = false;
        IConnection connection = InterfaceProxy<IConnection>.Create((method, _) => method.Name switch
        {
            nameof(IConnection.CloseAsync) => Task.FromException(closeFailure),
            nameof(IDisposable.Dispose) => Record(() => disposed = true),
            _ => Default(method.ReturnType),
        });
        ActiveMqConnectionContext context = CreateConnectionContext(connection);

        NMSException actual = await Assert.ThrowsAsync<NMSException>(async () => await context.DisposeAsync());

        Assert.Same(closeFailure, actual);
        Assert.True(disposed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "connection-cleanup-aggregates-in-stage-order")]
    public async Task ConnectionDispose_AggregatesMultipleFailuresInStageOrder()
    {
        var closeFailure = new NMSException("close failed");
        var disposeFailure = new InvalidOperationException("dispose failed");
        IConnection connection = InterfaceProxy<IConnection>.Create((method, _) => method.Name switch
        {
            nameof(IConnection.CloseAsync) => Task.FromException(closeFailure),
            nameof(IDisposable.Dispose) => throw disposeFailure,
            _ => Default(method.ReturnType),
        });
        ActiveMqConnectionContext context = CreateConnectionContext(connection);

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(async () => await context.DisposeAsync());

        Assert.Collection(
            actual.InnerExceptions,
            exception => Assert.Same(closeFailure, exception),
            exception => Assert.Same(disposeFailure, exception));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "single-aggregate-cleanup-failure-preserves-identity")]
    public async Task ConnectionDispose_PreservesASoleAggregateFailureAsTheOriginalStageException()
    {
        var closeFailure = new AggregateException(
            "provider close failed",
            new NMSException("socket failed"),
            new InvalidOperationException("listener failed"));
        IConnection connection = InterfaceProxy<IConnection>.Create((method, _) => method.Name switch
        {
            nameof(IConnection.CloseAsync) => Task.FromException(closeFailure),
            _ => Default(method.ReturnType),
        });
        ActiveMqConnectionContext context = CreateConnectionContext(connection);

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(async () => await context.DisposeAsync());

        Assert.Same(closeFailure, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "session-disposed-after-close-failure")]
    public async Task SessionDispose_AttemptsDisposeAfterCloseFails()
    {
        var closeFailure = new NMSException("close failed");
        bool disposed = false;
        ISession session = InterfaceProxy<ISession>.Create((method, _) => method.Name switch
        {
            nameof(ISession.CloseAsync) => Task.FromException(closeFailure),
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

        NMSException actual = await Assert.ThrowsAsync<NMSException>(async () => await context.DisposeAsync());

        Assert.Same(closeFailure, actual);
        Assert.True(disposed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "session-cleanup-aggregates-in-stage-order")]
    public async Task SessionDispose_AggregatesMultipleFailuresInStageOrder()
    {
        var closeFailure = new NMSException("close failed");
        var disposeFailure = new InvalidOperationException("dispose failed");
        ISession session = InterfaceProxy<ISession>.Create((method, _) => method.Name switch
        {
            nameof(ISession.CloseAsync) => Task.FromException(closeFailure),
            nameof(IDisposable.Dispose) => throw disposeFailure,
            _ => Default(method.ReturnType),
        });
        await using ActiveMqConnectionContext connectionContext = CreateConnectionContext(
            InterfaceProxy<IConnection>.Create((method, _) => method.Name switch
            {
                nameof(IConnection.CloseAsync) => Task.CompletedTask,
                _ => Default(method.ReturnType),
            }));
        var context = new ActiveMqSessionContext(connectionContext, session, TestContext.Current.CancellationToken);

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(async () => await context.DisposeAsync());

        Assert.Collection(
            actual.InnerExceptions,
            exception => Assert.Same(closeFailure, exception),
            exception => Assert.Same(disposeFailure, exception));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "auto-delete-cleanup-attempts-every-entity-after-failure")]
    public async Task AutoDeleteCleanup_AggregatesFailuresInStableOrderAndAttemptsEveryDistinctEntity()
    {
        var firstFailure = new NMSException("first queue failed");
        var secondFailure = new InvalidOperationException("topic failed");
        var attempted = new List<string>();
        bool sessionClosed = false;
        bool sessionDisposed = false;
        ISession stopSession = InterfaceProxy<ISession>.Create((method, args) => method.Name switch
        {
            nameof(ISession.GetQueue) => QueueDestination(Assert.IsType<string>(args![0])),
            nameof(ISession.GetTopic) => TopicDestination(Assert.IsType<string>(args![0])),
            nameof(ISession.DeleteDestination) => DeleteDestination(
                Assert.IsAssignableFrom<IDestination>(args![0]),
                firstFailure,
                secondFailure,
                attempted),
            nameof(ISession.CloseAsync) => Record(() => sessionClosed = true, Task.CompletedTask),
            nameof(IDisposable.Dispose) => Record(() => sessionDisposed = true),
            _ => Default(method.ReturnType),
        });
        ConnectionContext connection = InterfaceProxy<ConnectionContext>.Create((method, _) => method.Name switch
        {
            nameof(ConnectionContext.CreateSession) => Task.FromResult(stopSession),
            nameof(ConnectionContext.TryRemoveTemporaryEntity) => false,
            _ => Default(method.ReturnType),
        });
        var consumerQueue = QueueTopology("consumer-temp");
        var topic = TopicTopology("topic-temp");
        var queue = QueueTopology("queue-temp");
        ViciOne.ServiceBus.ActiveMqTransport.Topology.Consumer consumer =
            InterfaceProxy<ViciOne.ServiceBus.ActiveMqTransport.Topology.Consumer>.Create((method, _) => method.Name switch
        {
            "get_Destination" => consumerQueue,
            _ => Default(method.ReturnType),
        });
        var topology = new ActiveMqBrokerTopology([topic], [consumerQueue, queue], [consumer]);
        var agent = new RemoveAutoDeleteAgent(FixedConnectionSupervisor(connection), topology);

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(
            () => agent.Stop("endpoint stopping", TestContext.Current.CancellationToken));

        Assert.Collection(
            actual.InnerExceptions,
            exception => Assert.Same(firstFailure, exception),
            exception => Assert.Same(secondFailure, exception));
        Assert.Equal(["queue:consumer-temp", "topic:topic-temp", "queue:queue-temp"], attempted);
        Assert.True(sessionClosed);
        Assert.True(sessionDisposed);
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

    private static object? DeleteDestination(
        IDestination destination,
        Exception firstFailure,
        Exception secondFailure,
        ICollection<string> attempted)
    {
        if (destination is IQueue queue)
        {
            attempted.Add($"queue:{queue.QueueName}");
            if (queue.QueueName == "consumer-temp")
                throw firstFailure;
            return null;
        }

        var topic = Assert.IsAssignableFrom<ITopic>(destination);
        attempted.Add($"topic:{topic.TopicName}");
        throw secondFailure;
    }

    private static IQueue QueueDestination(string name) =>
        InterfaceProxy<IQueue>.Create((method, _) => method.Name switch
        {
            "get_IsQueue" => true,
            "get_IsTopic" => false,
            "get_QueueName" => name,
            _ => Default(method.ReturnType),
        });

    private static ITopic TopicDestination(string name) =>
        InterfaceProxy<ITopic>.Create((method, _) => method.Name switch
        {
            "get_IsQueue" => false,
            "get_IsTopic" => true,
            "get_TopicName" => name,
            _ => Default(method.ReturnType),
        });

    private static ViciOne.ServiceBus.ActiveMqTransport.Topology.Queue QueueTopology(string name) =>
        InterfaceProxy<ViciOne.ServiceBus.ActiveMqTransport.Topology.Queue>.Create((method, _) => method.Name switch
        {
            "get_EntityName" => name,
            "get_AutoDelete" => true,
            "get_Durable" => false,
            _ => Default(method.ReturnType),
        });

    private static Topic TopicTopology(string name) =>
        InterfaceProxy<Topic>.Create((method, _) => method.Name switch
        {
            "get_EntityName" => name,
            "get_AutoDelete" => true,
            "get_Durable" => false,
            _ => Default(method.ReturnType),
        });

    private static IConnectionContextSupervisor FixedConnectionSupervisor(ConnectionContext connection) =>
        InterfaceProxy<IConnectionContextSupervisor>.Create((method, args) => method.Name switch
        {
            "Send" => Assert.IsAssignableFrom<IPipe<ConnectionContext>>(args![0]).Send(connection),
            _ => Default(method.ReturnType),
        });

    private static object? Default(Type returnType)
    {
        if (returnType == typeof(void))
            return null;

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
