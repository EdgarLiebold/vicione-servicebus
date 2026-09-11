using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Internals.Dispatching;

/// <summary>Maps runtime send contracts to the endpoint's generic operations.</summary>
internal static class SendEndpointDispatcher
{
    static readonly ConcurrentDictionary<Type, Lazy<ISendEndpointConverter>> Converters = new();

    /// <summary>Sends a message using an explicit runtime contract type.</summary>
    /// <param name="endpoint">The send endpoint.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="messageType">The runtime message contract type.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the endpoint accepts the message.</returns>
    public static Task SendAsync(ISendEndpoint endpoint, object message, Type messageType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);

        return GetConverter(messageType).SendAsync(endpoint, message, cancellationToken);
    }

    /// <summary>Sends a message using an explicit runtime contract type and send-context pipe.</summary>
    /// <param name="endpoint">The send endpoint.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="messageType">The runtime message contract type.</param>
    /// <param name="pipe">The pipe that customizes the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the endpoint accepts the message.</returns>
    public static Task SendAsync(ISendEndpoint endpoint, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        return GetConverter(messageType).SendAsync(endpoint, message, pipe, cancellationToken);
    }

    /// <summary>Initializes and sends a message using an explicit runtime contract type.</summary>
    /// <param name="endpoint">The send endpoint.</param>
    /// <param name="messageType">The runtime message contract type.</param>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the endpoint accepts the initialized message.</returns>
    public static Task SendInitializerAsync(ISendEndpoint endpoint, Type messageType, object values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(values);

        return GetConverter(messageType).SendInitializerAsync(endpoint, values, cancellationToken);
    }

    /// <summary>Initializes and sends a message using an explicit runtime contract type and send-context pipe.</summary>
    /// <param name="endpoint">The send endpoint.</param>
    /// <param name="messageType">The runtime message contract type.</param>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="pipe">The pipe that customizes the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the endpoint accepts the initialized message.</returns>
    public static Task SendInitializerAsync(ISendEndpoint endpoint, Type messageType, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);

        return GetConverter(messageType).SendInitializerAsync(endpoint, values, pipe, cancellationToken);
    }

    static ISendEndpointConverter GetConverter(Type messageType)
    {
        ValidateMessageType(messageType);

        return Converters.GetOrAdd(messageType, CreateTypeConverter).Value;
    }

    static Lazy<ISendEndpointConverter> CreateTypeConverter(Type type)
    {
        return new Lazy<ISendEndpointConverter>(() => CreateConverter(type), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    static ISendEndpointConverter CreateConverter(Type type)
    {
        return Activator.CreateInstance(typeof(SendEndpointConverter<>).MakeGenericType(type)) as ISendEndpointConverter
            ?? throw new InvalidOperationException($"Unable to create a send converter for {TypeCache.GetShortName(type)}.");
    }

    static void ValidateMessageType(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        if (messageType.IsValueType || messageType.IsByRef || messageType.IsPointer || messageType.ContainsGenericParameters)
            throw new ArgumentException("The message contract must be a closed reference type.", nameof(messageType));
    }

    interface ISendEndpointConverter
    {
        Task SendAsync(ISendEndpoint endpoint, object message, CancellationToken cancellationToken);

        Task SendAsync(ISendEndpoint endpoint, object message, IPipe<SendContext> pipe, CancellationToken cancellationToken);

        Task SendInitializerAsync(ISendEndpoint endpoint, object values, CancellationToken cancellationToken);

        Task SendInitializerAsync(ISendEndpoint endpoint, object values, IPipe<SendContext> pipe, CancellationToken cancellationToken);
    }

    sealed class SendEndpointConverter<TMessage> :
        ISendEndpointConverter
        where TMessage : class
    {
        public Task SendAsync(ISendEndpoint endpoint, object message, CancellationToken cancellationToken)
        {
            if (message is TMessage typedMessage)
                return endpoint.SendAsync(typedMessage, cancellationToken);

            throw new ArgumentException($"The message is not assignable to {TypeCache.GetShortName(typeof(TMessage))}.", nameof(message));
        }

        public Task SendAsync(ISendEndpoint endpoint, object message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        {
            if (message is TMessage typedMessage)
                return endpoint.SendAsync(typedMessage, pipe, cancellationToken);

            throw new ArgumentException($"The message is not assignable to {TypeCache.GetShortName(typeof(TMessage))}.", nameof(message));
        }

        public Task SendInitializerAsync(ISendEndpoint endpoint, object values, CancellationToken cancellationToken)
        {
            return endpoint.SendAsync<TMessage>(values, cancellationToken);
        }

        public Task SendInitializerAsync(ISendEndpoint endpoint, object values, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        {
            return endpoint.SendAsync<TMessage>(values, pipe, cancellationToken);
        }
    }
}
