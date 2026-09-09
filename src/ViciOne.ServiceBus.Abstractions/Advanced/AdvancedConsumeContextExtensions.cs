namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides access to infrastructure operations carried by a consume context.</summary>
public static class AdvancedConsumeContextExtensions
{
    /// <summary>Returns an infrastructure consume context unchanged.</summary>
    /// <param name="context">The infrastructure consume context.</param>
    /// <returns>The supplied context.</returns>
    public static ConsumeContext Advanced(this ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context;
    }

    /// <summary>Returns the infrastructure consume context that backs an application consume context.</summary>
    /// <typeparam name="T">The consumed message contract type.</typeparam>
    /// <param name="context">The typed consume context to expose as an infrastructure contract.</param>
    /// <returns>The infrastructure consume context that backs the typed context.</returns>
    public static ConsumeContext Advanced<T>(this ConsumeContext<T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        return context as ConsumeContext
            ?? throw new NotSupportedException($"The consume context '{context.GetType().FullName}' does not expose advanced operations.");
    }

    /// <summary>Notifies the receive pipeline that a typed message was consumed.</summary>
    /// <typeparam name="T">The consumed message contract type.</typeparam>
    /// <param name="context">The consume context whose message completed successfully.</param>
    /// <param name="duration">The time spent consuming the message.</param>
    /// <param name="consumerType">The diagnostic consumer identity recorded with the notification.</param>
    /// <param name="cancellationToken">The token that cancels observer notification.</param>
    /// <returns>A task that completes after all consume observers have been notified.</returns>
    public static Task NotifyConsumedAsync<T>(this ConsumeContext<T> context, TimeSpan duration, string consumerType,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return context.Advanced().NotifyConsumedAsync(context, duration, consumerType, cancellationToken);
    }

    /// <summary>Notifies the receive pipeline that a typed message was consumed.</summary>
    /// <typeparam name="T">The consumed message contract type.</typeparam>
    /// <param name="context">The context that owns the receive observer pipeline.</param>
    /// <param name="messageContext">The typed message context that completed successfully.</param>
    /// <param name="duration">The time spent consuming the message.</param>
    /// <param name="consumerType">The diagnostic consumer identity recorded with the notification.</param>
    /// <param name="cancellationToken">The token that cancels observer notification.</param>
    /// <returns>A task that completes after all consume observers have been notified.</returns>
    public static Task NotifyConsumedAsync<T>(this ConsumeContext<T> context, ConsumeContext<T> messageContext, TimeSpan duration,
        string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        return context.Advanced().NotifyConsumedAsync(messageContext, duration, consumerType, cancellationToken);
    }

    /// <summary>Notifies the receive pipeline that typed message consumption faulted.</summary>
    /// <typeparam name="T">The consumed message contract type.</typeparam>
    /// <param name="context">The consume context whose message processing failed.</param>
    /// <param name="duration">The time spent before consumption failed.</param>
    /// <param name="consumerType">The diagnostic consumer identity recorded with the notification.</param>
    /// <param name="exception">The failure raised while consuming the message.</param>
    /// <param name="cancellationToken">The token that cancels observer notification.</param>
    /// <returns>A task that completes after all consume observers have been notified.</returns>
    public static Task NotifyFaultedAsync<T>(this ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return context.Advanced().NotifyFaultedAsync(context, duration, consumerType, exception, cancellationToken);
    }

    /// <summary>Notifies the receive pipeline that typed message consumption faulted.</summary>
    /// <typeparam name="T">The consumed message contract type.</typeparam>
    /// <param name="context">The context that owns the receive observer pipeline.</param>
    /// <param name="messageContext">The typed message context whose processing failed.</param>
    /// <param name="duration">The time spent before consumption failed.</param>
    /// <param name="consumerType">The diagnostic consumer identity recorded with the notification.</param>
    /// <param name="exception">The failure raised while consuming the message.</param>
    /// <param name="cancellationToken">The token that cancels observer notification.</param>
    /// <returns>A task that completes after all consume observers have been notified.</returns>
    public static Task NotifyFaultedAsync<T>(this ConsumeContext<T> context, ConsumeContext<T> messageContext, TimeSpan duration,
        string consumerType, Exception exception, CancellationToken cancellationToken = default)
        where T : class
    {
        return context.Advanced().NotifyFaultedAsync(messageContext, duration, consumerType, exception, cancellationToken);
    }

    /// <summary>Returns the original message identifier from a typed consume context.</summary>
    /// <typeparam name="T">The consumed message contract type.</typeparam>
    /// <param name="context">The consume context whose originating message is inspected.</param>
    /// <returns>The original message identifier, when the causal chain supplies one.</returns>
    public static Guid? GetOriginalMessageId<T>(this ConsumeContext<T> context)
        where T : class
    {
        return SendContextExtensions.GetOriginalMessageId(context.Advanced());
    }
}
