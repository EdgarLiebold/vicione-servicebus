namespace ViciOne.ServiceBus.Configuration;

/// <summary>Caches one convention instance per message contract and exposes it through a requested contract.</summary>
/// <typeparam name="TValue">The common convention contract.</typeparam>
public interface ITopologyConventionCache<in TValue>
    where TValue : class
{
    /// <summary>
    /// Gets the convention for a message contract, creating it when necessary.
    /// </summary>
    /// <typeparam name="TKey">The message contract type used as the cache key.</typeparam>
    /// <typeparam name="TResult">The convention contract returned to the caller.</typeparam>
    /// <returns>The stable convention instance for <typeparamref name="TKey" />.</returns>
    TResult GetOrAdd<TKey, TResult>()
        where TKey : class
        where TResult : class, TValue;
}
