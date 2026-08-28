using System.Collections.Concurrent;
using System.Reflection;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMqTransport.Configuration;
using ViciOne.ServiceBus.ActiveMqTransport.Tests.TestDoubles;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.Tests.ActiveMqTransport;

public sealed class ActiveMqLifecycleTests
{
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

    private static ActiveMqConnectionContext CreateConnectionContext(IConnection connection)
    {
        var topology = new ActiveMqTopologyConfiguration(ActiveMqBusFactory.CreateMessageTopology());
        var busConfiguration = new ActiveMqBusConfiguration(topology);
        busConfiguration.HostConfiguration.Settings = new OpenWireHostSettings(new Uri("activemq://broker.internal:61616"));
        return new ActiveMqConnectionContext(connection, busConfiguration.HostConfiguration, CancellationToken.None);
    }

    private static ConcurrentDictionary<string, IDestination> TemporaryEntities(ActiveMqConnectionContext context) =>
        Assert.IsType<ConcurrentDictionary<string, IDestination>>(
            typeof(ActiveMqConnectionContext)
                .GetField("_temporaryEntities", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(context));

    private static object? Record(Action action)
    {
        action();
        return null;
    }

    private static object? Default(Type returnType)
    {
        if (returnType == typeof(Task))
            return Task.CompletedTask;

        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }
}
