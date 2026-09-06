namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>One bounded inbox-quarantine page.</summary>
public sealed record ReliableInboxQuarantinePage(
    IReadOnlyList<ReliableInboxQuarantineEntry> Entries,
    ReliableInboxQuarantineQuery? Next);
