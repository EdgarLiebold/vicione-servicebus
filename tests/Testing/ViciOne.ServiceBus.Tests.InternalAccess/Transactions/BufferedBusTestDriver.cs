using ViciOne.ServiceBus.Transactions;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Transactions;

public sealed class BufferedBusTestDriver
{
    private readonly BufferedBus _bus;

    public BufferedBusTestDriver(int capacity = 1024)
        : this(UnusedBus.Create(), capacity)
    {
    }

    public BufferedBusTestDriver(IBus bus, int capacity = 1024)
    {
        _bus = new BufferedBus(bus, capacity);
    }

    public IBufferedBus Bus => _bus;

    public Task EnqueueAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default) =>
        _bus.AddAsync(action, cancellationToken);
}
