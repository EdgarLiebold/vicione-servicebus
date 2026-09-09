namespace ViciOne.ServiceBus.Operations;

/// <summary>A bounded query for one deterministic page of durable-send quarantine evidence.</summary>
public sealed record DurableSendQuarantineQuery
{
    /// <summary>Gets the requested page size.</summary>
    public int PageSize { get; init; } = 100;

    /// <summary>Opaque, restart-stable seek token returned by the preceding page.</summary>
    public string? ContinuationToken { get; init; }

    /// <summary>Creates a validated query for the first page.</summary>
    /// <param name="pageSize">The bounded number of entries to request.</param>
    /// <returns>A first-page query.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize" /> is outside the published finite range.</exception>
    public static DurableSendQuarantineQuery FirstPage(int pageSize = 100)
        => new() { PageSize = DurableSendOperationLimits.ValidateQuarantinePageSize(pageSize, nameof(pageSize)) };
}
