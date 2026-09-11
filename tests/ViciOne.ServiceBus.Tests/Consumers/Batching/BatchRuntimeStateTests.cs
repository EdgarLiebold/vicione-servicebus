using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Batching.Runtime;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Tests.Testing;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers.Batching;

public sealed class BatchRuntimeStateTests
{
    private static readonly DateTimeOffset StartTime = new(2043, 4, 5, 6, 7, 8, TimeSpan.Zero);
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CANCELLATION", "canceled-member-exits-and-is-excluded-from-partial-delivery")]
    public async Task CancelingOneMember_CancelsOnlyItsPipelineAndExcludesItFromTheBatchAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<Batch<StateItem>>();
        var collector = new BatchCollector<StateItem>(CreateOptions(messageLimit: 10), new CaptureBatchPipe(delivered));
        using var cancellation = new CancellationTokenSource();
        ConsumeContext<StateItem> canceledContext = CreateCancelableContext(new StateItem("shared", 1), timeProvider, cancellation);
        ConsumeContext<StateItem> remainingContext = CreateContext(new StateItem("shared", 2), timeProvider);

        BatchConsumer<StateItem> batch = await collector.CollectAsync(canceledContext, TestContext.Current.CancellationToken);
        Task canceledConsume = batch.ConsumeAsync(canceledContext);
        Assert.Same(batch, await collector.CollectAsync(remainingContext, TestContext.Current.CancellationToken));
        Task remainingConsume = batch.ConsumeAsync(remainingContext);

        cancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            canceledConsume.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
        Assert.Equal(cancellation.Token, exception.CancellationToken);

        await collector.DisposeAsync().AsTask().WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        await remainingConsume.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Batch<StateItem> deliveredBatch = Assert.Single(delivered);
        Assert.Equal(BatchCompletionMode.Forced, deliveredBatch.Mode);
        Assert.Equal(2, Assert.Single(deliveredBatch).Message.Sequence);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CANCELLATION", "canceling-only-member-closes-empty-batch-without-delivery")]
    public async Task CancelingTheOnlyMember_ClosesTheEmptyBatchWithoutDeliveryAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<Batch<StateItem>>();
        var collector = new BatchCollector<StateItem>(CreateOptions(messageLimit: 10), new CaptureBatchPipe(delivered));
        using var cancellation = new CancellationTokenSource();
        ConsumeContext<StateItem> context = CreateCancelableContext(new StateItem("shared", 1), timeProvider, cancellation);
        BatchConsumer<StateItem> batch = await collector.CollectAsync(context, TestContext.Current.CancellationToken);
        Task consume = batch.ConsumeAsync(context);

        cancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            consume.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
        await collector.CompleteAsync(batch, TestContext.Current.CancellationToken);

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.True(batch.IsCompleted);
        Assert.Equal(0, GetBufferedMessageCount(batch));
        Assert.Equal(0, timeProvider.ActiveTimerCount);
        Assert.Empty(delivered);

        await collector.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-DUPLICATE-SUPPRESSION", "collector-retains-first-context-for-duplicate-message-id")]
    public async Task DuplicateMessageIdentifier_IsRepresentedOnceWhileBothPipelinesCompleteAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<Batch<StateItem>>();
        var collector = new BatchCollector<StateItem>(CreateOptions(messageLimit: 2), new CaptureBatchPipe(delivered));
        Guid messageId = NewId.NextGuid();
        ConsumeContext<StateItem> first = CreateContext(new StateItem("shared", 1), timeProvider, messageId: messageId);
        ConsumeContext<StateItem> duplicate = CreateContext(new StateItem("shared", 2), timeProvider, messageId: messageId);

        BatchConsumer<StateItem> batch = await collector.CollectAsync(first, TestContext.Current.CancellationToken);
        Task firstConsume = batch.ConsumeAsync(first);
        Assert.Same(batch, await collector.CollectAsync(duplicate, TestContext.Current.CancellationToken));
        Task duplicateConsume = batch.ConsumeAsync(duplicate);
        Assert.False(batch.IsCompleted);

