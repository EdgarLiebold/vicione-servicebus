using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;


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

    /// <summary>Gets the entries.</summary>
    public IReadOnlyList<DurableSendQuarantineEntry> Entries { get; }

    /// <summary>Gets the continuation token.</summary>
    public string? ContinuationToken { get; }

    /// <summary>Gets a value indicating whether this instance has more.</summary>
    public bool HasMore => ContinuationToken is not null;

    /// <summary>Gets the next query.</summary>
    public DurableSendQuarantineQuery? NextQuery => ContinuationToken is null
        ? null
        : new DurableSendQuarantineQuery
        {
            PageSize = Entries.Count,
            ContinuationToken = ContinuationToken,
        };
}
