using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Caching;

/// <summary>Creates a cache-owned resource. The cancellation token belongs to the cache lifetime, not to an individual waiter.</summary>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
/// <typeparam name="TValue">The cache-owned resource type.</typeparam>
/// <param name="key">The key used to identify the requested entry.</param>
/// <param name="cancellationToken">The token that signals the end of cache ownership.</param>
/// <returns>The value produced by the operation.</returns>
public delegate ValueTask<TValue> ResourceFactory<in TKey, TValue>(TKey key, CancellationToken cancellationToken)
    where TValue : class;
