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
    ValueTask ResourceAddedAsync(TValue value, CancellationToken cancellationToken);
    ValueTask ResourceRemovedAsync(TValue value, CancellationToken cancellationToken);
    ValueTask CacheClearedAsync(CancellationToken cancellationToken);
}
