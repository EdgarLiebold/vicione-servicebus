using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.Outbox.InMemory;

/// <summary>Stores the delivery state and captured outgoing messages for one inbox entry.</summary>
internal sealed class InMemoryInboxMessage :
    IDisposable
{
    readonly SemaphoreSlim _inUse;

    readonly List<InMemoryOutboxMessage> _outboxMessages;

    /// <summary>Initializes an inbox entry for one incoming message and logical consumer.</summary>
    /// <param name="messageId">The identifier of the incoming message.</param>
    /// <param name="consumerId">The stable identifier of the logical consumer.</param>
    public InMemoryInboxMessage(Guid messageId, Guid consumerId)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException("The message identifier must not be empty.", nameof(messageId));
        if (consumerId == Guid.Empty)
            throw new ArgumentException("The consumer identifier must not be empty.", nameof(consumerId));

        MessageId = messageId;
        ConsumerId = consumerId;

        _inUse = new SemaphoreSlim(1, 1);
        _outboxMessages = [];
    }

    /// <summary>Gets the identifier of the incoming message.</summary>
    public Guid MessageId { get; }

    /// <summary>Gets the stable identifier of the logical consumer.</summary>
    public Guid ConsumerId { get; }

    /// <summary>Gets or sets when the inbox first received the message.</summary>
    public DateTimeOffset Received { get; set; }

    /// <summary>Gets or sets how many delivery attempts have acquired this inbox entry.</summary>
    public int ReceiveCount { get; set; }

    /// <summary>Gets or sets when the incoming message expires, if an expiration was supplied.</summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>Gets or sets when consumption was committed.</summary>
    public DateTimeOffset? Consumed { get; set; }

    /// <summary>Gets or sets when every captured outgoing message was delivered.</summary>
    public DateTimeOffset? Delivered { get; set; }

    /// <summary>Gets or sets the sequence number of the last successfully delivered outgoing message.</summary>
    public long? LastSequenceNumber { get; set; }

    /// <summary>Waits for exclusive ownership of this inbox entry.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when exclusive ownership is acquired.</returns>
    public Task MarkInUseAsync(CancellationToken cancellationToken)
    {
        return _inUse.WaitAsync(cancellationToken);
    }

    /// <summary>Releases exclusive ownership of this inbox entry.</summary>
    public void Release()
    {
        _inUse.Release();
    }

    /// <summary>Gets a snapshot of messages after the last successfully delivered sequence.</summary>
    /// <returns>The ordered messages still awaiting delivery.</returns>
    public List<InMemoryOutboxMessage> GetOutboxMessages()
    {
        var sequenceNumber = LastSequenceNumber ?? 0;

        lock (_outboxMessages)
            return _outboxMessages.Where(x => x.SequenceNumber > sequenceNumber).ToList();
    }

    /// <summary>Removes every captured outgoing message.</summary>
    public void RemoveOutboxMessages()
    {
        lock (_outboxMessages)
            _outboxMessages.Clear();
    }

    /// <summary>Appends an outgoing message and assigns its one-based delivery sequence.</summary>
    /// <param name="outboxMessage">The outgoing message to append.</param>
    public void AddOutboxMessage(InMemoryOutboxMessage outboxMessage)
    {
        ArgumentNullException.ThrowIfNull(outboxMessage);

        lock (_outboxMessages)
        {
            outboxMessage.SequenceNumber = _outboxMessages.Count + 1;

            _outboxMessages.Add(outboxMessage);
        }
    }

    /// <summary>Releases the synchronization resource owned by this inbox entry.</summary>
    public void Dispose()
    {
        _inUse.Dispose();
    }
}
