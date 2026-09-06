using System;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides an inbox state implementation.
/// </summary>
public class InboxState
{
    /// <summary>
    /// Primary key for table, to have ordered clustered index
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The MessageId of the incoming message
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// And MD5 hash of the endpoint name + consumer type
    /// </summary>
    public Guid ConsumerId { get; set; }

    /// <summary>
    /// Lock token to ensure row is locked within the transaction
    /// </summary>
    public Guid LockId { get; set; }

    /// <summary>
    /// EF RowVersion
    /// </summary>
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// When the message was first received
    /// </summary>
    public DateTimeOffset Received { get; set; }

    /// <summary>
    /// How many times the message has been received
    /// </summary>
    public int ReceiveCount { get; set; }

    /// <summary>
    /// If present, when the message expires (from the message header)
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// When the message was consumed, successfully
    /// </summary>
    public DateTimeOffset? Consumed { get; set; }

    /// <summary>
    /// When all messages in the outbox were delivered to the transport
    /// </summary>
    public DateTimeOffset? Delivered { get; set; }

    /// <summary>
    /// The last sequence number that was successfully delivered to the transport
    /// </summary>
    public long? LastSequenceNumber { get; set; }
}
