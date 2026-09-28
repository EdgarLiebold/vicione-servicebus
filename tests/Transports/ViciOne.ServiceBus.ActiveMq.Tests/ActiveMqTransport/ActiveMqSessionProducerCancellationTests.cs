using Apache.NMS;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.ActiveMq.Tests.TestDoubles;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.ActiveMqTransport;

public sealed class ActiveMqSessionProducerCancellationTests
{
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "first-sender-cancellation-does-not-abort-shared-producer-creation")]
    public async Task CanceledFirstSender_DoesNotAbortAnotherSendersQueuedProducerCreationAsync()
    {
        IDestination blockingDestination = Destination();
        IDestination sharedDestination = Destination();
        IMessage blockingMessage = Message();
        IMessage canceledMessage = Message();
        IMessage survivingMessage = Message();
        var blockingFactoryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseBlockingFactory = new TaskCompletionSource<IMessageProducer>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sharedFactoryCalls = 0;
        var sharedSendCalls = 0;
        var sharedDisposals = 0;
        IMessageProducer blockingProducer = Producer((method, _) => method.Name switch
        {
            nameof(IMessageProducer.SendAsync) => Task.CompletedTask,
            _ => Default(method.ReturnType),
        });
        IMessageProducer sharedProducer = Producer((method, args) => method.Name switch
        {
            nameof(IMessageProducer.SendAsync) => Record(() =>
            {
                Assert.Same(survivingMessage, args![0]);
                Interlocked.Increment(ref sharedSendCalls);
            }, Task.CompletedTask),
            nameof(IDisposable.Dispose) => Record(() => Interlocked.Increment(ref sharedDisposals)),
            _ => Default(method.ReturnType),
        });
        ISession session = InterfaceProxy<ISession>.Create((method, args) => method.Name switch
        {
            nameof(ISession.CreateProducerAsync) when ReferenceEquals(args![0], blockingDestination) =>
                Record(() => blockingFactoryStarted.TrySetResult(), releaseBlockingFactory.Task),
            nameof(ISession.CreateProducerAsync) when ReferenceEquals(args![0], sharedDestination) =>
                Record(() => Interlocked.Increment(ref sharedFactoryCalls), Task.FromResult(sharedProducer)),
            nameof(ISession.CloseAsync) => Task.CompletedTask,
            _ => Default(method.ReturnType),
        });
        IConnection connection = InterfaceProxy<IConnection>.Create((method, _) => method.Name switch
        {
            nameof(IConnection.CloseAsync) => Task.CompletedTask,
            _ => Default(method.ReturnType),
        });
        var topology = new ActiveMqTopologyConfiguration(ActiveMqBusFactory.CreateMessageTopology());
        var busConfiguration = new ActiveMqBusConfiguration(topology);
        busConfiguration.HostConfiguration.Settings = new OpenWireHostSettings(new Uri("activemq://broker.internal:61616"));
        await using var connectionContext = new ActiveMqConnectionContext(connection, busConfiguration.HostConfiguration, CancellationToken.None);
        var context = new ActiveMqSessionContext(connectionContext, session, CancellationToken.None);
        using var canceledSender = new CancellationTokenSource();

        try
        {
            Task blocking = context.SendAsync(blockingDestination, blockingMessage, TestContext.Current.CancellationToken);
            await blockingFactoryStarted.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            Task canceled = context.SendAsync(sharedDestination, canceledMessage, canceledSender.Token);
            Task surviving = context.SendAsync(sharedDestination, survivingMessage, TestContext.Current.CancellationToken);

            canceledSender.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                canceled.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
            releaseBlockingFactory.TrySetResult(blockingProducer);

            await blocking.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            await surviving.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            Assert.Equal(1, Volatile.Read(ref sharedFactoryCalls));
            Assert.Equal(1, Volatile.Read(ref sharedSendCalls));
            Assert.Equal(0, Volatile.Read(ref sharedDisposals));
        }
        finally
        {
            releaseBlockingFactory.TrySetResult(blockingProducer);
            await context.DisposeAsync();
        }

        Assert.Equal(1, Volatile.Read(ref sharedDisposals));
    }

    private static IDestination Destination() =>
        InterfaceProxy<IDestination>.Create((method, _) => Default(method.ReturnType));

    private static IMessage Message() =>
        InterfaceProxy<IMessage>.Create((method, _) => Default(method.ReturnType));

    private static IMessageProducer Producer(Func<System.Reflection.MethodInfo, object?[]?, object?> handler) =>
        InterfaceProxy<IMessageProducer>.Create(handler);

    private static object? Record(Action action, object? result = null)
    {
        action();
        return result;
    }

    private static object? Default(Type returnType) =>
        returnType == typeof(void)
            ? null
            : returnType.IsValueType
                ? Activator.CreateInstance(returnType)
                : null;
}
