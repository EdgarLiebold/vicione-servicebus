namespace ViciOne.ServiceBus.Advanced;

/// <summary>Sends messages from a consume scope through callback-configured send contexts.</summary>
public static class SendConsumeContextExecuteExtensions
{
    /// <summary>Sends a typed message configured by a synchronous context callback.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="callback">The callback that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token, combined with the consume-context token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync<TMessage>(this ConsumeContext context, Uri destinationAddress, TMessage message,
        Action<SendContext<TMessage>> callback, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        SendConsumeContextExtensions.ValidateContextAndDestination(context, destinationAddress);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<SendContext<TMessage>> pipe = callback.ToPipe();
        return SendConsumeContextExtensions.SendCoreAsync(
            context,
            destinationAddress,
            (endpoint, token) => endpoint.SendAsync(message, pipe, token),
            cancellationToken);
    }

    /// <summary>Sends a typed message configured by an asynchronous context callback.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="callback">The callback that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token, combined with the consume-context token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync<TMessage>(this ConsumeContext context, Uri destinationAddress, TMessage message,
        Func<SendContext<TMessage>, Task> callback, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        SendConsumeContextExtensions.ValidateContextAndDestination(context, destinationAddress);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<SendContext<TMessage>> pipe = callback.ToPipe();
        return SendConsumeContextExtensions.SendCoreAsync(
            context,
            destinationAddress,
            (endpoint, token) => endpoint.SendAsync(message, pipe, token),
            cancellationToken);
    }

    /// <summary>Sends a runtime-typed message configured by a synchronous context callback.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="callback">The callback that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token, combined with the consume-context token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message,
        Action<SendContext> callback, CancellationToken cancellationToken = default)
    {
        SendConsumeContextExtensions.ValidateContextAndDestination(context, destinationAddress);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<SendContext> pipe = callback.ToPipe();
        return SendConsumeContextExtensions.SendCoreAsync(
            context,
            destinationAddress,
            (endpoint, token) => endpoint.SendAsync(message, pipe, token),
            cancellationToken);
    }

    /// <summary>Sends a runtime-typed message configured by an asynchronous context callback.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="callback">The callback that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token, combined with the consume-context token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message,
        Func<SendContext, Task> callback, CancellationToken cancellationToken = default)
    {
        SendConsumeContextExtensions.ValidateContextAndDestination(context, destinationAddress);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<SendContext> pipe = callback.ToPipe();
        return SendConsumeContextExtensions.SendCoreAsync(
            context,
            destinationAddress,
            (endpoint, token) => endpoint.SendAsync(message, pipe, token),
            cancellationToken);
    }

    /// <summary>Sends a message as an explicit runtime contract configured by a synchronous context callback.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="callback">The callback that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token, combined with the consume-context token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message, Type messageType,
        Action<SendContext> callback, CancellationToken cancellationToken = default)
    {
        SendConsumeContextExtensions.ValidateContextAndDestination(context, destinationAddress);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<SendContext> pipe = callback.ToPipe();
        return SendConsumeContextExtensions.SendCoreAsync(
            context,
            destinationAddress,
            (endpoint, token) => endpoint.SendAsync(message, messageType, pipe, token),
            cancellationToken);
    }

    /// <summary>Sends a message as an explicit runtime contract configured by an asynchronous context callback.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="callback">The callback that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token, combined with the consume-context token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message, Type messageType,
        Func<SendContext, Task> callback, CancellationToken cancellationToken = default)
    {
        SendConsumeContextExtensions.ValidateContextAndDestination(context, destinationAddress);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<SendContext> pipe = callback.ToPipe();
        return SendConsumeContextExtensions.SendCoreAsync(
            context,
            destinationAddress,
            (endpoint, token) => endpoint.SendAsync(message, messageType, pipe, token),
            cancellationToken);
    }

    /// <summary>Initializes and sends a typed message configured by a synchronous context callback.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="values">The source values used to initialize the message.</param>
    /// <param name="callback">The callback that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token, combined with the consume-context token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync<TMessage>(this ConsumeContext context, Uri destinationAddress, object values,
        Action<SendContext<TMessage>> callback, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        SendConsumeContextExtensions.ValidateContextAndDestination(context, destinationAddress);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<SendContext<TMessage>> pipe = callback.ToPipe();
        return SendConsumeContextExtensions.SendCoreAsync(
            context,
            destinationAddress,
            (endpoint, token) => endpoint.SendAsync(values, pipe, token),
            cancellationToken);
    }

    /// <summary>Initializes and sends a typed message configured by an asynchronous context callback.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="values">The source values used to initialize the message.</param>
    /// <param name="callback">The callback that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token, combined with the consume-context token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync<TMessage>(this ConsumeContext context, Uri destinationAddress, object values,
        Func<SendContext<TMessage>, Task> callback, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        SendConsumeContextExtensions.ValidateContextAndDestination(context, destinationAddress);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(callback);
        IPipe<SendContext<TMessage>> pipe = callback.ToPipe();
        return SendConsumeContextExtensions.SendCoreAsync(
            context,
            destinationAddress,
            (endpoint, token) => endpoint.SendAsync(values, pipe, token),
            cancellationToken);
    }
}
