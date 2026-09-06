using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides extension methods for send consume context.</summary>
public static class SendConsumeContextExtensions
{
    /// <summary>Send a message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static async Task SendAsync<T>(this ConsumeContext context, Uri destinationAddress, T message, CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Send a message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static async Task SendAsync<T>(this ConsumeContext context, Uri destinationAddress, T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, pipe, context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Send a message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static async Task SendAsync<T>(this ConsumeContext context, Uri destinationAddress, T message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, pipe, context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Send a message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static async Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message, CancellationToken cancellationToken = default)
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Send a message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static async Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message, Type messageType, CancellationToken cancellationToken = default)
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, messageType, context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Send a message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static async Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, messageType, pipe, context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Send a message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static async Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, pipe, context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Send a message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="values">The values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static async Task SendAsync<T>(this ConsumeContext context, Uri destinationAddress, object values, CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync<T>(values, context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Send a message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static async Task SendAsync<T>(this ConsumeContext context, Uri destinationAddress, object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(values, pipe, context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Send a message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static async Task SendAsync<T>(this ConsumeContext context, Uri destinationAddress, object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync<T>(values, pipe, context.CancellationToken).ConfigureAwait(false);
    }
}
