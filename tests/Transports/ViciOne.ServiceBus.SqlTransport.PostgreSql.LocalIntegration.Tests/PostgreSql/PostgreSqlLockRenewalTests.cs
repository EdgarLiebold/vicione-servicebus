namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class PostgreSqlLockRenewalTests
{
    private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(1);

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0084", "postgresql-native-owner")]
    public async Task SlowConsumer_RenewsItsProviderLockThreeTimesAndCompletesExactlyOnce()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "lock-renewal",
            cancellationToken);
        await using NpgsqlConnection listener = fixture.CreateConnection();
        await listener.OpenWithin(fixture.OperationTimeout, cancellationToken);
        string channel = $"lock_renewal_{Guid.NewGuid():N}";
        await CreateRenewalAudit(listener, fixture.Schema, channel, cancellationToken);
        await Listen(listener, channel, cancellationToken);
        var state = new ConsumerState(expectedEntries: 1);
        string queueName = fixture.Name("renewed-input");
        IBusControl bus = CreateBus(fixture, queueName, TimeSpan.FromSeconds(10), state);
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await Send(bus, queueName, new LockMessage(Guid.NewGuid()), fixture.OperationTimeout, cancellationToken);
            await state.FirstStarted.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            await WaitForNotifications(listener, 3, fixture.OperationTimeout, cancellationToken);

            state.ReleaseFirst.TrySetResult(true);
            await state.AllCompleted.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            await using NpgsqlConnection inspection = fixture.CreateConnection();
            await inspection.OpenWithin(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(1, state.Entries);
            Assert.Equal(3, await RenewalCount(inspection, fixture.Schema, cancellationToken));
            Assert.Equal(0, await inspection.DeliveryCount(fixture.Schema, queueName, 1, cancellationToken));
        }
        finally
        {
            state.ReleaseFirst.TrySetResult(true);
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(RenewalBoundary.MaxDuration)]
    [InlineData(RenewalBoundary.ProviderRefusal)]
    [RequirementCoverage("OBL-R0-SQL-0112", "postgresql-native-owner")]
    public async Task RenewalBoundary_StopsFurtherRenewalOrMarksOwnershipLost(RenewalBoundary boundary)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "renewal-boundary",
            cancellationToken);
        await using NpgsqlConnection listener = fixture.CreateConnection();
        await listener.OpenWithin(fixture.OperationTimeout, cancellationToken);
        string channel = $"lock_boundary_{Guid.NewGuid():N}";
        await CreateRenewalAudit(listener, fixture.Schema, channel, cancellationToken);
        await Listen(listener, channel, cancellationToken);
        var state = new ConsumerState(expectedEntries: 1);
        string queueName = fixture.Name(boundary == RenewalBoundary.MaxDuration ? "max-duration" : "refused");
        TimeSpan maxLockDuration = boundary == RenewalBoundary.MaxDuration
            ? TimeSpan.FromSeconds(1)
            : TimeSpan.FromSeconds(10);
        IBusControl bus = CreateBus(fixture, queueName, maxLockDuration, state);
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await Send(bus, queueName, new LockMessage(Guid.NewGuid()), fixture.OperationTimeout, cancellationToken);
            await state.FirstStarted.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            SqlReceiveLockContext receiveLock = await state.ReceiveLock.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await WaitForNotifications(listener, 1, fixture.OperationTimeout, cancellationToken);
            int expectedRenewals;
            if (boundary == RenewalBoundary.MaxDuration)
            {
                expectedRenewals = 2;
                await WaitForNotifications(listener, 1, fixture.OperationTimeout, cancellationToken);
                Assert.Equal(expectedRenewals, await RenewalCount(listener, fixture.Schema, cancellationToken));
                Assert.Equal(2, await DistinctRenewalLockCount(listener, fixture.Schema, cancellationToken));
                await receiveLock.ValidateLockStatus();
            }
            else
            {
                expectedRenewals = 1;
                await RefuseCurrentRenewal(listener, fixture.Schema, queueName, cancellationToken);
                TransportException exception = await WaitForLostLock(
                    receiveLock,
                    fixture.OperationTimeout,
                    cancellationToken);
                Assert.Contains("Message Lock Lost", exception.Message, StringComparison.Ordinal);
            }

            Task stopTask = bus.StopAsync(CancellationToken.None);
            state.ReleaseFirst.TrySetResult(true);
            await state.AllCompleted.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await stopTask.WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;

            Assert.Equal(1, state.Entries);
            Assert.Equal(expectedRenewals, await RenewalCount(listener, fixture.Schema, cancellationToken));
        }
        finally
        {
            state.ReleaseFirst.TrySetResult(true);
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static IBusControl CreateBus(
        PostgreSqlTestDatabase fixture,
        string queueName,
        TimeSpan maxLockDuration,
        ConsumerState state) =>
        SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.LockDuration = LockDuration;
                endpoint.MaxLockDuration = maxLockDuration;
                endpoint.PollingInterval = TimeSpan.FromMilliseconds(200);
                endpoint.ConcurrentMessageLimit = 2;
                endpoint.Handler<LockMessage>(async context =>
                {
                    int entry = Interlocked.Increment(ref state.Entries);
                    if (entry == 1)
                    {
                        state.ReceiveLock.TrySetResult(context.ReceiveContext.GetPayload<SqlReceiveLockContext>());
                        state.FirstStarted.TrySetResult(true);
                        await state.ReleaseFirst.Task;
                    }

                    if (Interlocked.Increment(ref state.Completed) == state.ExpectedEntries)
                        state.AllCompleted.TrySetResult(true);
                });
            });
        });

    private static async Task Send(
        IBus bus,
        string queueName,
        LockMessage message,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
            .WaitAsync(timeout, cancellationToken);
        await endpoint.Send(message, cancellationToken).WaitAsync(timeout, cancellationToken);
    }

    private static async Task CreateRenewalAudit(
        NpgsqlConnection connection,
        string schema,
        string channel,
        CancellationToken cancellationToken)
    {
        string text = $"""
            CREATE TABLE "{schema}".renewal_audit
            (
                renewal_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                message_delivery_id bigint not null,
                lock_id uuid not null,
                prior_expiry timestamptz not null,
                renewed_expiry timestamptz not null
            );

            CREATE FUNCTION "{schema}".capture_lock_renewal()
                RETURNS trigger
                LANGUAGE plpgsql
            AS
            $$
            BEGIN
                IF OLD.lock_id IS NOT NULL
                   AND NEW.lock_id = OLD.lock_id
                   AND NEW.enqueue_time > OLD.enqueue_time THEN
                    INSERT INTO "{schema}".renewal_audit(message_delivery_id, lock_id, prior_expiry, renewed_expiry)
                    VALUES (NEW.message_delivery_id, NEW.lock_id, OLD.enqueue_time, NEW.enqueue_time);
                    PERFORM pg_notify('{channel}', NEW.message_delivery_id::text);
                END IF;
                RETURN NEW;
            END;
            $$;

            CREATE TRIGGER capture_lock_renewal
                AFTER UPDATE ON "{schema}".message_delivery
                FOR EACH ROW EXECUTE FUNCTION "{schema}".capture_lock_renewal();
            """;
        await using var command = new NpgsqlCommand(text, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task Listen(NpgsqlConnection connection, string channel, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand($"LISTEN \"{channel}\"", connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task WaitForNotifications(
        NpgsqlConnection listener,
        int count,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        for (int index = 0; index < count; index++)
            await listener.WaitAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
    }

    private static async Task RefuseCurrentRenewal(
        NpgsqlConnection connection,
        string schema,
        string queueName,
        CancellationToken cancellationToken)
    {
        string text = $"""
            UPDATE "{schema}".message_delivery d
               SET lock_id = NULL,
                   consumer_id = NULL,
                   enqueue_time = clock_timestamp()
              FROM "{schema}".queue q
             WHERE q.id = d.queue_id
               AND q.name = @queue
               AND q.type = 1
               AND d.lock_id IS NOT NULL
            """;
        await using var command = new NpgsqlCommand(text, connection);
        command.Parameters.AddWithValue("queue", queueName);
        Assert.Equal(1, await command.ExecuteNonQueryAsync(cancellationToken));
    }

    private static async Task<TransportException> WaitForLostLock(
        SqlReceiveLockContext receiveLock,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        while (true)
        {
            timeoutSource.Token.ThrowIfCancellationRequested();
            try
            {
                await receiveLock.ValidateLockStatus();
            }
            catch (TransportException exception)
            {
                return exception;
            }

            await Task.Yield();
        }
    }

    private static async Task<long> RenewalCount(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM \"{schema}\".renewal_audit", connection);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<long> DistinctRenewalLockCount(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT COUNT(DISTINCT lock_id) FROM \"{schema}\".renewal_audit",
            connection);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    public enum RenewalBoundary
    {
        MaxDuration,
        ProviderRefusal,
    }

    private sealed class ConsumerState(int expectedEntries)
    {
        public int ExpectedEntries { get; } = expectedEntries;

        public TaskCompletionSource<bool> FirstStarted { get; } = NewObservation();

        public TaskCompletionSource<SqlReceiveLockContext> ReceiveLock { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> ReleaseFirst { get; } = NewObservation();

        public TaskCompletionSource<bool> AllCompleted { get; } = NewObservation();

        public int Entries;

        public int Completed;
    }

    private static TaskCompletionSource<bool> NewObservation() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public sealed record LockMessage(Guid Id);
}
