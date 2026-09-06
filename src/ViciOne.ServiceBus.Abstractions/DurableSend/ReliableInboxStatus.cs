namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Durable processing state for an inbox record.</summary>
public enum ReliableInboxStatus
{
    /// <summary>The record is owned by a fenced consumer attempt.</summary>
    Processing = 0,

    /// <summary>The consumer transaction committed successfully.</summary>
    Consumed = 1,

    /// <summary>A durable retry is waiting for its due time.</summary>
    RetryScheduled = 2,

    /// <summary>The record requires an operator decision.</summary>
    Quarantined = 3,

    /// <summary>An operator retained the record as deliberately abandoned.</summary>
    Abandoned = 4,
}
