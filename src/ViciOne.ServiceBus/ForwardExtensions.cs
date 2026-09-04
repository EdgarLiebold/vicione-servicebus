using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus;

public static class ForwardExtensions
{
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
