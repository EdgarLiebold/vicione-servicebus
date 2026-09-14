using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Controls polling, batching, timeouts, and retries for one durable outbox delivery worker.</summary>
/// <typeparam name="TScope">The type that isolates one outbox option set.</typeparam>
public sealed class OutboxDeliveryServiceOptions<TScope>
    where TScope : class
{
    /// <summary>Gets or sets the maximum number of messages sent in one delivery batch.</summary>
    public int MessageDeliveryLimit { get; set; } = 100;
    /// <summary>Gets or sets the timeout for each transport-delivery operation.</summary>
    public TimeSpan MessageDeliveryTimeout { get; set; } = TimeSpan.FromSeconds(5);
    /// <summary>Gets or sets the delay between outbox polling cycles.</summary>
    public TimeSpan QueryDelay { get; set; } = TimeSpan.FromSeconds(5);
    /// <summary>Gets or sets the maximum number of messages loaded by one outbox query.</summary>
    public int QueryMessageLimit { get; set; } = 100;
    /// <summary>Gets or sets the timeout for each outbox query.</summary>
    public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(30);
    /// <summary>Gets or sets the maximum number of attempts for a failing delivery.</summary>
    public int MaximumDeliveryAttempts { get; set; } = 10;
    /// <summary>Gets or sets the delay before the first delivery retry.</summary>
    public TimeSpan InitialDeliveryRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    /// <summary>Gets or sets the upper bound for delivery retry backoff.</summary>
    public TimeSpan MaximumDeliveryRetryDelay { get; set; } = TimeSpan.FromMinutes(1);
}
