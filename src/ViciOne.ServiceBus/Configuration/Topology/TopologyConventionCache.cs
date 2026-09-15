using System;
using System.Collections.Concurrent;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates at most one convention instance for each message contract.</summary>
/// <typeparam name="TValue">The common convention contract.</typeparam>
public sealed class TopologyConventionCache<TValue> :
    ITopologyConventionCache<TValue>
    where TValue : class
{
    readonly ConcurrentDictionary<Type, Lazy<TValue>> _dictionary;
    readonly IConventionTypeFactory<TValue> _typeFactory;

    /// <summary>Creates an empty cache backed by the specified convention factory.</summary>
    /// <param name="typeFactory">The factory that creates a convention for a message contract.</param>
    public TopologyConventionCache(IConventionTypeFactory<TValue> typeFactory)
    {
        _dictionary = new ConcurrentDictionary<Type, Lazy<TValue>>();
        _typeFactory = typeFactory ?? throw new ArgumentNullException(nameof(typeFactory));
    }

    TResult ITopologyConventionCache<TValue>.GetOrAdd<T, TResult>()
    {
        var result = _dictionary.GetOrAdd(
            typeof(T),
            _ => new Lazy<TValue>(() => _typeFactory.Create<T>()
                ?? throw new InvalidOperationException("The topology convention factory returned null."))).Value as TResult;
        if (result == null)
            throw new InvalidOperationException(
                $"The cached topology convention does not implement {TypeCache<TResult>.ShortName}.");

        return result;
    }
}
