using System;

namespace ViciOne.ServiceBus.DurableSend;
/// <summary>Mutable bootstrap options for one typed bus; validated and frozen before its durable sender starts.</summary>
public sealed class DurableSenderOptions<TBus>
    where TBus : class, IBus
{

    public int MaximumStoredCount { get; set; } = 10_000;
    public long MaximumStoredBytes { get; set; } = 128L * 1024 * 1024;
    public int MaximumConcurrentDeliveries { get; set; } = 16;
    public int MaximumDeliveryAttempts { get; set; } = 10;
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan MaximumRetryDelay { get; set; } = TimeSpan.FromMinutes(5);
    public double RetryJitterFraction { get; set; } = 0.20;
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan ConsumerCompletionTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan TelemetrySnapshotInterval { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan HealthDegradedAfter { get; set; } = TimeSpan.FromMinutes(15);

    internal DurableSenderPolicy<TBus> ValidateAndFreeze()
    {
        if (MaximumStoredCount < 1)
            throw new ConfigurationException($"{nameof(MaximumStoredCount)} must be positive.");
        if (MaximumStoredBytes < 1)
            throw new ConfigurationException($"{nameof(MaximumStoredBytes)} must be positive.");
        if (MaximumConcurrentDeliveries is < 1 or > DurableSendOperationLimits.AbsoluteMaximumClaimCount)
            throw new ConfigurationException($"{nameof(MaximumConcurrentDeliveries)} must be between 1 and {DurableSendOperationLimits.AbsoluteMaximumClaimCount}.");
        if (MaximumDeliveryAttempts < 1)
            throw new ConfigurationException($"{nameof(MaximumDeliveryAttempts)} must be positive.");
        if (InitialRetryDelay <= TimeSpan.Zero)
            throw new ConfigurationException($"{nameof(InitialRetryDelay)} must be positive.");
        if (MaximumRetryDelay < InitialRetryDelay)
            throw new ConfigurationException($"{nameof(MaximumRetryDelay)} must not be less than {nameof(InitialRetryDelay)}.");
        if (RetryJitterFraction is < 0 or > 0.50)
            throw new ConfigurationException($"{nameof(RetryJitterFraction)} must be between 0 and 0.50.");
        if (LeaseDuration <= TimeSpan.Zero)
            throw new ConfigurationException($"{nameof(LeaseDuration)} must be positive.");
        if (ConsumerCompletionTimeout <= TimeSpan.Zero)
            throw new ConfigurationException($"{nameof(ConsumerCompletionTimeout)} must be positive.");
        if (PollInterval <= TimeSpan.Zero)
            throw new ConfigurationException($"{nameof(PollInterval)} must be positive.");
        if (TelemetrySnapshotInterval <= TimeSpan.Zero)
            throw new ConfigurationException($"{nameof(TelemetrySnapshotInterval)} must be positive.");
        if (HealthDegradedAfter <= TimeSpan.Zero)
            throw new ConfigurationException($"{nameof(HealthDegradedAfter)} must be positive.");

        return new DurableSenderPolicy<TBus>(
            new DurableSendStoreLimits(MaximumStoredCount, MaximumStoredBytes),
            MaximumConcurrentDeliveries,
            MaximumDeliveryAttempts,
            InitialRetryDelay,
            MaximumRetryDelay,
            RetryJitterFraction,
            LeaseDuration,
            ConsumerCompletionTimeout,
            PollInterval,
            TelemetrySnapshotInterval,
            HealthDegradedAfter);
    }
}

internal sealed record DurableSenderPolicy<TBus>(
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
    TimeSpan HealthDegradedAfter)
    where TBus : class, IBus;
