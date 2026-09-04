using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a service bus message send topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ServiceBusMessageSendTopology<TMessage> :
    MessageSendTopology<TMessage>,
    IServiceBusMessageSendTopologyConfigurator<TMessage>
    where TMessage : class
{
}
