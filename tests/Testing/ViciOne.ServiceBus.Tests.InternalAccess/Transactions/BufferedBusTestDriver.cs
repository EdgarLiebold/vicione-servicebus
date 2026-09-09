using ViciOne.ServiceBus.Transactions;
using ViciOne.ServiceBus.Transports;

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

    public ITransportSendEndpoint CreateSendEndpoint() =>
        new DeferredBusSendEndpoint(_bus, UnusedBus.CreateTransportSendEndpoint());

    public ISendEndpoint WrapSendEndpoint(ISendEndpoint endpoint) => new DeferredBusSendEndpoint(_bus, endpoint);

    public static ISendEndpoint CreateNonTransportSendEndpoint() => UnusedBus.CreateSendEndpoint();
}
