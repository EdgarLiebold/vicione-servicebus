using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Caching;

/// <summary>
/// Observes committed cache state. Observer failures never roll back or corrupt cache state.
/// Notifications are awaited directly; no unbounded background observer queue is used.
/// </summary>
/// <typeparam name="TValue">The observed cache-owned resource type.</typeparam>
public interface IResourceCacheObserver<in TValue>
    where TValue : class
{
    /// <summary>Reports that a resource has been added to the cache.</summary>
    /// <param name="value">The value to process.</param>
    /// <param name="cancellationToken">The token that signals the end of cache ownership.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask ResourceAddedAsync(TValue value, CancellationToken cancellationToken);

    /// <summary>Reports that a resource has been removed from the cache.</summary>
    /// <param name="value">The value to process.</param>
    /// <param name="cancellationToken">The token that signals the end of cache ownership.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask ResourceRemovedAsync(TValue value, CancellationToken cancellationToken);

    /// <summary>Reports that the cache has been cleared.</summary>
    /// <param name="cancellationToken">The token that signals the end of cache ownership.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask CacheClearedAsync(CancellationToken cancellationToken);
}
