using System;
using System.Collections.Concurrent;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Caches instance connector data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class InstanceConnectorCache<T> :
    IInstanceConnectorCache<T>
    where T : class
{
    readonly Lazy<InstanceConnector<T>> _connector;

    InstanceConnectorCache()
    {
        _connector = new Lazy<InstanceConnector<T>>(() => new InstanceConnector<T>());
    }

    /// <summary>Gets the connector.</summary>
    public static IInstanceConnector Connector => InstanceCache.Cached.Value.Connector;

    IInstanceConnector IInstanceConnectorCache<T>.Connector => _connector.Value;


    static class InstanceCache
    {
        internal static readonly Lazy<IInstanceConnectorCache<T>> Cached = new Lazy<IInstanceConnectorCache<T>>(() => new InstanceConnectorCache<T>());
    }
}


/// <summary>Caches instance connector data.</summary>
public static class InstanceConnectorCache
{
    /// <summary>Gets instance connector.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The instance connector.</returns>
    public static IInstanceConnector GetInstanceConnector<T>()
        where T : class
    {
        return InstanceCache.Cached.Value.GetOrAdd(typeof(T),
            _ => new Lazy<IInstanceConnector>(() => InstanceConnectorCache<T>.Connector)).Value;
    }

    /// <summary>Gets instance connector.</summary>
    /// <param name="type">The runtime type to inspect or use.</param>
    /// <returns>The instance connector.</returns>
    public static IInstanceConnector GetInstanceConnector(Type type)
    {
        return InstanceCache.Cached.Value.GetOrAdd(type, _ => new Lazy<IInstanceConnector>(() =>
            (IInstanceConnector)(Activator.CreateInstance(typeof(InstanceConnector<>).MakeGenericType(type))
                ?? throw new InvalidOperationException($"Could not create an instance connector for '{type}'.")))).Value;
    }


    static class InstanceCache
    {
        internal static readonly Lazy<ConcurrentDictionary<Type, Lazy<IInstanceConnector>>> Cached =
            new Lazy<ConcurrentDictionary<Type, Lazy<IInstanceConnector>>>(() => new ConcurrentDictionary<Type, Lazy<IInstanceConnector>>());
    }
}
