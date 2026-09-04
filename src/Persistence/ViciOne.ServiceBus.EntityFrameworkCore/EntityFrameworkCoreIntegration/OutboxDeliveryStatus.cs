namespace ViciOne.ServiceBus.EntityFrameworkCore;


/// <summary>
/// Specifies the available outbox delivery status values.
/// </summary>
public enum OutboxDeliveryStatus
{
    /// <summary>
    /// Indicates pending.
    /// </summary>
    Pending = 0,
    /// <summary>
    /// Indicates retry scheduled.
    /// </summary>
    RetryScheduled = 1,
    /// <summary>
    /// Indicates delivered.
    /// </summary>
    Delivered = 2,
    /// <summary>
    /// Indicates quarantined.
    /// </summary>
    Quarantined = 3
}
