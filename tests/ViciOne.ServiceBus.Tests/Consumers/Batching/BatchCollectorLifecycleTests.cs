using System.Collections.Concurrent;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Batching;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers.Batching;

public sealed class BatchCollectorLifecycleTests
{
    private static readonly DateTimeOffset StartTime = new(2041, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-LIFETIME", "terminal-disposal-flushes-partial-batch-once")]
    public async Task Disposal_FlushesAPartialBatchOnceAndRejectsFurtherAdmissionsAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<Batch<LifecycleItem>>();
        var collector = new BatchCollector<LifecycleItem>(CreateOptions(), new CaptureBatchPipe(delivered));
        ConsumeContext<LifecycleItem> context = CreateContext(new LifecycleItem("alpha", 1), timeProvider);

        BatchConsumer<LifecycleItem> consumer = await collector.CollectAsync(context, TestContext.Current.CancellationToken);
        Task consume = consumer.ConsumeAsync(context);
        Assert.False(consume.IsCompleted);
        Assert.Equal(1, timeProvider.ActiveTimerCount);

        Task firstDisposal = collector.DisposeAsync().AsTask();
        Task secondDisposal = collector.DisposeAsync().AsTask();
        await Task.WhenAll(firstDisposal, secondDisposal)
            .WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Batch<LifecycleItem> batch = Assert.Single(delivered);
        Assert.Equal(BatchCompletionMode.Forced, batch.Mode);
        Assert.Equal(1, batch.Length);
        Assert.Equal(("alpha", 1), (batch[0].Message.Group, batch[0].Message.Sequence));
        Assert.Equal(0, timeProvider.ActiveTimerCount);
        await consume.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        ConsumeContext<LifecycleItem> rejected = CreateContext(new LifecycleItem("alpha", 2), timeProvider);
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            collector.CollectAsync(rejected, TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-LIFETIME", "terminal-disposal-flushes-every-partial-group")]
    public async Task GroupedDisposal_FlushesEveryPartialGroupAndDrainsEveryMessageAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<Batch<LifecycleItem>>();
        var collector = new BatchCollector<LifecycleItem, string>(
            CreateOptions(concurrencyLimit: 2),
            new CaptureBatchPipe(delivered),
            new GroupKeyProvider<LifecycleItem, string>(context => context.Message.Group));
        ConsumeContext<LifecycleItem>[] contexts =
        [
            CreateContext(new LifecycleItem("alpha", 1), timeProvider),
            CreateContext(new LifecycleItem("beta", 2), timeProvider),
            CreateContext(new LifecycleItem("alpha", 3), timeProvider),
        ];

        var consumeTasks = new List<Task>();
        foreach (ConsumeContext<LifecycleItem> context in contexts)
        {
            BatchConsumer<LifecycleItem> consumer = await collector.CollectAsync(context, TestContext.Current.CancellationToken);
            consumeTasks.Add(consumer.ConsumeAsync(context));
        }

        Assert.All(consumeTasks, task => Assert.False(task.IsCompleted));
        Assert.Equal(2, timeProvider.ActiveTimerCount);

        await collector.DisposeAsync().AsTask().WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Batch<LifecycleItem>[] batches = delivered
            .OrderBy(batch => batch[0].Message.Group, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(2, batches.Length);
        Assert.All(batches, batch => Assert.Equal(BatchCompletionMode.Forced, batch.Mode));
        Assert.Equal(new[] { 1, 3 }, batches[0].Select(context => context.Message.Sequence));
        Assert.Equal(new[] { 2 }, batches[1].Select(context => context.Message.Sequence));
        Assert.Equal(0, timeProvider.ActiveTimerCount);
        await Task.WhenAll(consumeTasks).WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-LIFETIME", "collector-cancellation-preserves-origin-token")]
    public async Task CollectionCancellation_PreservesTheTokenThatStoppedAdmissionAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<Batch<LifecycleItem>>();
        var collector = new BatchCollector<LifecycleItem>(CreateOptions(), new CaptureBatchPipe(delivered));
        using var explicitCancellation = new CancellationTokenSource();
        explicitCancellation.Cancel();
        ConsumeContext<LifecycleItem> context = CreateContext(new LifecycleItem("alpha", 1), timeProvider);

        OperationCanceledException explicitException = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            collector.CollectAsync(context, explicitCancellation.Token));
        Assert.Equal(explicitCancellation.Token, explicitException.CancellationToken);

        using var contextCancellation = new CancellationTokenSource();
        contextCancellation.Cancel();
        ConsumeContext<LifecycleItem> canceledContext = InMemoryOutboxTestContextFactory.Create(
            new LifecycleItem("beta", 2),
            contextCancellation.Token);
        canceledContext.SetTimeProvider(timeProvider);
        OperationCanceledException contextException = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            collector.CollectAsync(canceledContext, TestContext.Current.CancellationToken));
        Assert.Equal(contextCancellation.Token, contextException.CancellationToken);

        await collector.DisposeAsync();
        Assert.Empty(delivered);
        Assert.Equal(0, timeProvider.ActiveTimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-LIFETIME", "terminal-cleanup-failure-is-observed-by-owner-and-message")]
    public async Task Disposal_PropagatesTimerCleanupFailureToTheOwnerAndPendingMessageAsync()
    {
        var cleanupFailure = new InvalidOperationException("timer cleanup failed");
        var timeProvider = new ObservableTimeProvider(StartTime, cleanupFailure);
        var delivered = new ConcurrentQueue<Batch<LifecycleItem>>();
        var collector = new BatchCollector<LifecycleItem>(CreateOptions(), new CaptureBatchPipe(delivered));
        ConsumeContext<LifecycleItem> context = CreateContext(new LifecycleItem("alpha", 1), timeProvider);
        BatchConsumer<LifecycleItem> consumer = await collector.CollectAsync(context, TestContext.Current.CancellationToken);
        Task consume = consumer.ConsumeAsync(context);

        InvalidOperationException disposalException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await collector.DisposeAsync());
        InvalidOperationException consumeException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await consume.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));

        Assert.Same(cleanupFailure, disposalException);
        Assert.Same(cleanupFailure, consumeException);
        Assert.Empty(delivered);
        Assert.Equal(0, timeProvider.ActiveTimerCount);
    }

    private static BatchOptions CreateOptions(int concurrencyLimit = 1) => new()
    {
        ConcurrencyLimit = concurrencyLimit,
        MessageLimit = 10,
        TimeLimit = TimeSpan.FromDays(1),
        TimeLimitStart = BatchTimeLimitStart.FromFirst,
    };

    private static ConsumeContext<LifecycleItem> CreateContext(
        LifecycleItem message,
        TimeProvider timeProvider)
    {
        ConsumeContext<LifecycleItem> context = InMemoryOutboxTestContextFactory.Create(message);
        context.SetTimeProvider(timeProvider);
        return context;
    }

    private sealed class CaptureBatchPipe(ConcurrentQueue<Batch<LifecycleItem>> delivered) :
        IPipe<ConsumeContext<Batch<LifecycleItem>>>
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(ConsumeContext<Batch<LifecycleItem>> context)
        {
            delivered.Enqueue(context.Message);
            return Task.CompletedTask;
        }
    }

    public sealed record LifecycleItem(string Group, int Sequence);
}
