using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Internals.Dispatching;

/// <summary>Maps runtime publish contracts to the endpoint's generic operations.</summary>
internal static class PublishEndpointDispatcher
{
    static readonly ConcurrentDictionary<Type, Lazy<IPublishEndpointConverter>> Converters = new();

    /// <summary>Publishes a message using an explicit runtime contract type.</summary>
    /// <param name="endpoint">The publish endpoint.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="messageType">The runtime message contract type.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the endpoint accepts the message.</returns>
    public static Task PublishAsync(IPublishEndpoint endpoint, object message, Type messageType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);

        return GetConverter(messageType).PublishAsync(endpoint, message, cancellationToken);
    }

    /// <summary>Publishes a message using an explicit runtime contract type and publish-context pipe.</summary>
    /// <param name="endpoint">The publish endpoint.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="messageType">The runtime message contract type.</param>
    /// <param name="pipe">The pipe that customizes the publish context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the endpoint accepts the message.</returns>
    public static Task PublishAsync(IPublishEndpoint endpoint, object message, Type messageType, IPipe<PublishContext> pipe,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        return GetConverter(messageType).PublishAsync(endpoint, message, pipe, cancellationToken);
    }

    /// <summary>Initializes and publishes a message using an explicit runtime contract type.</summary>
    /// <param name="endpoint">The publish endpoint.</param>
    /// <param name="messageType">The runtime message contract type.</param>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the endpoint accepts the initialized message.</returns>
    public static Task PublishInitializerAsync(IPublishEndpoint endpoint, Type messageType, object values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(values);

        return GetConverter(messageType).PublishInitializerAsync(endpoint, values, cancellationToken);
    }

    /// <summary>Initializes and publishes a message using an explicit runtime contract type and publish-context pipe.</summary>
    /// <param name="endpoint">The publish endpoint.</param>
    /// <param name="messageType">The runtime message contract type.</param>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="pipe">The pipe that customizes the publish context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the endpoint accepts the initialized message.</returns>
    public static Task PublishInitializerAsync(IPublishEndpoint endpoint, Type messageType, object values, IPipe<PublishContext> pipe,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);

        return GetConverter(messageType).PublishInitializerAsync(endpoint, values, pipe, cancellationToken);
    }

    static IPublishEndpointConverter GetConverter(Type messageType)
    {
        ValidateMessageType(messageType);

        return Converters.GetOrAdd(messageType, CreateTypeConverter).Value;
    }

    static Lazy<IPublishEndpointConverter> CreateTypeConverter(Type type)
    {
        return new Lazy<IPublishEndpointConverter>(() => CreateConverter(type), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    static IPublishEndpointConverter CreateConverter(Type type)
    {
        return Activator.CreateInstance(typeof(PublishEndpointConverter<>).MakeGenericType(type)) as IPublishEndpointConverter
            ?? throw new InvalidOperationException($"Unable to create a publish converter for {TypeCache.GetShortName(type)}.");
    }

    static void ValidateMessageType(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        if (messageType.IsValueType || messageType.IsByRef || messageType.IsPointer || messageType.ContainsGenericParameters)
            throw new ArgumentException("The message contract must be a closed reference type.", nameof(messageType));
    }

    interface IPublishEndpointConverter
    {
        Task PublishAsync(IPublishEndpoint endpoint, object message, CancellationToken cancellationToken);

        Task PublishAsync(IPublishEndpoint endpoint, object message, IPipe<PublishContext> pipe, CancellationToken cancellationToken);

        Task PublishInitializerAsync(IPublishEndpoint endpoint, object values, CancellationToken cancellationToken);

        Task PublishInitializerAsync(IPublishEndpoint endpoint, object values, IPipe<PublishContext> pipe, CancellationToken cancellationToken);
    }

    sealed class PublishEndpointConverter<TMessage> :
        IPublishEndpointConverter
        where TMessage : class
    {
        public Task PublishAsync(IPublishEndpoint endpoint, object message, CancellationToken cancellationToken)
        {
            if (message is TMessage typedMessage)
                return endpoint.PublishAsync(typedMessage, cancellationToken);

            throw new ArgumentException($"The message is not assignable to {TypeCache.GetShortName(typeof(TMessage))}.", nameof(message));
        }

        public Task PublishAsync(IPublishEndpoint endpoint, object message, IPipe<PublishContext> pipe, CancellationToken cancellationToken)
        {
            if (message is TMessage typedMessage)
                return endpoint.PublishAsync(typedMessage, pipe, cancellationToken);

            throw new ArgumentException($"The message is not assignable to {TypeCache.GetShortName(typeof(TMessage))}.", nameof(message));
        }

        public Task PublishInitializerAsync(IPublishEndpoint endpoint, object values, CancellationToken cancellationToken)
        {
            return endpoint.PublishAsync<TMessage>(values, cancellationToken);
        }

        public Task PublishInitializerAsync(IPublishEndpoint endpoint, object values, IPipe<PublishContext> pipe, CancellationToken cancellationToken)
        {
            return endpoint.PublishAsync<TMessage>(values, pipe, cancellationToken);
        }
    }
}
