using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Exposes the Azure Service Bus subscriptions applied to a receive endpoint.</summary>
public interface IServiceBusConsumeTopology :
    IConsumeTopology
{
    /// <summary>Gets the consume topology for a message contract.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <returns>The message-specific consume topology.</returns>
    new IServiceBusMessageConsumeTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Applies all configured subscriptions to a receive-endpoint topology builder.</summary>
    /// <param name="builder">The topology builder receiving the subscriptions.</param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
