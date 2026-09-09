namespace ViciOne.ServiceBus.Configuration;

internal sealed class ReliableSchedulerSelection<TBus>
    where TBus : class, IBus
{
    public ReliableSchedulerAdapterKind Kind { get; private set; } = ReliableSchedulerAdapterKind.Store;

    public Uri? EndpointAddress { get; private set; }

    public void SelectTransport()
    {
        Kind = ReliableSchedulerAdapterKind.Transport;
        EndpointAddress = null;
    }

    public void SelectEndpoint(Uri endpointAddress)
    {
        EndpointAddress = endpointAddress ?? throw new ArgumentNullException(nameof(endpointAddress));
        Kind = ReliableSchedulerAdapterKind.Endpoint;
    }
}
