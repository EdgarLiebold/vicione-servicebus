using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Provides cached access to send endpoint data.</summary>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
public interface ISendEndpointCache<TKey> :
    IAsyncDisposable
{
    /// <summary>Return a SendEndpoint from the cache, using the factory to create it if it doesn't exist in the cache.</summary>
    /// <param name="key">The key for the endpoint.</param>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<ISendEndpoint> GetSendEndpointAsync(TKey key, SendEndpointFactory<TKey> factory, CancellationToken cancellationToken = default);
}
