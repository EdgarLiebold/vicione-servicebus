#nullable enable

namespace ViciOne.ServiceBus;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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

    public IReadOnlyList<DurableSendQuarantineEntry> Entries { get; }

    public string? ContinuationToken { get; }

    public bool HasMore => ContinuationToken is not null;

    public DurableSendQuarantineQuery? NextQuery => ContinuationToken is null
        ? null
        : new DurableSendQuarantineQuery
        {
            PageSize = Entries.Count,
            ContinuationToken = ContinuationToken,
        };
}
