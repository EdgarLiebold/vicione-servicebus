namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Result of acquiring one inbox identity.</summary>
public sealed record ReliableInboxAcquireResult(
    ReliableInboxKey Key,
    ReliableInboxAcquireDisposition Disposition,
    ReliableInboxLease? Lease,
    int Attempt);
