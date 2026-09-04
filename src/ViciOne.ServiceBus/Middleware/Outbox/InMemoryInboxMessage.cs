using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>
/// Provides an in memory inbox message implementation.
/// </summary>
public class InMemoryInboxMessage
{
    readonly SemaphoreSlim _inUse;

    readonly List<InMemoryOutboxMessage> _outboxMessages;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageId">The message id value.</param>
    /// <param name="consumerId">The consumer id value.</param>
    public InMemoryInboxMessage(Guid messageId, Guid consumerId)
    {
        MessageId = messageId;
        ConsumerId = consumerId;

        _inUse = new SemaphoreSlim(1);
        _outboxMessages = new List<InMemoryOutboxMessage>();
    }

    /// <summary>
    /// The MessageId of the incoming message
    /// </summary>
    public Guid MessageId { get; }

    /// <summary>
    /// And MD5 hash of the endpoint name + consumer type
    /// </summary>
    public Guid ConsumerId { get; }

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

    /// <summary>
    /// Performs the mark in use operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task MarkInUseAsync(CancellationToken cancellationToken)
    {
        return _inUse.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Performs the release operation.
    /// </summary>
    public void Release()
    {
        _inUse.Release();
    }

    /// <summary>
    /// Gets outbox messages.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public List<InMemoryOutboxMessage> GetOutboxMessages()
    {
        var sequenceNumber = LastSequenceNumber ?? 0;

        lock (_outboxMessages)
            return _outboxMessages.Where(x => x.SequenceNumber > sequenceNumber).ToList();
    }

    /// <summary>
    /// Performs the remove outbox messages operation.
    /// </summary>
    public void RemoveOutboxMessages()
    {
        lock (_outboxMessages)
            _outboxMessages.Clear();
    }

    /// <summary>
    /// Adds outbox message to the configuration.
    /// </summary>
    /// <param name="outboxMessage">The outbox message value.</param>
    public void AddOutboxMessage(InMemoryOutboxMessage outboxMessage)
    {
        lock (_outboxMessages)
        {
            outboxMessage.SequenceNumber = _outboxMessages.Count + 1;

            _outboxMessages.Add(outboxMessage);
        }
    }
}
