using System.Reflection;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.SqlTransport.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Runtime;

public sealed class SqlQueueMaintenanceTests
{
    private static readonly DateTimeOffset StartTime = new(2037, 4, 5, 6, 7, 8, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-QUEUE-MAINTENANCE", "database-idle-duration-never-rounds-down")]
    public void DatabaseAutoDeleteSeconds_NeverShortensConfiguredIdleLifetime()
    {
        Assert.Null(SqlTransportDefaults.ToDatabaseAutoDeleteSeconds(null));
        Assert.Equal(1, SqlTransportDefaults.ToDatabaseAutoDeleteSeconds(TimeSpan.FromTicks(1)));
        Assert.Equal(1, SqlTransportDefaults.ToDatabaseAutoDeleteSeconds(TimeSpan.FromMilliseconds(500)));
        Assert.Equal(2, SqlTransportDefaults.ToDatabaseAutoDeleteSeconds(TimeSpan.FromMilliseconds(1500)));
        Assert.Equal(int.MaxValue, SqlTransportDefaults.ToDatabaseAutoDeleteSeconds(TimeSpan.FromSeconds(int.MaxValue)));
        Assert.Throws<ArgumentOutOfRangeException>(() => SqlTransportDefaults.ToDatabaseAutoDeleteSeconds(
            TimeSpan.FromSeconds(int.MaxValue) + TimeSpan.FromTicks(1)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-QUEUE-MAINTENANCE", "cleanup-and-keepalive-boundaries")]
    public async Task Schedule_EnforcesEveryMaintenanceAndKeepaliveBoundaryAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var timeProvider = new MutableTimeProvider(StartTime);
        var client = Client();
        var proxy = (MaintenanceClientContextProxy)(object)client;
        var maintenance = new SqlQueueMaintenance(client, new TestReceiveSettings(TimeSpan.FromMinutes(2)), timeProvider);

        await maintenance.RunAsync(cancellationToken, CancellationToken.None);

        Assert.Equal((1, 1), (proxy.DeadLetterCallCount, proxy.TouchCallCount));
        Assert.Equal(("input", 100, cancellationToken), proxy.LastDeadLetterCall);
        Assert.Equal(("input", cancellationToken), proxy.LastTouchCall);

        timeProvider.Advance(TimeSpan.FromSeconds(30));
        await maintenance.RunAsync(cancellationToken, CancellationToken.None);

        Assert.Equal((1, 1), (proxy.DeadLetterCallCount, proxy.TouchCallCount));

        timeProvider.Advance(TimeSpan.FromTicks(1));
        await maintenance.RunAsync(cancellationToken, CancellationToken.None);

        Assert.Equal((2, 1), (proxy.DeadLetterCallCount, proxy.TouchCallCount));

        timeProvider.Advance(TimeSpan.FromSeconds(30));
        await maintenance.RunAsync(cancellationToken, CancellationToken.None);

        Assert.Equal((2, 2), (proxy.DeadLetterCallCount, proxy.TouchCallCount));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-QUEUE-MAINTENANCE", "backlog-outcomes-control-retry-and-keepalive")]
    public async Task BacklogOutcomes_ControlImmediateCleanupAndKeepaliveAsync()
    {
        var client = Client();
        var proxy = (MaintenanceClientContextProxy)(object)client;
        proxy.EnqueueDeadLetterResult(100);
        proxy.EnqueueDeadLetterResult(100);
        proxy.EnqueueDeadLetterResult(null);
        proxy.EnqueueDeadLetterResult(0);
        var maintenance = new SqlQueueMaintenance(client, new TestReceiveSettings(TimeSpan.FromMinutes(2)), new MutableTimeProvider(StartTime));

        await maintenance.RunAsync(CancellationToken.None, CancellationToken.None);
        await maintenance.RunAsync(CancellationToken.None, CancellationToken.None);

        Assert.Equal((2, 0), (proxy.DeadLetterCallCount, proxy.TouchCallCount));

        await maintenance.RunAsync(CancellationToken.None, CancellationToken.None);
        await maintenance.RunAsync(CancellationToken.None, CancellationToken.None);

        Assert.Equal((4, 1), (proxy.DeadLetterCallCount, proxy.TouchCallCount));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-QUEUE-MAINTENANCE", "permanent-queue-is-never-touched")]
    public async Task QueueWithoutAutoDelete_NeverReceivesKeepaliveTouchesAsync()
    {
        var timeProvider = new MutableTimeProvider(StartTime);
        var client = Client();
        var proxy = (MaintenanceClientContextProxy)(object)client;
        var maintenance = new SqlQueueMaintenance(client, new TestReceiveSettings(autoDeleteOnIdle: null), timeProvider);

        await maintenance.RunAsync(CancellationToken.None, CancellationToken.None);
        timeProvider.Advance(TimeSpan.FromHours(1));
        await maintenance.RunAsync(CancellationToken.None, CancellationToken.None);

        Assert.Equal(2, proxy.DeadLetterCallCount);
        Assert.Equal(0, proxy.TouchCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-QUEUE-MAINTENANCE", "expected-polling-interruptions-are-contained")]
    public async Task ExpectedPollingInterruptions_AreContainedAsync()
    {
        using var operationCanceled = new CancellationTokenSource();
        operationCanceled.Cancel();
        using var stopping = new CancellationTokenSource();
        stopping.Cancel();

        await MaintenanceThrowing(new TimeoutException()).RunAsync(CancellationToken.None, CancellationToken.None);
        await MaintenanceThrowing(new ObjectDisposedException("connection")).RunAsync(CancellationToken.None, stopping.Token);
        await MaintenanceThrowing(new OperationCanceledException()).RunAsync(operationCanceled.Token, CancellationToken.None);
        await MaintenanceThrowing(new OperationCanceledException()).RunAsync(CancellationToken.None, stopping.Token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-QUEUE-MAINTENANCE", "unexpected-lifecycle-failures-propagate")]
    public async Task UnexpectedLifecycleExceptions_PropagateAsync()
    {
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            MaintenanceThrowing(new ObjectDisposedException("connection")).RunAsync(CancellationToken.None, CancellationToken.None));
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            MaintenanceThrowing(new OperationCanceledException()).RunAsync(CancellationToken.None, CancellationToken.None));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-QUEUE-MAINTENANCE", "failed-keepalive-remains-due")]
    public async Task FailedKeepalive_RemainsDueWithoutRepeatingCleanupAsync()
    {
        var client = Client();
        var proxy = (MaintenanceClientContextProxy)(object)client;
        proxy.EnqueueTouchFailure(new TimeoutException());
        var maintenance = new SqlQueueMaintenance(client, new TestReceiveSettings(TimeSpan.FromMinutes(2)), new MutableTimeProvider(StartTime));

        await maintenance.RunAsync(CancellationToken.None, CancellationToken.None);
        await maintenance.RunAsync(CancellationToken.None, CancellationToken.None);

        Assert.Equal(1, proxy.DeadLetterCallCount);
        Assert.Equal(2, proxy.TouchCallCount);
    }

    private static SqlQueueMaintenance MaintenanceThrowing(Exception exception)
    {
        ClientContext client = Client();
        ((MaintenanceClientContextProxy)(object)client).EnqueueDeadLetterFailure(exception);
        return new SqlQueueMaintenance(client, new TestReceiveSettings(TimeSpan.FromMinutes(2)), new MutableTimeProvider(StartTime));
    }

    private static ClientContext Client()
    {
        return DispatchProxy.Create<ClientContext, MaintenanceClientContextProxy>();
    }

    private class MaintenanceClientContextProxy : DispatchProxy
    {
        readonly Queue<Func<Task<int?>>> _deadLetterOutcomes = new();
        readonly Queue<Exception> _touchFailures = new();

        public int DeadLetterCallCount { get; private set; }
        public (string QueueName, int MessageCount, CancellationToken CancellationToken) LastDeadLetterCall { get; private set; }
        public (string QueueName, CancellationToken CancellationToken) LastTouchCall { get; private set; }
        public int TouchCallCount { get; private set; }

        public void EnqueueDeadLetterFailure(Exception exception) =>
            _deadLetterOutcomes.Enqueue(() => Task.FromException<int?>(exception));

        public void EnqueueDeadLetterResult(int? result) =>
            _deadLetterOutcomes.Enqueue(() => Task.FromResult(result));

        public void EnqueueTouchFailure(Exception exception) => _touchFailures.Enqueue(exception);

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name switch
            {
                "DeadLetterQueueAsync" => DeadLetterQueueAsync(args!),
                "TouchQueueAsync" => TouchQueueAsync(args!),
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }

        Task<int?> DeadLetterQueueAsync(object?[] args)
        {
            DeadLetterCallCount++;
            LastDeadLetterCall = ((string)args[0]!, (int)args[1]!, (CancellationToken)args[2]!);
            return _deadLetterOutcomes.TryDequeue(out Func<Task<int?>>? outcome)
                ? outcome()
                : Task.FromResult<int?>(0);
        }

        Task TouchQueueAsync(object?[] args)
        {
            TouchCallCount++;
            LastTouchCall = ((string)args[0]!, (CancellationToken)args[1]!);
            return _touchFailures.TryDequeue(out Exception? exception)
                ? Task.FromException(exception)
                : Task.CompletedTask;
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public void Advance(TimeSpan duration) => utcNow += duration;

        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class TestReceiveSettings(TimeSpan? autoDeleteOnIdle) : ReceiveSettings
    {
        public string QueueName => "input";
        public TimeSpan? AutoDeleteOnIdle => autoDeleteOnIdle;
        public int? MaxDeliveryCount => 10;
        public long? QueueId => 1;
        public int PrefetchCount => 1;
        public int ConcurrentMessageLimit => 1;
        public int ConcurrentDeliveryLimit => 1;
        public SqlReceiveMode ReceiveMode => SqlReceiveMode.Normal;
        public bool PurgeOnStartup => false;
        public TimeSpan LockDuration => TimeSpan.FromMinutes(1);
        public TimeSpan PollingInterval => TimeSpan.FromSeconds(1);
        public TimeSpan? UnlockDelay => null;
        public TimeSpan MaxLockDuration => TimeSpan.FromHours(1);
        public string EntityName => QueueName;
        public int MaintenanceBatchSize => 100;
        public bool DeadLetterExpiredMessages => false;
    }
}
