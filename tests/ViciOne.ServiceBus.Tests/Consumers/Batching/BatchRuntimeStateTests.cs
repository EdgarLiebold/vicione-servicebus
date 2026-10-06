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
        var delivered = new ConcurrentQueue<IMessageBatch<StateItem>>();
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

        IMessageBatch<StateItem> deliveredBatch = Assert.Single(delivered);
        Assert.Equal(BatchCompletionMode.Forced, deliveredBatch.Mode);
        Assert.Equal(2, Assert.Single(deliveredBatch).Message.Sequence);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CANCELLATION", "canceling-only-member-closes-empty-batch-without-delivery")]
    public async Task CancelingTheOnlyMember_ClosesTheEmptyBatchWithoutDeliveryAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<IMessageBatch<StateItem>>();
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
    [RequirementCoverage("REQ-VSB-BATCH-CANCELLATION", "timer-cleanup-failure-faults-canceled-member")]
    public async Task CancelingTheOnlyMember_PropagatesTimerCleanupFailureToItsPipelineAsync()
    {
        var cleanupFailure = new InvalidOperationException("timer cleanup failed");
        var timeProvider = new ObservableTimeProvider(
            StartTime,
            timerChangeException: cleanupFailure,
            successfulChangesBeforeFailure: 1);
        var delivered = new ConcurrentQueue<IMessageBatch<StateItem>>();
        var collector = new BatchCollector<StateItem>(CreateOptions(messageLimit: 10), new CaptureBatchPipe(delivered));
        using var cancellation = new CancellationTokenSource();
        Guid messageId = NewId.NextGuid();
        ConsumeContext<StateItem> context = CreateCancelableContext(
            new StateItem("shared", 1),
            timeProvider,
            cancellation,
            messageId);
        ConsumeContext<StateItem> duplicate = CreateContext(
            new StateItem("shared", 2),
            timeProvider,
            messageId: messageId);
        BatchConsumer<StateItem> batch = await collector.CollectAsync(context, TestContext.Current.CancellationToken);
        Task canceledConsume = batch.ConsumeAsync(context);
        Assert.Same(batch, await collector.CollectAsync(duplicate, TestContext.Current.CancellationToken));
        Task duplicateConsume = batch.ConsumeAsync(duplicate);

        cancellation.Cancel();

        OperationCanceledException canceledException = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            canceledConsume.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            duplicateConsume.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
        Assert.Equal(cancellation.Token, canceledException.CancellationToken);
        Assert.Same(cleanupFailure, exception);
        Assert.True(batch.IsCompleted);
        Assert.Equal(0, timeProvider.ActiveTimerCount);
        Assert.Empty(delivered);

        await collector.CompleteAsync(batch, TestContext.Current.CancellationToken);
        await collector.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-ADMISSION", "timer-restart-failure-terminates-every-owned-pipeline")]
    public async Task TimerRestartFailure_TerminatesTheBatchWithoutRetainingOrDeliveringMessagesAsync()
    {
        var timerFailure = new InvalidOperationException("timer restart failed");
        var timeProvider = new ObservableTimeProvider(
            StartTime,
            timerChangeException: timerFailure,
            successfulChangesBeforeFailure: 1);
        var delivered = new ConcurrentQueue<IMessageBatch<StateItem>>();
        var executor = new TaskExecutor();
        var dispatcher = new TaskExecutor();
        var batch = new BatchConsumer<StateItem>(
            new BatchRuntimeSettings(new BatchOptions
            {
                MessageLimit = 10,
                TimeLimit = TimeSpan.FromMinutes(1),
                TimeLimitStart = BatchTimeLimitStart.FromLast,
            }),
            executor,
            dispatcher,
            new CaptureBatchPipe(delivered),
            timeProvider);
        ConsumeContext<StateItem> first = CreateContext(new StateItem("shared", 1), timeProvider);
        ConsumeContext<StateItem> failing = CreateContext(new StateItem("shared", 2), timeProvider);

        try
        {
            await batch.AddAsync(first, null, TestContext.Current.CancellationToken);
            Task firstPipeline = batch.ConsumeAsync(first);

            InvalidOperationException admissionException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                batch.AddAsync(failing, null, TestContext.Current.CancellationToken));
            Assert.True(batch.IsCompleted);
            InvalidOperationException pipelineException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                firstPipeline.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));

            Assert.Same(timerFailure, admissionException);
            Assert.Same(timerFailure, pipelineException);
            Assert.Equal(0, GetBufferedMessageCount(batch));
            Assert.Equal(0, timeProvider.ActiveTimerCount);
            Assert.Empty(delivered);
        }
        finally
        {
            await dispatcher.DisposeAsync();
            await executor.DisposeAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-ADMISSION", "timer-rejection-terminates-admission-and-owned-pipeline")]
    public async Task TimerRejection_TerminatesAdmissionAndTheOwnedPipelineAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime, timerChangeResult: false);
        var delivered = new ConcurrentQueue<IMessageBatch<StateItem>>();
        var executor = new TaskExecutor();
        var dispatcher = new TaskExecutor();
        var batch = new BatchConsumer<StateItem>(
            new BatchRuntimeSettings(CreateOptions(messageLimit: 10)),
            executor,
            dispatcher,
            new CaptureBatchPipe(delivered),
            timeProvider);
        ConsumeContext<StateItem> context = CreateContext(new StateItem("shared", 1), timeProvider);

        try
        {
            InvalidOperationException admissionException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                batch.AddAsync(context, null, TestContext.Current.CancellationToken));
            InvalidOperationException pipelineException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                batch.ConsumeAsync(context).WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));

            Assert.Same(admissionException, pipelineException);
            Assert.Equal("The batch completion timer could not be scheduled.", admissionException.Message);
            Assert.True(batch.IsCompleted);
            Assert.Equal(0, GetBufferedMessageCount(batch));
            Assert.Equal(0, timeProvider.ActiveTimerCount);
            Assert.Empty(delivered);
        }
        finally
        {
            await dispatcher.DisposeAsync();
            await executor.DisposeAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-ADMISSION", "timer-restart-and-cleanup-failures-preserve-distinct-causes")]
    public async Task TimerRestartAndCleanupFailures_TerminateTheBatchWithEveryDistinctCauseAsync()
    {
        var timerFailure = new InvalidOperationException("timer restart failed");
        var cleanupFailure = new InvalidOperationException("timer disposal failed");
        var timeProvider = new ObservableTimeProvider(
            StartTime,
            timerDisposeException: cleanupFailure,
            timerChangeException: timerFailure,
            successfulChangesBeforeFailure: 1);
        var delivered = new ConcurrentQueue<IMessageBatch<StateItem>>();
        var executor = new TaskExecutor();
        var dispatcher = new TaskExecutor();
        var batch = new BatchConsumer<StateItem>(
            new BatchRuntimeSettings(new BatchOptions
            {
                MessageLimit = 10,
                TimeLimit = TimeSpan.FromMinutes(1),
                TimeLimitStart = BatchTimeLimitStart.FromLast,
            }),
            executor,
            dispatcher,
            new CaptureBatchPipe(delivered),
            timeProvider);
        ConsumeContext<StateItem> first = CreateContext(new StateItem("shared", 1), timeProvider);
        ConsumeContext<StateItem> failing = CreateContext(new StateItem("shared", 2), timeProvider);

        try
        {
            await batch.AddAsync(first, null, TestContext.Current.CancellationToken);
            Task firstPipeline = batch.ConsumeAsync(first);

            AggregateException admissionException = await Assert.ThrowsAsync<AggregateException>(() =>
                batch.AddAsync(failing, null, TestContext.Current.CancellationToken));
            AggregateException pipelineException = await Assert.ThrowsAsync<AggregateException>(() =>
                firstPipeline.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));

            Assert.Same(admissionException, pipelineException);
            Assert.Collection(
                admissionException.InnerExceptions,
                exception => Assert.Same(timerFailure, exception),
                exception => Assert.Same(cleanupFailure, exception));
            Assert.True(batch.IsCompleted);
            Assert.Equal(0, GetBufferedMessageCount(batch));
            Assert.Equal(0, timeProvider.ActiveTimerCount);
            Assert.Empty(delivered);
        }
        finally
        {
            await dispatcher.DisposeAsync();
            await executor.DisposeAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CLOCK", "saturated-collector-timeout-cannot-self-deadlock")]
    public async Task TimeLimitCallback_DoesNotBlockTheCollectorWorkerBehindItsOwnFullQueueAsync()
    {
        TimeSpan timeLimit = TimeSpan.FromMinutes(1);
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<IMessageBatch<StateItem>>();
        var executor = new TaskExecutor(capacity: 1, concurrencyLimit: 1);
        var dispatcher = new TaskExecutor();
        var batch = new BatchConsumer<StateItem>(
            new BatchRuntimeSettings(new BatchOptions { MessageLimit = 10, TimeLimit = timeLimit }),
            executor,
            dispatcher,
            new CaptureBatchPipe(delivered),
            timeProvider);
        ConsumeContext<StateItem> context = CreateContext(new StateItem("shared", 1), timeProvider);
        await batch.AddAsync(context, null, TestContext.Current.CancellationToken);
        Task pipeline = batch.ConsumeAsync(context);
        var workerEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseWorker = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task advanceClock = executor.ExecuteAsync(async () =>
        {
            workerEntered.TrySetResult();
            await releaseWorker.Task.ConfigureAwait(false);
            timeProvider.Advance(timeLimit);
        }, TestContext.Current.CancellationToken);

        try
        {
            await workerEntered.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            await executor.EnqueueAsync(static () => Task.CompletedTask, TestContext.Current.CancellationToken);
            releaseWorker.TrySetResult();

            await advanceClock.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            await pipeline.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

            IMessageBatch<StateItem> completed = Assert.Single(delivered);
            Assert.Equal(BatchCompletionMode.Time, completed.Mode);
            Assert.Equal(1, Assert.Single(completed).Message.Sequence);
            Assert.True(batch.IsCompleted);
            Assert.Equal(0, timeProvider.ActiveTimerCount);
        }
        finally
        {
            releaseWorker.TrySetResult();
            await dispatcher.DisposeAsync();
            if (advanceClock.IsCompleted)
                await executor.DisposeAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CANCELLATION", "saturated-collector-cancellation-cannot-self-deadlock")]
    public async Task CancellationCallback_DoesNotBlockTheCollectorWorkerBehindItsOwnFullQueueAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<IMessageBatch<StateItem>>();
        var executor = new TaskExecutor(capacity: 1, concurrencyLimit: 1);
        var dispatcher = new TaskExecutor();
        var batch = new BatchConsumer<StateItem>(
            new BatchRuntimeSettings(CreateOptions(messageLimit: 10)),
            executor,
            dispatcher,
            new CaptureBatchPipe(delivered),
            timeProvider);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        ConsumeContext<StateItem> context = CreateCancelableContext(
            new StateItem("shared", 1),
            timeProvider,
            cancellation);
        var workerEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseWorker = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task admission = executor.ExecuteAsync(async () =>
        {
            workerEntered.TrySetResult();
            await releaseWorker.Task.ConfigureAwait(false);
            await batch.AddAsync(context, null, TestContext.Current.CancellationToken).ConfigureAwait(false);
        }, TestContext.Current.CancellationToken);

        try
        {
            await workerEntered.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            await executor.EnqueueAsync(static () => Task.CompletedTask, TestContext.Current.CancellationToken);
            releaseWorker.TrySetResult();

            await admission.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            await executor.ExecuteAsync(static () => Task.CompletedTask, TestContext.Current.CancellationToken)
                .WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

            Assert.True(batch.IsCompleted);
            Assert.Equal(0, GetBufferedMessageCount(batch));
            Assert.Equal(0, timeProvider.ActiveTimerCount);
            Assert.Empty(delivered);
        }
        finally
        {
            releaseWorker.TrySetResult();
            await dispatcher.DisposeAsync();
            if (admission.IsCompleted)
                await executor.DisposeAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-ORDERING", "equal-primary-keys-preserve-surviving-admission-order")]
    public async Task EqualOrderingKeys_PreserveAdmissionOrderAfterCancellationReusesABufferSlotAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<IMessageBatch<StateItem>>();
        var executor = new TaskExecutor();
        var dispatcher = new TaskExecutor();
        var batch = new BatchConsumer<StateItem>(
            new BatchRuntimeSettings(CreateOptions(messageLimit: 10)),
            executor,
            dispatcher,
            new CaptureBatchPipe(delivered),
            timeProvider);
        using var cancellation = new CancellationTokenSource();
        ConsumeContext<StateItem> first = CreateContext(new StateItem("shared", 1), timeProvider, sentTime: StartTime);
        ConsumeContext<StateItem> removed = InMemoryOutboxTestContextFactory.Create(
            new StateItem("shared", 2),
            cancellation.Token,
            sentTime: StartTime);
        removed.SetTimeProvider(timeProvider);
        ConsumeContext<StateItem> third = CreateContext(new StateItem("shared", 3), timeProvider, sentTime: StartTime);
        ConsumeContext<StateItem> fourth = CreateContext(new StateItem("shared", 4), timeProvider, sentTime: StartTime);

        try
        {
            await batch.AddAsync(first, null, TestContext.Current.CancellationToken);
            await batch.AddAsync(removed, null, TestContext.Current.CancellationToken);
            await batch.AddAsync(third, null, TestContext.Current.CancellationToken);
            Task firstPipeline = batch.ConsumeAsync(first);
            Task removedPipeline = batch.ConsumeAsync(removed);
            Task thirdPipeline = batch.ConsumeAsync(third);

            cancellation.Cancel();
            OperationCanceledException cancellationException = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                removedPipeline.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
            Assert.Equal(cancellation.Token, cancellationException.CancellationToken);
            await executor.ExecuteAsync(static () => Task.CompletedTask, TestContext.Current.CancellationToken);

            await batch.AddAsync(fourth, null, TestContext.Current.CancellationToken);
            Task fourthPipeline = batch.ConsumeAsync(fourth);
            await batch.ForceCompleteAsync(TestContext.Current.CancellationToken);
            await Task.WhenAll(firstPipeline, thirdPipeline, fourthPipeline)
                .WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

            IMessageBatch<StateItem> completed = Assert.Single(delivered);
            Assert.Equal(BatchCompletionMode.Forced, completed.Mode);
            Assert.Equal(new[] { 1, 3, 4 }, completed.Select(context => context.Message.Sequence));
        }
        finally
        {
            await dispatcher.DisposeAsync();
            await executor.DisposeAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-DUPLICATE-SUPPRESSION", "collector-retains-first-context-for-duplicate-message-id")]
    public async Task DuplicateMessageIdentifier_IsRepresentedOnceWhileBothPipelinesCompleteAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<IMessageBatch<StateItem>>();
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

        IMessageBatch<StateItem> completed = Assert.Single(delivered);
        Assert.Equal(BatchCompletionMode.Forced, completed.Mode);
        Assert.Equal(1, Assert.Single(completed).Message.Sequence);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-DUPLICATE-SUPPRESSION", "duplicate-cannot-replace-admission-activity")]
    public async Task DuplicateMessageIdentifier_DoesNotReplaceTheAcceptedMessageActivityAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var observedActivity = new TaskCompletionSource<Activity?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var collector = new BatchCollector<StateItem>(CreateOptions(messageLimit: 2), new CaptureActivityPipe(observedActivity));
        Guid messageId = NewId.NextGuid();
        ConsumeContext<StateItem> first = CreateContext(new StateItem("shared", 1), timeProvider, messageId: messageId);
        ConsumeContext<StateItem> duplicate = CreateContext(new StateItem("shared", 2), timeProvider, messageId: messageId);

        using var acceptedActivity = new Activity("accepted-batch-message");
        acceptedActivity.Start();
        BatchConsumer<StateItem> batch = await collector.CollectAsync(first, TestContext.Current.CancellationToken);
        Task firstConsume = batch.ConsumeAsync(first);

        using var duplicateActivity = new Activity("duplicate-batch-message");
        duplicateActivity.Start();
        Assert.Same(batch, await collector.CollectAsync(duplicate, TestContext.Current.CancellationToken));
        Task duplicateConsume = batch.ConsumeAsync(duplicate);

        await collector.DisposeAsync().AsTask().WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        await Task.WhenAll(firstConsume, duplicateConsume).WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.Same(acceptedActivity, await observedActivity.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
        duplicateActivity.Stop();
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
        var delivered = new ConcurrentQueue<IMessageBatch<StateItem>>();
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
        var delivered = new ConcurrentQueue<IMessageBatch<StateItem>>();
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
        var delivered = new ConcurrentQueue<IMessageBatch<StateItem>>();
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

        IMessageBatch<StateItem> completed = Assert.Single(delivered);
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
        var delivered = new ConcurrentQueue<IMessageBatch<StateItem>>();
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

        IMessageBatch<StateItem>[] batches = delivered.ToArray();
        Assert.Equal(2, batches.Length);
        Assert.Contains(batches, batch => batch.Mode == BatchCompletionMode.Forced
            && batch.Count == 1
            && batch[0].Message.Sequence == 1);
        Assert.Contains(batches, batch => batch.Mode == BatchCompletionMode.Size
            && batch.Count == 1
            && batch[0].Message.Sequence == 2);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-RUNTIME-SNAPSHOT", "post-connection-option-mutation-cannot-change-active-runtime")]
    public async Task Collector_CapturesOptionsAndReleasesBufferedContextsAfterCompletionAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<IMessageBatch<StateItem>>();
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
        Assert.Equal(2, Assert.Single(delivered).Count);
        await collector.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-GROUP-LIFECYCLE", "completed-group-removal-does-not-reevaluate-selector")]
    public async Task CompletedGroup_IsRemovedByIdentityWithoutReevaluatingItsSelectorAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delivered = new ConcurrentQueue<IMessageBatch<StateItem>>();
        var invocationCount = 0;
        BatchCollector<StateItem, string> collector = CreateGroupedCollector(
            CreateOptions(messageLimit: 10),
            new CaptureBatchPipe(delivered),
            context =>
            {
                invocationCount++;
                return context.Message.Group;
            });
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
    [RequirementCoverage("REQ-VSB-BATCH-RUNTIME-BOUNDARIES", "constructors-require-valid-settings-and-collaborators")]
    public async Task RuntimeConstruction_RequiresValidSettingsAndEveryCollaboratorAsync()
    {
        var delivered = new ConcurrentQueue<IMessageBatch<StateItem>>();
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

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-RUNTIME-SNAPSHOT", "direct-configurator-shares-option-defaults")]
    public void DirectBatchConfigurator_UsesTheCanonicalBatchOptionDefaults()
    {
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, UnsupportedEndpointProxy>();
        var configurator = new BatchConfigurator<StateItem>(endpoint);
        var defaults = new BatchOptions();

        Assert.Equal(defaults.MessageLimit, ReadConfiguratorValue<int>(configurator, nameof(configurator.MessageLimit)));
        Assert.Equal(defaults.ConcurrencyLimit, ReadConfiguratorValue<int>(configurator, nameof(configurator.ConcurrencyLimit)));
        Assert.Equal(defaults.TimeLimit, ReadConfiguratorValue<TimeSpan>(configurator, nameof(configurator.TimeLimit)));
        Assert.Equal(defaults.TimeLimitStart, ReadConfiguratorValue<BatchTimeLimitStart>(configurator, nameof(configurator.TimeLimitStart)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-RUNTIME-PROBE", "collector-scope-and-input-boundary")]
    public async Task CollectorProbe_RequiresAContextAndDelegatesThroughItsNamedScopeAsync()
    {
        var collector = new BatchCollector<StateItem>(CreateOptions(messageLimit: 2), new ProbeBatchPipe());

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => collector.Probe(null!)).ParamName);
        IProbeResult result = collector.GetProbeResult(TestContext.Current.CancellationToken);

        IReadOnlyDictionary<string, object> scope = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(
            Assert.Contains("batchCollector", result.Results));
        Assert.Equal(true, Assert.Contains("batchPipeVisited", scope));

        await collector.DisposeAsync();
    }

    private static IBatchCollector<StateItem> CreateCollector(
        CollectorMode mode,
        BatchOptions options,
        ConcurrentQueue<IMessageBatch<StateItem>> delivered)
    {
        var pipe = new CaptureBatchPipe(delivered);
        return mode switch
        {
            CollectorMode.Ungrouped => new BatchCollector<StateItem>(options, pipe),
            CollectorMode.Keyed => CreateGroupedCollector(
                options,
                pipe,
                context => context.Message.Group),
            CollectorMode.KeyedFallback => CreateGroupedCollector(
                options,
                pipe,
                _ => null),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown collector mode."),
        };
    }

    private static BatchCollector<StateItem, string> CreateGroupedCollector(
        BatchOptions options,
        IPipe<ConsumeContext<IMessageBatch<StateItem>>> pipe,
        Func<ConsumeContext<StateItem>, string?> keySelector)
    {
        options.GroupBy(keySelector);
        PropertyInfo? providerProperty = typeof(BatchOptions).GetProperty(
            "GroupKeyProvider",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(providerProperty);
        object? provider = providerProperty.GetValue(options);
        Assert.NotNull(provider);
        return Assert.IsType<BatchCollector<StateItem, string>>(Activator.CreateInstance(
            typeof(BatchCollector<StateItem, string>),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [options, pipe, provider, "unknown"],
            culture: null));
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
        CancellationTokenSource cancellation,
        Guid? messageId = null)
    {
        ConsumeContext<StateItem> context = InMemoryOutboxTestContextFactory.Create(
            message,
            cancellation.Token,
            messageId: messageId);
        context.SetTimeProvider(timeProvider);
        return context;
    }

    private static int GetBufferedMessageCount(BatchConsumer<StateItem> batch)
    {
        FieldInfo messages = typeof(BatchConsumer<StateItem>).GetField("_messages", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("The batch buffer field was not found.");
        return Assert.IsAssignableFrom<IDictionary>(messages.GetValue(batch)).Count;
    }

    private static T ReadConfiguratorValue<T>(BatchConfigurator<StateItem> configurator, string propertyName)
    {
        PropertyInfo property = typeof(BatchConfigurator<StateItem>).GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Batch configurator property '{propertyName}' was not found.");
        MethodInfo getter = property.GetGetMethod(nonPublic: true)
            ?? throw new InvalidOperationException($"Batch configurator property '{propertyName}' has no getter.");
        return Assert.IsType<T>(getter.Invoke(configurator, null));
    }

    public enum CollectorMode
    {
        Ungrouped,
        Keyed,
        KeyedFallback,
    }

    public sealed record StateItem(string Group, int Sequence);

    public sealed record OtherItem(string Value);

    private sealed class CaptureBatchPipe(ConcurrentQueue<IMessageBatch<StateItem>> delivered) :
        IPipe<ConsumeContext<IMessageBatch<StateItem>>>
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(ConsumeContext<IMessageBatch<StateItem>> context)
        {
            delivered.Enqueue(context.Message);
            return Task.CompletedTask;
        }
    }

    private sealed class FaultingBatchPipe(Exception failure) :
        IPipe<ConsumeContext<IMessageBatch<StateItem>>>
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(ConsumeContext<IMessageBatch<StateItem>> context) => Task.FromException(failure);
    }

    private sealed class CaptureActivityPipe(TaskCompletionSource<Activity?> observedActivity) :
        IPipe<ConsumeContext<IMessageBatch<StateItem>>>
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(ConsumeContext<IMessageBatch<StateItem>> context)
        {
            observedActivity.TrySetResult(Activity.Current);
            return Task.CompletedTask;
        }
    }

    private sealed class ProbeBatchPipe : IPipe<ConsumeContext<IMessageBatch<StateItem>>>
    {
        public void Probe(ProbeContext context) => context.Add("batchPipeVisited", true);

        public Task SendAsync(ConsumeContext<IMessageBatch<StateItem>> context) =>
            throw new InvalidOperationException("The probe test must not deliver a batch.");
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

    private class UnsupportedEndpointProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}
