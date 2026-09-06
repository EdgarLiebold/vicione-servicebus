
namespace ViciOne.ServiceBus.Operations;

/// <summary>A bounded query for one deterministic page of durable-send quarantine evidence.</summary>
public sealed record DurableSendQuarantineQuery
{
    /// <summary>
    /// Gets or sets the page size value.
    /// </summary>
    public int PageSize { get; init; } = 100;

    /// <summary>Opaque, restart-stable seek token returned by the preceding page.</summary>
    public string? ContinuationToken { get; init; }

    /// <summary>
    /// Performs the first page operation.
    /// </summary>
    /// <param name="pageSize">The page size value.</param>
    /// <returns>The result of the operation.</returns>
    public static DurableSendQuarantineQuery FirstPage(int pageSize = 100)
        => new() { PageSize = pageSize };
}
