namespace ViciOne.ServiceBus.EventHubIntegration
{
    using Transports;


    public interface IProducerContextSupervisor :
        ITransportSupervisor<ProducerContext>
    {
    }
}
