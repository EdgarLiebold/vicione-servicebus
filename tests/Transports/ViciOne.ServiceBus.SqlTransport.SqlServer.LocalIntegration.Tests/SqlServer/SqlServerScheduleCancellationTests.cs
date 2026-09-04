using Microsoft.Data.SqlClient;
using ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.SqlServer;

public sealed class SqlServerScheduleCancellationTests
{
    private static readonly TimeSpan ScheduledDelay = TimeSpan.FromSeconds(5);

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0063", "sqlserver-native-owner")]
    public Task ConsumeContextCancellation_DeletesThePersistedFutureDelivery() =>
        AssertCancellation(cancelInsideConsumer: true);

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0065", "sqlserver-native-owner")]
    public Task CallerCancellation_DeletesThePersistedFutureDelivery() =>
        AssertCancellation(cancelInsideConsumer: false);

    private static async Task AssertCancellation(bool cancelInsideConsumer)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
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
            configurator.UseSqlMessageScheduler();
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Handler<ScheduleRequest>(async context =>
                {
                    ScheduledMessage<ScheduledPayload> scheduled = await context.ScheduleSend(
                        ScheduledDelay,
                        new ScheduledPayload(context.Message.Id),
                        context.CancellationToken);
                    MessageSchedulerContext scheduler = context.GetPayload<MessageSchedulerContext>();
                    scheduledReady.TrySetResult(new ScheduleState(scheduled, scheduler));
                    await allowHandler.Task;
                    if (cancelInsideConsumer)
                        await context.CancelScheduledSend(scheduled);
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
            ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.Send(new ScheduleRequest(id), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ScheduleState state = await scheduledReady.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await using SqlConnection inspection = fixture.CreateConnection();
            await inspection.OpenWithin(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(new PersistedSchedule(1, 1, 1), await PersistedState(
                inspection,
                fixture.Schema,
                state.Scheduled.TokenId,
                cancellationToken));

            if (cancelInsideConsumer)
                allowHandler.TrySetResult(true);
            else
            {
                await state.Scheduler.CancelScheduledSend(
                        state.Scheduled.Destination,
                        state.Scheduled.TokenId,
                        cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
                allowHandler.TrySetResult(true);
            }

            await handlerFinished.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(new PersistedSchedule(0, 0, 0), await PersistedState(
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

    private static async Task<PersistedSchedule> PersistedState(
        SqlConnection connection,
        string schema,
        Guid tokenId,
        CancellationToken cancellationToken)
    {
        string text = $"""
            SELECT COUNT(DISTINCT m.TransportMessageId),
                   COUNT(d.MessageDeliveryId),
                   COUNT(CASE WHEN d.EnqueueTime > SYSUTCDATETIME() THEN 1 END)
              FROM [{schema}].[Message] m
              LEFT JOIN [{schema}].[MessageDelivery] d
                ON d.TransportMessageId = m.TransportMessageId
             WHERE m.SchedulingTokenId = @tokenId
            """;
        await using var command = new SqlCommand(text, connection);
        command.Parameters.AddWithValue("tokenId", tokenId);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        Assert.True(await reader.ReadAsync(cancellationToken));
        var result = new PersistedSchedule(
            Convert.ToInt32(reader.GetValue(0)),
            Convert.ToInt32(reader.GetValue(1)),
            Convert.ToInt32(reader.GetValue(2)));
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
