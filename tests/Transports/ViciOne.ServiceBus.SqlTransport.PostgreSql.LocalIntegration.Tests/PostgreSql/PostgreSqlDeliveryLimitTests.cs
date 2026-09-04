using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

public sealed class PostgreSqlDeliveryLimitTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0092", "postgresql-native-owner")]
    public async Task ConfiguredMaxDeliveryCount_IsPersistedInsteadOfTheDatabaseDefault()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "delivery-limit",
            cancellationToken);
        string queueName = fixture.Name("limited-input");

        await DeclareQueue(fixture, queueName, maxDeliveryCount: 3, cancellationToken);
        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithin(fixture.OperationTimeout, cancellationToken);

        Assert.Equal(3, await QueueMaxDeliveryCount(connection, fixture.Schema, queueName, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0094", "postgresql-native-owner")]
    public async Task ExhaustedDelivery_IsExcludedFromFetchAndMovedByDeadLetterMaintenance()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "exhausted-delivery",
            cancellationToken);
        string queueName = fixture.Name("limited-input");
        await DeclareQueue(fixture, queueName, maxDeliveryCount: 3, cancellationToken);
        var exhausted = new LimitedMessage(Guid.NewGuid(), "exhausted");
        var control = new LimitedMessage(Guid.NewGuid(), "fetchable-control");
        await Send(fixture, queueName, [exhausted, control], cancellationToken);

        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithin(fixture.OperationTimeout, cancellationToken);
        int changed = await ExhaustDelivery(connection, fixture.Schema, exhausted.Id, cancellationToken);

        FetchedDelivery[] fetched = await FetchReadyDeliveries(
            connection,
            fixture.Schema,
            queueName,
            cancellationToken);

        Assert.Equal(1, changed);
        FetchedDelivery fetchedControl = Assert.Single(fetched);
        Assert.Equal(control.Id, fetchedControl.MessageId);
        Assert.DoesNotContain(fetched, item => item.MessageId == exhausted.Id);

        await DeleteFetchedDelivery(connection, fixture.Schema, fetchedControl, cancellationToken);
        long moved = await DeadLetterExhausted(connection, fixture.Schema, queueName, cancellationToken);

        Assert.Equal(1, moved);
        Assert.Equal(0, await connection.DeliveryCount(fixture.Schema, queueName, 1, cancellationToken));
        Assert.Equal(1, await connection.DeliveryCount(fixture.Schema, queueName, 3, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0116", "postgresql-native-owner")]
    public async Task RedeclaringQueueWithoutALimit_ResetsPriorConfiguredLimitToTen()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "redeclare-limit",
            cancellationToken);
        string queueName = fixture.Name("redeclared-input");

        await DeclareQueue(fixture, queueName, maxDeliveryCount: 3, cancellationToken);
        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithin(fixture.OperationTimeout, cancellationToken);
        Assert.Equal(3, await QueueMaxDeliveryCount(connection, fixture.Schema, queueName, cancellationToken));

        await DeclareQueue(fixture, queueName, maxDeliveryCount: null, cancellationToken);

        Assert.Equal(10, await QueueMaxDeliveryCount(connection, fixture.Schema, queueName, cancellationToken));
    }

    private static async Task DeclareQueue(
        PostgreSqlTestDatabase fixture,
        string queueName,
        int? maxDeliveryCount,
        CancellationToken cancellationToken)
    {
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.MaxDeliveryCount = maxDeliveryCount;
                endpoint.Handler<LimitedMessage>(_ => Task.CompletedTask);
            });
        });
        await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
    }

    private static async Task Send(
        PostgreSqlTestDatabase fixture,
        string queueName,
        IReadOnlyList<LimitedMessage> messages,
        CancellationToken cancellationToken)
    {
        IBusControl bus = SqlBusFactory.Create(fixture.ConfigureHost);
        bool started = false;
        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            foreach (LimitedMessage message in messages)
            {
                await endpoint.Send(
                        message,
                        context => context.MessageId = message.Id,
                        cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
            }
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static async Task<int> QueueMaxDeliveryCount(
        NpgsqlConnection connection,
        string schema,
        string queueName,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT max_delivery_count FROM \"{schema}\".queue WHERE name = @queueName AND type = 1",
            connection);
        command.Parameters.AddWithValue("queueName", queueName);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<int> ExhaustDelivery(
        NpgsqlConnection connection,
        string schema,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"UPDATE \"{schema}\".message_delivery d SET delivery_count = max_delivery_count "
            + $"FROM \"{schema}\".message m "
            + "WHERE m.transport_message_id = d.transport_message_id AND m.message_id = @messageId",
            connection);
        command.Parameters.AddWithValue("messageId", messageId);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<FetchedDelivery[]> FetchReadyDeliveries(
        NpgsqlConnection connection,
        string schema,
        string queueName,
        CancellationToken cancellationToken)
    {
        string commandText = $"SELECT message_id, message_delivery_id, lock_id FROM \"{schema}\".fetch_messages("
            + "@queueName, @consumerId, @lockId, @lockDuration, @fetchCount)";
        await using var command = new NpgsqlCommand(commandText, connection);
        command.Parameters.AddWithValue("queueName", queueName);
        command.Parameters.AddWithValue("consumerId", Guid.NewGuid());
        command.Parameters.AddWithValue("lockId", Guid.NewGuid());
        command.Parameters.AddWithValue("lockDuration", TimeSpan.FromMinutes(1));
        command.Parameters.AddWithValue("fetchCount", 10);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<FetchedDelivery>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new FetchedDelivery(reader.GetGuid(0), reader.GetInt64(1), reader.GetGuid(2)));
        return result.ToArray();
    }

    private static async Task DeleteFetchedDelivery(
        NpgsqlConnection connection,
        string schema,
        FetchedDelivery delivery,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT \"{schema}\".delete_message(@deliveryId, @lockId)",
            connection);
        command.Parameters.AddWithValue("deliveryId", delivery.DeliveryId);
        command.Parameters.AddWithValue("lockId", delivery.LockId);
        Assert.Equal(delivery.DeliveryId, Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)));
    }

    private static async Task<long> DeadLetterExhausted(
        NpgsqlConnection connection,
        string schema,
        string queueName,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT \"{schema}\".dead_letter_messages(@queueName, @messageCount)",
            connection);
        command.Parameters.AddWithValue("queueName", queueName);
        command.Parameters.AddWithValue("messageCount", 100);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private sealed record LimitedMessage(Guid Id, string Value);
    private sealed record FetchedDelivery(Guid MessageId, long DeliveryId, Guid LockId);
}
