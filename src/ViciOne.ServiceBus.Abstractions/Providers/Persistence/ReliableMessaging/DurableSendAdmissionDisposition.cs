namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Outcome of idempotent durable-send admission.</summary>
public enum DurableSendAdmissionDisposition
{
    /// <summary>A new durable intent was committed.</summary>
    Accepted = 0,

    /// <summary>The exact same durable intent was already stored and is still eligible for delivery.</summary>
    AlreadyAccepted = 1,

    /// <summary>The exact same durable intent already exists in terminal quarantine and requires an operator decision.</summary>
    AlreadyQuarantined = 2,
}
