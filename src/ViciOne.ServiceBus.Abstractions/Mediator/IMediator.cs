// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Mediator
{
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
}
