namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Describes an Azure Service Bus subscription that routes a topic to a queue.</summary>
public interface QueueSubscription
{
    /// <summary>Gets the source topic.</summary>
    Topic Source { get; }

    /// <summary>Gets the forwarding destination queue.</summary>
    Queue Destination { get; }

    /// <summary>Gets the subscription that performs the forwarding.</summary>
    Subscription Subscription { get; }
}
