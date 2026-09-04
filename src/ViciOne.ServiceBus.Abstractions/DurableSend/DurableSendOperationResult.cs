namespace ViciOne.ServiceBus;

/// <summary>Exact outcome of a bounded durable-send quarantine operation.</summary>
public enum DurableSendOperationOutcome
{
    Requeued = 0,
    Discarded = 1,
    NotFound = 2,
    NotQuarantined = 3,
}

/// <summary>Typed operator result that never collapses different recovery states into a Boolean.</summary>
public readonly record struct DurableSendOperationResult(DurableSendId Id, DurableSendOperationOutcome Outcome)
{
    public bool IsApplied => Outcome is DurableSendOperationOutcome.Requeued or DurableSendOperationOutcome.Discarded;
}
