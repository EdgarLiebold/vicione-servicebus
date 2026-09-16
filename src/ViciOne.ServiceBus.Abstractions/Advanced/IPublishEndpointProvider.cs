using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Resolves the transport-specific send endpoint used to publish a message contract.</summary>
public interface IPublishEndpointProvider :
    IPublishObserverConnector
{
    /// <summary>Gets the send endpoint used to publish a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="cancellationToken">The token that cancels endpoint resolution.</param>
    /// <returns>A non-null task containing the non-null resolved publish send endpoint.</returns>
    Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class;
}
