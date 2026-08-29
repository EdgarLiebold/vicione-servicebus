namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

using System.Collections.Concurrent;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class PostgreSqlBuiltInRedeliveryTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0059", "postgresql-native-owner")]
    public async Task BuiltInRedelivery_PersistsThreeOneSecondSchedulesBeforeTheFinalFault()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "built-in-redelivery",
            cancellationToken);
        await using NpgsqlConnection inspection = fixture.CreateConnection();
        await inspection.OpenWithin(fixture.OperationTimeout, cancellationToken);
        await CreateRedeliveryAudit(inspection, fixture.Schema, cancellationToken);
        string inputQueue = fixture.Name("faulting-input");
        string faultQueue = fixture.Name("fault-output");
        var attempts = new ConcurrentQueue<int>();
        var faulted = new TaskCompletionSource<ConsumeContext<Fault<RedeliveryMessage>>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(inputQueue, endpoint =>
            {
                endpoint.UseDelayedRedelivery(redelivery =>
                    redelivery.Interval(3, TimeSpan.FromSeconds(1)));
                endpoint.Handler<RedeliveryMessage>(context =>
                {
                    attempts.Enqueue(context.GetRedeliveryCount());
                    throw new ExpectedRedeliveryException(context.Message.Id);
                });
            });
            configurator.ReceiveEndpoint(faultQueue, endpoint =>
                endpoint.Handler<Fault<RedeliveryMessage>>(context =>
                {
                    faulted.TrySetResult(context);
                    return Task.CompletedTask;
                }));
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            var message = new RedeliveryMessage(Guid.NewGuid());
            ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{inputQueue}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.Send(message, cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<Fault<RedeliveryMessage>> fault = await faulted.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(message.Id, fault.Message.Message.Id);
            Assert.Contains(fault.Message.Exceptions, exception =>
                exception.ExceptionType == typeof(ExpectedRedeliveryException).FullName);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        (int[] deliveryAttempts, TimeSpan[] delays) = await RedeliveryAudit(
            inspection,
            fixture.Schema,
            cancellationToken);
        Assert.Equal([0, 1, 2, 3], attempts);
        Assert.Equal([1, 2, 3], deliveryAttempts);
        Assert.Equal(
            [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)],
            delays);
        Assert.Equal(0, await inspection.DeliveryCount(fixture.Schema, inputQueue, 1, cancellationToken));
        Assert.Equal(1, await inspection.DeliveryCount(fixture.Schema, inputQueue, 2, cancellationToken));
    }

    private static async Task CreateRedeliveryAudit(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        string text = $"""
            CREATE TABLE "{schema}".redelivery_audit
            (
                audit_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                delivery_attempt integer not null,
                scheduled_delay interval not null
            );

            CREATE FUNCTION "{schema}".capture_redelivery_schedule()
                RETURNS trigger
                LANGUAGE plpgsql
            AS
            $$
            BEGIN
                IF OLD.lock_id IS NOT NULL
                   AND NEW.lock_id IS NULL
                   AND NEW.queue_id = OLD.queue_id
                   AND NEW.enqueue_time > CURRENT_TIMESTAMP THEN
                    INSERT INTO "{schema}".redelivery_audit(delivery_attempt, scheduled_delay)
                    VALUES (NEW.delivery_count, NEW.enqueue_time - CURRENT_TIMESTAMP);
                END IF;
                RETURN NEW;
            END;
            $$;

            CREATE TRIGGER capture_redelivery_schedule
                AFTER UPDATE ON "{schema}".message_delivery
                FOR EACH ROW EXECUTE FUNCTION "{schema}".capture_redelivery_schedule();
            """;
        await using var command = new NpgsqlCommand(text, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<(int[] DeliveryAttempts, TimeSpan[] Delays)> RedeliveryAudit(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT delivery_attempt, scheduled_delay FROM \"{schema}\".redelivery_audit ORDER BY audit_id",
            connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        var attempts = new List<int>();
        var delays = new List<TimeSpan>();
        while (await reader.ReadAsync(cancellationToken))
        {
            attempts.Add(reader.GetInt32(0));
            delays.Add(reader.GetTimeSpan(1));
        }
        return (attempts.ToArray(), delays.ToArray());
    }

    private sealed record RedeliveryMessage(Guid Id);

    private sealed class ExpectedRedeliveryException(Guid id)
        : Exception($"Expected redelivery failure for {id:D}.");
}
