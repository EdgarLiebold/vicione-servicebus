using ViciOne.ServiceBus.Transactions;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Transactions;

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

    public Task EnqueueAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default) =>
        _bus.AddAsync(action, cancellationToken);
}
