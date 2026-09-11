using System.Reflection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class SendEndpointCacheTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-CACHE-LIFETIME", "dispose-awaits-and-releases-owned-endpoint-once")]
    public async Task DisposeAsync_AwaitsAndReleasesTheOwnedEndpointExactlyOnceAsync()
    {
        var cache = new SendEndpointCache<string>();
        TrackedTransportEndpoint endpoint = DispatchProxy.Create<TrackedTransportEndpoint, TrackedTransportEndpointProxy>();
        var tracker = (TrackedTransportEndpointProxy)(object)endpoint;

        ISendEndpoint first = await cache.GetSendEndpointAsync("queue-a", (_, _) => Task.FromResult<ISendEndpoint>(endpoint), TestContext.Current.CancellationToken);
        ISendEndpoint second = await cache.GetSendEndpointAsync("queue-a", (_, _) => throw new InvalidOperationException("The cached value must win."), TestContext.Current.CancellationToken);

        Assert.Same(first, second);
        Assert.IsAssignableFrom<IAsyncDisposable>(cache);

        Task disposal = cache.DisposeAsync().AsTask();
        await tracker.DisposeStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        Assert.False(disposal.IsCompleted);
        Assert.Equal(1, tracker.DisposeCount);

        tracker.ReleaseDisposal();
        await disposal.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, tracker.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-CACHE-LIFETIME", "creation-uses-cache-owned-cancellation")]
    public async Task CacheMiss_SeparatesCallerWaitCancellationFromOwnedCreationCancellationAsync()
    {
        var cache = new SendEndpointCache<string>();
        var creationStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var creationCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var callerCancellation = new CancellationTokenSource();
        Task<ISendEndpoint> lookup = cache.GetSendEndpointAsync(
            "queue-a",
            async (_, cancellationToken) =>
            {
                creationStarted.TrySetResult(cancellationToken);
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    throw new InvalidOperationException("The cache-owned creation token was expected to be canceled.");
                }
                catch (OperationCanceledException)
                {
                    creationCanceled.TrySetResult();
                    throw;
                }
            },
            callerCancellation.Token);

        CancellationToken creationToken = await creationStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.NotEqual(callerCancellation.Token, creationToken);
        Assert.True(creationToken.CanBeCanceled);

        callerCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => lookup);
        Assert.False(creationToken.IsCancellationRequested);

        Task disposal = cache.DisposeAsync().AsTask();
        await creationCanceled.Task.WaitAsync(TestContext.Current.CancellationToken);
        await disposal.WaitAsync(TestContext.Current.CancellationToken);
        Assert.True(creationToken.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-CACHE-IDENTITY", "concurrent-cold-and-warm-addresses")]
    public async Task ConcurrentColdAndWarmLookups_PreserveIdentityPerAddressAsync()
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"send-endpoint-cache-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(operationTimeout, cancellationToken);
            var firstAddress = new Uri(harness.BaseAddress, "queue-a");
            var secondAddress = new Uri(harness.BaseAddress, "queue-b");

            ISendEndpoint[] cold = await Task.WhenAll(
                    harness.Bus.GetSendEndpointAsync(firstAddress, cancellationToken),
                    harness.Bus.GetSendEndpointAsync(secondAddress, cancellationToken))
                .WaitAsync(operationTimeout, cancellationToken);
            ISendEndpoint[] warm = await Task.WhenAll(
                    harness.Bus.GetSendEndpointAsync(firstAddress, cancellationToken),
                    harness.Bus.GetSendEndpointAsync(secondAddress, cancellationToken))
                .WaitAsync(operationTimeout, cancellationToken);

            Assert.Same(cold[0], warm[0]);
            Assert.Same(cold[1], warm[1]);
            Assert.NotSame(cold[0], cold[1]);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(operationTimeout, cancellationToken);
        }
    }

    public interface TrackedTransportEndpoint : ITransportSendEndpoint, IAsyncDisposable;

    public class TrackedTransportEndpointProxy : DispatchProxy
    {
        private readonly TaskCompletionSource _disposeStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseDisposal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _disposeCount;

        public int DisposeCount => Volatile.Read(ref _disposeCount);
        public TaskCompletionSource DisposeStarted => _disposeStarted;

        public void ReleaseDisposal() => _releaseDisposal.TrySetResult();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(IAsyncDisposable.DisposeAsync))
                throw new InvalidOperationException($"Unexpected endpoint call: {targetMethod?.Name ?? "<null>"}.");

            Interlocked.Increment(ref _disposeCount);
            _disposeStarted.TrySetResult();
            return new ValueTask(_releaseDisposal.Task);
        }
    }
}
