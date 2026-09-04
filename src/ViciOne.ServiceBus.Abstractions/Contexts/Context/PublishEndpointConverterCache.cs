using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>
/// Caches the converters that allow a raw object to be published using the object's type through
/// the generic Send method.
/// </summary>
public class PublishEndpointConverterCache
{
    readonly ConcurrentDictionary<Type, Lazy<IPublishEndpointConverter>> _types = new ConcurrentDictionary<Type, Lazy<IPublishEndpointConverter>>();

    IPublishEndpointConverter this[Type type] => _types.GetOrAdd(type, CreateTypeConverter).Value;

    public static Task PublishAsync(IPublishEndpoint endpoint, object message, Type messageType, CancellationToken cancellationToken = default)
    {
        return Cached.Converters.Value[messageType].PublishAsync(endpoint, message, cancellationToken);
    }

    public static Task PublishAsync(IPublishEndpoint endpoint, object message, Type messageType, IPipe<PublishContext> pipe,
        CancellationToken cancellationToken = default)
    {
        return Cached.Converters.Value[messageType].PublishAsync(endpoint, message, pipe, cancellationToken);
    }

    public static Task PublishInitializerAsync(IPublishEndpoint endpoint, Type messageType, object values, CancellationToken cancellationToken = default)
    {
        return Cached.Converters.Value[messageType].PublishInitializerAsync(endpoint, values, cancellationToken);
    }

    public static Task PublishInitializerAsync(IPublishEndpoint endpoint, Type messageType, object values, IPipe<PublishContext> pipe,
        CancellationToken cancellationToken = default)
    {
        return Cached.Converters.Value[messageType].PublishInitializerAsync(endpoint, values, pipe, cancellationToken);
    }

    static Lazy<IPublishEndpointConverter> CreateTypeConverter(Type type)
    {
        return new Lazy<IPublishEndpointConverter>(() => CreateConverter(type));
    }

    static IPublishEndpointConverter CreateConverter(Type type)
    {
        return Activator.CreateInstance(typeof(PublishEndpointConverter<>).MakeGenericType(type)) as IPublishEndpointConverter
            ?? throw new InvalidOperationException("Failed to create PublishEndpointConverter");
    }


    /// <summary>
    /// Calls the generic version of the IPublishEndpoint.Send method with the object's type
    /// </summary>
    public interface IPublishEndpointConverter
    {
        Task PublishAsync(IPublishEndpoint endpoint, object message, CancellationToken cancellationToken = default);

        Task PublishAsync(IPublishEndpoint endpoint, object message, IPipe<PublishContext> pipe, CancellationToken cancellationToken = default);

        Task PublishInitializerAsync(IPublishEndpoint endpoint, object values, CancellationToken cancellationToken = default);

        Task PublishInitializerAsync(IPublishEndpoint endpoint, object values, IPipe<PublishContext> pipe, CancellationToken cancellationToken = default);
    }


    /// <summary>
    /// Converts the object message type to the generic type T and publishes it on the endpoint specified.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class PublishEndpointConverter<T> :
        IPublishEndpointConverter
        where T : class
    {
        public Task PublishAsync(IPublishEndpoint endpoint, object message, CancellationToken cancellationToken)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message is T msg)
                return endpoint.PublishAsync(msg, cancellationToken);

            throw new ArgumentException("Unexpected message type: " + TypeCache.GetShortName(message.GetType()));
        }

        public Task PublishAsync(IPublishEndpoint endpoint, object message, IPipe<PublishContext> pipe, CancellationToken cancellationToken)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (pipe == null)
                throw new ArgumentNullException(nameof(pipe));

            if (message is T msg)
                return endpoint.PublishAsync(msg, pipe, cancellationToken);

            throw new ArgumentException("Unexpected message type: " + TypeCache.GetShortName(message.GetType()));
        }

        public Task PublishInitializerAsync(IPublishEndpoint endpoint, object values, CancellationToken cancellationToken)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            return endpoint.PublishAsync<T>(values, cancellationToken);
        }

        public Task PublishInitializerAsync(IPublishEndpoint endpoint, object values, IPipe<PublishContext> pipe, CancellationToken cancellationToken)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (pipe == null)
                throw new ArgumentNullException(nameof(pipe));

            return endpoint.PublishAsync<T>(values, pipe, cancellationToken);
        }
    }


    static class Cached
    {
        internal static readonly Lazy<PublishEndpointConverterCache> Converters =
            new Lazy<PublishEndpointConverterCache>(() => new PublishEndpointConverterCache());
    }
}
