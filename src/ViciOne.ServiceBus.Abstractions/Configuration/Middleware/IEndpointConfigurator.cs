namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures endpoint.</summary>
public interface IEndpointConfigurator :
    IConsumePipeConfigurator,
    ISendPipelineConfigurator,
    IPublishPipelineConfigurator,
    IReceivePipelineConfigurator
{
}
