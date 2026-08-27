using System.Collections.Concurrent;
using System.Reflection;
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
        public List<ScheduledMessage> Scheduled { get; } = [];

        public List<Guid> CanceledTokens { get; } = [];

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
                return Task.CompletedTask;
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }
}
