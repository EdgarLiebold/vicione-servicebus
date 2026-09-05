using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

public sealed class PostgreSqlScheduleCancellationTests
{
    private static readonly TimeSpan ScheduledDelay = TimeSpan.FromSeconds(5);

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0062", "postgresql-native-owner")]
    public Task ConsumeContextCancellation_DeletesThePersistedFutureDeliveryAsync() =>
        AssertCancellationAsync(cancelInsideConsumer: true);

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0064", "postgresql-native-owner")]
    public Task CallerCancellation_DeletesThePersistedFutureDeliveryAsync() =>
        AssertCancellationAsync(cancelInsideConsumer: false);

    private static async Task AssertCancellationAsync(bool cancelInsideConsumer)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            cancelInsideConsumer ? "cancel-consumer" : "cancel-caller",
            cancellationToken);
        string queueName = fixture.Name("schedule-input");
        var scheduledReady = new TaskCompletionSource<ScheduleState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowHandler = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handlerFinished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        int secondMessageEntries = 0;
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ConfigureSqlMessageScheduler();
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Handler<ScheduleRequest>(async context =>
                {
                    ScheduledMessage<ScheduledPayload> scheduled = await context.Advanced().ScheduleSendAsync(
                        ScheduledDelay,
                        new ScheduledPayload(context.Message.Id),
                        context.CancellationToken);
                    MessageSchedulerContext scheduler = context.GetPayload<MessageSchedulerContext>();
                    scheduledReady.TrySetResult(new ScheduleState(scheduled, scheduler));
                    await allowHandler.Task;
                    if (cancelInsideConsumer)
                        await context.Advanced().CancelScheduledSendAsync(scheduled);
                    handlerFinished.TrySetResult(true);
                });
                endpoint.Handler<ScheduledPayload>(_ =>
                {
                    Interlocked.Increment(ref secondMessageEntries);
                    return Task.CompletedTask;
                });
            });
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            Guid id = Guid.NewGuid();
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.SendAsync(new ScheduleRequest(id), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ScheduleState state = await scheduledReady.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await using NpgsqlConnection inspection = fixture.CreateConnection();
            await inspection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(new PersistedSchedule(1, 1, 1), await PersistedStateAsync(
                inspection,
                fixture.Schema,
                state.Scheduled.TokenId,
                cancellationToken));

            if (cancelInsideConsumer)
                allowHandler.TrySetResult(true);
            else
            {
                await state.Scheduler.CancelScheduledSendAsync(
                        state.Scheduled.Destination,
                        state.Scheduled.TokenId,
                        cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
                allowHandler.TrySetResult(true);
            }

            await handlerFinished.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(new PersistedSchedule(0, 0, 0), await PersistedStateAsync(
                inspection,
                fixture.Schema,
                state.Scheduled.TokenId,
                cancellationToken));

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Assert.Equal(0, secondMessageEntries);
        }
        finally
        {
            allowHandler.TrySetResult(true);
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static async Task<PersistedSchedule> PersistedStateAsync(
        NpgsqlConnection connection,
        string schema,
        Guid tokenId,
        CancellationToken cancellationToken)
    {
        string text = $"""
            SELECT COUNT(DISTINCT m.transport_message_id),
                   COUNT(d.message_delivery_id),
                   COUNT(d.message_delivery_id) FILTER (WHERE d.enqueue_time > clock_timestamp())
              FROM "{schema}".message m
              LEFT JOIN "{schema}".message_delivery d
                ON d.transport_message_id = m.transport_message_id
             WHERE m.scheduling_token_id = @tokenId
            """;
        await using var command = new NpgsqlCommand(text, connection);
        command.Parameters.AddWithValue("tokenId", tokenId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        Assert.True(await reader.ReadAsync(cancellationToken));
        var result = new PersistedSchedule(
            checked((int)reader.GetInt64(0)),
            checked((int)reader.GetInt64(1)),
            checked((int)reader.GetInt64(2)));
        Assert.False(await reader.ReadAsync(cancellationToken));
        return result;
    }

    private sealed record ScheduleState(
        ScheduledMessage<ScheduledPayload> Scheduled,
        MessageSchedulerContext Scheduler);

    private sealed record PersistedSchedule(int Messages, int Deliveries, int FutureDeliveries);

    private sealed record ScheduleRequest(Guid Id);

    private sealed record ScheduledPayload(Guid Id);
}
