namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Identifies the persisted delivery state of a transactional outbox row.</summary>
public enum OutboxDeliveryStatus
{
    /// <summary>The row has not been assigned a valid delivery state.</summary>
    Unknown = 0,

    /// <summary>The row is eligible for delivery.</summary>
    Pending = 1,

    /// <summary>A failed delivery may be retried at its next-delivery time.</summary>
    RetryScheduled = 2,

    /// <summary>Every message has been sent and the row is awaiting final cleanup.</summary>
    Delivered = 3,

    /// <summary>Automatic delivery has stopped pending an operator action.</summary>
    Quarantined = 4,

    /// <summary>An operator transaction owns the row while permanently removing it and its messages.</summary>
    Discarding = 5,
}
