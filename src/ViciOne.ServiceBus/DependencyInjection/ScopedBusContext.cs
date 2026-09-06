namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Exposes state for scoped bus operations.</summary>
public interface ScopedBusContext
{
    /// <summary>Gets the send endpoint provider.</summary>
    ISendEndpointProvider SendEndpointProvider { get; }
    /// <summary>Gets the publish endpoint.</summary>
    IPublishEndpoint PublishEndpoint { get; }
    /// <summary>Gets the client factory.</summary>
    IScopedClientFactory ClientFactory { get; }
}
