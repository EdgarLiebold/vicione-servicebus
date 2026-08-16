namespace ViciOne.ServiceBus.EventHubIntegration
{
    using Transports;


    public interface IProcessorContextSupervisor :
        ITransportSupervisor<ProcessorContext>
    {
    }
}
