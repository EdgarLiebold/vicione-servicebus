using System;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Represents the SQL projection used to acquire and deliver a queued message.</summary>
public class MessageDelivery
{
    /// <summary>Gets or sets the queue id.</summary>
    public int QueueId { get; set; }
    /// <summary>Gets or sets the priority.</summary>
    public int Priority { get; set; }
    /// <summary>Gets or sets the enqueue time.</summary>
    public DateTimeOffset EnqueueTime { get; set; }
    /// <summary>Gets or sets the consumer id.</summary>
    public Guid? ConsumerId { get; set; }
    /// <summary>Gets or sets the transport message id.</summary>
    public Guid TransportMessageId { get; set; }
    /// <summary>Gets or sets the expiration time.</summary>
    public DateTimeOffset? ExpirationTime { get; set; }
    /// <summary>Gets or sets the delivery count.</summary>
    public int DeliveryCount { get; set; }
    /// <summary>Gets or sets the max delivery count.</summary>
    public int MaxDeliveryCount { get; set; }
    /// <summary>Gets or sets the last delivered.</summary>
    public DateTimeOffset? LastDelivered { get; set; }
    /// <summary>Gets or sets the session number.</summary>
    public long SessionNumber { get; set; }
    /// <summary>Gets or sets the reply to session id.</summary>
    public string? ReplyToSessionId { get; set; }
    /// <summary>Gets or sets the group id.</summary>
    public string? GroupId { get; set; }
    /// <summary>Gets or sets the group sequence number.</summary>
    public int? GroupSequenceNumber { get; set; }
    /// <summary>Gets or sets the transport headers.</summary>
    public string? TransportHeaders { get; set; }
}
