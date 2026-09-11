namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Describes a receive endpoint after its transport has completed.</summary>
public interface ReceiveEndpointCompleted :
    ReceiveEndpointEvent
{
    /// <summary>Gets the number of deliveries accepted by the endpoint's transport.</summary>
    long DeliveryCount { get; }

    /// <summary>Gets the highest number of deliveries observed concurrently.</summary>
    int MaxConcurrentDeliveryCount { get; }
}
