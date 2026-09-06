namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Bounded seek-pagination query for inbox quarantine.</summary>
public sealed record ReliableInboxQuarantineQuery
{
    /// <summary>Gets the exclusive timestamp cursor.</summary>
    public DateTimeOffset? AfterQuarantinedAt { get; init; }

    /// <summary>Gets the exclusive message-id cursor paired with the timestamp cursor.</summary>
    public Guid? AfterMessageId { get; init; }

    /// <summary>Gets the exclusive consumer-id cursor paired with the timestamp cursor.</summary>
    public Guid? AfterConsumerId { get; init; }

    /// <summary>Gets the requested page size. The absolute maximum is 1,000.</summary>
    public int PageSize { get; init; } = 100;
}
