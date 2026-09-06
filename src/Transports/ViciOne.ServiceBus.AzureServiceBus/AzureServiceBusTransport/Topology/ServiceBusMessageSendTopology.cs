using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Holds Azure Service Bus send conventions for a message contract.</summary>
/// <typeparam name="TMessage">The sent message contract.</typeparam>
public class ServiceBusMessageSendTopology<TMessage> :
    MessageSendTopology<TMessage>,
    IServiceBusMessageSendTopologyConfigurator<TMessage>
    where TMessage : class
{
}
