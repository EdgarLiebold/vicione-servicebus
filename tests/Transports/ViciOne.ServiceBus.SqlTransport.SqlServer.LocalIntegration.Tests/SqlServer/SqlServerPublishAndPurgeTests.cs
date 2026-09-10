using System.Collections.Concurrent;
using System.Data;
using Microsoft.Data.SqlClient;
using ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.SqlServer;

public sealed class SqlServerPublishAndPurgeTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0067", "sqlserver-native-owner")]
    public async Task UnsubscribedPublish_LeavesNeitherMessageNorDeliveryWhileSubscribedControlArrivesAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
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
            await using SqlConnection connection = fixture.CreateConnection();
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
    [RequirementCoverage("OBL-R0-SQL-0069", "sqlserver-native-owner")]
    public async Task PurgeOnStartup_RemovesExistingDeliveryAndThenConsumesOnlyTheNewMessageAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
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

        await using (SqlConnection before = fixture.CreateConnection())
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

        await using SqlConnection after = fixture.CreateConnection();
        await after.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);

        Assert.Equal(["after"], values);
        Assert.Equal(0, await after.DeliveryCountAsync(fixture.Schema, queueName, 1, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQLSERVER-PURGE-ISOLATION", "primary-purge-preserves-error-and-dead-letter-deliveries")]
    public async Task PurgeQueue_RemovesOnlyPrimaryDeliveriesAndPreservesErrorAndDeadLetterRowsAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "purge-isolation",
            cancellationToken);
        string queueName = fixture.Name("isolated-input");
        await StartAndStopEndpointAsync(fixture, queueName, cancellationToken);
        await using SqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        Guid primaryMessageId = Guid.NewGuid();
        Guid errorMessageId = Guid.NewGuid();
        Guid deadLetterMessageId = Guid.NewGuid();
        _ = await SendDirectAsync(connection, fixture.Schema, queueName, primaryMessageId, cancellationToken);
        long errorDeliveryId = await SendDirectAsync(connection, fixture.Schema, queueName, errorMessageId, cancellationToken);
        long deadLetterDeliveryId = await SendDirectAsync(connection, fixture.Schema, queueName, deadLetterMessageId, cancellationToken);
        await MoveDeliveryToQueueTypeAsync(connection, fixture.Schema, queueName, errorDeliveryId, 2, cancellationToken);
        await MoveDeliveryToQueueTypeAsync(connection, fixture.Schema, queueName, deadLetterDeliveryId, 3, cancellationToken);

        long purged = await PurgeQueueAsync(connection, fixture.Schema, queueName, cancellationToken);

        Assert.Equal(1, purged);
        Assert.Equal(0, await connection.DeliveryCountAsync(fixture.Schema, queueName, 1, cancellationToken));
        Assert.Equal(1, await connection.DeliveryCountAsync(fixture.Schema, queueName, 2, cancellationToken));
        Assert.Equal(1, await connection.DeliveryCountAsync(fixture.Schema, queueName, 3, cancellationToken));
        Assert.Equal(0, await connection.MessageCountAsync(fixture.Schema, primaryMessageId, cancellationToken));
        Assert.Equal(1, await connection.MessageCountAsync(fixture.Schema, errorMessageId, cancellationToken));
        Assert.Equal(1, await connection.MessageCountAsync(fixture.Schema, deadLetterMessageId, cancellationToken));
    }

    private static async Task StartAndStopEndpointAsync(
        SqlServerTestDatabase fixture,
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

    private static async Task<long> SendDirectAsync(
        SqlConnection connection,
        string schema,
        string queueName,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand($"[{schema}].[SendMessage]", connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        command.Parameters.AddWithValue("entityName", queueName);
        command.Parameters.AddWithValue("transportMessageId", Guid.NewGuid());
        command.Parameters.AddWithValue("messageId", messageId);
        command.Parameters.AddWithValue("sentTime", DateTimeOffset.UtcNow);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task MoveDeliveryToQueueTypeAsync(
        SqlConnection connection,
        string schema,
        string queueName,
        long deliveryId,
        int queueType,
        CancellationToken cancellationToken)
    {
        string text = $"UPDATE [{schema}].[MessageDelivery] SET QueueId = "
            + $"(SELECT Id FROM [{schema}].[Queue] WHERE Name = @queueName AND Type = @queueType) "
            + "WHERE MessageDeliveryId = @deliveryId";
        await using var command = new SqlCommand(text, connection);
        command.Parameters.AddWithValue("queueName", queueName);
        command.Parameters.AddWithValue("queueType", queueType);
        command.Parameters.AddWithValue("deliveryId", deliveryId);
        Assert.Equal(1, await command.ExecuteNonQueryAsync(cancellationToken));
    }

    private static async Task<long> PurgeQueueAsync(
        SqlConnection connection,
        string schema,
        string queueName,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand($"[{schema}].[PurgeQueue]", connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        command.Parameters.AddWithValue("queueName", queueName);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record SubscribedMessage(string Value);
    private sealed record UnsubscribedMessage(string Value);
    private sealed record PurgeMessage(string Value);
}
