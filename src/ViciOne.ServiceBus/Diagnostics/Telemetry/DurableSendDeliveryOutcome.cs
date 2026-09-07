namespace ViciOne.ServiceBus.Diagnostics.Telemetry;

/// <summary>Identifies the durable delivery state recorded by telemetry.</summary>
internal enum DurableSendDeliveryOutcome
{
    /// <summary>The provider accepted the message or the durable intent had already completed.</summary>
    Delivered = 0,

    /// <summary>The durable intent was retained for another delivery attempt.</summary>
    RetryScheduled = 1,

    /// <summary>The durable intent was isolated after a terminal delivery failure.</summary>
    Quarantined = 2,

    /// <summary>The current delivery attempt stopped because its operation was canceled.</summary>
    Canceled = 3,

    /// <summary>The provider outcome could not be persisted to the durable store.</summary>
    StatePersistenceFailed = 4,

    /// <summary>The provider accepted the message and retirement awaits consumer confirmation.</summary>
    AwaitingConsumerCompletion = 5,
}
