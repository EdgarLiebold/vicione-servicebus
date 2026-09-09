namespace ViciOne.ServiceBus.Advanced;

/// <summary>Sends messages from a consume scope to an explicit destination.</summary>
public static class SendConsumeContextExtensions
{
    /// <summary>Sends a typed message.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync<TMessage>(this ConsumeContext context, Uri destinationAddress, TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        return SendCoreAsync(context, destinationAddress, (endpoint, token) => endpoint.SendAsync(message, token), cancellationToken);
    }

    /// <summary>Sends a typed message through a typed send-context pipe.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="pipe">The pipe that configures the typed send context.</param>
    /// <param name="cancellationToken">The cancellation token, combined with the consume-context token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync<TMessage>(this ConsumeContext context, Uri destinationAddress, TMessage message,
        IPipe<SendContext<TMessage>> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        return SendCoreAsync(context, destinationAddress, (endpoint, token) => endpoint.SendAsync(message, pipe, token), cancellationToken);
    }

    /// <summary>Sends a typed message through an untyped send-context pipe.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="pipe">The pipe that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token, combined with the consume-context token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync<TMessage>(this ConsumeContext context, Uri destinationAddress, TMessage message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        return SendCoreAsync(context, destinationAddress, (endpoint, token) => endpoint.SendAsync(message, pipe, token), cancellationToken);
    }

    /// <summary>Sends a message using its runtime type.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">The cancellation token, combined with the consume-context token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return SendCoreAsync(context, destinationAddress, (endpoint, token) => endpoint.SendAsync(message, token), cancellationToken);
    }

    /// <summary>Sends a message as an explicit runtime contract.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The cancellation token, combined with the consume-context token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message, Type messageType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        return SendCoreAsync(
            context,
            destinationAddress,
            (endpoint, token) => endpoint.SendAsync(message, messageType, token),
            cancellationToken);
    }

    /// <summary>Sends a message as an explicit runtime contract through a send-context pipe.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="pipe">The pipe that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token, combined with the consume-context token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message, Type messageType,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(pipe);
        return SendCoreAsync(
            context,
            destinationAddress,
            (endpoint, token) => endpoint.SendAsync(message, messageType, pipe, token),
            cancellationToken);
    }

    /// <summary>Sends a runtime-typed message through a send-context pipe.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="pipe">The pipe that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token, combined with the consume-context token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync(this ConsumeContext context, Uri destinationAddress, object message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        return SendCoreAsync(
            context,
            destinationAddress,
            (endpoint, token) => endpoint.SendAsync(message, pipe, token),
            cancellationToken);
    }

    /// <summary>Initializes and sends a typed message.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="values">The source values used to initialize the message.</param>
    /// <param name="cancellationToken">The cancellation token, combined with the consume-context token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync<TMessage>(this ConsumeContext context, Uri destinationAddress, object values,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(values);
        return SendCoreAsync(
            context,
            destinationAddress,
            (endpoint, token) => endpoint.SendAsync<TMessage>(values, token),
            cancellationToken);
    }

    /// <summary>Initializes and sends a typed message through a typed send-context pipe.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="values">The source values used to initialize the message.</param>
    /// <param name="pipe">The pipe that configures the typed send context.</param>
    /// <param name="cancellationToken">The cancellation token, combined with the consume-context token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync<TMessage>(this ConsumeContext context, Uri destinationAddress, object values,
        IPipe<SendContext<TMessage>> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);
        return SendCoreAsync(
            context,
            destinationAddress,
            (endpoint, token) => endpoint.SendAsync(values, pipe, token),
            cancellationToken);
    }

    /// <summary>Initializes and sends a typed message through an untyped send-context pipe.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="values">The source values used to initialize the message.</param>
    /// <param name="pipe">The pipe that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token, combined with the consume-context token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync<TMessage>(this ConsumeContext context, Uri destinationAddress, object values,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);
        return SendCoreAsync(
            context,
            destinationAddress,
            (endpoint, token) => endpoint.SendAsync<TMessage>(values, pipe, token),
            cancellationToken);
    }

    internal static Task SendCoreAsync(ConsumeContext context, Uri destinationAddress,
        Func<ISendEndpoint, CancellationToken, Task> send, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(send);

        return SendCoreAsyncCore(context, destinationAddress, send, cancellationToken);
    }

    private static async Task SendCoreAsyncCore(ConsumeContext context, Uri destinationAddress,
        Func<ISendEndpoint, CancellationToken, Task> send, CancellationToken cancellationToken)
    {
        using CancellationTokenSource? linkedSource = CreateLinkedSource(context.CancellationToken, cancellationToken);
        CancellationToken effectiveToken = linkedSource?.Token
            ?? (cancellationToken.CanBeCanceled ? cancellationToken : context.CancellationToken);
        ISendEndpoint endpoint = await context.GetSendEndpointAsync(destinationAddress, effectiveToken).ConfigureAwait(false);
        await send(endpoint, effectiveToken).ConfigureAwait(false);
    }

    private static CancellationTokenSource? CreateLinkedSource(CancellationToken contextToken,
        CancellationToken cancellationToken)
    {
        return contextToken.CanBeCanceled && cancellationToken.CanBeCanceled && contextToken != cancellationToken
            ? CancellationTokenSource.CreateLinkedTokenSource(contextToken, cancellationToken)
            : null;
    }
}
