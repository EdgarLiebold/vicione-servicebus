namespace ViciOne.ServiceBus.Operations;

/// <summary>Bounded composite seek query for one inbox quarantine page.</summary>
public sealed record ReliableInboxQuarantineQuery
{
    /// <summary>Gets the exclusive quarantine-time cursor.</summary>
    public DateTimeOffset? AfterQuarantinedAt { get; init; }

    /// <summary>Gets the exclusive transport-message identity cursor.</summary>
    public Guid? AfterMessageId { get; init; }

    /// <summary>Gets the exclusive consumer-identity cursor.</summary>
    public Guid? AfterConsumerId { get; init; }

    /// <summary>Gets the requested page size.</summary>
    public int PageSize { get; init; } = 100;

    /// <summary>Creates a validated query for the first page.</summary>
    /// <param name="pageSize">The bounded number of entries to request.</param>
    /// <returns>A first-page query.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize" /> is outside the published finite range.</exception>
    public static ReliableInboxQuarantineQuery FirstPage(int pageSize = 100)
        => new() { PageSize = DurableSendOperationLimits.ValidateQuarantinePageSize(pageSize, nameof(pageSize)) };

    internal ReliableInboxQuarantineQuery Validate()
    {
        DurableSendOperationLimits.ValidateQuarantinePageSize(PageSize, nameof(PageSize));
        bool anyCursor = AfterQuarantinedAt.HasValue || AfterMessageId.HasValue || AfterConsumerId.HasValue;
        bool completeCursor = AfterQuarantinedAt.HasValue && AfterMessageId.HasValue && AfterConsumerId.HasValue;
        if (anyCursor && !completeCursor)
            throw new ArgumentException("All inbox quarantine cursor values must be supplied together.", "query");

        return this;
    }
}
