using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AmazonSqsTransport.Topology;

namespace ViciOne.ServiceBus.AmazonSqsTransport;

public interface ConnectionContext :
    PipeContext
{
    /// <summary>
    /// The Amazon Connection
    /// </summary>
    IConnection Connection { get; }

    /// <summary>
    /// The Host Address for this connection
    /// </summary>
    Uri HostAddress { get; }

    IAmazonSqsBusTopology Topology { get; }

    Task<QueueInfo> GetQueueAsync(Queue queue, CancellationToken cancellationToken);
    Task<QueueInfo> GetQueueByNameAsync(string name, CancellationToken cancellationToken);
    Task<bool> RemoveQueueByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<TopicInfo> GetTopicAsync(Topic topic, CancellationToken cancellationToken);
    Task<TopicInfo> GetTopicByNameAsync(string name, CancellationToken cancellationToken);
    Task<bool> RemoveTopicByNameAsync(string name, CancellationToken cancellationToken = default);

    ClientContext CreateClientContext(CancellationToken cancellationToken);
}
