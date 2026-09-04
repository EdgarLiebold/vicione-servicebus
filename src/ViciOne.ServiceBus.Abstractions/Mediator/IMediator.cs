namespace ViciOne.ServiceBus.Mediator;

/// <summary>
/// Defines the contract for mediator.
/// </summary>
public interface IMediator :
    ISendEndpoint,
    IPublishEndpoint,
    IPublishEndpointProvider,
    IClientFactory,
    IConsumePipeConnector,
    IRequestPipeConnector,
    IConsumeObserverConnector,
    IConsumeMessageObserverConnector
{
}
