using System.Collections.Concurrent;
using System.Reflection;
using ViciOne.ServiceBus.Batching;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.InMemoryOutbox;

public sealed class InMemoryOutboxCheckpointTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-CHECKPOINT", "foreign-checkpoint-is-rejected")]
    public async Task CheckpointFromAnotherOutbox_IsRejectedWithoutDiscardingItsActions()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var checkpointCaptured = new TaskCompletionSource<OutboxCheckpoint>(TaskCreationOptions.RunContinuationsAsynchronously);
        var rejectionCaptured = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = new InMemoryTestHarness($"outbox-checkpoint-owner-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
        {
            configurator.UseInMemoryOutbox();
            configurator.Handler<CheckpointOwnerCommand>(async context =>
            {
                Assert.True(context.TryGetPayload(out OutboxContext? outbox));
                Assert.NotNull(outbox);

                if (context.Message.Sequence == 1)
                {
                    checkpointCaptured.TrySetResult(outbox.CreateCheckpoint());
                    return;
                }

                OutboxCheckpoint foreignCheckpoint = await checkpointCaptured.Task.WaitAsync(timeout, context.CancellationToken);
                try
                {
                    await outbox.DiscardPendingActions(foreignCheckpoint);
                    rejectionCaptured.TrySetResult(new InvalidOperationException("The foreign checkpoint was accepted."));
                }
                catch (Exception exception)
                {
                    rejectionCaptured.TrySetResult(exception);
                }
            });
        };

        try
        {
            await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);

            await harness.InputQueueSendEndpoint.Send(new CheckpointOwnerCommand(1), cancellationToken);
            await checkpointCaptured.Task.WaitAsync(timeout, cancellationToken);
            await harness.InputQueueSendEndpoint.Send(new CheckpointOwnerCommand(2), cancellationToken);
            Exception rejection = await rejectionCaptured.Task.WaitAsync(timeout, cancellationToken);

            var argumentException = Assert.IsType<ArgumentException>(rejection);
            Assert.Equal("checkpoint", argumentException.ParamName);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-CHECKPOINT", "discard-only-failed-attempt-tail")]
    public async Task FailedAttempt_DiscardsOnlyActionsAddedAfterItsCheckpoint()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var executed = new ConcurrentQueue<string>();
        var outboxDrained = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = new InMemoryTestHarness($"outbox-checkpoint-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
        {
            configurator.UseInMemoryOutbox();
            configurator.Handler<CheckpointCommand>(async context =>
            {
                Assert.True(context.TryGetPayload(out OutboxContext? outbox));
                Assert.NotNull(outbox);

                await outbox.Add(() => Record("retained", executed));
                OutboxCheckpoint checkpoint = outbox.CreateCheckpoint();
                await outbox.Add(() => Record("discarded", executed));

                await outbox.DiscardPendingActions(checkpoint);
                await outbox.Add(() =>
                {
                    executed.Enqueue("after-rollback");
                    outboxDrained.TrySetResult();
                    return Task.CompletedTask;
                });
            });
        };

        try
        {
            await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);

            await harness.InputQueueSendEndpoint.Send(
                new CheckpointCommand(NewId.NextGuid()),
                cancellationToken);
            await outboxDrained.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(["retained", "after-rollback"], executed);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-CHECKPOINT", "cancel-only-failed-attempt-schedules")]
    public async Task FailedAttempt_CancelsOnlySchedulesCreatedAfterItsCheckpoint()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var scheduler = DispatchProxy.Create<IMessageScheduler, RecordingSchedulerProxy>();
        var schedulerProxy = (RecordingSchedulerProxy)(object)scheduler;
        ConsumeContext consumeContext = CreateConsumeContext(new Uri("loopback://localhost/input"));
        var clearToSend = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var context = new InMemoryOutboxMessageSchedulerContext(
            consumeContext,
            _ => scheduler,
            clearToSend.Task);

        ScheduledMessage retained = await context.SchedulePublish(
            new DateTime(2030, 1, 2, 3, 0, 0, DateTimeKind.Utc),
            new ScheduledCheckpointResult("retained"),
            typeof(ScheduledCheckpointResult),
            cancellationToken);
        ScheduledMessage? discarded = null;
        Guid queuedCancellation = Guid.Parse("fe18f3cf-ed0f-46db-854e-73816929c384");

        await InMemoryOutboxCheckpointDriver.DiscardActionsCreatedByAttempt(context, async () =>
        {
            discarded = await context.SchedulePublish(
                new DateTime(2030, 1, 2, 2, 0, 0, DateTimeKind.Utc),
                new ScheduledCheckpointResult("discarded"),
                typeof(ScheduledCheckpointResult),
                cancellationToken);
            await context.CancelScheduledPublish(
                typeof(ScheduledCheckpointResult),
                queuedCancellation,
                cancellationToken);
        });
        await context.ExecutePendingActions();

        Assert.NotNull(discarded);
        Assert.Equal(2, schedulerProxy.Scheduled.Count);
        Assert.Equal(retained.TokenId, schedulerProxy.Scheduled[0].TokenId);
        Assert.Equal(discarded.TokenId, schedulerProxy.Scheduled[1].TokenId);
        Assert.Equal([discarded.TokenId], schedulerProxy.CanceledTokens);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-CHECKPOINT", "failed-schedule-cancellation-remains-recoverable")]
    public async Task FailedScheduleCancellation_RemainsTrackedForFinalCleanup()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var scheduler = DispatchProxy.Create<IMessageScheduler, RecordingSchedulerProxy>();
        var schedulerProxy = (RecordingSchedulerProxy)(object)scheduler;
        ConsumeContext consumeContext = CreateConsumeContext(new Uri("loopback://localhost/input"));
        var clearToSend = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = new InMemoryOutboxMessageSchedulerContext(
            consumeContext,
            _ => scheduler,
            clearToSend.Task);
        var cleanupFailure = new InvalidOperationException("The scheduler rejected the first cancellation.");
        schedulerProxy.EnqueueCancelSendFailure(cleanupFailure);
        ScheduledMessage? scheduled = null;

        Exception actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            InMemoryOutboxCheckpointDriver.DiscardActionsCreatedByAttempt(context, async () =>
            {
                scheduled = await context.SchedulePublish(
                    new DateTime(2030, 1, 2, 2, 0, 0, DateTimeKind.Utc),
                    new ScheduledCheckpointResult("failed-cleanup"),
                    typeof(ScheduledCheckpointResult),
                    cancellationToken);
            }));

        Assert.Same(cleanupFailure, actual);
        Assert.NotNull(scheduled);
        Assert.Equal([scheduled.TokenId], schedulerProxy.CanceledTokens);

        await context.CancelAllScheduledMessages();

        Assert.Equal([scheduled.TokenId, scheduled.TokenId], schedulerProxy.CanceledTokens);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-CHECKPOINT", "batch-checkpoint-composes-parent-and-child-outboxes")]
    public async Task BatchCheckpoint_DiscardsEveryParentAndChildActionCreatedByTheAttempt()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var firstScheduler = DispatchProxy.Create<IMessageScheduler, RecordingSchedulerProxy>();
        var secondScheduler = DispatchProxy.Create<IMessageScheduler, RecordingSchedulerProxy>();
        ConsumeContext<BatchCheckpointMessage> first = InMemoryOutboxTestContextFactory.Create(
            new BatchCheckpointMessage("first"), cancellationToken, firstScheduler);
        ConsumeContext<BatchCheckpointMessage> second = InMemoryOutboxTestContextFactory.Create(
            new BatchCheckpointMessage("second"), cancellationToken, secondScheduler);
        var messageBatch = new MessageBatch<BatchCheckpointMessage>(
            new DateTime(2030, 1, 2, 1, 0, 0, DateTimeKind.Utc),
            new DateTime(2030, 1, 2, 1, 1, 0, DateTimeKind.Utc),
            BatchCompletionMode.Size,
            [first, second]);
        ConsumeContext<Batch<BatchCheckpointMessage>> consumeContext = InMemoryOutboxTestContextFactory.Create<Batch<BatchCheckpointMessage>>(
            messageBatch,
            cancellationToken);
        var outbox = new InMemoryOutboxConsumeContext<BatchCheckpointMessage>.Batch(consumeContext);
        var parent = (OutboxContext)outbox;
        var firstChild = Assert.IsAssignableFrom<OutboxContext>(outbox.Message[0]);
        var secondChild = Assert.IsAssignableFrom<OutboxContext>(outbox.Message[1]);
        var executed = new ConcurrentQueue<string>();
        Assert.True(outbox.Message[0].TryGetPayload(out MessageSchedulerContext? firstSchedulerContext));
        Assert.True(outbox.Message[1].TryGetPayload(out MessageSchedulerContext? secondSchedulerContext));
        Guid retainedCancellation = Guid.Parse("e51ce48d-3896-45d7-a9fd-a17b2fdbe7dd");
        Guid discardedCancellation = Guid.Parse("7f4bff3a-55fa-48f4-a0c7-69ca0b3b54a3");

        await parent.Add(() => Record("parent-retained", executed));
        await firstChild.Add(() => Record("first-retained", executed));
        await secondChild.Add(() => Record("second-retained", executed));
        ScheduledMessage retainedSchedule = await firstSchedulerContext.SchedulePublish(
            new DateTime(2030, 1, 2, 2, 0, 0, DateTimeKind.Utc),
            new ScheduledCheckpointResult("retained"),
            typeof(ScheduledCheckpointResult),
            cancellationToken);
        await secondSchedulerContext.CancelScheduledPublish(
            typeof(ScheduledCheckpointResult),
            retainedCancellation,
            cancellationToken);

        OutboxCheckpoint checkpoint = parent.CreateCheckpoint();

        await parent.Add(() => Record("parent-discarded", executed));
        await firstChild.Add(() => Record("first-send-discarded", executed));
        await firstChild.Add(() => Record("first-publish-discarded", executed));
        await secondChild.Add(() => Record("second-discarded", executed));
        ScheduledMessage discardedSchedule = await firstSchedulerContext.SchedulePublish(
            new DateTime(2030, 1, 2, 3, 0, 0, DateTimeKind.Utc),
            new ScheduledCheckpointResult("discarded"),
            typeof(ScheduledCheckpointResult),
            cancellationToken);
        await secondSchedulerContext.CancelScheduledPublish(
            typeof(ScheduledCheckpointResult),
            discardedCancellation,
            cancellationToken);

        await parent.DiscardPendingActions(checkpoint);
        await parent.ExecutePendingActions(concurrentMessageDelivery: false);

        Assert.Equal(["parent-retained", "first-retained", "second-retained"], executed);
        Assert.Equal([retainedSchedule.TokenId, discardedSchedule.TokenId], ((RecordingSchedulerProxy)(object)firstScheduler).Scheduled.Select(x => x.TokenId));
        Assert.Equal([discardedSchedule.TokenId], ((RecordingSchedulerProxy)(object)firstScheduler).CanceledTokens);
        Assert.Equal([retainedCancellation], ((RecordingSchedulerProxy)(object)secondScheduler).CanceledPublishTokens);
    }

    private static Task Record(string value, ConcurrentQueue<string> executed)
    {
        executed.Enqueue(value);
        return Task.CompletedTask;
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record CheckpointCommand(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record CheckpointOwnerCommand(int Sequence);

    private sealed record ScheduledCheckpointResult(string Value);

    public sealed record BatchCheckpointMessage(string Value);

    private static ConsumeContext CreateConsumeContext(Uri inputAddress)
    {
        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
        ((ReceiveContextProxy)(object)receiveContext).InputAddress = inputAddress;

        ConsumeContext consumeContext = DispatchProxy.Create<ConsumeContext, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)consumeContext).ReceiveContext = receiveContext;
        return consumeContext;
    }

    private class ReceiveContextProxy : DispatchProxy
    {
        public required Uri InputAddress { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == $"get_{nameof(ReceiveContext.InputAddress)}"
                ? InputAddress
                : throw new NotSupportedException(targetMethod?.Name);
    }

    private class ConsumeContextProxy : DispatchProxy
    {
        public required ReceiveContext ReceiveContext { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == $"get_{nameof(ViciOne.ServiceBus.ConsumeContext.ReceiveContext)}"
                ? ReceiveContext
                : throw new NotSupportedException(targetMethod?.Name);
    }

    private class RecordingSchedulerProxy : DispatchProxy
    {
        private readonly Queue<Exception> _cancelSendFailures = [];

        public List<ScheduledMessage> Scheduled { get; } = [];

        public List<Guid> CanceledTokens { get; } = [];

        public List<Guid> CanceledPublishTokens { get; } = [];

        public void EnqueueCancelSendFailure(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            _cancelSendFailures.Enqueue(exception);
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            ArgumentNullException.ThrowIfNull(args);

            if (targetMethod.Name == nameof(IMessageScheduler.SchedulePublish)
                && targetMethod.ReturnType == typeof(Task<ScheduledMessage>))
            {
                var scheduled = new ScheduledMessageHandle<object>(
                    NewId.NextGuid(),
                    (DateTime)args[0]!,
                    new Uri("loopback://localhost/scheduled"),
                    args[1]!);
                Scheduled.Add(scheduled);
                return Task.FromResult<ScheduledMessage>(scheduled);
            }

            if (targetMethod.Name == nameof(IMessageScheduler.CancelScheduledSend))
            {
                CanceledTokens.Add((Guid)args[1]!);
                return _cancelSendFailures.TryDequeue(out Exception? failure)
                    ? Task.FromException(failure)
                    : Task.CompletedTask;
            }

            if (targetMethod.Name == nameof(IMessageScheduler.CancelScheduledPublish))
            {
                CanceledPublishTokens.Add((Guid)args[1]!);
                return Task.CompletedTask;
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }
}
