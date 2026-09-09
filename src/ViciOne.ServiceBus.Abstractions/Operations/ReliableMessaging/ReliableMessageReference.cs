namespace ViciOne.ServiceBus.Operations;

/// <summary>Typed reference accepted by the common reliable-messaging operations API.</summary>
public readonly record struct ReliableMessageReference
{
    ReliableMessageReference(ReliableMessageKind kind, DurableSendId outboxId, ReliableInboxKey inboxKey)
    {
        Kind = kind;
        OutboxId = outboxId;
        InboxKey = inboxKey;
    }

    /// <summary>Gets whether this reference targets an outbox intent or inbox record.</summary>
    public ReliableMessageKind Kind { get; }

    /// <summary>Gets the durable-send identity when <see cref="Kind" /> is <see cref="ReliableMessageKind.Outbox" />.</summary>
    public DurableSendId OutboxId { get; }

    /// <summary>Gets the incoming-message and consumer identity when <see cref="Kind" /> is <see cref="ReliableMessageKind.Inbox" />.</summary>
    public ReliableInboxKey InboxKey { get; }

    /// <summary>Creates an outbox reference.</summary>
    /// <param name="id">The nonempty durable-send identity.</param>
    /// <returns>A typed outbox reference.</returns>
    /// <exception cref="ArgumentException"><paramref name="id" /> is empty.</exception>
    public static ReliableMessageReference Outbox(DurableSendId id)
    {
        if (id.Value == Guid.Empty)
            throw new ArgumentException("An outbox reference requires a nonempty durable-send identity.", nameof(id));

        return new ReliableMessageReference(ReliableMessageKind.Outbox, id, default);
    }

    /// <summary>Creates an inbox reference.</summary>
    /// <param name="key">The incoming-message and consumer identity.</param>
    /// <returns>A typed inbox reference.</returns>
    /// <exception cref="ArgumentException">Either identity component is empty.</exception>
    public static ReliableMessageReference Inbox(ReliableInboxKey key) =>
        new(ReliableMessageKind.Inbox, default, key.Validate());

    internal ReliableMessageReference Validate()
    {
        if (Kind is not (ReliableMessageKind.Outbox or ReliableMessageKind.Inbox))
            throw new ArgumentException("The reliable-message reference kind is undefined.", nameof(Kind));
        if (Kind == ReliableMessageKind.Outbox)
        {
            if (OutboxId.Value == Guid.Empty)
                throw new ArgumentException("An outbox reference requires a nonempty durable-send identity.", nameof(OutboxId));
        }
        else
            _ = InboxKey.Validate();

        return this;
    }
}
