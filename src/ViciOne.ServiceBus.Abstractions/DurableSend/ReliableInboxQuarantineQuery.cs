namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Bounded seek-pagination query for inbox quarantine.</summary>
public sealed record ReliableInboxQuarantineQuery
{
    /// <summary>Gets or sets the after quarantined at.</summary>
    public DateTimeOffset? AfterQuarantinedAt { get; init; }

    /// <summary>Gets or sets the after message id.</summary>
    public Guid? AfterMessageId { get; init; }

    /// <summary>Gets or sets the after consumer id.</summary>
    public Guid? AfterConsumerId { get; init; }

    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; init; } = 100;
}
