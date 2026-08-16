namespace ViciOne.ServiceBus
{
    public interface IEndpointConfigurator :
        IConsumePipeConfigurator,
        ISendPipelineConfigurator,
        IPublishPipelineConfigurator,
        IReceivePipelineConfigurator
    {
    }
}
