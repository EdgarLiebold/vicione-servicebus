namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Result of acquiring one inbox identity.</summary>
/// <param name="Key">The key.</param>
/// <param name="Disposition">The disposition.</param>
/// <param name="Lease">The lease.</param>
/// <param name="Attempt">The attempt.</param>
public sealed record ReliableInboxAcquireResult(
    ReliableInboxKey Key,
    ReliableInboxAcquireDisposition Disposition,
    ReliableInboxLease? Lease,
    int Attempt);
