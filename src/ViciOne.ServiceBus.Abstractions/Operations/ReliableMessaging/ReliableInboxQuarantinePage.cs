using System.Collections.ObjectModel;

namespace ViciOne.ServiceBus.Operations;

/// <summary>One immutable bounded page of payload-free inbox quarantine evidence.</summary>
public sealed class ReliableInboxQuarantinePage
{
    /// <summary>Creates an immutable page from validated payload-free entries.</summary>
    /// <param name="entries">The entries in deterministic seek order.</param>
    /// <param name="next">The validated query for the next page, or <see langword="null" /> for the final page.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entries" /> or one of its elements is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">An entry or continuation query is invalid.</exception>
    public ReliableInboxQuarantinePage(
        IReadOnlyList<ReliableInboxQuarantineEntry> entries,
        ReliableInboxQuarantineQuery? next)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ReliableInboxQuarantineEntry[] snapshot = [.. entries];
        foreach (ReliableInboxQuarantineEntry entry in snapshot)
        {
            ArgumentNullException.ThrowIfNull(entry);
            _ = entry.Validate();
        }

        if (next is not null)
            _ = next.Validate();

        Entries = new ReadOnlyCollection<ReliableInboxQuarantineEntry>(snapshot);
        Next = next;
    }

    /// <summary>Gets the immutable payload-free entries in this page.</summary>
    public IReadOnlyList<ReliableInboxQuarantineEntry> Entries { get; }

    /// <summary>Gets the next seek query, or <see langword="null" /> at the end of the result set.</summary>
    public ReliableInboxQuarantineQuery? Next { get; }

    /// <summary>Gets whether another page is available.</summary>
    public bool HasMore => Next is not null;
}
