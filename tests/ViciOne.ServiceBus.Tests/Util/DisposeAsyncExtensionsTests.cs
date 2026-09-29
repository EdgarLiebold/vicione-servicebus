using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Util;

public sealed class DisposeAsyncExtensionsTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-ASYNC-DISPOSAL", "unsafe-base-lookup-still-invokes-cleanup-and-rethrows-original")]
    public async Task UnsafeBaseLookup_StillInvokesCleanupAndRethrowsTheOriginalFailureAsync(bool valueTaskCallback, bool nullBase)
    {
        var failure = new UnsafeBaseFailure(nullBase);
        int disposals = 0;

        async Task OperationAsync()
        {
            if (valueTaskCallback)
                await failure.DisposeAsync<int>((Func<ValueTask>)(() =>
                {
                    disposals++;
                    return ValueTask.CompletedTask;
                }));
            else
                await failure.DisposeAsync<int>((Func<Task>)(() =>
                {
                    disposals++;
                    return Task.CompletedTask;
                }));
        }

        UnsafeBaseFailure actual = await Assert.ThrowsAsync<UnsafeBaseFailure>(OperationAsync);

        Assert.Same(failure, actual);
        Assert.Equal(1, disposals);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASYNC-DISPOSAL", "valid-base-lookup-rethrows-root-after-cleanup")]
    public async Task ValidBaseLookup_RethrowsTheRootAfterCleanupAsync(bool valueTaskCallback)
    {
        var root = new TimeoutException("root failure");
        var wrapper = new InvalidOperationException("outer", root);
        int disposals = 0;

        async Task OperationAsync()
        {
            if (valueTaskCallback)
                await wrapper.DisposeAsync<int>((Func<ValueTask>)(() =>
                {
                    disposals++;
                    return ValueTask.CompletedTask;
                }));
            else
                await wrapper.DisposeAsync<int>((Func<Task>)(() =>
                {
                    disposals++;
                    return Task.CompletedTask;
                }));
        }

        TimeoutException actual = await Assert.ThrowsAsync<TimeoutException>(OperationAsync);

        Assert.Same(root, actual);
        Assert.Equal(1, disposals);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASYNC-DISPOSAL", "pending-cleanup-is-awaited-before-original-failure")]
    public async Task PendingCleanup_IsAwaitedBeforeRethrowingTheOriginalFailureAsync(bool valueTaskCallback)
    {
        var failure = new UnsafeBaseFailure(false);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int disposals = 0;

        Task<int> operation = valueTaskCallback
            ? failure.DisposeAsync<int>((Func<ValueTask>)(() =>
            {
                disposals++;
                return new ValueTask(release.Task);
            }), TestContext.Current.CancellationToken).AsTask()
            : failure.DisposeAsync<int>((Func<Task>)(() =>
            {
                disposals++;
                return release.Task;
            }), TestContext.Current.CancellationToken).AsTask();

        Assert.Equal(1, disposals);
        Assert.False(operation.IsCompleted);

        release.TrySetResult();
        UnsafeBaseFailure actual = await Assert.ThrowsAsync<UnsafeBaseFailure>(() =>
            operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

        Assert.Same(failure, actual);
        Assert.Equal(1, disposals);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-ASYNC-DISPOSAL", "cleanup-failure-preserves-original-before-cleanup-cause")]
    public async Task CleanupFailure_PreservesTheOriginalBeforeTheCleanupCauseAsync(bool valueTaskCallback, bool wrapped)
    {
        var root = new TimeoutException("root failure");
        Exception original = wrapped
            ? new InvalidOperationException("outer", root)
            : new UnsafeBaseFailure(false);
        var cleanup = new InvalidOperationException("cleanup failed");
        int disposals = 0;

        Task<int> operation = valueTaskCallback
            ? original.DisposeAsync<int>((Func<ValueTask>)(() =>
            {
                disposals++;
                return ValueTask.FromException(cleanup);
            }), TestContext.Current.CancellationToken).AsTask()
            : original.DisposeAsync<int>((Func<Task>)(() =>
            {
                disposals++;
                return Task.FromException(cleanup);
            }), TestContext.Current.CancellationToken).AsTask();

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() => operation);

        Assert.Collection(actual.InnerExceptions,
            exception => Assert.Same(wrapped ? root : original, exception),
            exception => Assert.Same(cleanup, exception));
        Assert.Equal(1, disposals);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASYNC-DISPOSAL", "pre-cancellation-skips-cleanup-and-base-lookup")]
    public async Task PreCanceledToken_SkipsCleanupAndExceptionDiagnosticsAsync(bool valueTaskCallback)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var failure = new UnsafeBaseFailure(false);
        int disposals = 0;

        Task<int> operation = valueTaskCallback
            ? failure.DisposeAsync<int>((Func<ValueTask>)(() =>
            {
                disposals++;
                return ValueTask.CompletedTask;
            }), cancellation.Token).AsTask()
            : failure.DisposeAsync<int>((Func<Task>)(() =>
            {
                disposals++;
                return Task.CompletedTask;
            }), cancellation.Token).AsTask();

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        Assert.Equal(cancellation.Token, actual.CancellationToken);
        Assert.Equal(0, disposals);
        Assert.Equal(0, failure.BaseLookupCount);
    }

    private sealed class UnsafeBaseFailure(bool nullBase) : Exception("original failure")
    {
        public int BaseLookupCount { get; private set; }

        public override Exception GetBaseException()
        {
            BaseLookupCount++;
            return nullBase ? null! : throw new InvalidOperationException("base lookup failed");
        }
    }
}
