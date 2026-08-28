namespace ViciOne.ServiceBus.ActiveMqTransport
{
    using System.Threading;
    using System.Threading.Tasks;
    using Apache.NMS;
    using Topology;


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
        /// Measured against the pinned fixture: resolving a topic name returns an NMS destination
        /// object while the broker's topic list stays empty, so a topology deployed that way is
        /// deployed nowhere. What the broker records is a producer opened for the destination and
        /// closed again. A consumer would do it too and is not used: it would create a subscription
        /// with delivery semantics, where a deployment announces the destination and nothing else.
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
}
