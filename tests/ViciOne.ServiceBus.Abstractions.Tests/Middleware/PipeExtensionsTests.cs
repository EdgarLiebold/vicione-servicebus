using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Middleware;

public sealed class PipeExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-EMPTY-CLASSIFICATION", "null-empty-executable")]
    public void EmptyClassification_DistinguishesMissingEmptyAndExecutablePipes()
    {
        IPipe<SendContext>? missing = null;
        IPipe<SendContext> empty = Pipe.Empty<SendContext>();
        IPipe<SendContext> executable = Pipe.Execute<SendContext>(_ => { });

        Assert.True(missing.IsEmpty());
        Assert.False(missing.IsNotEmpty());
        Assert.True(empty.IsEmpty());
        Assert.False(empty.IsNotEmpty());
        Assert.False(executable.IsEmpty());
        Assert.True(executable.IsNotEmpty());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ONE-TIME-SETUP", "concurrent-single-flight")]
    public async Task OneTimeSetup_ExecutesOnceForEveryConcurrentCallerAsync()
    {
        var context = new TestPipeContext();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackCount = 0;

        Task<OneTimeContext<SetupMarker>> first = context.OneTimeSetupAsync<SetupMarker>(async () =>
        {
            Interlocked.Increment(ref callbackCount);
            entered.SetResult();
            await release.Task;
        }, cancellationToken: TestContext.Current.CancellationToken);
        await entered.Task;

        Task<OneTimeContext<SetupMarker>>[] remaining = Enumerable.Range(0, 31)
            .Select(_ => context.OneTimeSetupAsync<SetupMarker>(() =>
            {
                Interlocked.Increment(ref callbackCount);
                return Task.CompletedTask;
            }))
            .ToArray();

        release.SetResult();
        OneTimeContext<SetupMarker>[] results = await Task.WhenAll(remaining.Prepend(first));

        Assert.Equal(1, callbackCount);
        Assert.All(results, result => Assert.Same(results[0], result));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ONE-TIME-SETUP", "failure-is-retryable")]
    public async Task OneTimeSetup_AfterAnIsolatedFailureAllowsAHealthyRetryAsync()
    {
        var context = new TestPipeContext();
        var expected = new SetupException("first attempt");
        var callbackCount = 0;

        SetupException actual = await Assert.ThrowsAsync<SetupException>(() =>
            context.OneTimeSetupAsync<SetupMarker>(() =>
            {
                Interlocked.Increment(ref callbackCount);
                return Task.FromException(expected);
            }, cancellationToken: TestContext.Current.CancellationToken));

        OneTimeContext<SetupMarker> result = await context.OneTimeSetupAsync<SetupMarker>(() =>
        {
            Interlocked.Increment(ref callbackCount);
            return Task.CompletedTask;
        }, cancellationToken: TestContext.Current.CancellationToken);
        OneTimeContext<SetupMarker> cached = await context.OneTimeSetupAsync<SetupMarker>(() =>
        {
            Interlocked.Increment(ref callbackCount);
            return Task.CompletedTask;
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Same(expected, actual);
        Assert.NotNull(result);
        Assert.Same(result, cached);
        Assert.Equal(2, callbackCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ONE-TIME-SETUP", "concurrent-failure-shared-and-later-retry")]
    public async Task OneTimeSetup_ConcurrentCallersShareTheFailedAttemptAndOnlyALaterCallerRetriesAsync()
    {
        var context = new TestPipeContext();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var expected = new SetupException("leader failed");
        var callbackCount = 0;

        Task<OneTimeContext<SetupMarker>> leader = context.OneTimeSetupAsync<SetupMarker>(async () =>
        {
            Interlocked.Increment(ref callbackCount);
            entered.SetResult();
            await release.Task;
            throw expected;
        }, cancellationToken: TestContext.Current.CancellationToken);
        await entered.Task;

        Task<OneTimeContext<SetupMarker>> concurrent = context.OneTimeSetupAsync<SetupMarker>(() =>
        {
            Interlocked.Increment(ref callbackCount);
            return Task.FromException(new SetupException("a concurrent callback must not run"));
        }, cancellationToken: TestContext.Current.CancellationToken);
        release.SetResult();

        SetupException leaderFailure = await Assert.ThrowsAsync<SetupException>(() => leader);
        SetupException concurrentFailure = await Assert.ThrowsAsync<SetupException>(() => concurrent);
        OneTimeContext<SetupMarker> retry = await context.OneTimeSetupAsync<SetupMarker>(() =>
        {
            Interlocked.Increment(ref callbackCount);
            return Task.CompletedTask;
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Same(expected, leaderFailure);
        Assert.Same(expected, concurrentFailure);
        Assert.NotNull(retry);
        Assert.Equal(2, callbackCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ONE-TIME-SETUP", "eviction-lifecycle")]
    public async Task OneTimeSetup_EvictionRerunsSetupAndRejectsEvictionWhileRunningAsync()
    {
        var context = new TestPipeContext();
        var callbackCount = 0;
        OneTimeContext<SetupMarker> control = await context.OneTimeSetupAsync<SetupMarker>(() =>
        {
            Interlocked.Increment(ref callbackCount);
            return Task.CompletedTask;
        }, cancellationToken: TestContext.Current.CancellationToken);
        control.Evict();

        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<OneTimeContext<SetupMarker>> rerun = context.OneTimeSetupAsync<SetupMarker>(async () =>
        {
            Interlocked.Increment(ref callbackCount);
            entered.SetResult();
            await release.Task;
        }, cancellationToken: TestContext.Current.CancellationToken);
        await entered.Task;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(control.Evict);
        release.SetResult();
        await rerun;

        Assert.Contains("while it is running", exception.Message, StringComparison.Ordinal);
        Assert.Equal(2, callbackCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ONE-TIME-SETUP", "cancellation-and-invalid-callback")]
    public async Task OneTimeSetup_PreservesCancellationAndRejectsANullCallbackTaskWithoutPoisoningTheContextAsync()
    {
        var context = new TestPipeContext();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            context.OneTimeSetupAsync<SetupMarker>(() => Task.FromCanceled(cancellation.Token), cancellationToken: TestContext.Current.CancellationToken));
        InvalidOperationException invalid = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.OneTimeSetupAsync<SetupMarker>(() => null!, cancellationToken: TestContext.Current.CancellationToken));
        OneTimeContext<SetupMarker> result = await context.OneTimeSetupAsync<SetupMarker>(() => Task.CompletedTask, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(cancellation.Token, canceled.CancellationToken);
        Assert.Equal("The one-time setup callback returned null.", invalid.Message);
        Assert.NotNull(result);
    }

    private sealed class TestPipeContext : BasePipeContext
    {
    }

    private sealed class SetupMarker
    {
    }

    private sealed class SetupException(string message) : Exception(message);
}
