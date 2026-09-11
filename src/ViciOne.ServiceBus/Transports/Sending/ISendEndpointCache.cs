using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Provides bounded, owned access to send endpoints by normalized key.</summary>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
internal interface ISendEndpointCache<TKey> :
    IAsyncDisposable
    where TKey : notnull
{
    /// <summary>Gets an endpoint, creating and owning it when the key is not cached.</summary>
    /// <param name="key">The normalized endpoint key.</param>
    /// <param name="factory">The endpoint factory used on a cache miss.</param>
    /// <param name="cancellationToken">The token that cancels this caller's wait without canceling shared creation.</param>
    /// <returns>A task that produces the cached endpoint.</returns>
    Task<ISendEndpoint> GetSendEndpointAsync(TKey key, SendEndpointFactory<TKey> factory, CancellationToken cancellationToken = default);
}
