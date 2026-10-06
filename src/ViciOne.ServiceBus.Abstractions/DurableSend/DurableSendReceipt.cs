namespace ViciOne.ServiceBus;

/// <summary>
/// Receipt proving that one typed send intent crossed the producer-side durable persistence boundary.
/// It is deliberately not a transport-delivery or consumer-completion receipt.
/// </summary>
public readonly record struct DurableSendReceipt
{
    /// <summary>Creates a receipt from the committed admission result and retained-store totals.</summary>
    /// <param name="id">The admitted durable-send identity.</param>
    /// <param name="disposition">Whether the persistence boundary admitted or already contained the intent.</param>
    /// <param name="storedCount">The retained-record count after admission.</param>
    /// <param name="storedBytes">The retained logical content bytes after admission.</param>
    /// <exception cref="ArgumentException"><paramref name="id" /> is empty or <paramref name="disposition" /> is undefined.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Either retained-store total is negative.</exception>
    public DurableSendReceipt(
        DurableSendId id,
        DurableSendAdmissionDisposition disposition,
        int storedCount,
        long storedBytes)
    {
        if (id.Value == Guid.Empty)
            throw new ArgumentException("A durable-send receipt requires a nonempty identity.", nameof(id));
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

    /// <summary>Gets whether the intent was newly admitted or already retained.</summary>
    public DurableSendAdmissionDisposition Disposition { get; }

    /// <summary>Gets the retained-record count after admission.</summary>
    public int StoredCount { get; }

    /// <summary>Gets the retained logical content bytes after admission.</summary>
    public long StoredBytes { get; }

    /// <summary>Gets whether this admission committed a new durable intent.</summary>
    /// <remarks>The default, uninitialized result does not report success.</remarks>
    public bool IsNew => Id.Value != Guid.Empty && Disposition == DurableSendAdmissionDisposition.Accepted;
}
