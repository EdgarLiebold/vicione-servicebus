namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.Helpers;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class PostgreSqlNotificationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("OBL-R0-SQL-0104", "postgresql-native-owner")]
    public async Task WaitingReceiver_IsReleasedByNotificationOrItsConfiguredPollingInterval(bool notificationsEnabled)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "notification-polling",
            cancellationToken);
        string queueName = fixture.Name(notificationsEnabled ? "notified" : "polled");
        var delivered = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        int entries = 0;
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.PollingInterval = notificationsEnabled
                    ? fixture.OperationTimeout + TimeSpan.FromSeconds(10)
                    : TimeSpan.FromSeconds(1);
                endpoint.Handler<NotificationMessage>(context =>
                {
                    Interlocked.Increment(ref entries);
                    delivered.TrySetResult(context.Message.Id);
                    return Task.CompletedTask;
                });
            });
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await using NpgsqlConnection inspection = fixture.CreateConnection();
            await inspection.OpenWithin(fixture.OperationTimeout, cancellationToken);
            long queueId = await QueueId(inspection, fixture.Schema, queueName, cancellationToken);
            string channel = $"{NotifyChannel.SanitizeSchemaName(fixture.Schema)}_msg_{queueId}";
            await WaitUntilReceiverListens(
                inspection,
                channel,
                fixture.OperationTimeout,
                cancellationToken);

            if (!notificationsEnabled)
            {
                await using var disable = new NpgsqlCommand(
                    $"ALTER TABLE \"{fixture.Schema}\".message_delivery DISABLE TRIGGER message_delivery_notify_trigger",
                    inspection);
                await disable.ExecuteNonQueryAsync(cancellationToken);
                Assert.Equal("D", await TriggerState(inspection, fixture.Schema, cancellationToken));
            }

            Guid id = Guid.NewGuid();
            ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.Send(new NotificationMessage(id), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(id, await delivered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Assert.Equal(1, entries);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static async Task<long> QueueId(
        NpgsqlConnection connection,
        string schema,
        string queueName,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT id FROM \"{schema}\".queue WHERE name = @queue AND type = 1",
            connection);
        command.Parameters.AddWithValue("queue", queueName);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task WaitUntilReceiverListens(
        NpgsqlConnection connection,
        string channel,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        string expectedQuery = $"LISTEN \"{channel}\"";
        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM pg_stat_activity "
            + "WHERE datname = current_database() AND state = 'idle' AND query = @query",
            connection);
        command.Parameters.AddWithValue("query", expectedQuery);
        while (true)
        {
            timeoutSource.Token.ThrowIfCancellationRequested();
            if (Convert.ToInt64(await command.ExecuteScalarAsync(timeoutSource.Token)) == 1)
                return;
            await Task.Yield();
        }
    }

    private static async Task<string> TriggerState(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string text = """
            SELECT t.tgenabled::text
              FROM pg_trigger t
              JOIN pg_class c ON c.oid = t.tgrelid
              JOIN pg_namespace n ON n.oid = c.relnamespace
             WHERE n.nspname = @schema
               AND c.relname = 'message_delivery'
               AND t.tgname = 'message_delivery_notify_trigger'
            """;
        await using var command = new NpgsqlCommand(text, connection);
        command.Parameters.AddWithValue("schema", schema);
        return Assert.IsType<string>(await command.ExecuteScalarAsync(cancellationToken));
    }

    public sealed record NotificationMessage(Guid Id);
}
