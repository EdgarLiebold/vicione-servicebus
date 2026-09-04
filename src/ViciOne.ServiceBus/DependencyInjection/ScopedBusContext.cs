namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Defines the contract for scoped bus context.
/// </summary>
public interface ScopedBusContext
{
    /// <summary>
    /// Gets the send endpoint provider value.
    /// </summary>
    ISendEndpointProvider SendEndpointProvider { get; }
    /// <summary>
    /// Gets the publish endpoint value.
    /// </summary>
    IPublishEndpoint PublishEndpoint { get; }
    /// <summary>
    /// Gets the client factory value.
    /// </summary>
    IScopedClientFactory ClientFactory { get; }
}
