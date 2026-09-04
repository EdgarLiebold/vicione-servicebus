using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ViciOne.ServiceBus.Caching;

public interface IResourceCacheIndex<TKey, TValue>
    where TKey : notnull
    where TValue : class
{
    ValueTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default);

    ValueTask<TValue> GetOrAddAsync(TKey key, ResourceFactory<TKey, TValue>? factory = null,
        CancellationToken cancellationToken = default);

    ValueTask<bool> RemoveAsync(TKey key, CancellationToken cancellationToken = default);
}
