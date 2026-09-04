using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines configuration options for outbox delivery service.
/// </summary>
/// <typeparam name="TScope">The t scope type.</typeparam>
public class OutboxDeliveryServiceOptions<TScope>
    where TScope : class
{
    /// <summary>
    /// Gets or sets the message delivery limit value.
    /// </summary>
    public int MessageDeliveryLimit { get; set; } = 100;
    /// <summary>
    /// Gets or sets the message delivery timeout value.
    /// </summary>
    public TimeSpan MessageDeliveryTimeout { get; set; } = TimeSpan.FromSeconds(5);
    /// <summary>
    /// Gets or sets the query delay value.
    /// </summary>
    public TimeSpan QueryDelay { get; set; } = TimeSpan.FromSeconds(5);
    /// <summary>
    /// Gets or sets the query message limit value.
    /// </summary>
    public int QueryMessageLimit { get; set; } = 100;
    /// <summary>
    /// Gets or sets the query timeout value.
    /// </summary>
    public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(30);
    /// <summary>
    /// Gets or sets the maximum delivery attempts value.
    /// </summary>
    public int MaximumDeliveryAttempts { get; set; } = 10;
    /// <summary>
    /// Gets or sets the initial delivery retry delay value.
    /// </summary>
    public TimeSpan InitialDeliveryRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    /// <summary>
    /// Gets or sets the maximum delivery retry delay value.
    /// </summary>
    public TimeSpan MaximumDeliveryRetryDelay { get; set; } = TimeSpan.FromMinutes(1);
}
