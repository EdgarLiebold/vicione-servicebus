namespace ViciOne.ServiceBus.Advanced;

/// <summary>Sends responses through callback-configured send contexts.</summary>
public static class RespondAsyncExecuteExtensions
{
    /// <summary>Sends a typed response configured by a synchronous context callback.</summary>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="message">The response message.</param>
    /// <param name="callback">The callback that configures the response send context.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static Task RespondAsync<TResponse>(this ConsumeContext context, TResponse message,
        Action<SendContext<TResponse>> callback)
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(callback);
        return context.RespondAsync(message, callback.ToPipe());
    }

    /// <summary>Sends a typed response configured by an asynchronous context callback.</summary>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="message">The response message.</param>
    /// <param name="callback">The callback that configures the response send context.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static Task RespondAsync<TResponse>(this ConsumeContext context, TResponse message,
        Func<SendContext<TResponse>, Task> callback)
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(callback);
        return context.RespondAsync(message, callback.ToPipe());
    }

    /// <summary>Sends a runtime-typed response configured by a synchronous context callback.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="message">The response message.</param>
    /// <param name="callback">The callback that configures the response send context.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static Task RespondAsync(this ConsumeContext context, object message, Action<SendContext> callback)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(callback);
        return context.RespondAsync(message, callback.ToPipe());
    }

    /// <summary>Sends a runtime-typed response configured by an asynchronous context callback.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="message">The response message.</param>
    /// <param name="callback">The callback that configures the response send context.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static Task RespondAsync(this ConsumeContext context, object message, Func<SendContext, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(callback);
        return context.RespondAsync(message, callback.ToPipe());
    }

    /// <summary>Sends a response as an explicit runtime contract configured by a synchronous context callback.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="message">The response message.</param>
    /// <param name="messageType">The runtime response contract type.</param>
    /// <param name="callback">The callback that configures the response send context.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static Task RespondAsync(this ConsumeContext context, object message, Type messageType, Action<SendContext> callback)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(callback);
        return context.RespondAsync(message, messageType, callback.ToPipe());
    }

    /// <summary>Sends a response as an explicit runtime contract configured by an asynchronous context callback.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="message">The response message.</param>
    /// <param name="messageType">The runtime response contract type.</param>
    /// <param name="callback">The callback that configures the response send context.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static Task RespondAsync(this ConsumeContext context, object message, Type messageType, Func<SendContext, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(callback);
        return context.RespondAsync(message, messageType, callback.ToPipe());
    }

    /// <summary>Initializes and sends a typed response configured by a synchronous context callback.</summary>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="values">The source values used to initialize the response.</param>
    /// <param name="callback">The callback that configures the response send context.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static Task RespondAsync<TResponse>(this ConsumeContext context, object values,
        Action<SendContext<TResponse>> callback)
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(callback);
        return context.RespondAsync(values, callback.ToPipe());
    }

    /// <summary>Initializes and sends a typed response configured by an asynchronous context callback.</summary>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="values">The source values used to initialize the response.</param>
    /// <param name="callback">The callback that configures the response send context.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static Task RespondAsync<TResponse>(this ConsumeContext context, object values,
        Func<SendContext<TResponse>, Task> callback)
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(callback);
        return context.RespondAsync(values, callback.ToPipe());
    }
}
