using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Caching;

/// <summary>
/// Observes committed cache changes through serialized, awaited notifications.
/// Callback failures are logged and isolated without rolling back the committed change.
/// Callbacks must not start resource lookups or mutations, register indexes or observers,
/// or dispose the same cache. Statistics and committed-value snapshots remain available.
/// </summary>
/// <typeparam name="TValue">The observed cache-owned resource type.</typeparam>
public interface IResourceCacheObserver<in TValue>
    where TValue : class
{
    /// <summary>Reports a resource after it has been committed to the cache and its indexes.</summary>
    /// <param name="value">The committed cache-owned resource.</param>
    /// <param name="cancellationToken">The cache-lifetime token, not the caller's lookup or mutation token.</param>
    /// <returns>A value task awaited before the adding operation completes.</returns>
    ValueTask ResourceAddedAsync(TValue value, CancellationToken cancellationToken);

    /// <summary>Reports a removed resource before its disposal during explicit removal, expiration or capacity eviction.</summary>
    /// <param name="value">The resource removed from the cache and its indexes.</param>
    /// <param name="cancellationToken">The cache-lifetime token, or an uncancelable token for periodic cleanup.</param>
    /// <returns>A value task awaited before the removed resource is disposed.</returns>
    ValueTask ResourceRemovedAsync(TValue value, CancellationToken cancellationToken);

    /// <summary>Reports an explicit clear after removed resources and invalidated creations have released ownership.</summary>
    /// <param name="cancellationToken">The cache-lifetime token, not the caller's clear token.</param>
    /// <returns>A value task awaited before the clear operation completes.</returns>
    ValueTask CacheClearedAsync(CancellationToken cancellationToken);
}
