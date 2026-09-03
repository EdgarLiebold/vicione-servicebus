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
    public async Task DisposeAsync_AwaitsAndReleasesTheOwnedEndpointExactlyOnce()
    {
        var cache = new SendEndpointCache<string>();
        TrackedTransportEndpoint endpoint = DispatchProxy.Create<TrackedTransportEndpoint, TrackedTransportEndpointProxy>();
        var tracker = (TrackedTransportEndpointProxy)(object)endpoint;

        ISendEndpoint first = await cache.GetSendEndpoint("queue-a", _ => Task.FromResult<ISendEndpoint>(endpoint));
        ISendEndpoint second = await cache.GetSendEndpoint("queue-a", _ => throw new InvalidOperationException("The cached value must win."));

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
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-CACHE-IDENTITY", "concurrent-cold-and-warm-addresses")]
    public async Task ConcurrentColdAndWarmLookups_PreserveIdentityPerAddress()
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
            await harness.Start(cancellationToken).WaitAsync(operationTimeout, cancellationToken);
            var firstAddress = new Uri(harness.BaseAddress, "queue-a");
            var secondAddress = new Uri(harness.BaseAddress, "queue-b");

            ISendEndpoint[] cold = await Task.WhenAll(
                    harness.Bus.GetSendEndpoint(firstAddress),
                    harness.Bus.GetSendEndpoint(secondAddress))
                .WaitAsync(operationTimeout, cancellationToken);
            ISendEndpoint[] warm = await Task.WhenAll(
                    harness.Bus.GetSendEndpoint(firstAddress),
                    harness.Bus.GetSendEndpoint(secondAddress))
                .WaitAsync(operationTimeout, cancellationToken);

            Assert.Same(cold[0], warm[0]);
            Assert.Same(cold[1], warm[1]);
            Assert.NotSame(cold[0], cold[1]);
        }
        finally
        {
            await harness.Stop().WaitAsync(operationTimeout, cancellationToken);
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
