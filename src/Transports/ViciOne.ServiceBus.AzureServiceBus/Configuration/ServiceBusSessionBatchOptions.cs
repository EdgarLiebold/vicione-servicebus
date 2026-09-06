using System;
using ViciOne.ServiceBus.AzureServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Controls how messages from each Azure Service Bus session are collected into consumer batches.</summary>
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

    /// <summary>Gets or sets the maximum number of messages from one session in a batch.</summary>
    public int MessageLimitPerSession { get; set; } = 10;

    /// <summary>Gets or sets the maximum number of sessions whose batches are processed concurrently.</summary>
    public int MaxConcurrentSessions { get; set; } = 1;

    /// <summary>Gets or sets how long the processor waits for another message before releasing an inactive session.</summary>
    public TimeSpan? SessionIdleTimeout { get; set; } = Defaults.SessionIdleTimeout;

    /// <summary>Gets or sets the maximum time to wait before delivering a partial batch.</summary>
    public TimeSpan TimeLimit { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Gets or sets the event from which <see cref="TimeLimit"/> is measured.</summary>
    public BatchTimeLimitStart TimeLimitStart { get; set; } = BatchTimeLimitStart.FromFirst;

    /// <summary>Sets the maximum number of messages in a single batch.</summary>
    /// <param name="limit">The positive per-session message limit.</param>
    /// <returns>This options instance for fluent chaining.</returns>
    public ServiceBusSessionBatchOptions SetMessageLimitPerSession(int limit)
    {
        MessageLimitPerSession = limit;
        return this;
    }

    /// <summary>Sets the maximum number of concurrent sessions.</summary>
    /// <param name="limit">The maximum number of concurrent sessions.</param>
    /// <returns>This options instance for fluent chaining.</returns>
    public ServiceBusSessionBatchOptions SetMaxConcurrentSessions(int limit)
    {
        MaxConcurrentSessions = limit;
        return this;
    }

    /// <summary>Sets how long the processor waits for another message before releasing an inactive session.</summary>
    /// <param name="limit">The positive session idle timeout.</param>
    /// <returns>This options instance for fluent chaining.</returns>
    public ServiceBusSessionBatchOptions SetSessionIdleTimeout(TimeSpan limit)
    {
        SessionIdleTimeout = limit;
        return this;
    }

    /// <summary>Sets the maximum time to wait before delivering a partial batch.</summary>
    /// <param name="limit">The positive batch time limit.</param>
    /// <returns>This options instance for fluent chaining.</returns>
    public ServiceBusSessionBatchOptions SetTimeLimit(TimeSpan limit)
    {
        TimeLimit = limit;
        return this;
    }

    /// <summary>Sets the starting point for the <see cref="TimeLimit" />.</summary>
    /// <param name="timeLimitStart">The event from which the time limit is measured.</param>
    /// <returns>This options instance for fluent chaining.</returns>
    public ServiceBusSessionBatchOptions SetTimeLimitStart(BatchTimeLimitStart timeLimitStart)
    {
        TimeLimitStart = timeLimitStart;
        return this;
    }

    static ConfigurationException Invalid(string property, string problem, string fix) =>
        new($"Azure Service Bus session batching for bus 'default': {property} {problem}. {fix}.");
}
