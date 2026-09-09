namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Result of acquiring one inbox identity.</summary>
public sealed record ReliableInboxAcquireResult
{
    /// <summary>Creates a durable acquisition result.</summary>
    /// <param name="key">The incoming-message and consumer identity.</param>
    /// <param name="disposition">The duplicate, timing, or ownership outcome.</param>
    /// <param name="lease">The fenced lease when the result grants ownership.</param>
    /// <param name="attempt">The one-based persisted processing-attempt count.</param>
    /// <exception cref="ArgumentException">An identity, disposition, or lease combination is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="attempt" /> is less than one.</exception>
    public ReliableInboxAcquireResult(
        ReliableInboxKey key,
        ReliableInboxAcquireDisposition disposition,
        ReliableInboxLease? lease,
        int attempt)
    {
        Key = key.Validate();
        if (!Enum.IsDefined(disposition))
            throw new ArgumentException("The inbox acquisition disposition is undefined.", nameof(disposition));
        if ((disposition == ReliableInboxAcquireDisposition.Acquired) != lease.HasValue)
            throw new ArgumentException("Exactly an acquired inbox result must contain a lease.", nameof(lease));
        if (lease is { Token: var token } && token == Guid.Empty)
            throw new ArgumentException("An acquired inbox result requires a nonempty lease token.", nameof(lease));

        ArgumentOutOfRangeException.ThrowIfLessThan(attempt, 1);

        Disposition = disposition;
        Lease = lease;
        Attempt = attempt;
    }

    /// <summary>Gets the incoming-message and consumer identity.</summary>
    public ReliableInboxKey Key { get; }

    /// <summary>Gets the duplicate, timing, or ownership outcome.</summary>
    public ReliableInboxAcquireDisposition Disposition { get; }

    /// <summary>Gets the fenced processing lease when ownership was acquired.</summary>
    public ReliableInboxLease? Lease { get; }

    /// <summary>Gets the one-based persisted processing-attempt count.</summary>
    public int Attempt { get; }
}
