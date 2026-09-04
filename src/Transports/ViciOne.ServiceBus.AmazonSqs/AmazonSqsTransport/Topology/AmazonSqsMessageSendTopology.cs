using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides an amazon sqs message send topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class AmazonSqsMessageSendTopology<TMessage> :
    MessageSendTopology<TMessage>,
    IAmazonSqsMessageSendTopologyConfigurator<TMessage>
    where TMessage : class
{
}
