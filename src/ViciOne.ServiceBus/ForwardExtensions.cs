using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides extension methods for forward.
/// </summary>
public static class ForwardExtensions
{
    /// <summary>
    /// Performs the forward operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task ForwardAsync<T>(this ConsumeContext<T> context, Uri address)
        where T : class
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (address == null)
            throw new ArgumentNullException(nameof(address));

        var endpoint = await context.Advanced().GetSendEndpointAsync(address).ConfigureAwait(false);

        await ForwardAsync(context, endpoint).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the forward operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="address">The address value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task ForwardAsync<T>(this ConsumeContext<T> context, Uri address, IPipe<SendContext<T>> pipe)
        where T : class
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (address == null)
            throw new ArgumentNullException(nameof(address));

        var endpoint = await context.Advanced().GetSendEndpointAsync(address).ConfigureAwait(false);

        await ForwardAsync(context, endpoint, pipe).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the forward operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="endpoint">The endpoint value.</param>
    /// <returns>The result of the operation.</returns>
    public static Task ForwardAsync<T>(this ConsumeContext<T> context, ISendEndpoint endpoint)
        where T : class
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (endpoint == null)
            throw new ArgumentNullException(nameof(endpoint));

        var messagePipe = new ForwardMessagePipe<T>(context);

        return endpoint.SendAsync(context.Message, messagePipe, context.CancellationToken);
    }

    /// <summary>
    /// Performs the forward operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="endpoint">The endpoint value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public static Task ForwardAsync<T>(this ConsumeContext<T> context, ISendEndpoint endpoint, IPipe<SendContext<T>> pipe)
        where T : class
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (endpoint == null)
            throw new ArgumentNullException(nameof(endpoint));

        var messagePipe = new ForwardMessagePipe<T>(context, pipe);

        return endpoint.SendAsync(context.Message, messagePipe, context.CancellationToken);
    }

    /// <summary>
    /// Performs the forward operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="address">The address value.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task ForwardAsync<T>(this ConsumeContext context, Uri address, T message)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(address);

        var endpoint = await context.GetSendEndpointAsync(address).ConfigureAwait(false);

        await ForwardAsync(context, endpoint, message).ConfigureAwait(false);
    }

    /// <summary>
    /// Forward the message to another consumer
    /// </summary>
    /// <param name="context"></param>
    /// <param name="endpoint">The destination endpoint</param>
    /// <param name="message"></param>
    public static Task ForwardAsync<T>(this ConsumeContext context, ISendEndpoint endpoint, T message)
        where T : class
    {
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
