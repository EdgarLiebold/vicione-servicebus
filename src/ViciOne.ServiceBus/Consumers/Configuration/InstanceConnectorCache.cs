using System;
using System.Collections.Concurrent;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an instance connector cache implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class InstanceConnectorCache<T> :
    IInstanceConnectorCache<T>
    where T : class
{
    readonly Lazy<InstanceConnector<T>> _connector;

    InstanceConnectorCache()
    {
        _connector = new Lazy<InstanceConnector<T>>(() => new InstanceConnector<T>());
    }

    /// <summary>
    /// Gets the connector value.
    /// </summary>
    public static IInstanceConnector Connector => InstanceCache.Cached.Value.Connector;

    IInstanceConnector IInstanceConnectorCache<T>.Connector => _connector.Value;


    static class InstanceCache
    {
        internal static readonly Lazy<IInstanceConnectorCache<T>> Cached = new Lazy<IInstanceConnectorCache<T>>(() => new InstanceConnectorCache<T>());
    }
}


/// <summary>
/// Provides an instance connector cache implementation.
/// </summary>
public static class InstanceConnectorCache
{
    /// <summary>
    /// Gets instance connector.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public static IInstanceConnector GetInstanceConnector<T>()
        where T : class
    {
        return InstanceCache.Cached.Value.GetOrAdd(typeof(T),
            _ => new Lazy<IInstanceConnector>(() => InstanceConnectorCache<T>.Connector)).Value;
    }

    /// <summary>
    /// Gets instance connector.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the operation.</returns>
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
