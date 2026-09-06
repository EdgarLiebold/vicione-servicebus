using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Caching;

/// <summary>Defines the operations required by resource cache index.</summary>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
/// <typeparam name="TValue">The value stored by the member.</typeparam>
public interface IResourceCacheIndex<TKey, TValue>
    where TKey : notnull
    where TValue : class
{
    /// <summary>Retrieves the requested value.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    ValueTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default);

    /// <summary>Gets or add.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    ValueTask<TValue> GetOrAddAsync(TKey key, ResourceFactory<TKey, TValue>? factory = null,
        CancellationToken cancellationToken = default);

    /// <summary>Removes the selected value.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the remove outcome.</returns>
    ValueTask<bool> RemoveAsync(TKey key, CancellationToken cancellationToken = default);
}
