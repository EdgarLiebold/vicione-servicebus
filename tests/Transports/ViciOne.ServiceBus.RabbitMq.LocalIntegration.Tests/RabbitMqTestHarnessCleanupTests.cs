using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.RabbitMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.LocalIntegration.Tests;

public sealed class RabbitMqTestHarnessCleanupTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-TEST-HARNESS", "real-broker-dedicated-vhost-cleanup")]
    public async Task CleanAsync_RemovesEveryUserEntityFromADedicatedRealVirtualHostAsync()
    {
        using RabbitMqBroker broker = RabbitMqBroker.Create("harness-clean");
        string virtualHost = broker.Name("vhost");
        string queueName = broker.Name("queue");
        string exchangeName = broker.Name("exchange");
        using var harness = broker.CreateTestHarness(virtualHost);
        using CancellationTokenSource timeout = broker.OperationCancellation();

        await harness.RecreateVirtualHostAsync(timeout.Token);
        try
        {
            RabbitMqHostSettings settings = harness.GetHostSettings();
            ConnectionFactory factory = settings.GetConnectionFactory();
            await using IConnection connection = await factory.CreateConnectionAsync(timeout.Token)
                .WaitAsync(broker.OperationTimeout, timeout.Token);
            await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: timeout.Token)
                .WaitAsync(broker.OperationTimeout, timeout.Token);
            await channel.ExchangeDeclareAsync(
                    exchangeName,
                    ExchangeType.Fanout,
                    durable: false,
                    autoDelete: false,
                    arguments: null,
                    noWait: false,
                    cancellationToken: timeout.Token)
                .WaitAsync(broker.OperationTimeout, timeout.Token);
            await channel.QueueDeclareAsync(
                    queueName,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    noWait: false,
                    cancellationToken: timeout.Token)
                .WaitAsync(broker.OperationTimeout, timeout.Token);

            var customCleanupCalls = 0;
            CancellationToken observedCleanupToken = default;
            harness.CleanupVirtualHostAsync = (cleanupChannel, cancellationToken) =>
            {
                Assert.True(cleanupChannel.IsOpen);
                observedCleanupToken = cancellationToken;
                Interlocked.Increment(ref customCleanupCalls);
                return Task.CompletedTask;
            };

            await harness.CleanAsync(timeout.Token).WaitAsync(broker.OperationTimeout, timeout.Token);

            Assert.Equal(1, Volatile.Read(ref customCleanupCalls));
            Assert.Equal(timeout.Token, observedCleanupToken);
            Assert.False(harness.CleanVirtualHostOnStart);
            Assert.False(await broker.VirtualHostQueueExistsAsync(virtualHost, queueName, timeout.Token));
            Assert.False(await broker.VirtualHostExchangeExistsAsync(virtualHost, exchangeName, timeout.Token));
        }
        finally
        {
            await broker.DeleteVirtualHostAsync(virtualHost, timeout.Token);
        }
    }
}