        await collector.DisposeAsync().AsTask().WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        await Task.WhenAll(firstConsume, duplicateConsume).WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Batch<StateItem> completed = Assert.Single(delivered);
        Assert.Equal(BatchCompletionMode.Forced, completed.Mode);
        Assert.Equal(1, Assert.Single(completed).Message.Sequence);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-BATCH-DELIVERY", "delivery-failure-propagates-unless-transport-already-delivered")]
    public async Task DeliveryFailure_IsSuppressedOnlyForAnAlreadyDeliveredContextAsync(bool isDelivered)
    {
        var failure = new InvalidOperationException("batch delivery failed");
        var timeProvider = new ObservableTimeProvider(StartTime);
        var collector = new BatchCollector<StateItem>(CreateOptions(messageLimit: 1), new FaultingBatchPipe(failure));
        ConsumeContext<StateItem> context = CreateContext(new StateItem("shared", 1), timeProvider, isDelivered: isDelivered);

        BatchConsumer<StateItem> batch = await collector.CollectAsync(context, TestContext.Current.CancellationToken);
        Task consume = batch.ConsumeAsync(context);

        if (isDelivered)
            await consume.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        else
        {
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                consume.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
            Assert.Same(failure, exception);
        }

        await collector.CompleteAsync(batch, TestContext.Current.CancellationToken);
        await collector.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CANCELLATION", "dispatch-admission-cancellation-preserves-token-and-terminal-state")]
    public async Task CanceledDispatchAdmission_CancelsTheOwnedPipelineWithTheAdmissionTokenAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<Batch<StateItem>>();
        var collectorExecutor = new TaskExecutor();
        var dispatcher = new TaskExecutor(capacity: 1, concurrencyLimit: 1);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            await dispatcher.EnqueueAsync(async () =>
            {
                started.TrySetResult();
                await release.Task.ConfigureAwait(false);
            }, TestContext.Current.CancellationToken);
            await started.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            await dispatcher.EnqueueAsync(static () => Task.CompletedTask, TestContext.Current.CancellationToken);

            var batch = new BatchConsumer<StateItem>(
                new BatchRuntimeSettings(CreateOptions(messageLimit: 1)),
                collectorExecutor,
                dispatcher,
                new CaptureBatchPipe(delivered),
                timeProvider);
            ConsumeContext<StateItem> context = CreateContext(new StateItem("shared", 1), timeProvider);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            OperationCanceledException admissionException = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                batch.AddAsync(context, null, cancellation.Token));
            OperationCanceledException consumeException = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                batch.ConsumeAsync(context).WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));

            Assert.Equal(cancellation.Token, admissionException.CancellationToken);
            Assert.Equal(cancellation.Token, consumeException.CancellationToken);
            Assert.True(batch.IsCompleted);
            Assert.Equal(0, GetBufferedMessageCount(batch));
            Assert.Empty(delivered);
        }
        finally
        {
            release.TrySetResult();
            await dispatcher.DisposeAsync();
            await collectorExecutor.DisposeAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CANCELLATION", "batch-pipe-cancellation-preserves-owning-context-token")]
    public async Task CanceledBatchDelivery_CancelsTheOwnedPipelineWithTheContextTokenAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var timeProvider = new ObservableTimeProvider(StartTime);
        var collector = new BatchCollector<StateItem>(
            CreateOptions(messageLimit: 1),
            new FaultingBatchPipe(new OperationCanceledException(cancellation.Token)));
        ConsumeContext<StateItem> context = CreateCancelableContext(new StateItem("shared", 1), timeProvider, cancellation);

        BatchConsumer<StateItem> batch = await collector.CollectAsync(context, TestContext.Current.CancellationToken);
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            batch.ConsumeAsync(context).WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        await collector.CompleteAsync(batch, TestContext.Current.CancellationToken);
        await collector.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-DELIVERY", "unavailable-dispatcher-faults-admission-and-owned-pipeline")]
    public async Task UnavailableDispatcher_FaultsAdmissionAndTheOwnedPipelineAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<Batch<StateItem>>();
        var collectorExecutor = new TaskExecutor();
        var dispatcher = new TaskExecutor();
        await dispatcher.DisposeAsync();
        var batch = new BatchConsumer<StateItem>(
            new BatchRuntimeSettings(CreateOptions(messageLimit: 1)),
            collectorExecutor,
            dispatcher,
            new CaptureBatchPipe(delivered),
            timeProvider);
        ConsumeContext<StateItem> context = CreateContext(new StateItem("shared", 1), timeProvider);

        try
        {
            ObjectDisposedException admissionException = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                batch.AddAsync(context, null, TestContext.Current.CancellationToken));
            ObjectDisposedException consumeException = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                batch.ConsumeAsync(context).WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));

            Assert.Same(admissionException, consumeException);
            Assert.True(batch.IsCompleted);
            Assert.Equal(0, GetBufferedMessageCount(batch));
            Assert.Empty(delivered);
        }
        finally
        {
            await collectorExecutor.DisposeAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-DELIVERY", "admission-activity-is-restored-for-batch-pipe")]
    public async Task Delivery_RestoresTheActivityCapturedDuringAdmissionAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var observedActivity = new TaskCompletionSource<Activity?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var collector = new BatchCollector<StateItem>(CreateOptions(messageLimit: 1), new CaptureActivityPipe(observedActivity));
        ConsumeContext<StateItem> context = CreateContext(new StateItem("shared", 1), timeProvider);
        using var activity = new Activity("batch-admission").Start();

        BatchConsumer<StateItem> batch = await collector.CollectAsync(context, TestContext.Current.CancellationToken);
        await batch.ConsumeAsync(context).WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.Same(activity, await observedActivity.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
        await collector.CompleteAsync(batch, TestContext.Current.CancellationToken);
        await collector.DisposeAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-BATCH-ORDERING", "transport-sequence-and-sent-time-fallback-ordering")]
    public async Task CompletedBatch_IsOrderedByTransportSequenceOrSentTimeFallbackAsync(bool useTransportSequence)
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<Batch<StateItem>>();
        var collector = new BatchCollector<StateItem>(CreateOptions(messageLimit: 3), new CaptureBatchPipe(delivered));
        StateItem[] items = [new("shared", 30), new("shared", 10), new("shared", 20)];
        var consumeTasks = new List<Task>();

        foreach (StateItem item in items)
        {
            ConsumeContext<StateItem> context = CreateContext(
                item,
                timeProvider,
                sentTime: StartTime.AddTicks(item.Sequence),
                transportSequenceNumber: useTransportSequence ? (ulong)item.Sequence : null);
            BatchConsumer<StateItem> batch = await collector.CollectAsync(context, TestContext.Current.CancellationToken);
            consumeTasks.Add(batch.ConsumeAsync(context));
        }

        await Task.WhenAll(consumeTasks).WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        await collector.DisposeAsync();

        Batch<StateItem> completed = Assert.Single(delivered);
        Assert.Equal(BatchCompletionMode.Size, completed.Mode);
        Assert.Equal(new[] { 10, 20, 30 }, completed.Select(context => context.Message.Sequence));
    }

    [Theory]
    [InlineData(CollectorMode.Ungrouped)]
    [InlineData(CollectorMode.Keyed)]
    [InlineData(CollectorMode.KeyedFallback)]
    [RequirementCoverage("REQ-VSB-BATCH-RETRY-ISOLATION", "retry-closes-active-window-before-own-delivery")]
    public async Task RetryAdmission_ClosesTheActiveBatchBeforeDeliveringTheRetryAsync(CollectorMode mode)
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<Batch<StateItem>>();
        IBatchCollector<StateItem> collector = CreateCollector(mode, CreateOptions(messageLimit: 10), delivered);
        ConsumeContext<StateItem> first = CreateContext(new StateItem("shared", 1), timeProvider);
        ConsumeContext<StateItem> retry = CreateContext(new StateItem("shared", 2), timeProvider);
        retry.GetOrAddPayload<ConsumeRetryContext>(() => new RetryMarker());

        BatchConsumer<StateItem> firstBatch = await collector.CollectAsync(first, TestContext.Current.CancellationToken);
        Task firstConsume = firstBatch.ConsumeAsync(first);
        BatchConsumer<StateItem> retryBatch = await collector.CollectAsync(retry, TestContext.Current.CancellationToken);
        Task retryConsume = retryBatch.ConsumeAsync(retry);

        Assert.NotSame(firstBatch, retryBatch);
        await Task.WhenAll(firstConsume, retryConsume).WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        await collector.CompleteAsync(firstBatch, TestContext.Current.CancellationToken);
        await collector.CompleteAsync(retryBatch, TestContext.Current.CancellationToken);
        await collector.DisposeAsync();

        Batch<StateItem>[] batches = delivered.ToArray();
        Assert.Equal(2, batches.Length);
        Assert.Contains(batches, batch => batch.Mode == BatchCompletionMode.Forced
            && batch.Length == 1
            && batch[0].Message.Sequence == 1);
        Assert.Contains(batches, batch => batch.Mode == BatchCompletionMode.Size
            && batch.Length == 1
            && batch[0].Message.Sequence == 2);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-RUNTIME-SNAPSHOT", "post-connection-option-mutation-cannot-change-active-runtime")]
    public async Task Collector_CapturesOptionsAndReleasesBufferedContextsAfterCompletionAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<Batch<StateItem>>();
        BatchOptions options = CreateOptions(messageLimit: 2);
        var collector = new BatchCollector<StateItem>(options, new CaptureBatchPipe(delivered));
        options.MessageLimit = 1;
        options.TimeLimit = TimeSpan.FromTicks(1);
        ConsumeContext<StateItem> first = CreateContext(new StateItem("shared", 1), timeProvider);
        ConsumeContext<StateItem> second = CreateContext(new StateItem("shared", 2), timeProvider);

        BatchConsumer<StateItem> batch = await collector.CollectAsync(first, TestContext.Current.CancellationToken);
        Task firstConsume = batch.ConsumeAsync(first);
        Assert.False(batch.IsCompleted);

        Assert.Same(batch, await collector.CollectAsync(second, TestContext.Current.CancellationToken));
        Task secondConsume = batch.ConsumeAsync(second);
        await Task.WhenAll(firstConsume, secondConsume).WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.True(batch.IsCompleted);
        Assert.Equal(0, GetBufferedMessageCount(batch));
        Assert.Equal(2, Assert.Single(delivered).Length);
        await collector.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-GROUP-LIFECYCLE", "completed-group-removal-does-not-reevaluate-selector")]
    public async Task CompletedGroup_IsRemovedByIdentityWithoutReevaluatingItsSelectorAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<Batch<StateItem>>();
        var invocationCount = 0;
        var collector = new BatchCollector<StateItem, string>(
            CreateOptions(messageLimit: 10),
            new CaptureBatchPipe(delivered),
            new GroupKeyProvider<StateItem, string>(context =>
            {
                invocationCount++;
                return context.Message.Group;
            }));
        ConsumeContext<StateItem> context = CreateContext(new StateItem("alpha", 1), timeProvider);

        BatchConsumer<StateItem> batch = await collector.CollectAsync(context, TestContext.Current.CancellationToken);
        Task consume = batch.ConsumeAsync(context);
        await batch.ForceCompleteAsync(TestContext.Current.CancellationToken);
        await consume.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        await collector.CompleteAsync(batch, TestContext.Current.CancellationToken);
        await collector.DisposeAsync();

        Assert.Equal(1, invocationCount);
        Assert.Single(delivered);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-GROUP-LIFECYCLE", "present-group-key-must-be-non-null")]
    public async Task GroupingProvider_CannotReportANullKeyAsPresentAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<Batch<StateItem>>();
        var collector = new BatchCollector<StateItem, string>(
            CreateOptions(messageLimit: 10),
            new CaptureBatchPipe(delivered),
            new InvalidGroupKeyProvider());
        ConsumeContext<StateItem> context = CreateContext(new StateItem("alpha", 1), timeProvider);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            collector.CollectAsync(context, TestContext.Current.CancellationToken));

        Assert.Contains("null key", exception.Message, StringComparison.Ordinal);
        await collector.DisposeAsync();
        Assert.Empty(delivered);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-RUNTIME-BOUNDARIES", "constructors-require-valid-settings-and-collaborators")]
    public async Task RuntimeConstruction_RequiresValidSettingsAndEveryCollaboratorAsync()
    {
        var delivered = new ConcurrentQueue<Batch<StateItem>>();
        var pipe = new CaptureBatchPipe(delivered);
        BatchOptions options = CreateOptions(messageLimit: 2);

        Assert.Equal("options", Assert.Throws<ArgumentNullException>(() => new BatchRuntimeSettings(null!)).ParamName);
        Assert.Equal(
            "options",
            Assert.Throws<ArgumentException>(() => new BatchRuntimeSettings(new BatchOptions { MessageLimit = 0 })).ParamName);
        Assert.Equal("options", Assert.Throws<ArgumentNullException>(() => new BatchCollector<StateItem>(null!, pipe)).ParamName);
        Assert.Equal("consumerPipe", Assert.Throws<ArgumentNullException>(() => new BatchCollector<StateItem>(options, null!)).ParamName);
        Assert.Equal(
            "keyProvider",
            Assert.Throws<ArgumentNullException>(() => new BatchCollector<StateItem, string>(options, pipe, null!)).ParamName);

        var settings = new BatchRuntimeSettings(options);
        await using var executor = new TaskExecutor();
        await using var dispatcher = new TaskExecutor();
        Assert.Equal(
            "settings",
            Assert.Throws<ArgumentNullException>(() => new BatchConsumer<StateItem>(null!, executor, dispatcher, pipe, TimeProvider.System))
                .ParamName);
        Assert.Equal(
            "executor",
            Assert.Throws<ArgumentNullException>(() => new BatchConsumer<StateItem>(settings, null!, dispatcher, pipe, TimeProvider.System))
                .ParamName);
        Assert.Equal(
            "dispatcher",
            Assert.Throws<ArgumentNullException>(() => new BatchConsumer<StateItem>(settings, executor, null!, pipe, TimeProvider.System))
                .ParamName);
        Assert.Equal(
            "consumerPipe",
            Assert.Throws<ArgumentNullException>(() => new BatchConsumer<StateItem>(settings, executor, dispatcher, null!, TimeProvider.System))
                .ParamName);
        Assert.Equal(
            "timeProvider",
            Assert.Throws<ArgumentNullException>(() => new BatchConsumer<StateItem>(settings, executor, dispatcher, pipe, null!))
                .ParamName);

        var recordingCollector = new RecordingCollector();
        var factory = new BatchConsumerFactory<StateItem>(options, recordingCollector);
        ConsumeContext<OtherItem> wrongContext = InMemoryOutboxTestContextFactory.Create(
            new OtherItem("wrong"),
            TestContext.Current.CancellationToken);
        MessageException typeException = await Assert.ThrowsAsync<MessageException>(() => factory.SendAsync(
            wrongContext,
            Pipe.Empty<ConsumerConsumeContext<BatchConsumer<StateItem>, OtherItem>>()));
        Assert.Contains(nameof(StateItem), typeException.Message, StringComparison.Ordinal);
        await factory.DisposeAsync();
        Assert.Equal(1, recordingCollector.DisposeCount);
        Assert.Equal("collector", Assert.Throws<ArgumentNullException>(() => new BatchConsumerFactory<StateItem>(options, null!)).ParamName);
    }

    private static IBatchCollector<StateItem> CreateCollector(
        CollectorMode mode,
        BatchOptions options,
        ConcurrentQueue<Batch<StateItem>> delivered)
    {
        var pipe = new CaptureBatchPipe(delivered);
        return mode switch
        {
            CollectorMode.Ungrouped => new BatchCollector<StateItem>(options, pipe),
            CollectorMode.Keyed => new BatchCollector<StateItem, string>(
                options,
                pipe,
                new GroupKeyProvider<StateItem, string>(context => context.Message.Group)),
            CollectorMode.KeyedFallback => new BatchCollector<StateItem, string>(
                options,
                pipe,
                new GroupKeyProvider<StateItem, string>(_ => null!)),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown collector mode."),
        };
    }

    private static BatchOptions CreateOptions(int messageLimit) => new()
    {
        ConcurrencyLimit = 1,
        MessageLimit = messageLimit,
        TimeLimit = TimeSpan.FromDays(1),
        TimeLimitStart = BatchTimeLimitStart.FromFirst,
    };

    private static ConsumeContext<StateItem> CreateContext(
        StateItem message,
        TimeProvider timeProvider,
        DateTimeOffset? sentTime = null,
        ulong? transportSequenceNumber = null,
        Guid? messageId = null,
        bool isDelivered = false)
    {
        ConsumeContext<StateItem> context = InMemoryOutboxTestContextFactory.Create(
            message,
            TestContext.Current.CancellationToken,
            sentTime: sentTime,
            transportSequenceNumber: transportSequenceNumber,
            messageId: messageId,
            isDelivered: isDelivered);
        context.SetTimeProvider(timeProvider);
        return context;
    }

    private static ConsumeContext<StateItem> CreateCancelableContext(
        StateItem message,
        TimeProvider timeProvider,
        CancellationTokenSource cancellation)
    {
        ConsumeContext<StateItem> context = InMemoryOutboxTestContextFactory.Create(message, cancellation.Token);
        context.SetTimeProvider(timeProvider);
        return context;
    }

    private static int GetBufferedMessageCount(BatchConsumer<StateItem> batch)
    {
        FieldInfo messages = typeof(BatchConsumer<StateItem>).GetField("_messages", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("The batch buffer field was not found.");
        return Assert.IsAssignableFrom<IDictionary>(messages.GetValue(batch)).Count;
    }

    public enum CollectorMode
    {
        Ungrouped,
        Keyed,
        KeyedFallback,
    }

    public sealed record StateItem(string Group, int Sequence);

    public sealed record OtherItem(string Value);

    private sealed class CaptureBatchPipe(ConcurrentQueue<Batch<StateItem>> delivered) :
        IPipe<ConsumeContext<Batch<StateItem>>>
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(ConsumeContext<Batch<StateItem>> context)
        {
            delivered.Enqueue(context.Message);
            return Task.CompletedTask;
        }
    }

    private sealed class FaultingBatchPipe(Exception failure) :
        IPipe<ConsumeContext<Batch<StateItem>>>
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(ConsumeContext<Batch<StateItem>> context) => Task.FromException(failure);
    }

    private sealed class CaptureActivityPipe(TaskCompletionSource<Activity?> observedActivity) :
        IPipe<ConsumeContext<Batch<StateItem>>>
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(ConsumeContext<Batch<StateItem>> context)
        {
            observedActivity.TrySetResult(Activity.Current);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingCollector : IBatchCollector<StateItem>
    {
        public int DisposeCount { get; private set; }

        public Task<BatchConsumer<StateItem>> CollectAsync(
            ConsumeContext<StateItem> context,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task CompleteAsync(
            BatchConsumer<StateItem> consumer,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void Probe(ProbeContext context)
        {
        }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RetryMarker : ConsumeRetryContext
    {
        public int RetryAttempt => 1;

        public int RetryCount => 0;

        public TContext CreateNext<TContext>(RetryContext retryContext)
            where TContext : class, ConsumeRetryContext =>
            throw new NotSupportedException();

        public Task NotifyPendingFaultsAsync(CancellationToken cancellationToken = default) =>
            cancellationToken.IsCancellationRequested
                ? Task.FromCanceled(cancellationToken)
                : Task.CompletedTask;
    }

    private sealed class InvalidGroupKeyProvider : IGroupKeyProvider<StateItem, string>
    {
        public bool TryGetKey(ConsumeContext<StateItem> context, out string key)
        {
            key = null!;
            return true;
        }
    }
}
