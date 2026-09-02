using ViciOne.ServiceBus.AmazonSqsTransport;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

// Caller and store-lifetime cancellation are the behavior under test; every coordination
// point that is not deliberately cancellation-controlled is bounded by OperationTimeout.
#pragma warning disable xUnit1051

namespace ViciOne.ServiceBus.AmazonSqsTransport.Tests;

public sealed class DurableResourceStoreTests
{
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "durable-store-single-flight-and-caller-cancellation-isolation")]
    public async Task ConcurrentWaiters_ShareOneFactoryAndCallerCancellationDoesNotOwnCreation()
    {
        await using var store = new DurableResourceStore<string, TrackedResource>(TestContext.Current.CancellationToken);
        using var callerCancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var expected = new TrackedResource();
        var factoryCalls = 0;

        Task<TrackedResource> canceledCaller = store.GetOrAddAsync(
            "queue",
            async (_, ownerToken) =>
            {
                Interlocked.Increment(ref factoryCalls);
                started.TrySetResult();
                await release.Task.WaitAsync(ownerToken);
                return expected;
            },
            callerCancellation.Token);
        await started.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Task<TrackedResource> survivingCaller = store.GetOrAddAsync(
            "queue",
            (_, _) => throw new InvalidOperationException("A waiter must never replace the owner factory."),
            TestContext.Current.CancellationToken);

        callerCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledCaller);
        release.TrySetResult();

        Assert.Same(expected, await survivingCaller.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
        Assert.Equal(1, Volatile.Read(ref factoryCalls));
        Assert.True(store.TryGet("queue", out TrackedResource? cached));
        Assert.Same(expected, cached);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "durable-store-faulted-creation-is-retryable")]
    public async Task FaultedFactory_IsEvictedAndTheNextAttemptCanRecover()
    {
        await using var store = new DurableResourceStore<string, TrackedResource>(TestContext.Current.CancellationToken);
        var expectedFailure = new InvalidOperationException("first attempt failed");
        var recovered = new TrackedResource();
        var calls = 0;

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => store.GetOrAddAsync(
            "topic",
            (_, _) =>
            {
                Interlocked.Increment(ref calls);
                return ValueTask.FromException<TrackedResource>(expectedFailure);
            },
            TestContext.Current.CancellationToken));
        TrackedResource actualResource = await store.GetOrAddAsync(
            "topic",
            (_, _) =>
            {
                Interlocked.Increment(ref calls);
                return ValueTask.FromResult(recovered);
            },
            TestContext.Current.CancellationToken);

        Assert.Same(expectedFailure, actual);
        Assert.Same(recovered, actualResource);
        Assert.Equal(2, Volatile.Read(ref calls));
        Assert.True(store.TryGet("topic", out TrackedResource? cached));
        Assert.Same(recovered, cached);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "durable-store-pending-removal-releases-late-value")]
    public async Task RemoveAsync_WaitsForPendingOwnershipAndDisposesALateValueExactlyOnce()
    {
        await using var store = new DurableResourceStore<string, TrackedResource>(TestContext.Current.CancellationToken);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ownerCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lateValue = new TrackedResource();

        Task<TrackedResource> creation = store.GetOrAddAsync(
            "queue",
            async (_, ownerToken) =>
            {
                using CancellationTokenRegistration registration = ownerToken.Register(() => ownerCanceled.TrySetResult());
                started.TrySetResult();
                await release.Task.WaitAsync(TestContext.Current.CancellationToken);
                return lateValue;
            },
            TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Task<bool> removal = store.RemoveAsync("queue");
        await ownerCanceled.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Assert.False(removal.IsCompleted);

        release.TrySetResult();

        Assert.True(await removal.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => creation);
        Assert.Equal(1, lateValue.DisposalCount);
        Assert.False(store.TryGet("queue", out _));
        Assert.False(await store.RemoveAsync("queue"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "durable-store-cancellation-callbacks-run-outside-state-lock")]
    public async Task OwnershipCancellationCallbacks_CanReadStoreStateWithoutLockInversion(bool disposeStore)
    {
        await using var store = new DurableResourceStore<string, TrackedResource>(TestContext.Current.CancellationToken);
        var stable = new TrackedResource();
        var pendingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackResult = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        await store.GetOrAddAsync("stable", (_, _) => ValueTask.FromResult(stable), TestContext.Current.CancellationToken);
        Task<TrackedResource> pending = store.GetOrAddAsync(
            "pending",
            async (_, ownerToken) =>
            {
                using CancellationTokenRegistration registration = ownerToken.Register(() =>
                {
                    Task<bool> reentrantRead = Task.Run(() =>
                    {
                        try
                        {
                            return store.TryGet("stable", out TrackedResource? actual) && ReferenceEquals(stable, actual);
                        }
                        catch (ObjectDisposedException)
                        {
                            return disposeStore;
                        }
                    });

                    bool completed = reentrantRead.Wait(OperationTimeout);
                    callbackResult.TrySetResult(completed && reentrantRead.IsCompletedSuccessfully && reentrantRead.Result);
                });

                pendingStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, ownerToken);
                return new TrackedResource();
            },
            TestContext.Current.CancellationToken);
        await pendingStarted.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        if (disposeStore)
            await store.DisposeAsync().AsTask().WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        else
            Assert.True(await store.RemoveAsync("pending").WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));

        Assert.True(await callbackResult.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "durable-store-cancellation-callback-fault-is-contained")]
    public async Task ThrowingOwnershipCancellationCallback_DoesNotAbortRelease(bool disposeStore)
    {
        await using var store = new DurableResourceStore<string, TrackedResource>(TestContext.Current.CancellationToken);
        var pendingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackInvocations = 0;

        Task<TrackedResource> pending = store.GetOrAddAsync(
            "pending",
            async (_, ownerToken) =>
            {
                using CancellationTokenRegistration registration = ownerToken.Register(() =>
                {
                    Interlocked.Increment(ref callbackInvocations);
                    throw new InvalidOperationException("cancellation observer failed");
                });

                pendingStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, ownerToken);
                return new TrackedResource();
            },
            TestContext.Current.CancellationToken);
        await pendingStarted.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        if (disposeStore)
            await store.DisposeAsync().AsTask().WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        else
            Assert.True(await store.RemoveAsync("pending").WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(1, Volatile.Read(ref callbackInvocations));
        if (!disposeStore)
            Assert.False(store.TryGet("pending", out _));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "durable-store-retains-values-until-explicit-release")]
    public async Task CompletedResources_RemainOwnedUntilRemovalOrStoreDisposal()
    {
        var store = new DurableResourceStore<string, TrackedResource>(TestContext.Current.CancellationToken);
        TrackedResource[] resources = Enumerable.Range(0, 32).Select(_ => new TrackedResource()).ToArray();

        for (var index = 0; index < resources.Length; index++)
        {
            string key = $"resource-{index:D2}";
            Assert.Same(
                resources[index],
                await store.GetOrAddAsync(key, (_, _) => ValueTask.FromResult(resources[index]), TestContext.Current.CancellationToken));
        }

        Assert.All(resources, resource => Assert.Equal(0, resource.DisposalCount));
        Assert.True(await store.RemoveAsync("resource-00"));
        Assert.Equal(1, resources[0].DisposalCount);
        Assert.All(resources[1..], resource => Assert.Equal(0, resource.DisposalCount));

        await store.DisposeAsync();

        Assert.All(resources, resource => Assert.Equal(1, resource.DisposalCount));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "durable-store-disposal-cancels-pending-and-is-idempotent")]
    public async Task DisposeAsync_CancelsPendingCreationAndReleasesCommittedResourcesExactlyOnce()
    {
        var store = new DurableResourceStore<string, TrackedResource>(TestContext.Current.CancellationToken);
        var committed = new TrackedResource();
        var pendingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Assert.Same(
            committed,
            await store.GetOrAddAsync("committed", (_, _) => ValueTask.FromResult(committed), TestContext.Current.CancellationToken));
        Task<TrackedResource> pending = store.GetOrAddAsync(
            "pending",
            async (_, ownerToken) =>
            {
                pendingStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, ownerToken);
                return new TrackedResource();
            },
            TestContext.Current.CancellationToken);
        await pendingStarted.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        await store.DisposeAsync().AsTask().WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        await store.DisposeAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(1, committed.DisposalCount);
        Assert.Throws<ObjectDisposedException>(() => store.TryGet("committed", out _));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => store.GetOrAddAsync(
            "after-disposal",
            (_, _) => ValueTask.FromResult(new TrackedResource()),
            TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => store.RemoveAsync("committed"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "durable-store-concurrent-disposal-shares-one-completion")]
    public async Task ConcurrentDisposeAsync_WaitsForTheSamePendingOwnershipAndReleasesTheLateResourceOnce()
    {
        var store = new DurableResourceStore<string, TrackedResource>(TestContext.Current.CancellationToken);
        var pendingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ownerCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lateResource = new TrackedResource();

        Task<TrackedResource> pending = store.GetOrAddAsync(
            "pending",
            async (_, ownerToken) =>
            {
                using CancellationTokenRegistration registration = ownerToken.Register(() => ownerCanceled.TrySetResult());
                pendingStarted.TrySetResult();
                await release.Task.WaitAsync(TestContext.Current.CancellationToken);
                return lateResource;
            },
            TestContext.Current.CancellationToken);
        await pendingStarted.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Task firstDisposal = store.DisposeAsync().AsTask();
        await ownerCanceled.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Task secondDisposal = store.DisposeAsync().AsTask();
        bool secondCompletedBeforeOwnershipReleased = secondDisposal.IsCompleted;

        release.TrySetResult();
        await Task.WhenAll(firstDisposal, secondDisposal).WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.False(secondCompletedBeforeOwnershipReleased);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(1, lateResource.DisposalCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "durable-store-disposal-fault-is-contained")]
    public async Task DisposalFault_DoesNotPreventRemainingResourcesFromBeingReleased()
    {
        var store = new DurableResourceStore<string, TrackedResource>(TestContext.Current.CancellationToken);
        var faulting = new TrackedResource(new InvalidOperationException("dispose failed"));
        var healthy = new TrackedResource();

        await store.GetOrAddAsync("faulting", (_, _) => ValueTask.FromResult(faulting), TestContext.Current.CancellationToken);
        await store.GetOrAddAsync("healthy", (_, _) => ValueTask.FromResult(healthy), TestContext.Current.CancellationToken);

        await store.DisposeAsync();

        Assert.Equal(1, faulting.DisposalCount);
        Assert.Equal(1, healthy.DisposalCount);
    }

    private sealed class TrackedResource(Exception? disposalFailure = null) : IAsyncDisposable
    {
        private int _disposalCount;

        public int DisposalCount => Volatile.Read(ref _disposalCount);

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposalCount);
            return disposalFailure is null ? ValueTask.CompletedTask : ValueTask.FromException(disposalFailure);
        }
    }
}
