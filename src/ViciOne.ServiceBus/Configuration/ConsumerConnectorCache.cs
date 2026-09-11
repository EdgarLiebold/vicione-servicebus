using System;
using System.Collections.Concurrent;
using ViciOne.ServiceBus.Consumers;
using ViciOne.ServiceBus.Consumers.Metadata;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides the shared connector for a consumer type.</summary>
/// <typeparam name="TConsumer">The consumer implementation whose contracts are connected.</typeparam>
public static class ConsumerConnectorCache<TConsumer>
    where TConsumer : class
{
    static readonly object CacheLock = new();
    static IConsumerConnector? _connector;
    static long _version = -1;

    /// <summary>Gets the connector for the current consumer-convention version.</summary>
    public static IConsumerConnector Connector
    {
        get
        {
            long version = ConsumerMetadataCache<TConsumer>.Version;
            lock (CacheLock)
            {
                if (_connector != null && _version == version)
                    return _connector;

                IConsumerConnector connector = new ConsumerConnector<TConsumer>();
                _connector = connector;
                _version = version;
                return connector;
            }
        }
    }
}


/// <summary>Connects consumer types known only at runtime through shared typed connector instances.</summary>
public static class ConsumerConnectorCache
{
    static CachedConnector GetOrAdd(Type type)
    {
        return Cached.Instance.GetOrAdd(type, _ =>
            (CachedConnector)(Activator.CreateInstance(typeof(CachedConnector<>).MakeGenericType(type))
                ?? throw new InvalidOperationException($"Could not create a consumer connector for '{type}'.")));
    }

    /// <summary>Connects every convention-discovered contract of a runtime consumer type.</summary>
    /// <param name="consumePipe">The consume pipe that will dispatch messages to the consumer.</param>
    /// <param name="consumerType">The closed reference type whose message contracts will be connected.</param>
    /// <param name="objectFactory">The delegate that creates the requested runtime consumer type.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public static ConnectHandle Connect(IConsumePipeConnector consumePipe, Type consumerType, Func<Type, object> objectFactory)
    {
        ArgumentNullException.ThrowIfNull(consumePipe);
        ArgumentNullException.ThrowIfNull(consumerType);
        ArgumentNullException.ThrowIfNull(objectFactory);
        if (consumerType.IsValueType || consumerType.IsByRef || consumerType.IsPointer || consumerType.ContainsGenericParameters)
            throw new ArgumentException("The consumer type must be a closed reference type.", nameof(consumerType));

        return GetOrAdd(consumerType).Connect(consumePipe, objectFactory);
    }


    static class Cached
    {
        internal static readonly ConcurrentDictionary<Type, CachedConnector> Instance =
            new ConcurrentDictionary<Type, CachedConnector>();
    }


    interface CachedConnector
    {
        ConnectHandle Connect(IConsumePipeConnector consumePipe, Func<Type, object> objectFactory);
    }


    sealed class CachedConnector<TConsumer> :
        CachedConnector
        where TConsumer : class
    {
        public ConnectHandle Connect(IConsumePipeConnector consumePipe, Func<Type, object> objectFactory)
        {
            var consumerFactory = new ObjectConsumerFactory<TConsumer>(objectFactory);
            IConsumerConnector connector = ConsumerConnectorCache<TConsumer>.Connector;

            IConsumerSpecification<TConsumer> specification = connector.CreateConsumerSpecification<TConsumer>();

            return connector.ConnectConsumer(consumePipe, consumerFactory, specification);
        }
    }
}
