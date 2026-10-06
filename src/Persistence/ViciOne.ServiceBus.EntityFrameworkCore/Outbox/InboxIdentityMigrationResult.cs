namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Reports committed identity changes; this is not a delivery acknowledgement.</summary>
/// <param name="InboxRowsMigrated">The number of replaced inbox principals.</param>
/// <param name="OutboxMessagesReparented">The number of outgoing rows assigned to those replacements.</param>
public sealed record InboxIdentityMigrationResult(long InboxRowsMigrated, long OutboxMessagesReparented);
