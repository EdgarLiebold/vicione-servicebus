using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ViciOne.ServiceBus.Caching;

/// <summary>
/// Creates a cache-owned resource. The cancellation token belongs to the cache lifetime, not to an individual waiter.
/// </summary>
public delegate ValueTask<TValue> ResourceFactory<in TKey, TValue>(TKey key, CancellationToken cancellationToken)
    where TValue : class;
