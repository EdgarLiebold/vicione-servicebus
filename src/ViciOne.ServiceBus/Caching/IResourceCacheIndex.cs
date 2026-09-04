using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ViciOne.ServiceBus.Caching;

/// <summary>
/// Defines the contract for resource cache index.
/// </summary>
/// <typeparam name="TKey">The t key type.</typeparam>
/// <typeparam name="TValue">The t value type.</typeparam>
public interface IResourceCacheIndex<TKey, TValue>
    where TKey : notnull
    where TValue : class
{
    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or add.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="factory">The factory value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask<TValue> GetOrAddAsync(TKey key, ResourceFactory<TKey, TValue>? factory = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the remove operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask<bool> RemoveAsync(TKey key, CancellationToken cancellationToken = default);
}
