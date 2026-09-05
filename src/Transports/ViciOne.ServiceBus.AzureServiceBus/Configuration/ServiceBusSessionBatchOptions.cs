using System;
using ViciOne.ServiceBus.AzureServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines configuration options for service bus session batch.
/// </summary>
public sealed class ServiceBusSessionBatchOptions
{
    internal void Validate()
    {
        if (MessageLimitPerSession <= 0)
            throw Invalid(nameof(MessageLimitPerSession), "must be greater than zero", "Set a positive per-session message limit");
        if (MaxConcurrentSessions <= 0)
            throw Invalid(nameof(MaxConcurrentSessions), "must be greater than zero", "Set a positive session concurrency limit");
        if (SessionIdleTimeout.HasValue && SessionIdleTimeout.Value <= TimeSpan.Zero)
            throw Invalid(nameof(SessionIdleTimeout), "must be greater than zero when specified", "Set a positive timeout or null");
        if (TimeLimit <= TimeSpan.Zero)
            throw Invalid(nameof(TimeLimit), "must be greater than zero", "Set a positive batch time limit");
        if (!Enum.IsDefined(TimeLimitStart))
            throw Invalid(nameof(TimeLimitStart), $"has the undefined value '{TimeLimitStart}'", "Select a defined BatchTimeLimitStart value");
    }

    /// <summary>
    /// The maximum number of messages in a single batch
    /// </summary>
    public int MessageLimitPerSession { get; set; } = 10;

    /// <summary>
    /// The maximum number of concurrent sessions
    /// </summary>
    public int MaxConcurrentSessions { get; set; } = 1;

    /// <summary>
    /// The timeout before a message session is abandoned
    /// </summary>
    public TimeSpan? SessionIdleTimeout { get; set; } = Defaults.SessionIdleTimeout;

    /// <summary>
    /// The maximum time to wait before delivering a partial batch
    /// </summary>
    public TimeSpan TimeLimit { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The starting point for the <see cref="TimeLimit" />
    /// </summary>
    public BatchTimeLimitStart TimeLimitStart { get; set; } = BatchTimeLimitStart.FromFirst;

    /// <summary>
    /// Sets the maximum number of messages in a single batch
    /// </summary>
    /// <param name="limit">The message limit</param>
    public ServiceBusSessionBatchOptions SetMessageLimitPerSession(int limit)
    {
        MessageLimitPerSession = limit;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of concurrent sessions
    /// </summary>
    /// <param name="limit">The maximum number of concurrent sessions</param>
    public ServiceBusSessionBatchOptions SetMaxConcurrentSessions(int limit)
    {
        MaxConcurrentSessions = limit;
        return this;
    }

    /// <summary>
    /// Sets the maximum time to wait for messages within a session before abandoning the session for another
    /// </summary>
    /// <param name="limit">The time limit</param>
    public ServiceBusSessionBatchOptions SetSessionIdleTimeout(TimeSpan limit)
    {
        SessionIdleTimeout = limit;
        return this;
    }

    /// <summary>
    /// Sets the maximum time to wait before delivering a partial batch
    /// </summary>
    /// <param name="limit">The time limit</param>
    public ServiceBusSessionBatchOptions SetTimeLimit(TimeSpan limit)
    {
        TimeLimit = limit;
        return this;
    }

    /// <summary>
    /// Sets the starting point for the <see cref="TimeLimit" />
    /// </summary>
    /// <param name="timeLimitStart">The starting point</param>
    public ServiceBusSessionBatchOptions SetTimeLimitStart(BatchTimeLimitStart timeLimitStart)
    {
        TimeLimitStart = timeLimitStart;
        return this;
    }

    static ConfigurationException Invalid(string property, string problem, string fix) =>
        new($"Azure Service Bus session batching for bus 'default': {property} {problem}. {fix}.");
}
