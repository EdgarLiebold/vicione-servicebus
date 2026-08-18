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
        /// Makes the broker hold this destination, rather than only resolving its name on the client.
        /// <para>
        /// Measured against the pinned fixture: SessionUtil.GetTopic returns an NMS destination object
        /// and the broker's topic list stays empty, so a topology deployed with it is deployed nowhere.
        /// Opening a producer for the destination and closing it again is what the broker records. A
        /// consumer would do it too, and is not used: it would create a subscription with delivery
        /// semantics, where a deployment is meant to announce the destination and nothing else.
        /// </para>
        /// </summary>
        Task Materialize(IDestination destination);

        Task<IDestination> GetDestination(string destinationName, DestinationType destinationType);

        Task<IMessageConsumer> CreateMessageConsumer(IDestination destination, string selector, bool noLocal, string consumerName = null, bool shared = false);

        Task SendAsync(IDestination destination, IMessage message, CancellationToken cancellationToken);

        IBytesMessage CreateBytesMessage(byte[] content);

        ITextMessage CreateTextMessage(string content);

        IMessage CreateMessage();

        Task DeleteTopic(string topicName);

        Task DeleteQueue(string queueName);

        IDestination GetTemporaryDestination(string name);
    }
}
