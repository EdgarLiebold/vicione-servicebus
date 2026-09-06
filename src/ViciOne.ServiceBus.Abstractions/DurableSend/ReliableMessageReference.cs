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

    /// <summary>Gets the kind.</summary>
    public ReliableMessageKind Kind { get; }

    /// <summary>Gets the outbox id.</summary>
    public DurableSendId OutboxId { get; }

    /// <summary>Gets the inbox key.</summary>
    public ReliableInboxKey InboxKey { get; }

    /// <summary>Creates an outbox reference.</summary>
    /// <param name="id">The id.</param>
    /// <returns>The reliable message reference produced by the operation.</returns>
    public static ReliableMessageReference Outbox(DurableSendId id) =>
        new(ReliableMessageKind.Outbox, id, default);

    /// <summary>Creates an inbox reference.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <returns>The reliable message reference produced by the operation.</returns>
    public static ReliableMessageReference Inbox(ReliableInboxKey key) =>
        new(ReliableMessageKind.Inbox, default, key);
}
