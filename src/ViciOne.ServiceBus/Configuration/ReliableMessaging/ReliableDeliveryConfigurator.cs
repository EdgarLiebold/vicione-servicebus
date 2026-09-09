namespace ViciOne.ServiceBus.Configuration;

internal sealed class ReliableDeliveryConfigurator : IReliableDeliveryConfigurator
{
    public int MaximumConcurrentDeliveries { get; set; } = 16;

    public int MaximumAttempts { get; set; } = 10;

    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(15);

    public TimeSpan MaximumRetryDelay { get; set; } = TimeSpan.FromMinutes(5);

    public double RetryJitterFraction { get; set; } = 0.20;

    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan ConsumerCompletionTimeout { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan TelemetrySnapshotInterval { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan HealthDegradedAfter { get; set; } = TimeSpan.FromMinutes(15);
}
