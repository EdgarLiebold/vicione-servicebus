using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides extension methods for send consume context execute.</summary>
public static class SendConsumeContextExecuteExtensions
{
    /// <summary>Send a message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message.</param>
    /// <param name="callback">The callback for the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static async Task SendAsync<T>(this ConsumeContext context, Uri destinationAddress, T message, Action<SendContext<T>> callback, CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, callback.ToPipe(), context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Send a message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message.</param>
    /// <param name="callback">The callback for the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static async Task SendAsync<T>(this ConsumeContext context, Uri destinationAddress, T message, Func<SendContext<T>, Task> callback, CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, callback.ToPipe(), context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Send a message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message.</param>
    /// <param name="callback">The callback for the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static async Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message, Action<SendContext> callback, CancellationToken cancellationToken = default)
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, callback.ToPipe(), context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Send a message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message.</param>
    /// <param name="callback">The callback for the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static async Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message, Func<SendContext, Task> callback, CancellationToken cancellationToken = default)
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, callback.ToPipe(), context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Send a message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="callback">The callback for the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static async Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message, Type messageType, Action<SendContext> callback, CancellationToken cancellationToken = default)
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, messageType, callback.ToPipe(), context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Send a message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="callback">The callback for the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static async Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message, Type messageType, Func<SendContext, Task> callback, CancellationToken cancellationToken = default)
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, messageType, callback.ToPipe(), context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Send a message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="values">The values.</param>
    /// <param name="callback">The callback for the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static async Task SendAsync<T>(this ConsumeContext context, Uri destinationAddress, object values, Action<SendContext<T>> callback, CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(values, callback.ToPipe(), context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Send a message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="values">The values.</param>
    /// <param name="callback">The callback for the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static async Task SendAsync<T>(this ConsumeContext context, Uri destinationAddress, object values, Func<SendContext<T>, Task> callback, CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(values, callback.ToPipe(), context.CancellationToken).ConfigureAwait(false);
    }
}
