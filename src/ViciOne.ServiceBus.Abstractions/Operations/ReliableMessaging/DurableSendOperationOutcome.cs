namespace ViciOne.ServiceBus.Operations;

/// <summary>Identifies the exact outcome of a durable-send quarantine operation.</summary>
public enum DurableSendOperationOutcome
{
    /// <summary>The quarantined intent was returned to the delivery queue.</summary>
    Requeued = 0,

    /// <summary>The quarantined intent was permanently removed.</summary>
    Discarded = 1,

    /// <summary>No retained intent exists for the requested identity.</summary>
    NotFound = 2,

    /// <summary>The retained intent exists but is not quarantined.</summary>
    NotQuarantined = 3,
}
