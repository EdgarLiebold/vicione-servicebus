namespace ViciOne.ServiceBus
{
    using EventHubIntegration;
    using Transports;


    public interface IEventHubReceiveEndpointContext :
        ReceiveEndpointContext
    {
        IProcessorContextSupervisor ContextSupervisor { get; }
    }
}
