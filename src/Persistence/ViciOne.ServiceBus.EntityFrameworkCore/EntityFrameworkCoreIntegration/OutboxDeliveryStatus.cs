namespace ViciOne.ServiceBus.EntityFrameworkCore;


/// <summary>Identifies the persisted delivery state of a transactional outbox row.</summary>
public enum OutboxDeliveryStatus
{
    /// <summary>The row is eligible for delivery.</summary>
    Pending = 0,
    /// <summary>A failed delivery may be retried at its next-delivery time.</summary>
    RetryScheduled = 1,
    /// <summary>Every message has been sent and the row is awaiting final cleanup.</summary>
    Delivered = 2,
    /// <summary>Automatic delivery has stopped pending an operator action.</summary>
    Quarantined = 3
}
