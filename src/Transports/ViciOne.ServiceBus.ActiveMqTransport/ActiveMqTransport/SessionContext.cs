using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMqTransport.Topology;

namespace ViciOne.ServiceBus.ActiveMqTransport;

public interface SessionContext :
    PipeContext
{
    ISession Session { get; }

    ConnectionContext ConnectionContext { get; }

    Task<ITopic> GetTopic(Topic topic);

    Task<IQueue> GetQueue(Queue queue);

    /// <summary>
    /// Makes the broker hold this topic, so that a deployed publish topology exists on the broker
    /// and not only in the client.
    /// <para>
    /// Resolving a topic name only creates an NMS destination object and does not materialize the
    /// topic on the broker. Opening and closing a producer materializes the destination without
    /// creating the subscription and delivery semantics that a consumer would introduce.
    /// </para>
    /// <para>
    /// The caller asks for the outcome. How the outcome is reached is a property of the session,
    /// and a filter that orchestrated the NMS steps itself would own an implementation detail it
    /// cannot see the consequences of.
    /// </para>
    /// </summary>
    Task EnsureTopicExists(Topic topic);

    Task<IDestination> GetDestination(string destinationName, DestinationType destinationType);

    Task<IMessageConsumer> CreateMessageConsumer(
        IDestination destination,
        string selector,
        bool noLocal,
        string consumerName = null,
        bool shared = false,
        bool durable = true);

    Task SendAsync(IDestination destination, IMessage message, CancellationToken cancellationToken);

    IBytesMessage CreateBytesMessage(byte[] content);

    ITextMessage CreateTextMessage(string content);

    IMessage CreateMessage();

    Task DeleteTopic(string topicName);

    Task DeleteQueue(string queueName);

    IDestination GetTemporaryDestination(string name);
}
