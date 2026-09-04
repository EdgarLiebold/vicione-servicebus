using Microsoft.Data.SqlClient;
using ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.SqlServer;

public sealed class SqlServerLockRenewalTests
{
    private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(1);

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0085", "sqlserver-native-owner")]
    public async Task SlowConsumer_RenewsItsProviderLockThreeTimesAndCompletesExactlyOnce()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "lock-renewal",
            cancellationToken);
        await using SqlConnection inspection = fixture.CreateConnection();
        await inspection.OpenWithin(fixture.OperationTimeout, cancellationToken);
        await CreateRenewalAudit(inspection, fixture.Schema, cancellationToken);
        var state = new ConsumerState();
        string queueName = fixture.Name("renewed-input");
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.LockDuration = LockDuration;
                endpoint.MaxLockDuration = TimeSpan.FromSeconds(10);
                endpoint.PollingInterval = TimeSpan.FromMilliseconds(200);
                endpoint.ConcurrentMessageLimit = 2;
                endpoint.Handler<LockMessage>(async _ =>
                {
                    Interlocked.Increment(ref state.Entries);
                    state.FirstStarted.TrySetResult();
                    await state.Release.Task;
                    state.Completed.TrySetResult();
                });
            });
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.Send(new LockMessage(Guid.NewGuid()), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await state.FirstStarted.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            await WaitForRenewalCount(inspection, fixture.Schema, 3, fixture.OperationTimeout, cancellationToken);

            state.Release.TrySetResult();
            await state.Completed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;

            Assert.Equal(1, state.Entries);
            Assert.Equal(3, await RenewalCount(inspection, fixture.Schema, cancellationToken));
            Assert.Equal(0, await inspection.DeliveryCount(fixture.Schema, queueName, 1, cancellationToken));
        }
        finally
        {
            state.Release.TrySetResult();
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static async Task CreateRenewalAudit(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        string tableSql = $"""
            CREATE TABLE [{schema}].[RenewalAudit]
            (
                RenewalId bigint IDENTITY(1,1) PRIMARY KEY,
                MessageDeliveryId bigint NOT NULL,
                LockId uniqueidentifier NOT NULL,
                PriorExpiry datetime2 NOT NULL,
                RenewedExpiry datetime2 NOT NULL
            );
            """;
        string triggerSql = $"""
            CREATE OR ALTER TRIGGER [{schema}].[CaptureLockRenewal]
            ON [{schema}].[MessageDelivery]
            AFTER UPDATE
            AS
            BEGIN
                SET NOCOUNT ON;
                INSERT INTO [{schema}].[RenewalAudit](MessageDeliveryId, LockId, PriorExpiry, RenewedExpiry)
                SELECT i.MessageDeliveryId, i.LockId, d.EnqueueTime, i.EnqueueTime
                FROM inserted i
                INNER JOIN deleted d ON d.MessageDeliveryId = i.MessageDeliveryId
                WHERE d.LockId IS NOT NULL
                  AND i.LockId = d.LockId
                  AND i.EnqueueTime > d.EnqueueTime;
            END;
            """;
        await using (var table = new SqlCommand(tableSql, connection))
            await table.ExecuteNonQueryAsync(cancellationToken);
        await using var trigger = new SqlCommand(triggerSql, connection);
        await trigger.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task WaitForRenewalCount(
        SqlConnection connection,
        string schema,
        long expected,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        while (await RenewalCount(connection, schema, timeoutSource.Token) < expected)
            await Task.Yield();
    }

    private static async Task<long> RenewalCount(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand($"SELECT COUNT(*) FROM [{schema}].[RenewalAudit]", connection);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private sealed class ConsumerState
    {
        public TaskCompletionSource FirstStarted { get; } = NewObservation();
        public TaskCompletionSource Release { get; } = NewObservation();
        public TaskCompletionSource Completed { get; } = NewObservation();
        public int Entries;
    }

    private static TaskCompletionSource NewObservation() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record LockMessage(Guid Id);
}
