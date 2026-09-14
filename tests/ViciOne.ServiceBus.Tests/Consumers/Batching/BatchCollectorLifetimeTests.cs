using ViciOne.ServiceBus.Batching.Runtime;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers.Batching;

public sealed class BatchCollectorLifetimeTests
{
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-LIFETIME", "disposal-waits-for-admitted-work-and-flushes-once")]
    public async Task Disposal_WaitsForAdmittedWorkFlushesOnceAndStopsBothExecutorsAsync()
    {
        var lifetime = new BatchCollectorLifetime(dispatcherConcurrencyLimit: 2);
        var flushCount = 0;
        Assert.True(lifetime.TryBeginOperation());

        Task firstDisposal = lifetime.DisposeAsync(() =>
        {
            flushCount++;
            return Task.CompletedTask;
        }).AsTask();
        Task secondDisposal = lifetime.DisposeAsync(() =>
        {
            flushCount++;
            return Task.CompletedTask;
        }).AsTask();

        Assert.Same(firstDisposal, secondDisposal);
        Assert.False(firstDisposal.IsCompleted);
        Assert.False(lifetime.TryBeginOperation());

        lifetime.CompleteOperation();
        await firstDisposal.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.Equal(1, flushCount);
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            lifetime.Collector.ExecuteAsync(static () => Task.CompletedTask, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            lifetime.Dispatcher.ExecuteAsync(static () => Task.CompletedTask, TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-LIFETIME", "flush-failure-is-preserved-after-both-executors-drain")]
    public async Task Disposal_PreservesTheFlushFailureAfterStoppingBothExecutorsAsync()
    {
        var lifetime = new BatchCollectorLifetime(dispatcherConcurrencyLimit: 1);
        var failure = new InvalidOperationException("flush failed");

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            lifetime.DisposeAsync(() => Task.FromException(failure)).AsTask()
                .WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
        InvalidOperationException repeated = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            lifetime.DisposeAsync(static () => Task.CompletedTask).AsTask()
                .WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));

        Assert.Same(failure, exception);
        Assert.Same(failure, repeated);
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            lifetime.Collector.ExecuteAsync(static () => Task.CompletedTask, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            lifetime.Dispatcher.ExecuteAsync(static () => Task.CompletedTask, TestContext.Current.CancellationToken));
    }
}
