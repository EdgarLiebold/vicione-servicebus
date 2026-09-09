namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures receive, consume, send, and publish middleware for an endpoint.</summary>
public interface IEndpointConfigurator :
    IConsumePipeConfigurator,
    ISendPipelineConfigurator,
    IPublishPipelineConfigurator,
    IReceivePipelineConfigurator
{
}
