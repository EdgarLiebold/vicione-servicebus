using System.Reflection;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Runtime;

public sealed class SqlReceiveLockContextTests
{
    private static readonly Uri InputAddress = new("db://localhost/transport/input");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SQL-LOCK-TRANSITION", "unsafe-base-lookup-still-unlocks-with-original-failure")]
    public async Task FaultedAsync_UnsafeBaseLookupStillUnlocksWithTheOriginalFailureAsync(bool nullBase)
    {
        var client = Client(unlockResult: true);
        var proxy = (LockClientContextProxy)(object)client;
        var context = CreateContext(client);
        var failure = new UnsafeBaseException(nullBase);
        CancellationToken token = TestContext.Current.CancellationToken;

        await context.FaultedAsync(failure, token);

        Assert.Equal(1, proxy.UnlockCallCount);
        Assert.Equal(0, proxy.DeleteCallCount);
        object?[] arguments = Assert.IsType<object?[]>(proxy.UnlockArguments);
        Assert.Equal(token, Assert.IsType<CancellationToken>(arguments[^1]));
        SendHeaders headers = Assert.IsAssignableFrom<SendHeaders>(arguments[3]);
        Assert.Equal("fault", headers.Get<string>(MessageHeaders.Reason));
        Assert.Equal("original SQL failure", headers.Get<string>(MessageHeaders.FaultMessage));
        Assert.EndsWith(nameof(UnsafeBaseException),
            headers.Get<string>(MessageHeaders.FaultExceptionType), StringComparison.Ordinal);

        await context.FaultedAsync(failure, token);
        Assert.Equal(1, proxy.UnlockCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-LOCK-TRANSITION", "failed-unlock-is-reported-as-lock-loss")]
    public async Task ScheduleRedeliveryAsync_ReportsARejectedUnlockAsLockLossAsync()
    {
        var client = Client(unlockResult: false);
        var context = CreateContext(client);

        TransportException exception = await Assert.ThrowsAsync<TransportException>(() =>
            context.ScheduleRedeliveryAsync(TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken));

        Assert.Contains("lock", exception.Message, StringComparison.OrdinalIgnoreCase);
        await Assert.ThrowsAsync<TransportException>(() => context.ValidateLockStatusAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-LOCK-TRANSITION", "redelivery-callback-is-never-silently-ignored")]
    public async Task ScheduleRedeliveryAsync_RejectsAnUnsupportedSendContextCallbackAsync()
    {
        var client = Client(unlockResult: true);
        var context = CreateContext(client);

        NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
            context.ScheduleRedeliveryAsync(TimeSpan.Zero, static (_, _) => { }, TestContext.Current.CancellationToken));

        Assert.Contains("callback", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, ((LockClientContextProxy)(object)client).UnlockCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-LOCK-TRANSITION", "concurrent-terminal-operations-settle-exactly-once")]
    public async Task ConcurrentTerminalOperations_SettleTheDeliveryExactlyOnceAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var client = Client(unlockResult: true, holdDelete: true);
        var proxy = (LockClientContextProxy)(object)client;
        var context = CreateContext(client);

        Task complete = context.CompleteAsync(cancellationToken);
        await proxy.DeleteEntered.WaitAsync(cancellationToken);

        Task faulted = context.FaultedAsync(new InvalidOperationException("consumer failed"), cancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);

        Assert.Equal(1, proxy.DeleteCallCount);
        Assert.Equal(0, proxy.UnlockCallCount);
        Assert.False(faulted.IsCompleted);

        proxy.ReleaseDelete();
        await Task.WhenAll(complete, faulted);

        Assert.Equal(1, proxy.DeleteCallCount);
        Assert.Equal(0, proxy.UnlockCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-LOCK-TRANSITION", "terminal-settlement-cancels-in-flight-renewal")]
    public async Task CompleteAsync_CancelsAnInFlightProviderRenewalAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var client = Client(unlockResult: true, blockRenewal: true);
        var proxy = (LockClientContextProxy)(object)client;
        var context = CreateContext(client, new TestReceiveSettings(TimeSpan.Zero));

        await proxy.RenewEntered.WaitAsync(cancellationToken);
        await context.CompleteAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);

        Assert.True(proxy.RenewalToken.CanBeCanceled);
        Assert.True(proxy.RenewalToken.IsCancellationRequested);
        Assert.Equal(1, proxy.DeleteCallCount);
    }

    private static SqlReceiveLockContext CreateContext(ClientContext client, ReceiveSettings? settings = null)
    {
        var message = new SqlTransportMessage
        {
            LockId = NewId.NextGuid(),
            MessageDeliveryId = 27,
            MessageId = NewId.NextGuid(),
            TransportHeaders = "[]",
        };

        return new SqlReceiveLockContext(InputAddress, message, settings ?? new TestReceiveSettings(), client, TimeProvider.System);
    }

    private static ClientContext Client(bool unlockResult, bool holdDelete = false, bool blockRenewal = false)
    {
        ClientContext client = DispatchProxy.Create<ClientContext, LockClientContextProxy>();
        var proxy = (LockClientContextProxy)(object)client;
        proxy.UnlockResult = unlockResult;
        proxy.HoldDelete = holdDelete;
        proxy.BlockRenewal = blockRenewal;
        return client;
    }

    private class LockClientContextProxy : DispatchProxy
    {
        readonly TaskCompletionSource<bool> _deleteCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _deleteEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _renewEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool BlockRenewal { get; set; }
        public Task DeleteEntered => _deleteEntered.Task;
        public int DeleteCallCount { get; private set; }
        public bool HoldDelete { get; set; }
        public Task RenewEntered => _renewEntered.Task;
        public CancellationToken RenewalToken { get; private set; }
        public bool UnlockResult { get; set; }
        public int UnlockCallCount { get; private set; }
        public object?[]? UnlockArguments { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name switch
            {
                "get_CancellationToken" => CancellationToken.None,
                "UnlockAsync" => UnlockAsync(args),
                "DeleteMessageAsync" => DeleteMessageAsync(),
                "RenewLockAsync" => RenewLockAsync(args),
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }

        public void ReleaseDelete() => _deleteCompletion.TrySetResult(true);

        private Task<bool> DeleteMessageAsync()
        {
            DeleteCallCount++;
            _deleteEntered.TrySetResult();
            return HoldDelete ? _deleteCompletion.Task : Task.FromResult(true);
        }

        private Task<bool> RenewLockAsync(object?[]? args)
        {
            if (!BlockRenewal)
                return Task.FromResult(true);

            RenewalToken = (CancellationToken)args![3]!;
            _renewEntered.TrySetResult();

            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            RenewalToken.Register(() => completion.TrySetCanceled(RenewalToken));
            return completion.Task;
        }

        private Task<bool> UnlockAsync(object?[]? args)
        {
            UnlockCallCount++;
            UnlockArguments = args?.ToArray();
            return Task.FromResult(UnlockResult);
        }
    }

    private sealed class TestReceiveSettings(TimeSpan? lockDuration = null) : ReceiveSettings
    {
        public string QueueName => "input";
        public TimeSpan? AutoDeleteOnIdle => null;
        public int? MaxDeliveryCount => 10;
        public long? QueueId => 1;
        public int PrefetchCount => 1;
        public int ConcurrentMessageLimit => 1;
        public int ConcurrentDeliveryLimit => 1;
        public SqlReceiveMode ReceiveMode => SqlReceiveMode.Normal;
        public bool PurgeOnStartup => false;
        public TimeSpan LockDuration => lockDuration ?? TimeSpan.FromHours(1);
        public TimeSpan PollingInterval => TimeSpan.FromSeconds(1);
        public TimeSpan? UnlockDelay => null;
        public TimeSpan MaxLockDuration => TimeSpan.FromHours(2);
        public string EntityName => QueueName;
        public int MaintenanceBatchSize => 100;
        public bool DeadLetterExpiredMessages => false;
    }

    private sealed class UnsafeBaseException(bool nullBase) : Exception("original SQL failure")
    {
        public override Exception GetBaseException() => nullBase
            ? null!
            : throw new InvalidOperationException("base lookup failed");
    }
}
