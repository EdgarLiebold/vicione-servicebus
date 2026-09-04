#nullable enable

namespace ViciOne.ServiceBus;

/// <summary>A bounded query for one deterministic page of durable-send quarantine evidence.</summary>
public sealed record DurableSendQuarantineQuery
{
    public int PageSize { get; init; } = 100;

    /// <summary>Opaque, restart-stable seek token returned by the preceding page.</summary>
    public string? ContinuationToken { get; init; }

    public static DurableSendQuarantineQuery FirstPage(int pageSize = 100)
        => new() { PageSize = pageSize };
}
