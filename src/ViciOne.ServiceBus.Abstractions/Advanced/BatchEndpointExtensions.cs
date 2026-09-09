using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides concurrent batch send and publish operations for messaging endpoints.</summary>
public static class BatchEndpointExtensions
{
    /// <summary>Sends a batch of messages concurrently to the endpoint.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync<T>(this ISendEndpoint endpoint, IEnumerable<T> messages, CancellationToken cancellationToken = default)
        where T : class
    {
        T[] batch = ValidateBatch(endpoint, messages);
        return Task.WhenAll(batch.Select(message => endpoint.SendAsync(message, cancellationToken)));
    }

    /// <summary>Sends a batch of messages concurrently through a typed send pipeline.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync<T>(this ISendEndpoint endpoint, IEnumerable<T> messages, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class
    {
        T[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(pipe);

        return Task.WhenAll(batch.Select(message => endpoint.SendAsync(message, pipe, cancellationToken)));
    }

    /// <summary>Sends a batch of messages concurrently through synchronous context configuration.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="callback">The callback for the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync<T>(this ISendEndpoint endpoint, IEnumerable<T> messages, Action<SendContext<T>> callback,
        CancellationToken cancellationToken = default)
        where T : class
    {
        T[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<SendContext<T>> pipe = callback.ToPipe();

        return Task.WhenAll(batch.Select(message => endpoint.SendAsync(message, pipe, cancellationToken)));
    }

    /// <summary>Sends a batch of messages concurrently through asynchronous context configuration.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="callback">The callback for the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync<T>(this ISendEndpoint endpoint, IEnumerable<T> messages, Func<SendContext<T>, Task> callback,
        CancellationToken cancellationToken = default)
        where T : class
    {
        T[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<SendContext<T>> pipe = callback.ToPipe();

        return Task.WhenAll(batch.Select(message => endpoint.SendAsync(message, pipe, cancellationToken)));
    }

    /// <summary>Sends a runtime-typed batch of messages concurrently to the endpoint.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync(this ISendEndpoint endpoint, IEnumerable<object> messages, CancellationToken cancellationToken = default)
    {
        object[] batch = ValidateBatch(endpoint, messages);
        return Task.WhenAll(batch.Select(message => endpoint.SendAsync(message, cancellationToken)));
    }

    /// <summary>Sends a runtime-typed batch of messages concurrently through a send pipeline.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync(this ISendEndpoint endpoint, IEnumerable<object> messages, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
    {
        object[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(pipe);

        return Task.WhenAll(batch.Select(message => endpoint.SendAsync(message, pipe, cancellationToken)));
    }

    /// <summary>Sends a runtime-typed batch concurrently through synchronous context configuration.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="callback">The callback for the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync(this ISendEndpoint endpoint, IEnumerable<object> messages, Action<SendContext> callback,
        CancellationToken cancellationToken = default)
    {
        object[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<SendContext> pipe = callback.ToPipe();

        return Task.WhenAll(batch.Select(message => endpoint.SendAsync(message, pipe, cancellationToken)));
    }

    /// <summary>Sends a runtime-typed batch concurrently through asynchronous context configuration.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="callback">The callback for the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync(this ISendEndpoint endpoint, IEnumerable<object> messages, Func<SendContext, Task> callback,
        CancellationToken cancellationToken = default)
    {
        object[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<SendContext> pipe = callback.ToPipe();

        return Task.WhenAll(batch.Select(message => endpoint.SendAsync(message, pipe, cancellationToken)));
    }

    /// <summary>Sends a batch concurrently as an explicitly selected message contract.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync(this ISendEndpoint endpoint, IEnumerable<object> messages, Type messageType, CancellationToken cancellationToken = default)
    {
        object[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(messageType);

        return Task.WhenAll(batch.Select(message => endpoint.SendAsync(message, messageType, cancellationToken)));
    }

    /// <summary>Sends a batch concurrently as an explicit contract through a send pipeline.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync(this ISendEndpoint endpoint, IEnumerable<object> messages, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
    {
        object[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(pipe);

        return Task.WhenAll(batch.Select(message => endpoint.SendAsync(message, messageType, pipe, cancellationToken)));
    }

    /// <summary>Sends a batch as an explicit contract through synchronous context configuration.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="callback">The callback for the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync(this ISendEndpoint endpoint, IEnumerable<object> messages, Type messageType, Action<SendContext> callback,
        CancellationToken cancellationToken = default)
    {
        object[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<SendContext> pipe = callback.ToPipe();

        return Task.WhenAll(batch.Select(message => endpoint.SendAsync(message, messageType, pipe, cancellationToken)));
    }

    /// <summary>Sends a batch as an explicit contract through asynchronous context configuration.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="callback">The callback for the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync(this ISendEndpoint endpoint, IEnumerable<object> messages, Type messageType, Func<SendContext, Task> callback,
        CancellationToken cancellationToken = default)
    {
        object[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<SendContext> pipe = callback.ToPipe();

        return Task.WhenAll(batch.Select(message => endpoint.SendAsync(message, messageType, pipe, cancellationToken)));
    }

    /// <summary>Publishes a batch of messages concurrently using their compile-time contract.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync<T>(this IPublishEndpoint endpoint, IEnumerable<T> messages, CancellationToken cancellationToken = default)
        where T : class
    {
        T[] batch = ValidateBatch(endpoint, messages);
        return Task.WhenAll(batch.Select(message => endpoint.PublishAsync(message, cancellationToken)));
    }

    /// <summary>Publishes a batch of messages concurrently through a typed publish pipeline.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync<T>(this IPublishEndpoint endpoint, IEnumerable<T> messages, IPipe<PublishContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class
    {
        T[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(pipe);

        return Task.WhenAll(batch.Select(message => endpoint.PublishAsync(message, pipe, cancellationToken)));
    }

    /// <summary>Publishes a batch of messages concurrently through synchronous context configuration.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="callback">The callback for the publish context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync<T>(this IPublishEndpoint endpoint, IEnumerable<T> messages, Action<PublishContext<T>> callback,
        CancellationToken cancellationToken = default)
        where T : class
    {
        T[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<PublishContext<T>> pipe = callback.ToPipe();

        return Task.WhenAll(batch.Select(message => endpoint.PublishAsync(message, pipe, cancellationToken)));
    }

    /// <summary>Publishes a batch of messages concurrently through asynchronous context configuration.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="callback">The callback for the publish context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync<T>(this IPublishEndpoint endpoint, IEnumerable<T> messages, Func<PublishContext<T>, Task> callback,
        CancellationToken cancellationToken = default)
        where T : class
    {
        T[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<PublishContext<T>> pipe = callback.ToPipe();

        return Task.WhenAll(batch.Select(message => endpoint.PublishAsync(message, pipe, cancellationToken)));
    }

    /// <summary>Publishes a runtime-typed batch of messages concurrently.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync(this IPublishEndpoint endpoint, IEnumerable<object> messages, CancellationToken cancellationToken = default)
    {
        object[] batch = ValidateBatch(endpoint, messages);
        return Task.WhenAll(batch.Select(message => endpoint.PublishAsync(message, cancellationToken)));
    }

    /// <summary>Publishes a runtime-typed batch concurrently through a publish pipeline.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync(this IPublishEndpoint endpoint, IEnumerable<object> messages, IPipe<PublishContext> pipe,
        CancellationToken cancellationToken = default)
    {
        object[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(pipe);

        return Task.WhenAll(batch.Select(message => endpoint.PublishAsync(message, pipe, cancellationToken)));
    }

    /// <summary>Publishes a runtime-typed batch concurrently through synchronous context configuration.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="callback">The callback for the publish context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync(this IPublishEndpoint endpoint, IEnumerable<object> messages, Action<PublishContext> callback,
        CancellationToken cancellationToken = default)
    {
        object[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<PublishContext> pipe = callback.ToPipe();

        return Task.WhenAll(batch.Select(message => endpoint.PublishAsync(message, pipe, cancellationToken)));
    }

    /// <summary>Publishes a runtime-typed batch concurrently through asynchronous context configuration.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="callback">The callback for the publish context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync(this IPublishEndpoint endpoint, IEnumerable<object> messages, Func<PublishContext, Task> callback,
        CancellationToken cancellationToken = default)
    {
        object[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<PublishContext> pipe = callback.ToPipe();

        return Task.WhenAll(batch.Select(message => endpoint.PublishAsync(message, pipe, cancellationToken)));
    }

    /// <summary>Publishes a batch concurrently as an explicitly selected message contract.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync(this IPublishEndpoint endpoint, IEnumerable<object> messages, Type messageType,
        CancellationToken cancellationToken = default)
    {
        object[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(messageType);

        return Task.WhenAll(batch.Select(message => endpoint.PublishAsync(message, messageType, cancellationToken)));
    }

    /// <summary>Publishes a batch as an explicit contract through a publish pipeline.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync(this IPublishEndpoint endpoint, IEnumerable<object> messages, Type messageType, IPipe<PublishContext> pipe,
        CancellationToken cancellationToken = default)
    {
        object[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(pipe);

        return Task.WhenAll(batch.Select(message => endpoint.PublishAsync(message, messageType, pipe, cancellationToken)));
    }

    /// <summary>Publishes a batch as an explicit contract through synchronous context configuration.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="callback">The callback for the publish context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync(this IPublishEndpoint endpoint, IEnumerable<object> messages, Type messageType, Action<PublishContext> callback,
        CancellationToken cancellationToken = default)
    {
        object[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<PublishContext> pipe = callback.ToPipe();

        return Task.WhenAll(batch.Select(message => endpoint.PublishAsync(message, messageType, pipe, cancellationToken)));
    }

    /// <summary>Publishes a batch as an explicit contract through asynchronous context configuration.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="callback">The callback for the publish context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync(this IPublishEndpoint endpoint, IEnumerable<object> messages, Type messageType, Func<PublishContext, Task> callback,
        CancellationToken cancellationToken = default)
    {
        object[] batch = ValidateBatch(endpoint, messages);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<PublishContext> pipe = callback.ToPipe();

        return Task.WhenAll(batch.Select(message => endpoint.PublishAsync(message, messageType, pipe, cancellationToken)));
    }

    static T[] ValidateBatch<T>(object endpoint, IEnumerable<T> messages)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(messages);

        T[] batch = messages.ToArray();
        if (Array.Exists(batch, static message => message is null))
            throw new ArgumentException("The batch must not contain null messages.", nameof(messages));

        return batch;
    }
}
