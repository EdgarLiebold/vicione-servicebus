namespace ViciOne.ServiceBus.Monitoring.Telemetry;

/// <summary>Identifies why durable-send admission was rejected.</summary>
internal enum DurableSendAdmissionFailure
{
    /// <summary>The configured durable-send capacity cannot accept another message.</summary>
    CapacityExceeded = 0,

    /// <summary>The durable identifier already belongs to a different message.</summary>
    IdentityConflict = 1,

    /// <summary>The message contract has not been registered for durable serialization.</summary>
    ContractNotRegistered = 2,

    /// <summary>The durable store failed while evaluating or persisting the admission.</summary>
    StoreFailure = 3,
}
