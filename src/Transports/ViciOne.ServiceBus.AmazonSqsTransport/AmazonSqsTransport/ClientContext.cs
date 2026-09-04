using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqsTransport.Topology;

namespace ViciOne.ServiceBus.AmazonSqsTransport;

public interface ClientContext :
    PipeContext
{
    ConnectionContext ConnectionContext { get; }

    Task<TopicInfo> CreateTopicAsync(Topology.Topic topic, CancellationToken cancellationToken);

    Task<QueueInfo> CreateQueueAsync(Queue queue, CancellationToken cancellationToken);

    Task<bool> CreateQueueSubscriptionAsync(Topology.Topic topic, Queue queue, CancellationToken cancellationToken);

    Task DeleteTopicAsync(Topology.Topic topic, CancellationToken cancellationToken);

    Task DeleteQueueAsync(Queue queue, CancellationToken cancellationToken);

    Task PublishAsync(string topicName, PublishBatchRequestEntry request, CancellationToken cancellationToken);

    Task SendMessageAsync(string queueName, SendMessageBatchRequestEntry request, CancellationToken cancellationToken);

    Task DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken cancellationToken);

    Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken);

    Task<IList<Message>> ReceiveMessagesAsync(string queueName, int messageLimit, int waitTime, CancellationToken cancellationToken);

    Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken);

    Task ChangeMessageVisibilityAsync(string queueUrl, string receiptHandle, int seconds, CancellationToken cancellationToken);
}
