namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Typed reference accepted by the common reliable-messaging operations API.</summary>
public readonly record struct ReliableMessageReference
{
    ReliableMessageReference(ReliableMessageKind kind, DurableSendId outboxId, ReliableInboxKey inboxKey)
    {
        Kind = kind;
        OutboxId = outboxId;
        InboxKey = inboxKey;
    }

    /// <summary>Gets the referenced side.</summary>
    public ReliableMessageKind Kind { get; }

    /// <summary>Gets the outbox id when <see cref="Kind"/> is <see cref="ReliableMessageKind.Outbox"/>.</summary>
    public DurableSendId OutboxId { get; }

    /// <summary>Gets the inbox key when <see cref="Kind"/> is <see cref="ReliableMessageKind.Inbox"/>.</summary>
    public ReliableInboxKey InboxKey { get; }

    /// <summary>Creates an outbox reference.</summary>
    public static ReliableMessageReference Outbox(DurableSendId id) =>
        new(ReliableMessageKind.Outbox, id, default);

    /// <summary>Creates an inbox reference.</summary>
    public static ReliableMessageReference Inbox(ReliableInboxKey key) =>
        new(ReliableMessageKind.Inbox, default, key);
}
