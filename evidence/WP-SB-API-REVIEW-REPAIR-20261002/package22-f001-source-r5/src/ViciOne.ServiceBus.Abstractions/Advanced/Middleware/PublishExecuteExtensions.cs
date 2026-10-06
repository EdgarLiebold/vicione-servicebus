namespace ViciOne.ServiceBus.Advanced;

/// <summary>Publishes messages through callback-configured send contexts.</summary>
public static class PublishExecuteExtensions
{
    /// <summary>Publishes a typed message configured by a synchronous context callback.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="endpoint">The publish endpoint.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="callback">The callback that configures the publish context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static Task PublishAsync<TMessage>(this IPublishEndpoint endpoint, TMessage message,
        Action<PublishContext<TMessage>> callback,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(callback);
        return endpoint.PublishAsync(message, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Publishes a typed message configured by an asynchronous context callback.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="endpoint">The publish endpoint.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="callback">The callback that configures the publish context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static Task PublishAsync<TMessage>(this IPublishEndpoint endpoint, TMessage message,
        Func<PublishContext<TMessage>, Task> callback,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(callback);
        return endpoint.PublishAsync(message, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Publishes a runtime-typed message configured by a synchronous context callback.</summary>
    /// <param name="endpoint">The publish endpoint.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="callback">The callback that configures the publish context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static Task PublishAsync(this IPublishEndpoint endpoint, object message, Action<PublishContext> callback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(callback);
        return endpoint.PublishAsync(message, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Publishes a runtime-typed message configured by an asynchronous context callback.</summary>
    /// <param name="endpoint">The publish endpoint.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="callback">The callback that configures the publish context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static Task PublishAsync(this IPublishEndpoint endpoint, object message, Func<PublishContext, Task> callback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(callback);
        return endpoint.PublishAsync(message, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Publishes a message as an explicit runtime contract configured by a synchronous context callback.</summary>
    /// <param name="endpoint">The publish endpoint.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="messageType">The runtime message contract type.</param>
    /// <param name="callback">The callback that configures the publish context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static Task PublishAsync(this IPublishEndpoint endpoint, object message, Type messageType, Action<PublishContext> callback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(callback);
        return endpoint.PublishAsync(message, messageType, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Publishes a message as an explicit runtime contract configured by an asynchronous context callback.</summary>
    /// <param name="endpoint">The publish endpoint.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="messageType">The runtime message contract type.</param>
    /// <param name="callback">The callback that configures the publish context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static Task PublishAsync(this IPublishEndpoint endpoint, object message, Type messageType, Func<PublishContext, Task> callback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(callback);
        return endpoint.PublishAsync(message, messageType, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Initializes and publishes a typed message configured by a synchronous context callback.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="endpoint">The publish endpoint.</param>
    /// <param name="values">The source values used to initialize the message.</param>
    /// <param name="callback">The callback that configures the publish context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static Task PublishAsync<TMessage>(this IPublishEndpoint endpoint, object values,
        Action<PublishContext<TMessage>> callback,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(callback);
        return endpoint.PublishAsync(values, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Initializes and publishes a typed message configured by an asynchronous context callback.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="endpoint">The publish endpoint.</param>
    /// <param name="values">The source values used to initialize the message.</param>
    /// <param name="callback">The callback that configures the publish context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static Task PublishAsync<TMessage>(this IPublishEndpoint endpoint, object values,
        Func<PublishContext<TMessage>, Task> callback,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(callback);
        return endpoint.PublishAsync(values, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Creates a pipe that invokes a synchronous typed publish callback.</summary>
    /// <typeparam name="TMessage">The published message type.</typeparam>
    /// <param name="callback">The callback invoked for each context.</param>
    /// <returns>A pipe that invokes the callback.</returns>
    public static IPipe<PublishContext<TMessage>> ToPipe<TMessage>(this Action<PublishContext<TMessage>> callback)
        where TMessage : class
    {
        return Middleware.Pipe.Execute(callback);
    }

    /// <summary>Creates a pipe that invokes an asynchronous typed publish callback.</summary>
    /// <typeparam name="TMessage">The published message type.</typeparam>
    /// <param name="callback">The callback invoked for each context.</param>
    /// <returns>A pipe that invokes the callback.</returns>
    public static IPipe<PublishContext<TMessage>> ToPipe<TMessage>(this Func<PublishContext<TMessage>, Task> callback)
        where TMessage : class
    {
        return Middleware.Pipe.ExecuteAwaited(callback);
    }

    /// <summary>Creates a pipe that invokes a synchronous publish callback.</summary>
    /// <param name="callback">The callback invoked for each context.</param>
    /// <returns>A pipe that invokes the callback.</returns>
    public static IPipe<PublishContext> ToPipe(this Action<PublishContext> callback)
    {
        return Middleware.Pipe.Execute(callback);
    }

    /// <summary>Creates a pipe that invokes an asynchronous publish callback.</summary>
    /// <param name="callback">The callback invoked for each context.</param>
    /// <returns>A pipe that invokes the callback.</returns>
    public static IPipe<PublishContext> ToPipe(this Func<PublishContext, Task> callback)
    {
        return Middleware.Pipe.ExecuteAwaited(callback);
    }
}
