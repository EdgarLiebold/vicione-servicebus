namespace ViciOne.ServiceBus.Mediator;

/// <summary>Defines the operations required by mediator.</summary>
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
