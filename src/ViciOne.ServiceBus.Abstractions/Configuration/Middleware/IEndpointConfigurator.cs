namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for endpoint configurator.
/// </summary>
public interface IEndpointConfigurator :
    IConsumePipeConfigurator,
    ISendPipelineConfigurator,
    IPublishPipelineConfigurator,
    IReceivePipelineConfigurator
{
}
