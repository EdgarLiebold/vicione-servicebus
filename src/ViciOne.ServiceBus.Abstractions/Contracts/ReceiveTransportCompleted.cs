namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Describes the final delivery counters of a completed receive transport.</summary>
public interface ReceiveTransportCompleted :
    ReceiveTransportEvent
{
    /// <summary>Gets the number of deliveries accepted by the transport.</summary>
    long DeliveryCount { get; }

    /// <summary>Gets the highest number of deliveries observed concurrently.</summary>
    int MaxConcurrentDeliveryCount { get; }
}
