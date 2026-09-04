using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.RabbitMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.LocalIntegration.Tests;

public sealed class RabbitMqBrokerContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-BROKER", "exclusive-queue-refusal-is-peer-405-and-recoverable")]
    public async Task ExclusiveQueue_RefusesOnlyTheContenderWithPeer405ThenTransfersOwnershipAsync()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("exclusive");
        string queue = fixture.Name("owned");
        ConnectionFactory factory = fixture.CreateConnectionFactory();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await using IConnection holder = await factory.CreateConnectionAsync(cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        await using IChannel holderChannel = await holder.CreateChannelAsync(cancellationToken: cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        await holderChannel.QueueDeclareAsync(
                queue,
                durable: false,
                exclusive: true,
                autoDelete: true,
                arguments: null,
                cancellationToken: cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);

        await using IConnection contender = await factory.CreateConnectionAsync(cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        IChannel refusedChannel = await contender.CreateChannelAsync(cancellationToken: cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        OperationInterruptedException refusal = await Assert.ThrowsAsync<OperationInterruptedException>(() =>
            refusedChannel.QueueDeclareAsync(
                    queue,
                    durable: false,
                    exclusive: true,
                    autoDelete: true,
                    arguments: null,
                    cancellationToken: cancellationToken));
        await refusedChannel.DisposeAsync();

        Assert.NotNull(refusal.ShutdownReason);
        Assert.Equal(ShutdownInitiator.Peer, refusal.ShutdownReason!.Initiator);
        Assert.Equal((ushort)405, refusal.ShutdownReason.ReplyCode);
        Assert.Contains("RESOURCE_LOCKED", refusal.ShutdownReason.ReplyText, StringComparison.Ordinal);
        Assert.True(contender.IsOpen);

        await holderChannel.CloseAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        await holder.CloseAsync(200, "exclusive-owner-complete", cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        await fixture.WaitUntilQueueIsReleasedAsync(queue, cancellationToken);
        await using IChannel successor = await contender.CreateChannelAsync(cancellationToken: cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        QueueDeclareOk acquired = await successor.QueueDeclareAsync(
                queue,
                durable: false,
                exclusive: true,
                autoDelete: true,
                arguments: null,
                cancellationToken: cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        Assert.Equal(queue, acquired.QueueName);
    }
}
