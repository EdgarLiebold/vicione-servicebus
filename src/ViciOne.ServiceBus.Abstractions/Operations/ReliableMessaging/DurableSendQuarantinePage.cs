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
        DurableSendQuarantineEntry[] snapshot = [.. entries];
        foreach (DurableSendQuarantineEntry entry in snapshot)
        {
            ArgumentNullException.ThrowIfNull(entry);
            _ = entry.Validate();
        }

        Entries = new ReadOnlyCollection<DurableSendQuarantineEntry>(snapshot);
        ContinuationToken = continuationToken;
    }

    /// <summary>Gets the immutable payload-free entries in this page.</summary>
    public IReadOnlyList<DurableSendQuarantineEntry> Entries { get; }

    /// <summary>Gets the opaque seek token for the next page, when one exists.</summary>
    public string? ContinuationToken { get; }

    /// <summary>Gets whether another page is available.</summary>
    public bool HasMore => ContinuationToken is not null;

    /// <summary>Gets a query for the next page, or <see langword="null" /> at the end of the result set.</summary>
    public DurableSendQuarantineQuery? NextQuery => ContinuationToken is null
        ? null
        : new DurableSendQuarantineQuery
        {
            PageSize = Entries.Count,
            ContinuationToken = ContinuationToken,
        };
}
