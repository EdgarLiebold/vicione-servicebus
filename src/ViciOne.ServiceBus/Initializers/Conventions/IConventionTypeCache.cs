namespace ViciOne.ServiceBus.Initializers.Conventions;

/// <summary>Resolves a cached convention adapter by its closed generic type key.</summary>
internal interface IConventionTypeCache
{
    /// <summary>Returns the adapter for <typeparamref name="TKey" />, creating it once when first requested.</summary>
    /// <typeparam name="TKey">The closed type used as the cache key.</typeparam>
    /// <typeparam name="TResult">The convention contract expected by the caller.</typeparam>
    /// <returns>The cached convention adapter.</returns>
    TResult GetOrAdd<TKey, TResult>()
        where TKey : class
        where TResult : class;
}
