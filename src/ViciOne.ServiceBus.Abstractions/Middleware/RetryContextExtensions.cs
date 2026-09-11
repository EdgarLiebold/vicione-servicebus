namespace ViciOne.ServiceBus.Advanced;

/// <summary>Reads retry and redelivery counters from a consume context.</summary>
public static class RetryContextExtensions
{
    /// <summary>Gets the one-based retry attempt currently in progress.</summary>
    /// <param name="context">The consumed message context.</param>
    /// <returns>The current retry attempt, or zero during the initial delivery.</returns>
    public static int GetRetryAttempt(this ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.TryGetPayload(out ConsumeRetryContext? retryContext) ? retryContext!.RetryAttempt : 0;
    }

    /// <summary>Gets the number of retry attempts completed before the current attempt.</summary>
    /// <param name="context">The consumed message context.</param>
    /// <returns>The number of completed retries, or zero during the initial delivery or first retry.</returns>
    public static int GetRetryCount(this ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.TryGetPayload(out ConsumeRetryContext? retryContext) ? retryContext!.RetryCount : 0;
    }

    /// <summary>Gets the broker-independent redelivery count from envelope or transport metadata.</summary>
    /// <param name="context">The consumed message context.</param>
    /// <returns>The redelivery count, or zero when the message has not been redelivered.</returns>
    public static int GetRedeliveryCount(this ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Headers.Get(MessageHeaders.RedeliveryCount, default(int?))
            ?? context.ReceiveContext.TransportHeaders.Get(MessageHeaders.RedeliveryCount, default(int?))
            ?? 0;
    }
}
