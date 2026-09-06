namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>One bounded inbox-quarantine page.</summary>
/// <param name="Entries">The entries.</param>
/// <param name="Next">The next.</param>
public sealed record ReliableInboxQuarantinePage(
    IReadOnlyList<ReliableInboxQuarantineEntry> Entries,
    ReliableInboxQuarantineQuery? Next);
