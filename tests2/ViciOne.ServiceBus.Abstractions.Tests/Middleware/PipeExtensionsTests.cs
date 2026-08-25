using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Middleware;
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
    public async Task OneTimeSetup_ExecutesOnceForEveryConcurrentCaller()
    {
        var context = new TestPipeContext();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackCount = 0;

        Task<OneTimeContext<SetupMarker>> first = context.OneTimeSetup<SetupMarker>(async () =>
        {
            Interlocked.Increment(ref callbackCount);
            entered.SetResult();
            await release.Task;
        });
        await entered.Task;

        Task<OneTimeContext<SetupMarker>>[] remaining = Enumerable.Range(0, 31)
            .Select(_ => context.OneTimeSetup<SetupMarker>(() =>
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
    public async Task OneTimeSetup_AfterAnIsolatedFailureAllowsAHealthyRetry()
    {
        var context = new TestPipeContext();
        var expected = new SetupException("first attempt");
        var callbackCount = 0;

        SetupException actual = await Assert.ThrowsAsync<SetupException>(() =>
            context.OneTimeSetup<SetupMarker>(() =>
            {
                Interlocked.Increment(ref callbackCount);
                return Task.FromException(expected);
            }));

        OneTimeContext<SetupMarker> result = await context.OneTimeSetup<SetupMarker>(() =>
        {
            Interlocked.Increment(ref callbackCount);
            return Task.CompletedTask;
        });
        OneTimeContext<SetupMarker> cached = await context.OneTimeSetup<SetupMarker>(() =>
        {
            Interlocked.Increment(ref callbackCount);
            return Task.CompletedTask;
        });

        Assert.Same(expected, actual);
        Assert.NotNull(result);
        Assert.Same(result, cached);
        Assert.Equal(2, callbackCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ONE-TIME-SETUP", "queued-fallback")]
    public async Task OneTimeSetup_AQueuedHealthyCallerCompletesAfterTheLeaderFails()
    {
        var context = new TestPipeContext();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var expected = new SetupException("leader failed");
        var callbackCount = 0;

        Task<OneTimeContext<SetupMarker>> leader = context.OneTimeSetup<SetupMarker>(async () =>
        {
            Interlocked.Increment(ref callbackCount);
            entered.SetResult();
            await release.Task;
            throw expected;
        });
        await entered.Task;

        Task<OneTimeContext<SetupMarker>> fallback = context.OneTimeSetup<SetupMarker>(() =>
        {
            Interlocked.Increment(ref callbackCount);
            return Task.CompletedTask;
        });
        release.SetResult();

        SetupException actual = await Assert.ThrowsAsync<SetupException>(() => leader);
        OneTimeContext<SetupMarker> result = await fallback;

        Assert.Same(expected, actual);
        Assert.NotNull(result);
        Assert.Equal(2, callbackCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ONE-TIME-SETUP", "eviction-lifecycle")]
    public async Task OneTimeSetup_EvictionRerunsSetupAndRejectsEvictionWhileRunning()
    {
        var context = new TestPipeContext();
        var callbackCount = 0;
        OneTimeContext<SetupMarker> control = await context.OneTimeSetup<SetupMarker>(() =>
        {
            Interlocked.Increment(ref callbackCount);
            return Task.CompletedTask;
        });
        control.Evict();

        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<OneTimeContext<SetupMarker>> rerun = context.OneTimeSetup<SetupMarker>(async () =>
        {
            Interlocked.Increment(ref callbackCount);
            entered.SetResult();
            await release.Task;
        });
        await entered.Task;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(control.Evict);
        release.SetResult();
        await rerun;

        Assert.Contains("while it is running", exception.Message, StringComparison.Ordinal);
        Assert.Equal(2, callbackCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ONE-TIME-SETUP", "cancellation-and-invalid-callback")]
    public async Task OneTimeSetup_PreservesCancellationAndRejectsANullCallbackTaskWithoutPoisoningTheContext()
    {
        var context = new TestPipeContext();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            context.OneTimeSetup<SetupMarker>(() => Task.FromCanceled(cancellation.Token)));
        InvalidOperationException invalid = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.OneTimeSetup<SetupMarker>(() => null!));
        OneTimeContext<SetupMarker> result = await context.OneTimeSetup<SetupMarker>(() => Task.CompletedTask);

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
