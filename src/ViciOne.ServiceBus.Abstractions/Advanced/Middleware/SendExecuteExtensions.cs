namespace ViciOne.ServiceBus.Advanced;

/// <summary>Sends messages through callback-configured send contexts.</summary>
public static class SendExecuteExtensions
{
    /// <summary>Sends a typed message configured by a synchronous context callback.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="endpoint">The send endpoint.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="callback">The callback that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync<TMessage>(this ISendEndpoint endpoint, TMessage message,
        Action<SendContext<TMessage>> callback, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(callback);
        return endpoint.SendAsync(message, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Sends a typed message configured by an asynchronous context callback.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="endpoint">The send endpoint.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="callback">The callback that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync<TMessage>(this ISendEndpoint endpoint, TMessage message,
        Func<SendContext<TMessage>, Task> callback, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(callback);
        return endpoint.SendAsync(message, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Sends a runtime-typed message configured by a synchronous context callback.</summary>
    /// <param name="endpoint">The send endpoint.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="callback">The callback that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync(this ISendEndpoint endpoint, object message, Action<SendContext> callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(callback);
        return endpoint.SendAsync(message, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Sends a runtime-typed message configured by an asynchronous context callback.</summary>
    /// <param name="endpoint">The send endpoint.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="callback">The callback that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync(this ISendEndpoint endpoint, object message, Func<SendContext, Task> callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(callback);
        return endpoint.SendAsync(message, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Sends a message as an explicit runtime contract configured by a synchronous context callback.</summary>
    /// <param name="endpoint">The send endpoint.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="messageType">The runtime message contract type.</param>
    /// <param name="callback">The callback that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync(this ISendEndpoint endpoint, object message, Type messageType, Action<SendContext> callback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(callback);
        return endpoint.SendAsync(message, messageType, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Sends a message as an explicit runtime contract configured by an asynchronous context callback.</summary>
    /// <param name="endpoint">The send endpoint.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="messageType">The runtime message contract type.</param>
    /// <param name="callback">The callback that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync(this ISendEndpoint endpoint, object message, Type messageType, Func<SendContext, Task> callback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(callback);
        return endpoint.SendAsync(message, messageType, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Initializes and sends a typed message configured by a synchronous context callback.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="endpoint">The send endpoint.</param>
    /// <param name="values">The source values used to initialize the message.</param>
    /// <param name="callback">The callback that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync<TMessage>(this ISendEndpoint endpoint, object values,
        Action<SendContext<TMessage>> callback, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(callback);
        return endpoint.SendAsync(values, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Initializes and sends a typed message configured by an asynchronous context callback.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="endpoint">The send endpoint.</param>
    /// <param name="values">The source values used to initialize the message.</param>
    /// <param name="callback">The callback that configures the send context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task SendAsync<TMessage>(this ISendEndpoint endpoint, object values,
        Func<SendContext<TMessage>, Task> callback,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(callback);
        return endpoint.SendAsync(values, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Creates a pipe that invokes a synchronous typed send callback.</summary>
    /// <typeparam name="TMessage">The sent message type.</typeparam>
    /// <param name="callback">The callback invoked for each context.</param>
    /// <returns>A pipe that invokes the callback.</returns>
    public static IPipe<SendContext<TMessage>> ToPipe<TMessage>(this Action<SendContext<TMessage>> callback)
        where TMessage : class
    {
        return Middleware.Pipe.Execute(callback);
    }

    /// <summary>Creates a pipe that invokes an asynchronous typed send callback.</summary>
    /// <typeparam name="TMessage">The sent message type.</typeparam>
    /// <param name="callback">The callback invoked for each context.</param>
    /// <returns>A pipe that invokes the callback.</returns>
    public static IPipe<SendContext<TMessage>> ToPipe<TMessage>(this Func<SendContext<TMessage>, Task> callback)
        where TMessage : class
    {
        return Middleware.Pipe.ExecuteAwaited(callback);
    }

    /// <summary>Creates a pipe that invokes a synchronous send callback.</summary>
    /// <param name="callback">The callback invoked for each context.</param>
    /// <returns>A pipe that invokes the callback.</returns>
    public static IPipe<SendContext> ToPipe(this Action<SendContext> callback)
    {
        return Middleware.Pipe.Execute(callback);
    }

    /// <summary>Creates a pipe that invokes an asynchronous send callback.</summary>
    /// <param name="callback">The callback invoked for each context.</param>
    /// <returns>A pipe that invokes the callback.</returns>
    public static IPipe<SendContext> ToPipe(this Func<SendContext, Task> callback)
    {
        return Middleware.Pipe.ExecuteAwaited(callback);
    }
}
