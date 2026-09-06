using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Forwards consumed messages while preserving their receive-context metadata.</summary>
public static class ForwardExtensions
{
    /// <summary>Forwards the current message.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <param name="context">The message context to forward.</param>
    /// <param name="address">The destination address.</param>
    /// <returns>A task that completes when the destination transport accepts the forwarded message.</returns>
    public static async Task ForwardAsync<T>(this ConsumeContext<T> context, Uri address)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(address);

        var endpoint = await context.Advanced().GetSendEndpointAsync(address).ConfigureAwait(false);

        await ForwardAsync(context, endpoint).ConfigureAwait(false);
    }

    /// <summary>Forwards the current message.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <param name="context">The message context to forward.</param>
    /// <param name="address">The destination address.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A task that completes when the destination transport accepts the forwarded message.</returns>
    public static async Task ForwardAsync<T>(this ConsumeContext<T> context, Uri address, IPipe<SendContext<T>> pipe)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(pipe);

        var endpoint = await context.Advanced().GetSendEndpointAsync(address).ConfigureAwait(false);

        await ForwardAsync(context, endpoint, pipe).ConfigureAwait(false);
    }

    /// <summary>Forwards the current message.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <param name="context">The message context to forward.</param>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <returns>A task that completes when the destination transport accepts the forwarded message.</returns>
    public static Task ForwardAsync<T>(this ConsumeContext<T> context, ISendEndpoint endpoint)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(endpoint);

        var messagePipe = new ForwardMessagePipe<T>(context);

        return endpoint.SendAsync(context.Message, messagePipe, context.CancellationToken);
    }

    /// <summary>Forwards the current message.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <param name="context">The message context to forward.</param>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A task that completes when the destination transport accepts the forwarded message.</returns>
    public static Task ForwardAsync<T>(this ConsumeContext<T> context, ISendEndpoint endpoint, IPipe<SendContext<T>> pipe)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(pipe);

        var messagePipe = new ForwardMessagePipe<T>(context, pipe);

        return endpoint.SendAsync(context.Message, messagePipe, context.CancellationToken);
    }

    /// <summary>Forwards the current message.</summary>
    /// <typeparam name="T">The forwarded message type.</typeparam>
    /// <param name="context">The source consume context.</param>
    /// <param name="address">The destination address.</param>
    /// <param name="message">The message to forward.</param>
    /// <returns>A task that completes when the destination transport accepts the forwarded message.</returns>
    public static async Task ForwardAsync<T>(this ConsumeContext context, Uri address, T message)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(message);

        var endpoint = await context.GetSendEndpointAsync(address).ConfigureAwait(false);

        await ForwardAsync(context, endpoint, message).ConfigureAwait(false);
    }

    /// <summary>Forwards the supplied message while preserving metadata from the source consume context.</summary>
    /// <typeparam name="T">The forwarded message type.</typeparam>
    /// <param name="context">The source consume context.</param>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="message">The message to forward.</param>
    /// <returns>A task that completes when the destination transport accepts the forwarded message.</returns>
    public static Task ForwardAsync<T>(this ConsumeContext context, ISendEndpoint endpoint, T message)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);

        void AddForwarderAddress(ConsumeContext consumeContext, SendContext sendContext)
        {
            TimeProvider timeProvider = consumeContext.GetTimeProvider();
            sendContext.SetTimeProvider(timeProvider);

            var forwarderAddress = consumeContext.ReceiveContext.InputAddress ?? consumeContext.DestinationAddress;
            if (forwarderAddress != null && forwarderAddress != context.DestinationAddress)
                sendContext.Headers.Set(MessageHeaders.ForwarderAddress, forwarderAddress.ToString());

            ForwardingExpiration.MarkIfExpired(sendContext, consumeContext.ExpirationTime, timeProvider);
        }

        return endpoint.SendAsync(message, new CopyContextPipe(context, AddForwarderAddress), context.CancellationToken);
    }
}
