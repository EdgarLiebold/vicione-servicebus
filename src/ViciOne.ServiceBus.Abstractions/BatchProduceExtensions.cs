using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides extension methods for batch produce.</summary>
public static class BatchProduceExtensions
{
    /// <summary>Send a message batch.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync<T>(this ISendEndpoint endpoint, IEnumerable<T> messages, CancellationToken cancellationToken = default)
        where T : class
    {
        return Task.WhenAll(messages.Select(x => endpoint.SendAsync(x, cancellationToken)));
    }

    /// <summary>Send a message.</summary>
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
        return Task.WhenAll(messages.Select(x => endpoint.SendAsync(x, pipe, cancellationToken)));
    }

    /// <summary>Send a message batch.</summary>
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
        IPipe<SendContext<T>> pipe = callback.ToPipe();

        return Task.WhenAll(messages.Select(x => endpoint.SendAsync(x, pipe, cancellationToken)));
    }

    /// <summary>Send a message batch.</summary>
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
        IPipe<SendContext<T>> pipe = callback.ToPipe();

        return Task.WhenAll(messages.Select(x => endpoint.SendAsync(x, pipe, cancellationToken)));
    }

    /// <summary>Send a message batch.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync(this ISendEndpoint endpoint, IEnumerable<object> messages, CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(messages.Select(x => endpoint.SendAsync(x, cancellationToken)));
    }

    /// <summary>Send a message batch.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync(this ISendEndpoint endpoint, IEnumerable<object> messages, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(messages.Select(x => endpoint.SendAsync(x, pipe, cancellationToken)));
    }

    /// <summary>Send a message batch.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="callback">The callback for the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync(this ISendEndpoint endpoint, IEnumerable<object> messages, Action<SendContext> callback,
        CancellationToken cancellationToken = default)
    {
        IPipe<SendContext> pipe = callback.ToPipe();

        return Task.WhenAll(messages.Select(x => endpoint.SendAsync(x, pipe, cancellationToken)));
    }

    /// <summary>Send a message batch.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="callback">The callback for the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync(this ISendEndpoint endpoint, IEnumerable<object> messages, Func<SendContext, Task> callback,
        CancellationToken cancellationToken = default)
    {
        IPipe<SendContext> pipe = callback.ToPipe();

        return Task.WhenAll(messages.Select(x => endpoint.SendAsync(x, pipe, cancellationToken)));
    }

    /// <summary>Send a message.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync(this ISendEndpoint endpoint, IEnumerable<object> messages, Type messageType, CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(messages.Select(x => endpoint.SendAsync(x, messageType, cancellationToken)));
    }

    /// <summary>Send a message.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync(this ISendEndpoint endpoint, IEnumerable<object> messages, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(messages.Select(x => endpoint.SendAsync(x, messageType, pipe, cancellationToken)));
    }

    /// <summary>Send a message.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="callback">The callback for the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync(this ISendEndpoint endpoint, IEnumerable<object> messages, Type messageType, Action<SendContext> callback,
        CancellationToken cancellationToken = default)
    {
        IPipe<SendContext> pipe = callback.ToPipe();

        return Task.WhenAll(messages.Select(x => endpoint.SendAsync(x, messageType, pipe, cancellationToken)));
    }

    /// <summary>Send a message.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="callback">The callback for the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendBatchAsync(this ISendEndpoint endpoint, IEnumerable<object> messages, Type messageType, Func<SendContext, Task> callback,
        CancellationToken cancellationToken = default)
    {
        IPipe<SendContext> pipe = callback.ToPipe();

        return Task.WhenAll(messages.Select(x => endpoint.SendAsync(x, messageType, pipe, cancellationToken)));
    }

    /// <summary>Send a message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync<T>(this IPublishEndpoint endpoint, IEnumerable<T> messages, CancellationToken cancellationToken = default)
        where T : class
    {
        return Task.WhenAll(messages.Select(x => endpoint.PublishAsync(x, cancellationToken)));
    }

    /// <summary>Publish a message batch.</summary>
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
        return Task.WhenAll(messages.Select(x => endpoint.PublishAsync(x, pipe, cancellationToken)));
    }

    /// <summary>Publish a message batch.</summary>
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
        IPipe<PublishContext<T>> pipe = callback.ToPipe();

        return Task.WhenAll(messages.Select(x => endpoint.PublishAsync(x, pipe, cancellationToken)));
    }

    /// <summary>Publish a message batch.</summary>
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
        IPipe<PublishContext<T>> pipe = callback.ToPipe();

        return Task.WhenAll(messages.Select(x => endpoint.PublishAsync(x, pipe, cancellationToken)));
    }

    /// <summary>Publish a message batch.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync(this IPublishEndpoint endpoint, IEnumerable<object> messages, CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(messages.Select(x => endpoint.PublishAsync(x, cancellationToken)));
    }

    /// <summary>Publish a message batch.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync(this IPublishEndpoint endpoint, IEnumerable<object> messages, IPipe<PublishContext> pipe,
        CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(messages.Select(x => endpoint.PublishAsync(x, pipe, cancellationToken)));
    }

    /// <summary>Publish a message batch.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="callback">The callback for the publish context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync(this IPublishEndpoint endpoint, IEnumerable<object> messages, Action<PublishContext> callback,
        CancellationToken cancellationToken = default)
    {
        IPipe<PublishContext> pipe = callback.ToPipe();

        return Task.WhenAll(messages.Select(x => endpoint.PublishAsync(x, pipe, cancellationToken)));
    }

    /// <summary>Publish a message batch.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="callback">The callback for the publish context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync(this IPublishEndpoint endpoint, IEnumerable<object> messages, Func<PublishContext, Task> callback,
        CancellationToken cancellationToken = default)
    {
        IPipe<PublishContext> pipe = callback.ToPipe();

        return Task.WhenAll(messages.Select(x => endpoint.PublishAsync(x, pipe, cancellationToken)));
    }

    /// <summary>Publish a message batch.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync(this IPublishEndpoint endpoint, IEnumerable<object> messages, Type messageType,
        CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(messages.Select(x => endpoint.PublishAsync(x, messageType, cancellationToken)));
    }

    /// <summary>Publish a message batch.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync(this IPublishEndpoint endpoint, IEnumerable<object> messages, Type messageType, IPipe<PublishContext> pipe,
        CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(messages.Select(x => endpoint.PublishAsync(x, messageType, pipe, cancellationToken)));
    }

    /// <summary>Publish a message batch.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="callback">The callback for the publish context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync(this IPublishEndpoint endpoint, IEnumerable<object> messages, Type messageType, Action<PublishContext> callback,
        CancellationToken cancellationToken = default)
    {
        IPipe<PublishContext> pipe = callback.ToPipe();

        return Task.WhenAll(messages.Select(x => endpoint.PublishAsync(x, messageType, pipe, cancellationToken)));
    }

    /// <summary>Publish a message batch.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="messages">The messages.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="callback">The callback for the publish context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task PublishBatchAsync(this IPublishEndpoint endpoint, IEnumerable<object> messages, Type messageType, Func<PublishContext, Task> callback,
        CancellationToken cancellationToken = default)
    {
        IPipe<PublishContext> pipe = callback.ToPipe();

        return Task.WhenAll(messages.Select(x => endpoint.PublishAsync(x, messageType, pipe, cancellationToken)));
    }
}
