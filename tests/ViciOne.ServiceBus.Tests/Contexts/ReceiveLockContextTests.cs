using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Contexts;

public sealed class ReceiveLockContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-LOCK", "no-lock-settlement-completes-without-transport-work")]
    public async Task NoLockSettlement_CompletesEverySupportedOperationAsync()
    {
        ReceiveLockContext context = NoLockReceiveContext.Instance;

        await context.CompleteAsync(TestContext.Current.CancellationToken);
        await context.FaultedAsync(new ExpectedDeliveryException(), TestContext.Current.CancellationToken);
        await context.ValidateLockStatusAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-LOCK", "no-lock-settlement-preserves-cancellation")]
    public async Task NoLockSettlement_PreservesCancellationAcrossEveryOperationAsync()
    {
        ReceiveLockContext context = NoLockReceiveContext.Instance;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        TaskCanceledException complete = await Assert.ThrowsAsync<TaskCanceledException>(() =>
            context.CompleteAsync(cancellation.Token));
        TaskCanceledException faulted = await Assert.ThrowsAsync<TaskCanceledException>(() =>
            context.FaultedAsync(new ExpectedDeliveryException(), cancellation.Token));
        TaskCanceledException validate = await Assert.ThrowsAsync<TaskCanceledException>(() =>
            context.ValidateLockStatusAsync(cancellation.Token));

        Assert.Equal(cancellation.Token, complete.CancellationToken);
        Assert.Equal(cancellation.Token, faulted.CancellationToken);
        Assert.Equal(cancellation.Token, validate.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-LOCK", "no-lock-fault-requires-exception")]
    public void NoLockFaultSettlement_RejectsAMissingFailureSynchronously()
    {
        void SettleWithoutFailure() => _ = NoLockReceiveContext.Instance.FaultedAsync(
            null!,
            TestContext.Current.CancellationToken);

        Assert.Equal("exception", Assert.Throws<ArgumentNullException>(SettleWithoutFailure).ParamName);
    }

    private sealed class ExpectedDeliveryException : Exception;
}
