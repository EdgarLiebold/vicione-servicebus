using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Defines the contract for event hub producer cache.
/// </summary>
/// <typeparam name="TKey">The t key type.</typeparam>
public interface IEventHubProducerCache<TKey> :
    IAsyncDisposable
    where TKey : notnull
{
    /// <summary>
    /// Gets producer.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="factory">The factory value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<IEventHubProducer> GetProducerAsync(TKey key, Func<TKey, Task<IEventHubProducer>> factory, CancellationToken cancellationToken = default);
}
