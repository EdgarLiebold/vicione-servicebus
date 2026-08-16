namespace ViciOne.ServiceBus.EventHubIntegration
{
    using Transports;


    public interface IConnectionContextSupervisor :
        ITransportSupervisor<ConnectionContext>
    {
    }
}
