using System.Collections.Concurrent;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

public sealed class PostgreSqlPublishAndPurgeTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0066", "postgresql-native-owner")]
    public async Task UnsubscribedPublish_LeavesNeitherMessageNorDeliveryWhileSubscribedControlArrivesAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "unsubscribed-publish",
            cancellationToken);
        string queueName = fixture.Name("subscribed-input");
        var subscribed = NewObservation<ConsumeContext<SubscribedMessage>>();
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint => endpoint.Handler<SubscribedMessage>(context =>
            {
                subscribed.TrySetResult(context);
                return Task.CompletedTask;
            }));
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            Guid subscribedId = Guid.NewGuid();
            await bus.PublishAsync(
                    new SubscribedMessage("control"),
                    context => context.MessageId = subscribedId,
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<SubscribedMessage> control = await subscribed.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Guid orphanId = Guid.NewGuid();
            await bus.PublishAsync(
                    new UnsubscribedMessage("orphan"),
                    context => context.MessageId = orphanId,
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await using NpgsqlConnection connection = fixture.CreateConnection();
            await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(subscribedId, control.MessageId);
            Assert.Equal(0, await connection.MessageCountAsync(fixture.Schema, orphanId, cancellationToken));
            Assert.Equal(0, await connection.DeliveryCountForMessageAsync(fixture.Schema, orphanId, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0068", "postgresql-native-owner")]
    public async Task PurgeOnStartup_RemovesExistingDeliveryAndThenConsumesOnlyTheNewMessageAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "purge-on-startup",
            cancellationToken);
        string queueName = fixture.Name("purge-input");
        await StartAndStopEndpointAsync(fixture, queueName, cancellationToken);

        IBusControl sender = SqlBusFactory.Create(fixture.ConfigureHost);
        bool senderStarted = false;
        try
        {
            await sender.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            senderStarted = true;
            ISendEndpoint endpoint = await sender.GetSendEndpointAsync(new Uri($"queue:{queueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.SendAsync(new PurgeMessage("before"), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        finally
        {
            if (senderStarted)
                await sender.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        await using (NpgsqlConnection before = fixture.CreateConnection())
        {
            await before.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(1, await before.DeliveryCountAsync(fixture.Schema, queueName, 1, cancellationToken));
        }

        var values = new ConcurrentQueue<string>();
        var delivered = NewObservation<ConsumeContext<PurgeMessage>>();
        IBusControl receiver = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.PurgeOnStartup = true;
                endpoint.ConfigureConsumeTopology = false;
                endpoint.Handler<PurgeMessage>(context =>
                {
                    values.Enqueue(context.Message.Value);
                    if (context.Message.Value == "after")
                        delivered.TrySetResult(context);
                    return Task.CompletedTask;
                });
            });
        });
        bool receiverStarted = false;

        try
        {
            await receiver.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            receiverStarted = true;
            ISendEndpoint endpoint = await receiver.GetSendEndpointAsync(new Uri($"queue:{queueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.SendAsync(new PurgeMessage("after"), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await delivered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        finally
        {
            if (receiverStarted)
                await receiver.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        await using NpgsqlConnection after = fixture.CreateConnection();
        await after.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);

        Assert.Equal(["after"], values);
        Assert.Equal(0, await after.DeliveryCountAsync(fixture.Schema, queueName, 1, cancellationToken));
    }

    private static async Task StartAndStopEndpointAsync(
        PostgreSqlTestDatabase fixture,
        string queueName,
        CancellationToken cancellationToken)
    {
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.PurgeOnStartup = false;
                endpoint.ConfigureConsumeTopology = false;
                endpoint.Handler<PurgeMessage>(_ => Task.CompletedTask);
            });
        });
        await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record SubscribedMessage(string Value);
    private sealed record UnsubscribedMessage(string Value);
    private sealed record PurgeMessage(string Value);
}
