namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Defines the operations required by receive endpoint completed.</summary>
public interface ReceiveEndpointCompleted :
    ReceiveEndpointEvent
{
    /// <summary>The number of messages delivered to the receive endpoint.</summary>
    long DeliveryCount { get; }

    /// <summary>The maximum concurrent messages delivery to the receive endpoint.</summary>
    long ConcurrentDeliveryCount { get; }
}
