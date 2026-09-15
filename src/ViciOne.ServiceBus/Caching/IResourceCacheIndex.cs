using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Caching;

/// <summary>Provides strongly typed access to one unique index of a resource cache.</summary>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
/// <typeparam name="TValue">The indexed cache-owned resource type.</typeparam>
public interface IResourceCacheIndex<TKey, TValue>
    where TKey : notnull
    where TValue : class
{
    /// <summary>Retrieves a committed resource or waits for its pending creation.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="cancellationToken">The token checked before lookup and used to cancel this caller's pending-resource wait.</param>
    /// <returns>A value task that yields the resource, or fails if the key is absent.</returns>
    ValueTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default);

    /// <summary>Gets a resource or shares one cache-owned creation for the requested key.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="factory">The creation factory, or null to use the factory registered with this index.</param>
    /// <param name="cancellationToken">The token that cancels this caller's capacity or resource wait without canceling shared creation.</param>
    /// <returns>A value task that yields the resource, or fails if the key is absent and no factory is available.</returns>
    ValueTask<TValue> GetOrAddAsync(TKey key, ResourceFactory<TKey, TValue>? factory = null,
        CancellationToken cancellationToken = default);

    /// <summary>Removes and releases the committed resource for the key; a pending creation is left unchanged.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="cancellationToken">The token checked before removal starts; it does not cancel committed resource release.</param>
    /// <returns>A value task that yields true only when a committed resource was removed.</returns>
    ValueTask<bool> RemoveAsync(TKey key, CancellationToken cancellationToken = default);
}
