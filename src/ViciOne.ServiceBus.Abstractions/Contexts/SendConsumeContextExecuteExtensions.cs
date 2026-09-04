using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public static class SendConsumeContextExecuteExtensions
{
    /// <summary>
    /// Send a message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="context"></param>
    /// <param name="destinationAddress"></param>
    /// <param name="message">The message</param>
    /// <param name="callback">The callback for the send context</param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task SendAsync<T>(this ConsumeContext context, Uri destinationAddress, T message, Action<SendContext<T>> callback, CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, callback.ToPipe(), context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="context"></param>
    /// <param name="destinationAddress"></param>
    /// <param name="message">The message</param>
    /// <param name="callback">The callback for the send context</param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task SendAsync<T>(this ConsumeContext context, Uri destinationAddress, T message, Func<SendContext<T>, Task> callback, CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, callback.ToPipe(), context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <param name="context"></param>
    /// <param name="destinationAddress"></param>
    /// <param name="message">The message</param>
    /// <param name="callback">The callback for the send context</param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message, Action<SendContext> callback, CancellationToken cancellationToken = default)
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, callback.ToPipe(), context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <param name="context"></param>
    /// <param name="destinationAddress"></param>
    /// <param name="message">The message</param>
    /// <param name="callback">The callback for the send context</param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message, Func<SendContext, Task> callback, CancellationToken cancellationToken = default)
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, callback.ToPipe(), context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <param name="context"></param>
    /// <param name="destinationAddress"></param>
    /// <param name="message">The message</param>
    /// <param name="messageType"></param>
    /// <param name="callback">The callback for the send context</param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message, Type messageType, Action<SendContext> callback, CancellationToken cancellationToken = default)
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, messageType, callback.ToPipe(), context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <param name="context"></param>
    /// <param name="destinationAddress"></param>
    /// <param name="message">The message</param>
    /// <param name="messageType"></param>
    /// <param name="callback">The callback for the send context</param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message, Type messageType, Func<SendContext, Task> callback, CancellationToken cancellationToken = default)
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, messageType, callback.ToPipe(), context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="context"></param>
    /// <param name="destinationAddress"></param>
    /// <param name="values"></param>
    /// <param name="callback">The callback for the send context</param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task SendAsync<T>(this ConsumeContext context, Uri destinationAddress, object values, Action<SendContext<T>> callback, CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(values, callback.ToPipe(), context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="context"></param>
    /// <param name="destinationAddress"></param>
    /// <param name="values"></param>
    /// <param name="callback">The callback for the send context</param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task SendAsync<T>(this ConsumeContext context, Uri destinationAddress, object values, Func<SendContext<T>, Task> callback, CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(values, callback.ToPipe(), context.CancellationToken).ConfigureAwait(false);
    }
}
