using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

#nullable enable

namespace ViciOne.ServiceBus.Operations;
/// <summary>One immutable bounded page of payload-free quarantine evidence.</summary>
public sealed class DurableSendQuarantinePage
{
    internal DurableSendQuarantinePage(
        IReadOnlyList<DurableSendQuarantineEntry> entries,
        string? continuationToken)
    {
        ArgumentNullException.ThrowIfNull(entries);
        Entries = new ReadOnlyCollection<DurableSendQuarantineEntry>([.. entries]);
        ContinuationToken = continuationToken;
    }

    /// <summary>
    /// Gets the entries value.
    /// </summary>
    public IReadOnlyList<DurableSendQuarantineEntry> Entries { get; }

    /// <summary>
    /// Gets the continuation token value.
    /// </summary>
    public string? ContinuationToken { get; }

    /// <summary>
    /// Gets the has more value.
    /// </summary>
    public bool HasMore => ContinuationToken is not null;

    /// <summary>
    /// Gets the next query value.
    /// </summary>
    public DurableSendQuarantineQuery? NextQuery => ContinuationToken is null
        ? null
        : new DurableSendQuarantineQuery
        {
            PageSize = Entries.Count,
            ContinuationToken = ContinuationToken,
        };
}
