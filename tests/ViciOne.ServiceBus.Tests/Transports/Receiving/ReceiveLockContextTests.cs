using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Transports;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports.Receiving;

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

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-LOCK", "duplicate-lock-validation-fallback-and-single-settlement")]
    public async Task PendingSettlement_UsesTheFirstValidDuplicateLockAndClearsAllFallbacksAsync()
    {
        using ReceiveEndpointDispatcherReceiveContext firstContext = CreateReceiveContext();
        using ReceiveEndpointDispatcherReceiveContext duplicateContext = CreateReceiveContext();
        var invalidLock = new RecordingReceiveLockContext(new ExpectedDeliveryException());
        var validLock = new RecordingReceiveLockContext();
        var pending = new PendingReceiveLockContext();

        Assert.True(pending.Enqueue(firstContext, invalidLock));
        Assert.False(pending.Enqueue(duplicateContext, validLock));

        await pending.ValidateLockStatusAsync(TestContext.Current.CancellationToken);
        await pending.CompleteAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, invalidLock.ValidationCount);
        Assert.Equal(0, invalidLock.CompletionCount);
        Assert.Equal(1, validLock.ValidationCount);
        Assert.Equal(1, validLock.CompletionCount);
        Assert.True(pending.IsEmpty);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-LOCK", "concurrent-terminal-settlement-is-serialized-once")]
    public async Task ConcurrentCompletion_SettlesTheRetainedDeliveryExactlyOnceAsync()
    {
        using ReceiveEndpointDispatcherReceiveContext receiveContext = CreateReceiveContext();
        var receiveLock = new BlockingReceiveLockContext();
        var pending = new PendingReceiveLockContext();
        Assert.True(pending.Enqueue(receiveContext, receiveLock));

        Task first = pending.CompleteAsync(TestContext.Current.CancellationToken);
        await receiveLock.CompletionStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        Task second = pending.CompleteAsync(TestContext.Current.CancellationToken);

        Assert.False(first.IsCompleted);
        Assert.False(second.IsCompleted);

        receiveLock.ReleaseCompletion();
        await Task.WhenAll(first, second).WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, receiveLock.CompletionCount);
        Assert.True(pending.IsEmpty);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-LOCK", "cancel-propagates-to-every-retained-delivery")]
    public void PendingSettlement_CancelsTheActiveAndEveryFallbackDelivery()
    {
        using ReceiveEndpointDispatcherReceiveContext firstContext = CreateReceiveContext();
        using ReceiveEndpointDispatcherReceiveContext duplicateContext = CreateReceiveContext();
        var pending = new PendingReceiveLockContext();
        Assert.True(pending.Enqueue(firstContext, NoLockReceiveContext.Instance));
        Assert.False(pending.Enqueue(duplicateContext, NoLockReceiveContext.Instance));

        pending.Cancel();

        Assert.True(firstContext.CancellationToken.IsCancellationRequested);
        Assert.True(duplicateContext.CancellationToken.IsCancellationRequested);
    }

    private static ReceiveEndpointDispatcherReceiveContext CreateReceiveContext()
    {
        var limits = new MessageLimits
        {
            MaxBodyBytes = 1024,
            MaxEnvelopeBytes = 1024,
            MaxJsonDepth = 16,
        };
        var endpointContext = new ReceiveMessageLimitsTestContext(
            limits,
            new Uri("loopback://receive-lock/input"));
        return new ReceiveEndpointDispatcherReceiveContext(endpointContext, [], new Dictionary<string, object>());
    }

    private sealed class ExpectedDeliveryException : Exception;

    private sealed class RecordingReceiveLockContext(Exception? validationFailure = null) : ReceiveLockContext
    {
        private int _completionCount;
        private int _validationCount;

        public int CompletionCount => Volatile.Read(ref _completionCount);
        public int ValidationCount => Volatile.Read(ref _validationCount);

        public Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _completionCount);
            return Task.CompletedTask;
        }

        public Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(exception);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _validationCount);
            return validationFailure is null ? Task.CompletedTask : Task.FromException(validationFailure);
        }
    }

    private sealed class BlockingReceiveLockContext : ReceiveLockContext
    {
        private readonly TaskCompletionSource _releaseCompletion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _completionCount;

        public TaskCompletionSource CompletionStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int CompletionCount => Volatile.Read(ref _completionCount);

        public async Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _completionCount);
            CompletionStarted.TrySetResult();
            await _releaseCompletion.Task.WaitAsync(cancellationToken);
        }

        public Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(exception);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public void ReleaseCompletion() => _releaseCompletion.TrySetResult();
    }
}
