// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DependencyInjection
{
    public interface ScopedBusContext
    {
        ISendEndpointProvider SendEndpointProvider { get; }
        IPublishEndpoint PublishEndpoint { get; }
        IScopedClientFactory ClientFactory { get; }
    }
}
