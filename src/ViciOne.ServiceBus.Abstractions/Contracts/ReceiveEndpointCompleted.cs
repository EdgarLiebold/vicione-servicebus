namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Describes a receive endpoint after its transport has completed.</summary>
public interface ReceiveEndpointCompleted :
    ReceiveEndpointEvent
{
    /// <summary>Gets the number of deliveries accepted by the endpoint's transport.</summary>
    long DeliveryCount { get; }

    /// <summary>Gets the deliveries still executing when transport completion was observed.</summary>
    long ConcurrentDeliveryCount { get; }
}
