namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Describes Amazon SNS subscription topology used to consume a message type from Amazon SQS.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IAmazonSqsMessageConsumeTopology<TMessage> :
    IMessageConsumeTopology<TMessage>
    where TMessage : class
{
}
