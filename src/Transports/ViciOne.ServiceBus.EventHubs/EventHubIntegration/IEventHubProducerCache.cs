using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Caches Event Hubs producers by a caller-defined key.</summary>
/// <typeparam name="TKey">The producer cache-key type.</typeparam>
public interface IEventHubProducerCache<TKey> :
    IAsyncDisposable
    where TKey : notnull
{
    /// <summary>Gets a cached producer or creates and caches one for the key.</summary>
    /// <param name="key">The key that identifies the producer.</param>
    /// <param name="factory">Creates a producer when the key is not cached.</param>
    /// <param name="cancellationToken">Cancels cache lookup or producer creation.</param>
    /// <returns>A task whose result is the cached or newly created producer.</returns>
    Task<IEventHubProducer> GetProducerAsync(TKey key, Func<TKey, Task<IEventHubProducer>> factory, CancellationToken cancellationToken = default);
}
