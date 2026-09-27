using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Runtime;

public sealed class SqlSettlementBoundaryTests
{
    private static TimeSpan Timeout => TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;

    [Theory]
    [InlineData(RenewalOutcome.TimeoutThenSuccess)]
    [InlineData(RenewalOutcome.Refused)]
    [InlineData(RenewalOutcome.ProviderFailure)]
    [RequirementCoverage("REQ-VSB-SQL-LOCK-TRANSITION", "renewal-timeout-retry-versus-terminal-loss")]
    public async Task RenewalOutcome_ControlsLockValidityAndStopsFurtherProviderWorkAsync(RenewalOutcome outcome)
    {
        var clock = new FakeTimeProvider();
        var message = new SqlTransportMessage { LockId = Guid.NewGuid(), MessageDeliveryId = 937, MessageId = Guid.NewGuid() };
        var observed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var renewals = new List<(Guid LockId, long Delivery, TimeSpan Duration)>();
        var deleted = new List<(Guid LockId, long Delivery)>();
        ClientContext client = DispatchProxy.Create<ClientContext, ClientProxy>();
        ((ClientProxy)(object)client).Callback = (method, args) =>
        {
            if (method == "get_CancellationToken")
                return CancellationToken.None;
            if (method == "DeleteMessageAsync")
            {
                deleted.Add(((Guid)args[0]!, (long)args[1]!));
                return Task.FromResult(true);
            }
            if (method != "RenewLockAsync")
                throw new NotSupportedException(method);

            renewals.Add(((Guid)args[0]!, (long)args[1]!, (TimeSpan)args[2]!));
            if (outcome == RenewalOutcome.TimeoutThenSuccess && renewals.Count == 1)
                return Task.FromException<bool>(new TimeoutException("transient renewal timeout"));
            observed.TrySetResult();
            return outcome == RenewalOutcome.ProviderFailure
                ? Task.FromException<bool>(new InvalidOperationException("renewal provider failed"))
                : Task.FromResult(outcome == RenewalOutcome.TimeoutThenSuccess);
        };
        var context = new SqlReceiveLockContext(new Uri("db://localhost/transport/input"), message,
            new Settings(TimeSpan.FromSeconds(10)), client, clock);

        clock.Advance(TimeSpan.FromSeconds(7));
        try
        {
            await observed.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            if (outcome == RenewalOutcome.TimeoutThenSuccess)
                await context.ValidateLockStatusAsync(TestContext.Current.CancellationToken);
            else
            {
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
                deadline.CancelAfter(Timeout);
                TransportException? lost = null;
                while (lost is null)
                {
                    deadline.Token.ThrowIfCancellationRequested();
                    try
                    {
                        await context.ValidateLockStatusAsync(deadline.Token);
                    }
                    catch (TransportException failure)
                    {
                        lost = failure;
                    }
                    if (lost is null)
                        await Task.Yield();
                }
                Assert.Contains("937", lost.Message, StringComparison.Ordinal);
                Assert.Contains(message.LockId.Value.ToString(), lost.Message, StringComparison.Ordinal);
            }
        }
        finally
        {
            await context.CompleteAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }

        clock.Advance(TimeSpan.FromHours(3));
        Assert.Equal(outcome == RenewalOutcome.TimeoutThenSuccess ? 2 : 1, renewals.Count);
        Assert.All(renewals, renewal =>
        {
            Assert.Equal(message.LockId.Value, renewal.LockId);
            Assert.Equal(937L, renewal.Delivery);
            Assert.Equal(TimeSpan.FromSeconds(10), renewal.Duration);
        });
        if (outcome == RenewalOutcome.TimeoutThenSuccess)
            Assert.Equal(new[] { (message.LockId.Value, 937L) }, deleted);
        else
            Assert.Empty(deleted);
        await Assert.ThrowsAsync<TransportException>(() => context.ValidateLockStatusAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(Operation.Complete, true)]
    [InlineData(Operation.Complete, false)]
    [InlineData(Operation.Fault, true)]
    [InlineData(Operation.Fault, false)]
    [InlineData(Operation.Redeliver, true)]
    [InlineData(Operation.Redeliver, false)]
    [InlineData(Operation.Expire, true)]
    [InlineData(Operation.Expire, false)]
    [RequirementCoverage("REQ-VSB-SQL-LOCK-TRANSITION", "terminal-provider-arguments-and-rejected-ownership")]
    public async Task TerminalSettlement_PreservesExactDeliveryHeadersAndReportsRejectedOwnershipAsync(Operation operation, bool accepted)
    {
        using var caller = new CancellationTokenSource();
        var expiration = new DateTimeOffset(2041, 2, 3, 4, 5, 6, TimeSpan.Zero);
        var message = new SqlTransportMessage
        {
            LockId = Guid.NewGuid(),
            MessageDeliveryId = 729,
            MessageId = Guid.NewGuid(),
            ExpirationTime = expiration,
            Headers = "[{\"Key\":\"application\",\"Value\":\"unchanged\"}]",
            TransportHeaders = JsonSerializer.Serialize(new[]
            {
                new KeyValuePair<string, object>(MessageHeaders.RedeliveryCount, 4),
                new KeyValuePair<string, object>("custom", "retained")
            })
        };
        string originalApplicationHeaders = message.Headers;
        var calls = new List<(string Method, object?[] Arguments)>();
        ClientContext client = DispatchProxy.Create<ClientContext, ClientProxy>();
        ((ClientProxy)(object)client).Callback = (method, args) =>
        {
            if (method == "get_CancellationToken")
                return CancellationToken.None;
            if (method is not ("DeleteMessageAsync" or "UnlockAsync" or "MoveMessageAsync"))
                throw new NotSupportedException(method);
            calls.Add((method, args.ToArray()));
            return Task.FromResult(accepted);
        };
        var context = new SqlReceiveLockContext(new Uri("db://localhost/transport/input"),
            message, new Settings(), client, TimeProvider.System);
        var cause = CaptureConsumerFailure();
        var wrapper = new Exception("outer diagnostic wrapper", cause);

        Task settlement = operation switch
        {
            Operation.Complete => context.CompleteAsync(caller.Token),
            Operation.Fault => context.FaultedAsync(wrapper, caller.Token),
            Operation.Redeliver => context.ScheduleRedeliveryAsync(TimeSpan.FromSeconds(19), null, caller.Token),
            Operation.Expire => context.ExpiredAsync(caller.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        if (accepted)
            await settlement.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        else
        {
            TransportException failure = await Assert.ThrowsAsync<TransportException>(() =>
                settlement.WaitAsync(Timeout, TestContext.Current.CancellationToken));
            Assert.Contains("729", failure.Message, StringComparison.Ordinal);
            Assert.Contains(message.LockId.Value.ToString(), failure.Message, StringComparison.Ordinal);
        }

        var call = Assert.Single(calls);
        Assert.Equal(message.LockId.Value, Assert.IsType<Guid>(call.Arguments[0]));
        Assert.Equal(729L, Assert.IsType<long>(call.Arguments[1]));
        Assert.Equal(caller.Token, Assert.IsType<CancellationToken>(call.Arguments[^1]));
        Assert.Equal(originalApplicationHeaders, message.Headers);

        if (operation == Operation.Complete)
        {
            Assert.Equal("DeleteMessageAsync", call.Method);
            Assert.Equal(3, call.Arguments.Length);
        }
        else
        {
            int headerIndex = operation == Operation.Expire ? 5 : 3;
            SendHeaders headers = Assert.IsAssignableFrom<SendHeaders>(call.Arguments[headerIndex]);
            Assert.Equal("retained", headers.Get<string>("custom"));
            Assert.Equal(operation == Operation.Redeliver ? 5 : 4, headers.Get<int>(MessageHeaders.RedeliveryCount));

            if (operation == Operation.Expire)
            {
                Assert.Equal("MoveMessageAsync", call.Method);
                Assert.Equal("input", Assert.IsType<string>(call.Arguments[2]));
                Assert.Equal(SqlQueueType.DeadLetterQueue, Assert.IsType<SqlQueueType>(call.Arguments[3]));
                Assert.Equal(expiration, Assert.IsType<DateTimeOffset>(call.Arguments[4]));
                Assert.Equal("expired", headers.Get<string>(MessageHeaders.Reason));
                Assert.False(headers.TryGetHeader(MessageHeaders.FaultMessage, out _));
            }
            else
            {
                Assert.Equal("UnlockAsync", call.Method);
                Assert.Equal(TimeSpan.FromSeconds(operation == Operation.Fault ? 11 : 19),
                    Assert.IsType<TimeSpan>(call.Arguments[2]));
                if (operation == Operation.Fault)
                {
                    Assert.Equal("fault", headers.Get<string>(MessageHeaders.Reason));
                    Assert.Equal("consumer cause", headers.Get<string>(MessageHeaders.FaultMessage));
                    Assert.EndsWith("InvalidOperationException", headers.Get<string>(MessageHeaders.FaultExceptionType), StringComparison.Ordinal);
                    Assert.Contains(nameof(CaptureConsumerFailure), headers.Get<string>(MessageHeaders.FaultStackTrace), StringComparison.Ordinal);
                }
                else
                {
                    Assert.False(headers.TryGetHeader(MessageHeaders.Reason, out _));
                    Assert.False(headers.TryGetHeader(MessageHeaders.FaultMessage, out _));
                }
            }
        }

        await Assert.ThrowsAsync<TransportException>(() => context.ValidateLockStatusAsync(caller.Token));
        await context.CompleteAsync(caller.Token);
        await context.FaultedAsync(new Exception("second terminal attempt"), caller.Token);
        Assert.Single(calls);
    }

    private static InvalidOperationException CaptureConsumerFailure()
    {
        try
        {
            throw new InvalidOperationException("consumer cause");
        }
        catch (InvalidOperationException failure)
        {
            return failure;
        }
    }

    public enum Operation
    {
        Complete,
        Fault,
        Redeliver,
        Expire
    }

    public enum RenewalOutcome
    {
        TimeoutThenSuccess,
        Refused,
        ProviderFailure
    }

    private class ClientProxy : DispatchProxy
    {
        public Func<string, object?[], object?> Callback { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Callback(targetMethod!.Name, args ?? []);
    }

    private sealed class Settings(TimeSpan? lockDuration = null) : ReceiveSettings
    {
        public string QueueName => "input";
        public string EntityName => QueueName;
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
        public TimeSpan? UnlockDelay => TimeSpan.FromSeconds(11);
        public TimeSpan MaxLockDuration => TimeSpan.FromHours(2);
        public int MaintenanceBatchSize => 100;
        public bool DeadLetterExpiredMessages => true;
    }
}
