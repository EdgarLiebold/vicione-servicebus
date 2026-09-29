using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Topology;

public sealed class RabbitMqQuorumTopologyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-BROKER-TOPOLOGY", "direct-queue-declaration-normalizes-all-quorum-flags")]
    public void QueueDeclare_WithQuorumArguments_ProducesValidBrokerFlags(bool withExpiration)
    {
        var arguments = new Dictionary<string, object?>
        {
            [RabbitMQ.Client.Headers.XQueueType] = "quorum",
        };
        if (withExpiration)
            arguments[RabbitMQ.Client.Headers.XExpires] = 60000L;

        var builder = new ReceiveEndpointBrokerTopologyBuilder();
        builder.QueueDeclare("bound-quorum", durable: false, autoDelete: true, exclusive: true, arguments);

        var queue = Assert.Single(builder.BuildBrokerTopology().Queues);
        Assert.Equal("bound-quorum", queue.QueueName);
        Assert.True(queue.Durable);
        Assert.False(queue.AutoDelete);
        Assert.False(queue.Exclusive);
        Assert.Equal("quorum", queue.QueueArguments[RabbitMQ.Client.Headers.XQueueType]);
        Assert.Equal(withExpiration, queue.QueueArguments.ContainsKey(RabbitMQ.Client.Headers.XExpires));
    }
}
