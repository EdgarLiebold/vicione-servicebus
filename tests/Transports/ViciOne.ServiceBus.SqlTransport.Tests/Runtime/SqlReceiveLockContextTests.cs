using System.Reflection;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Runtime;

public sealed class SqlReceiveLockContextTests
{
    private static readonly Uri InputAddress = new("db://localhost/transport/input");

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

    private static SqlReceiveLockContext CreateContext(ClientContext client)
    {
        var message = new SqlTransportMessage
        {
            LockId = NewId.NextGuid(),
            MessageDeliveryId = 27,
            MessageId = NewId.NextGuid(),
            TransportHeaders = "[]",
        };

        return new SqlReceiveLockContext(InputAddress, message, new TestReceiveSettings(), client, TimeProvider.System);
    }

    private static ClientContext Client(bool unlockResult)
    {
        ClientContext client = DispatchProxy.Create<ClientContext, LockClientContextProxy>();
        ((LockClientContextProxy)(object)client).UnlockResult = unlockResult;
        return client;
    }

    private class LockClientContextProxy : DispatchProxy
    {
        public bool UnlockResult { get; set; }
        public int UnlockCallCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name switch
            {
                "get_CancellationToken" => CancellationToken.None,
                "UnlockAsync" => UnlockAsync(),
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }

        private Task<bool> UnlockAsync()
        {
            UnlockCallCount++;
            return Task.FromResult(UnlockResult);
        }
    }

    private sealed class TestReceiveSettings : ReceiveSettings
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
        public TimeSpan LockDuration => TimeSpan.FromHours(1);
        public TimeSpan PollingInterval => TimeSpan.FromSeconds(1);
        public TimeSpan? UnlockDelay => null;
        public TimeSpan MaxLockDuration => TimeSpan.FromHours(2);
        public string EntityName => QueueName;
        public int MaintenanceBatchSize => 100;
        public bool DeadLetterExpiredMessages => false;
    }
}
