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
    /// <summary>Retrieves the requested value.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="cancellationToken">The token that cancels the lookup or pending-resource wait.</param>
    /// <returns>A task that produces the requested value.</returns>
    ValueTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default);

    /// <summary>Gets an existing resource or creates and caches one for the key.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="cancellationToken">The token that cancels lookup, admission or pending-resource waits before commit.</param>
    /// <returns>A task that produces the requested value.</returns>
    ValueTask<TValue> GetOrAddAsync(TKey key, ResourceFactory<TKey, TValue>? factory = null,
        CancellationToken cancellationToken = default);

    /// <summary>Removes the selected value.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="cancellationToken">The token that cancels removal before it is committed.</param>
    /// <returns>A task that produces the remove outcome.</returns>
    ValueTask<bool> RemoveAsync(TKey key, CancellationToken cancellationToken = default);
}
