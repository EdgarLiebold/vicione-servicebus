namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides access to infrastructure operations carried by a consume context.</summary>
public static class AdvancedConsumeContextExtensions
{
    /// <summary>Returns an infrastructure consume context unchanged.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The consume context produced by the operation.</returns>
    public static ConsumeContext Advanced(this ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context;
    }

    /// <summary>Returns the infrastructure consume context that backs an application consume context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The consume context produced by the operation.</returns>
    public static ConsumeContext Advanced<T>(this ConsumeContext<T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        return context as ConsumeContext
            ?? throw new NotSupportedException($"The consume context '{context.GetType().FullName}' does not expose advanced operations.");
    }

    /// <summary>Notifies the receive pipeline that a typed message was consumed.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration used by the operation.</param>
    /// <param name="consumerType">The consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task NotifyConsumedAsync<T>(this ConsumeContext<T> context, TimeSpan duration, string consumerType,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return context.Advanced().NotifyConsumedAsync(context, duration, consumerType, cancellationToken);
    }

    /// <summary>Notifies the receive pipeline that a typed message was consumed.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="messageContext">The message context used by the operation.</param>
    /// <param name="duration">The duration used by the operation.</param>
    /// <param name="consumerType">The consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task NotifyConsumedAsync<T>(this ConsumeContext<T> context, ConsumeContext<T> messageContext, TimeSpan duration,
        string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        return context.Advanced().NotifyConsumedAsync(messageContext, duration, consumerType, cancellationToken);
    }

    /// <summary>Notifies the receive pipeline that typed message consumption faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration used by the operation.</param>
    /// <param name="consumerType">The consumer type used by the operation.</param>
    /// <param name="exception">The exception used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task NotifyFaultedAsync<T>(this ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return context.Advanced().NotifyFaultedAsync(context, duration, consumerType, exception, cancellationToken);
    }

    /// <summary>Notifies the receive pipeline that typed message consumption faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="messageContext">The message context used by the operation.</param>
    /// <param name="duration">The duration used by the operation.</param>
    /// <param name="consumerType">The consumer type used by the operation.</param>
    /// <param name="exception">The exception used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task NotifyFaultedAsync<T>(this ConsumeContext<T> context, ConsumeContext<T> messageContext, TimeSpan duration,
        string consumerType, Exception exception, CancellationToken cancellationToken = default)
        where T : class
    {
        return context.Advanced().NotifyFaultedAsync(messageContext, duration, consumerType, exception, cancellationToken);
    }

    /// <summary>Returns the original message identifier from a typed consume context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The original message id.</returns>
    public static Guid? GetOriginalMessageId<T>(this ConsumeContext<T> context)
        where T : class
    {
        return SendContextExtensions.GetOriginalMessageId(context.Advanced());
    }
}
