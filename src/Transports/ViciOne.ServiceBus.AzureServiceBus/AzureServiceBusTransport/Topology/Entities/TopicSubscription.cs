namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Describes an Azure Service Bus subscription that forwards from one topic to another.</summary>
public interface TopicSubscription
{
    /// <summary>Gets the source topic.</summary>
    Topic Source { get; }

    /// <summary>Gets the forwarding destination topic.</summary>
    Topic Destination { get; }

    /// <summary>Gets the subscription that performs the forwarding.</summary>
    Subscription Subscription { get; }
}
