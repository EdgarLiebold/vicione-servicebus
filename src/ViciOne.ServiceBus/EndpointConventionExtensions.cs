using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides extension methods for endpoint convention.
/// </summary>
public static class EndpointConventionExtensions
{
    /// <summary>
    /// Send a message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="provider"></param>
    /// <param name="message">The message</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static async Task SendAsync<T>(this ISendEndpointProvider provider, T message, CancellationToken cancellationToken = default)
        where T : class
    {
        if (!EndpointConvention.TryGetDestinationAddress<T>(provider, out var destinationAddress))
            throw new ArgumentException($"A convention for the message type {TypeCache<T>.ShortName} was not found");

        var endpoint = await provider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="provider"></param>
    /// <param name="message">The message</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static async Task SendAsync<T>(this ISendEndpointProvider provider, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (!EndpointConvention.TryGetDestinationAddress<T>(provider, out var destinationAddress))
            throw new ArgumentException($"A convention for the message type {TypeCache<T>.ShortName} was not found");

        var endpoint = await provider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, pipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="provider"></param>
    /// <param name="message">The message</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task SendAsync<T>(this ISendEndpointProvider provider, T message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return SendAsync(provider, message, (IPipe<SendContext<T>>)pipe, cancellationToken);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="message">The message</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static async Task SendAsync(this ISendEndpointProvider provider, object message, CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        if (!EndpointConvention.TryGetDestinationAddress(provider, messageType, out var destinationAddress))
            throw new ArgumentException($"A convention for the message type {TypeCache.GetShortName(messageType)} was not found");

        var endpoint = await provider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, messageType, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="message">The message</param>
    /// <param name="messageType"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static async Task SendAsync(this ISendEndpointProvider provider, object message, Type messageType, CancellationToken cancellationToken = default)
    {
        if (!EndpointConvention.TryGetDestinationAddress(provider, messageType, out var destinationAddress))
            throw new ArgumentException($"A convention for the message type {TypeCache.GetShortName(messageType)} was not found");

        var endpoint = await provider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, messageType, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="message">The message</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static async Task SendAsync(this ISendEndpointProvider provider, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        if (!EndpointConvention.TryGetDestinationAddress(provider, messageType, out var destinationAddress))
            throw new ArgumentException($"A convention for the message type {TypeCache.GetShortName(messageType)} was not found");

        var endpoint = await provider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, pipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="message">The message</param>
    /// <param name="messageType"></param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static async Task SendAsync(this ISendEndpointProvider provider, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        if (!EndpointConvention.TryGetDestinationAddress(provider, messageType, out var destinationAddress))
            throw new ArgumentException($"A convention for the message type {TypeCache.GetShortName(messageType)} was not found");

        var endpoint = await provider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, messageType, pipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="provider"></param>
    /// <param name="values"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task SendAsync<T>(this ISendEndpointProvider provider, object values, CancellationToken cancellationToken = default)
        where T : class
    {
        return SendAsync(provider, values, Pipe.Empty<SendContext<T>>(), cancellationToken);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="provider"></param>
    /// <param name="values"></param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static async Task SendAsync<T>(this ISendEndpointProvider provider, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        if (!EndpointConvention.TryGetDestinationAddress<T>(provider, out var destinationAddress))
            throw new ArgumentException($"A convention for the message type {TypeCache<T>.ShortName} was not found");

        var endpoint = await provider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        (var message, IPipe<SendContext<T>> sendPipe) = provider is ConsumeContext consumeContext
            ? await MessageInitializerCache<T>.InitializeMessageAsync(consumeContext, values, pipe, cancellationToken: cancellationToken).ConfigureAwait(false)
            : await MessageInitializerCache<T>.InitializeMessageAsync(values, pipe, cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="provider"></param>
    /// <param name="values"></param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task SendAsync<T>(this ISendEndpointProvider provider, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return SendAsync(provider, values, (IPipe<SendContext<T>>)pipe, cancellationToken);
    }
}
