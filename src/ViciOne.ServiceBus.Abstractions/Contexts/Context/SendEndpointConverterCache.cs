using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>
/// Caches the converters that allow a raw object to be published using the object's type through
/// the generic Send method.
/// </summary>
public class SendEndpointConverterCache
{
    readonly ConcurrentDictionary<Type, Lazy<ISendEndpointConverter>> _types = new ConcurrentDictionary<Type, Lazy<ISendEndpointConverter>>();

    ISendEndpointConverter this[Type type] => _types.GetOrAdd(type, CreateTypeConverter).Value;

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="endpoint">The endpoint value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task SendAsync(ISendEndpoint endpoint, object message, Type messageType, CancellationToken cancellationToken = default)
    {
        return Cached.Converters.Value[messageType].SendAsync(endpoint, message, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="endpoint">The endpoint value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task SendAsync(ISendEndpoint endpoint, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
    {
        return Cached.Converters.Value[messageType].SendAsync(endpoint, message, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends initializer.
    /// </summary>
    /// <param name="endpoint">The endpoint value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task SendInitializerAsync(ISendEndpoint endpoint, Type messageType, object values, CancellationToken cancellationToken = default)
    {
        return Cached.Converters.Value[messageType].SendInitializerAsync(endpoint, values, cancellationToken);
    }

    /// <summary>
    /// Sends initializer.
    /// </summary>
    /// <param name="endpoint">The endpoint value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task SendInitializerAsync(ISendEndpoint endpoint, Type messageType, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
    {
        return Cached.Converters.Value[messageType].SendInitializerAsync(endpoint, values, pipe, cancellationToken);
    }

    static Lazy<ISendEndpointConverter> CreateTypeConverter(Type type)
    {
        return new Lazy<ISendEndpointConverter>(() => CreateConverter(type));
    }

    static ISendEndpointConverter CreateConverter(Type type)
    {
        return Activator.CreateInstance(typeof(SendEndpointConverter<>).MakeGenericType(type)) as ISendEndpointConverter
            ?? throw new InvalidOperationException("Failed to create SendEndpointConverter");
    }


    /// <summary>
    /// Calls the generic version of the ISendEndpoint.Send method with the object's type
    /// </summary>
    interface ISendEndpointConverter
    {
        Task SendAsync(ISendEndpoint endpoint, object message, CancellationToken cancellationToken = default);

        Task SendAsync(ISendEndpoint endpoint, object message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default);

        Task SendInitializerAsync(ISendEndpoint endpoint, object values, CancellationToken cancellationToken = default);

        Task SendInitializerAsync(ISendEndpoint endpoint, object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = default);
    }


    /// <summary>
    /// Converts the object type message to the appropriate generic type and invokes the send method with that
    /// generic overload.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class SendEndpointConverter<T> :
        ISendEndpointConverter
        where T : class
    {
        public Task SendAsync(ISendEndpoint endpoint, object message, CancellationToken cancellationToken)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message is T msg)
                return endpoint.SendAsync(msg, cancellationToken);

            throw new ArgumentException("Unexpected message type: " + TypeCache.GetShortName(message.GetType()));
        }

        public Task SendAsync(ISendEndpoint endpoint, object message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (pipe == null)
                throw new ArgumentNullException(nameof(pipe));

            if (message is T msg)
                return endpoint.SendAsync(msg, pipe, cancellationToken);

            throw new ArgumentException("Unexpected message type: " + TypeCache.GetShortName(message.GetType()));
        }

        public Task SendInitializerAsync(ISendEndpoint endpoint, object values, CancellationToken cancellationToken)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            return endpoint.SendAsync<T>(values, cancellationToken);
        }

        public Task SendInitializerAsync(ISendEndpoint endpoint, object values, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (pipe == null)
                throw new ArgumentNullException(nameof(pipe));

            return endpoint.SendAsync<T>(values, pipe, cancellationToken);
        }
    }


    static class Cached
    {
        internal static readonly Lazy<SendEndpointConverterCache> Converters = new Lazy<SendEndpointConverterCache>(() => new SendEndpointConverterCache());
    }
}
