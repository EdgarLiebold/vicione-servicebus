namespace ViciOne.ServiceBus.Operations;

/// <summary>Typed operator result that never collapses different recovery states into a Boolean.</summary>
public readonly record struct DurableSendOperationResult
{
    /// <summary>Creates an outbox operation result.</summary>
    /// <param name="id">The targeted durable-send identity.</param>
    /// <param name="outcome">The exact state-transition outcome.</param>
    /// <exception cref="ArgumentException"><paramref name="id" /> is empty or <paramref name="outcome" /> is undefined.</exception>
    public DurableSendOperationResult(DurableSendId id, DurableSendOperationOutcome outcome)
    {
        if (id.Value == Guid.Empty)
            throw new ArgumentException("A durable-send operation result requires a nonempty identity.", nameof(id));
        if (!Enum.IsDefined(outcome))
            throw new ArgumentException("The durable-send operation outcome is undefined.", nameof(outcome));

        Id = id;
        Outcome = outcome;
    }

    /// <summary>Gets the targeted durable-send identity.</summary>
    public DurableSendId Id { get; }

    /// <summary>Gets the exact state-transition outcome.</summary>
    public DurableSendOperationOutcome Outcome { get; }

    /// <summary>Gets whether the requested state transition was applied.</summary>
    /// <remarks>The default, uninitialized result does not report success.</remarks>
    public bool IsApplied => Id.Value != Guid.Empty
        && Outcome is DurableSendOperationOutcome.Requeued or DurableSendOperationOutcome.Discarded;
}
