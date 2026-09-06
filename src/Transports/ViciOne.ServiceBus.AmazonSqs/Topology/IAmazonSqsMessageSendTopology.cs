namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Describes Amazon SQS send-topology conventions for a message type.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IAmazonSqsMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
}
