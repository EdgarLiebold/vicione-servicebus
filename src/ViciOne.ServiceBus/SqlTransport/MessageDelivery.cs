using System;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Provides a message delivery implementation.
/// </summary>
public class MessageDelivery
{
    /// <summary>
    /// Gets or sets the queue id value.
    /// </summary>
    public int QueueId { get; set; }
    /// <summary>
    /// Gets or sets the priority value.
    /// </summary>
    public int Priority { get; set; }
    /// <summary>
    /// Gets or sets the enqueue time value.
    /// </summary>
    public DateTimeOffset EnqueueTime { get; set; }
    /// <summary>
    /// Gets or sets the consumer id value.
    /// </summary>
    public Guid? ConsumerId { get; set; }
    /// <summary>
    /// Gets or sets the transport message id value.
    /// </summary>
    public Guid TransportMessageId { get; set; }
    /// <summary>
    /// Gets or sets the expiration time value.
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }
    /// <summary>
    /// Gets or sets the delivery count value.
    /// </summary>
    public int DeliveryCount { get; set; }
    /// <summary>
    /// Gets or sets the max delivery count value.
    /// </summary>
    public int MaxDeliveryCount { get; set; }
    /// <summary>
    /// Gets or sets the last delivered value.
    /// </summary>
    public DateTimeOffset? LastDelivered { get; set; }
    /// <summary>
    /// Gets or sets the session number value.
    /// </summary>
    public long SessionNumber { get; set; }
    /// <summary>
    /// Gets or sets the reply to session id value.
    /// </summary>
    public string? ReplyToSessionId { get; set; }
    /// <summary>
    /// Gets or sets the group id value.
    /// </summary>
    public string? GroupId { get; set; }
    /// <summary>
    /// Gets or sets the group sequence number value.
    /// </summary>
    public int? GroupSequenceNumber { get; set; }
    /// <summary>
    /// Gets or sets the transport headers value.
    /// </summary>
    public string? TransportHeaders { get; set; }
}
