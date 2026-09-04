using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

public interface ISendEndpointCache<TKey> :
    IAsyncDisposable
{
    /// <summary>
    /// Return a SendEndpoint from the cache, using the factory to create it if it doesn't exist in the cache.
    /// </summary>
    /// <param name="key">The key for the endpoint</param>
    /// <param name="factory"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ISendEndpoint> GetSendEndpointAsync(TKey key, SendEndpointFactory<TKey> factory, CancellationToken cancellationToken = default);
}
