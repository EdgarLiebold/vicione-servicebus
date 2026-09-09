namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Result of atomically admitting a durable send intent.</summary>
public readonly record struct DurableSendAdmissionResult
{
    /// <summary>Creates an admission result with the store totals observed after the atomic operation.</summary>
    /// <param name="id">The admitted durable-send identity.</param>
    /// <param name="disposition">The idempotent admission outcome.</param>
    /// <param name="storedCount">The retained-record count after admission.</param>
    /// <param name="storedBytes">The retained logical content bytes after admission.</param>
    /// <exception cref="ArgumentException"><paramref name="id" /> is empty or <paramref name="disposition" /> is undefined.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Either retained-store total is negative.</exception>
    public DurableSendAdmissionResult(
        DurableSendId id,
        DurableSendAdmissionDisposition disposition,
        int storedCount,
        long storedBytes)
    {
        if (id.Value == Guid.Empty)
            throw new ArgumentException("A durable-send admission result requires a nonempty identity.", nameof(id));
        if (!Enum.IsDefined(disposition))
            throw new ArgumentException("The durable-send admission disposition is undefined.", nameof(disposition));

        ArgumentOutOfRangeException.ThrowIfNegative(storedCount);
        ArgumentOutOfRangeException.ThrowIfNegative(storedBytes);

        Id = id;
        Disposition = disposition;
        StoredCount = storedCount;
        StoredBytes = storedBytes;
    }

    /// <summary>Gets the admitted durable-send identity.</summary>
    public DurableSendId Id { get; }

    /// <summary>Gets the idempotent admission outcome.</summary>
    public DurableSendAdmissionDisposition Disposition { get; }

    /// <summary>Gets the retained-record count after admission.</summary>
    public int StoredCount { get; }

    /// <summary>Gets the retained logical content bytes after admission.</summary>
    public long StoredBytes { get; }

    /// <summary>Gets whether this admission committed a new durable intent.</summary>
    public bool IsNew => Disposition == DurableSendAdmissionDisposition.Accepted;
}
