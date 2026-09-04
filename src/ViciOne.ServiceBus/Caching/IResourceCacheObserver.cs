using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ViciOne.ServiceBus.Caching;

/// <summary>
/// Observes committed cache state. Observer failures never roll back or corrupt cache state.
/// Notifications are awaited directly; no unbounded background observer queue is used.
/// </summary>
public interface IResourceCacheObserver<in TValue>
    where TValue : class
{
    /// <summary>
    /// Performs the resource added operation.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask ResourceAddedAsync(TValue value, CancellationToken cancellationToken);
    /// <summary>
    /// Performs the resource removed operation.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask ResourceRemovedAsync(TValue value, CancellationToken cancellationToken);
    /// <summary>
    /// Performs the cache cleared operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask CacheClearedAsync(CancellationToken cancellationToken);
}
