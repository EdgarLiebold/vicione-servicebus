using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.SqlTransport.SqlServer;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Runtime;

public sealed class SqlClientLeaseAndTimerContractTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-SQL-CLIENT-LEASE-CANCELLATION", "lease-and-caller-cancel-only-owned-operations")]
    public async Task ClientWrappers_PropagateCancellationForEveryResultShapeWithoutCancelingSharedRootAsync(
        bool shared, bool cancelCaller)
    {
        using var root = new CancellationTokenSource();
        using var lease = new CancellationTokenSource();
        using var caller = new CancellationTokenSource();
        using var neighborLease = new CancellationTokenSource();
        ClientContext provider = DispatchProxy.Create<ClientContext, HeldClientProxy>();
        var spy = (HeldClientProxy)(object)provider;
        spy.LifetimeToken = root.Token;
        ClientContext context = Wrap(provider, lease.Token, shared);
        ClientContext neighbor = Wrap(provider, neighborLease.Token, shared);
        Guid lockId = Guid.Parse("38e30332-3ac3-4b4b-806d-123a0a2a7d91");
        Task[] operations = [];
        Task<long>? neighboring = null;

        try
        {
            Assert.Equal(lease.Token, context.CancellationToken);
            operations =
            [
                context.PurgeQueueAsync("owned-purge", caller.Token),
                context.RenewLockAsync(lockId, 37, TimeSpan.FromSeconds(5), caller.Token),
                context.TouchQueueAsync("owned-touch", caller.Token),
                context.ReceiveMessagesAsync("owned-fetch", SqlReceiveMode.PartitionedOrdered, 3, 2,
                    TimeSpan.FromSeconds(9), caller.Token),
            ];
            neighboring = neighbor.PurgeQueueAsync("neighbor-purge", CancellationToken.None);
            Assert.Equal(5, spy.Calls.Count);
            Assert.All(operations, operation => Assert.False(operation.IsCompleted));
            Assert.False(neighboring.IsCompleted);
            Assert.Equal("owned-purge", spy.Calls[0].Arguments[0]);
            Assert.Equal(lockId, spy.Calls[1].Arguments[0]);
            Assert.Equal(37L, spy.Calls[1].Arguments[1]);
            Assert.Equal(TimeSpan.FromSeconds(5), spy.Calls[1].Arguments[2]);
            Assert.Equal("owned-touch", spy.Calls[2].Arguments[0]);
            Assert.Equal("owned-fetch", spy.Calls[3].Arguments[0]);
            Assert.Equal(SqlReceiveMode.PartitionedOrdered, spy.Calls[3].Arguments[1]);
            Assert.Equal(3, spy.Calls[3].Arguments[2]);
            Assert.Equal(2, spy.Calls[3].Arguments[3]);
            Assert.Equal(TimeSpan.FromSeconds(9), spy.Calls[3].Arguments[4]);

            if (cancelCaller)
                caller.Cancel();
            else
                lease.Cancel();

            Assert.All(spy.Calls.Take(4), call => Assert.True(call.Token.IsCancellationRequested));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.WhenAll(operations));
            Assert.All(operations, operation => Assert.True(operation.IsCanceled));
            Assert.False(root.IsCancellationRequested);
            Assert.Equal(cancelCaller, caller.IsCancellationRequested);
            Assert.Equal(!cancelCaller, lease.IsCancellationRequested);
            Assert.False(neighborLease.IsCancellationRequested);
            Assert.False(spy.Calls[4].Token.IsCancellationRequested);
            Assert.False(neighboring.IsCompleted);
            spy.Calls[4].Release();
            Assert.Equal(73L, await neighboring);
        }
        finally
        {
            spy.ReleaseAll();
            foreach (Task operation in operations)
            {
                try { await operation; }
                catch (OperationCanceledException) { }
            }
            if (neighboring != null)
                await neighboring;
            spy.DisposeRegistrations();
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-SQL-LOCK-INITIAL-RENEWAL-TIMER", "valid-db-duration-never-causes-late-system-lock-loss")]
    public async Task ValidDatabaseLockDuration_RemainsValidWithSupportedInitialRenewalSchedulingAsync(
        bool longDuration, bool useFakeClock)
    {
        TimeSpan duration = longDuration ? TimeSpan.FromDays(100) : TimeSpan.FromHours(1);
        var topology = new SqlTopologyConfiguration(SqlBusFactory.CreateMessageTopology());
        var bus = new SqlBusConfiguration(topology);
        var host = Assert.IsType<SqlHostConfiguration>(bus.HostConfiguration);
        host.Settings = new SqlServerHostSettings(new SqlTransportOptions
        {
            Host = "localhost", Database = "transport_tests", Schema = "transport",
            Username = "test_user", Password = "test_password",
        });
        var endpoint = Assert.IsType<SqlReceiveEndpointConfiguration>(
            host.CreateReceiveEndpointConfiguration("timer-contract", configure =>
            {
                configure.LockDuration = duration;
                configure.MaxLockDuration = duration + TimeSpan.FromHours(1);
            }));
        Assert.Empty(endpoint.Validate());
        Assert.InRange(duration.TotalSeconds, 1, int.MaxValue);
        TimeProvider clock = useFakeClock ? new FakeTimeProvider() : TimeProvider.System;
        ClientContext provider = DispatchProxy.Create<ClientContext, LockSettlementProxy>();
        var spy = (LockSettlementProxy)(object)provider;
        var message = new SqlTransportMessage
        {
            LockId = Guid.Parse("009b5b0c-4dfe-4d32-adb4-8b9785e39998"), MessageDeliveryId = 91,
            DeliveryCount = 1, TransportHeaders = "[]",
        };
        var context = new SqlReceiveLockContext(new Uri("db://localhost/transport/timer-contract"),
            message, endpoint.Settings, provider, clock);

        try
        {
            await context.ValidateLockStatusAsync(TestContext.Current.CancellationToken);
            Assert.Equal(0, spy.RenewCount);
            Assert.Equal(0, spy.DeleteCount);
        }
        finally
        {
            await context.CompleteAsync(CancellationToken.None);
        }

        Assert.Equal(1, spy.DeleteCount);
        Assert.Equal(message.LockId.Value, spy.DeleteArguments![0]);
        Assert.Equal(91L, spy.DeleteArguments[1]);
        Assert.Equal(CancellationToken.None, spy.DeleteArguments[2]);
        await Assert.ThrowsAsync<TransportException>(() => context.ValidateLockStatusAsync(CancellationToken.None));
    }

    private static ClientContext Wrap(ClientContext provider, CancellationToken token, bool shared) =>
        shared ? new SharedClientContext(provider, token) : new ScopeClientContext(provider, token);

    public class HeldClientProxy : DispatchProxy
    {
        public CancellationToken LifetimeToken { get; set; }
        public List<HeldCall> Calls { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == "get_CancellationToken")
                return LifetimeToken;
            Assert.NotNull(args);
            CancellationToken token = Assert.IsType<CancellationToken>(args[^1]);
            return targetMethod.Name switch
            {
                "PurgeQueueAsync" => CaptureAsync(args, token, 73L),
                "RenewLockAsync" => CaptureAsync(args, token, true),
                "TouchQueueAsync" => CaptureAsync(args, token, 0),
                "ReceiveMessagesAsync" => CaptureAsync<IEnumerable<SqlTransportMessage>>(args, token, Array.Empty<SqlTransportMessage>()),
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }

        private Task<T> CaptureAsync<T>(object?[] args, CancellationToken token, T result)
        {
            var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration registration = token.Register(() => completion.TrySetCanceled(token));
            Calls.Add(new HeldCall(args.ToArray(), token, () => completion.TrySetResult(result), registration));
            return completion.Task;
        }

        public void ReleaseAll()
        {
            foreach (HeldCall call in Calls)
                call.Release();
        }

        public void DisposeRegistrations()
        {
            foreach (HeldCall call in Calls)
                call.Registration.Dispose();
        }
    }

    public sealed record HeldCall(object?[] Arguments, CancellationToken Token, Action Release,
        CancellationTokenRegistration Registration);

    public class LockSettlementProxy : DispatchProxy
    {
        public int RenewCount { get; private set; }
        public int DeleteCount { get; private set; }
        public object?[]? DeleteArguments { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            switch (targetMethod.Name)
            {
                case "get_CancellationToken": return CancellationToken.None;
                case "RenewLockAsync": RenewCount++; return Task.FromResult(true);
                case "DeleteMessageAsync": DeleteCount++; DeleteArguments = args!.ToArray(); return Task.FromResult(true);
                default: throw new NotSupportedException(targetMethod.Name);
            }
        }
    }
}
