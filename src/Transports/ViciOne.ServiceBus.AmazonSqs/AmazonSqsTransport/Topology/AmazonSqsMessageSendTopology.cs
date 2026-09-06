using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Represents Amazon SQS send-topology conventions for a message type.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class AmazonSqsMessageSendTopology<TMessage> :
    MessageSendTopology<TMessage>,
    IAmazonSqsMessageSendTopologyConfigurator<TMessage>
    where TMessage : class
{
}
