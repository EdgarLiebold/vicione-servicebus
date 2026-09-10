namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Specifies the available sql queue type values.</summary>
public enum SqlQueueType
{
    /// <summary>Indicates queue.</summary>
    Queue = 1,
    /// <summary>Indicates error queue.</summary>
    ErrorQueue = 2,
    /// <summary>Indicates dead letter queue.</summary>
    DeadLetterQueue = 3
}
