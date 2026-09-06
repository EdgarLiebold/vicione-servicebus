
namespace ViciOne.ServiceBus.Operations;

/// <summary>A bounded query for one deterministic page of durable-send quarantine evidence.</summary>
public sealed record DurableSendQuarantineQuery
{
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; init; } = 100;

    /// <summary>Opaque, restart-stable seek token returned by the preceding page.</summary>
    public string? ContinuationToken { get; init; }

    /// <summary>Returns the first page of results.</summary>
    /// <param name="pageSize">The page size.</param>
    /// <returns>The durable send quarantine query produced by the operation.</returns>
    public static DurableSendQuarantineQuery FirstPage(int pageSize = 100)
        => new() { PageSize = pageSize };
}
