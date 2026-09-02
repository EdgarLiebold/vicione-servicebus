namespace ViciOne.ServiceBus.Tests.InternalAccess.Transactions;

using System.Reflection;
using System.Transactions;
using ViciOne.ServiceBus.Transactions;


public sealed class BufferedBusTestDriver
{
    private readonly BufferedBus _bus;

    public BufferedBusTestDriver()
        : this(UnusedBus.Create())
    {
    }

    public BufferedBusTestDriver(IBus bus)
    {
        _bus = new BufferedBus(bus);
    }

    public IBufferedBus Bus => _bus;

    public Task Enqueue(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default) =>
        _bus.Add(action, cancellationToken);
}


public sealed class AmbientTransactionBusTestDriver
{
    private readonly AmbientTransactionBus _bus;

    public AmbientTransactionBusTestDriver()
        : this(UnusedBus.Create())
    {
    }

    public AmbientTransactionBusTestDriver(IBus bus)
    {
        _bus = new AmbientTransactionBus(bus);
    }

    public IAmbientTransactionBus Bus => _bus;

    public int PendingTransactionCount => _bus.PendingTransactionCount;

    public Task Enqueue(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default) =>
        _bus.Add(action, cancellationToken);
}


public sealed class AmbientTransactionNotificationTestDriver
{
    private readonly AmbientTransactionNotification _notification = new();

    public Task Enqueue(Func<CancellationToken, Task> action)
    {
        _notification.Add(action);
        return Task.CompletedTask;
    }

    public void CompleteInDoubtThroughARealEnlistment()
    {
        using var transaction = new CommittableTransaction();
        transaction.EnlistVolatile(new InDoubtForwarder(_notification), EnlistmentOptions.None);
        transaction.Rollback();
    }

    private sealed class InDoubtForwarder(AmbientTransactionNotification notification) : IEnlistmentNotification
    {
        public void Prepare(PreparingEnlistment preparingEnlistment) => preparingEnlistment.Prepared();

        public void Commit(Enlistment enlistment) => enlistment.Done();

        public void Rollback(Enlistment enlistment) => notification.InDoubt(enlistment);

        public void InDoubt(Enlistment enlistment) => notification.InDoubt(enlistment);
    }
}


internal static class UnusedBus
{
    public static IBus Create() => DispatchProxy.Create<IBus, UnexpectedBusProxy>();

    private class UnexpectedBusProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The test-only bus member '{targetMethod?.Name}' was not expected to be called.");
    }
}
