using System;
using System.Collections.Concurrent;
using ViciOne.ServiceBus.Consumers.Metadata;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides the shared connector for an existing consumer type.</summary>
/// <typeparam name="TConsumer">The consumer type.</typeparam>
public static class InstanceConnectorCache<TConsumer>
    where TConsumer : class
{
    static readonly object CacheLock = new();
    static IInstanceConnector? _connector;
    static long _version = -1;

    /// <summary>Gets the connector for the current consumer-convention version.</summary>
    public static IInstanceConnector Connector
    {
        get
        {
            long version = ConsumerMetadataCache<TConsumer>.Version;
            lock (CacheLock)
            {
                if (_connector != null && _version == version)
                    return _connector;

                IInstanceConnector connector = new InstanceConnector<TConsumer>();
                _connector = connector;
                _version = version;
                return connector;
            }
        }
    }
}


/// <summary>Provides shared instance connectors for consumer types known only at runtime.</summary>
public static class InstanceConnectorCache
{
    /// <summary>Gets the instance connector for the current consumer-convention version.</summary>
    /// <typeparam name="TConsumer">The consumer implementation whose contracts are connected.</typeparam>
    /// <returns>The current connector for the consumer type.</returns>
    public static IInstanceConnector GetInstanceConnector<TConsumer>()
        where TConsumer : class
    {
        return InstanceConnectorCache<TConsumer>.Connector;
    }

    /// <summary>Gets the version-aware instance connector for a runtime consumer type.</summary>
    /// <param name="type">The closed reference type whose message contracts will be connected.</param>
    /// <returns>The current connector for the runtime consumer type.</returns>
    public static IInstanceConnector GetInstanceConnector(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (type.IsValueType || type.IsByRef || type.IsPointer || type.ContainsGenericParameters)
            throw new ArgumentException("The consumer type must be a closed reference type.", nameof(type));

        return InstanceCache.Accessors.GetOrAdd(type, static consumerType =>
            (ConnectorAccessor)(Activator.CreateInstance(typeof(ConnectorAccessor<>).MakeGenericType(consumerType))
                ?? throw new InvalidOperationException($"Could not create an instance-connector accessor for '{consumerType}'."))).Connector;
    }


    static class InstanceCache
    {
        internal static readonly ConcurrentDictionary<Type, ConnectorAccessor> Accessors = new();
    }


    interface ConnectorAccessor
    {
        IInstanceConnector Connector { get; }
    }


    sealed class ConnectorAccessor<TConsumer> : ConnectorAccessor
        where TConsumer : class
    {
        public IInstanceConnector Connector => InstanceConnectorCache<TConsumer>.Connector;
    }
}
