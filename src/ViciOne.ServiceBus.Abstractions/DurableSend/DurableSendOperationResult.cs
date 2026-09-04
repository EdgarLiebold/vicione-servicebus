namespace ViciOne.ServiceBus.Operations;

/// <summary>Exact outcome of a bounded durable-send quarantine operation.</summary>
public enum DurableSendOperationOutcome
{
    /// <summary>
    /// Indicates requeued.
    /// </summary>
    Requeued = 0,
    /// <summary>
    /// Indicates discarded.
    /// </summary>
    Discarded = 1,
    /// <summary>
    /// Indicates not found.
    /// </summary>
    NotFound = 2,
    /// <summary>
    /// Indicates not quarantined.
    /// </summary>
    NotQuarantined = 3,
}

/// <summary>Typed operator result that never collapses different recovery states into a Boolean.</summary>
public readonly record struct DurableSendOperationResult(DurableSendId Id, DurableSendOperationOutcome Outcome)
{
    /// <summary>
    /// Gets the is applied value.
    /// </summary>
    public bool IsApplied => Outcome is DurableSendOperationOutcome.Requeued or DurableSendOperationOutcome.Discarded;
}
