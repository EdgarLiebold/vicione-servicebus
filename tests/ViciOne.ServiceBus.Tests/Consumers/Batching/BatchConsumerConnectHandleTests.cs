using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers.Batching;

public sealed class BatchConsumerConnectHandleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CONNECTION-LIFECYCLE", "constructor-boundaries")]
    public void Constructor_RejectsEveryMissingOwnedResource()
    {
        var connection = new RecordingConnectHandle();
        var lifetime = new RecordingAsyncDisposable();

        Assert.Equal(
            "handle",
            Assert.Throws<ArgumentNullException>(() => new BatchConsumerConnectHandle(null!, lifetime)).ParamName);
        Assert.Equal(
            "batchLifetime",
            Assert.Throws<ArgumentNullException>(() => new BatchConsumerConnectHandle(connection, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CONNECTION-LIFECYCLE", "idempotent-disconnect-and-awaited-drain")]
    public async Task EveryTerminationForm_SharesOneDisconnectAndOneAwaitedCleanupAsync()
    {
        var cleanupRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var connection = new RecordingConnectHandle();
        var lifetime = new RecordingAsyncDisposable(cleanup: () => new ValueTask(cleanupRelease.Task));
        var handle = new BatchConsumerConnectHandle(connection, lifetime);

        Task first = handle.DisposeAsync().AsTask();
        Task second = handle.DisposeAsync().AsTask();
        handle.Disconnect();
        handle.Dispose();

        Assert.Same(first, second);
        Assert.False(first.IsCompleted);
        Assert.Equal(1, connection.DisconnectCount);
        Assert.Equal(1, lifetime.DisposeCount);

        cleanupRelease.SetResult();
        await Task.WhenAll(first, second).WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, connection.DisconnectCount);
        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CONNECTION-LIFECYCLE", "disconnect-failure-after-cleanup")]
    public async Task DisposeAsync_PreservesADisconnectFailureAfterCleanupAsync()
    {
        var disconnectFailure = new InvalidOperationException("disconnect failed");
        var lifetime = new RecordingAsyncDisposable();
        var handle = new BatchConsumerConnectHandle(
            new RecordingConnectHandle(disconnectFailure),
            lifetime);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handle.DisposeAsync());

        Assert.Same(disconnectFailure, exception);
        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-BATCH-CONNECTION-LIFECYCLE", "asynchronous-and-synchronous-cleanup-failure")]
    public async Task DisposeAsync_PreservesEveryCleanupFailureFormAsync(bool synchronous)
    {
        var cleanupFailure = new InvalidOperationException("cleanup failed");
        var connection = new RecordingConnectHandle();
        var lifetime = new RecordingAsyncDisposable(cleanupFailure, synchronous);
        var handle = new BatchConsumerConnectHandle(connection, lifetime);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handle.DisposeAsync());

        Assert.Same(cleanupFailure, exception);
        Assert.Equal(1, connection.DisconnectCount);
        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CONNECTION-LIFECYCLE", "independent-failures-aggregated-in-owner-order")]
    public async Task DisposeAsync_AggregatesIndependentDisconnectAndCleanupFailuresAsync()
    {
        var disconnectFailure = new InvalidOperationException("disconnect failed");
        var cleanupFailure = new InvalidOperationException("cleanup failed");
        var handle = new BatchConsumerConnectHandle(
            new RecordingConnectHandle(disconnectFailure),
            new RecordingAsyncDisposable(cleanupFailure));

        AggregateException exception = await Assert.ThrowsAsync<AggregateException>(async () =>
            await handle.DisposeAsync());

        Assert.Collection(
            exception.InnerExceptions,
            failure => Assert.Same(disconnectFailure, failure),
            failure => Assert.Same(cleanupFailure, failure));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CONNECTION-LIFECYCLE", "synchronous-disconnect-observes-cleanup-failure")]
    public async Task Disconnect_ObservesTheSharedCleanupWithoutChangingItsAwaitedOutcomeAsync()
    {
        var cleanupFailure = new InvalidOperationException("cleanup failed");
        var connection = new RecordingConnectHandle();
        var lifetime = new RecordingAsyncDisposable(cleanupFailure);
        var handle = new BatchConsumerConnectHandle(connection, lifetime);

        handle.Disconnect();
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handle.DisposeAsync());

        Assert.Same(cleanupFailure, exception);
        Assert.Equal(1, connection.DisconnectCount);
        Assert.Equal(1, lifetime.DisposeCount);
    }

    private sealed class RecordingConnectHandle(Exception? failure = null) : ConnectHandle
    {
        public int DisconnectCount { get; private set; }

        public void Dispose() => Disconnect();

        public void Disconnect()
        {
            DisconnectCount++;
            if (failure != null)
                throw failure;
        }
    }

    private sealed class RecordingAsyncDisposable(
        Exception? failure = null,
        bool synchronousFailure = false,
        Func<ValueTask>? cleanup = null) : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            if (failure != null)
            {
                if (synchronousFailure)
                    throw failure;

                return new ValueTask(Task.FromException(failure));
            }

            return cleanup?.Invoke() ?? ValueTask.CompletedTask;
        }
    }
}
