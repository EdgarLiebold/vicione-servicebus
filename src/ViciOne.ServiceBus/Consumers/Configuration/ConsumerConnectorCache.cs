using System;
using System.Collections.Concurrent;
using ViciOne.ServiceBus.Consumer;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a consumer connector cache implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public class ConsumerConnectorCache<TConsumer> :
    IConsumerConnectorCache
    where TConsumer : class
{
    readonly Lazy<ConsumerConnector<TConsumer>> _connector;

    ConsumerConnectorCache()
    {
        _connector = new Lazy<ConsumerConnector<TConsumer>>(() => new ConsumerConnector<TConsumer>());
    }

    /// <summary>
    /// Gets the connector value.
    /// </summary>
    public static IConsumerConnector Connector => Cached.Instance.Value.Connector;

    IConsumerConnector IConsumerConnectorCache.Connector => _connector.Value;


    static class Cached
    {
        internal static readonly Lazy<IConsumerConnectorCache> Instance = new Lazy<IConsumerConnectorCache>(() => new ConsumerConnectorCache<TConsumer>());
    }
}


/// <summary>
/// Provides a consumer connector cache implementation.
/// </summary>
public static class ConsumerConnectorCache
{
    static CachedConnector GetOrAdd(Type type)
    {
        return Cached.Instance.GetOrAdd(type, _ =>
            (CachedConnector)(Activator.CreateInstance(typeof(CachedConnector<>).MakeGenericType(type))
                ?? throw new InvalidOperationException($"Could not create a consumer connector for '{type}'.")));
    }

    /// <summary>
    /// Performs the connect operation.
    /// </summary>
    /// <param name="consumePipe">The consume pipe value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="objectFactory">The object factory value.</param>
    /// <returns>The result of the operation.</returns>
    public static ConnectHandle Connect(IConsumePipeConnector consumePipe, Type consumerType, Func<Type, object> objectFactory)
    {
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


    class CachedConnector<T> :
        CachedConnector
        where T : class
    {
        readonly Lazy<IConsumerConnector> _connector;

        public CachedConnector()
        {
            _connector = new Lazy<IConsumerConnector>(() => ConsumerConnectorCache<T>.Connector);
        }

        public ConnectHandle Connect(IConsumePipeConnector consumePipe, Func<Type, object> objectFactory)
        {
            var consumerFactory = new ObjectConsumerFactory<T>(objectFactory);

            IConsumerSpecification<T> specification = _connector.Value.CreateConsumerSpecification<T>();

            return _connector.Value.ConnectConsumer(consumePipe, consumerFactory, specification);
        }
    }
}
