namespace ViciOne.ServiceBus.Configuration;

internal sealed record ReliableMessagingPolicy<TBus>(
    DurableSendStoreLimits Limits,
    int MaximumConcurrentDeliveries,
    int MaximumDeliveryAttempts,
    TimeSpan InitialRetryDelay,
    TimeSpan MaximumRetryDelay,
    double RetryJitterFraction,
    TimeSpan LeaseDuration,
    TimeSpan ConsumerCompletionTimeout,
    TimeSpan PollInterval,
    TimeSpan TelemetrySnapshotInterval,
    TimeSpan HealthDegradedAfter,
    TimeSpan Retention)
    where TBus : class, IBus;
