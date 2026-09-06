using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Integration;

public sealed class InMemoryJobServiceTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-FAULT", "permanent-fault-publishes-terminal-and-typed-fault")]
    public async Task PermanentFailure_PublishesTheSubmittedFaultedAndTypedFaultContractsAsync()
    {
        var consumer = new FaultingJobConsumer(completeOnRetry: false);
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync(consumer);
        Guid jobId = NewId.NextGuid();

        Guid accepted = await fixture.SubmitAsync(jobId, new InMemoryJob("permanent-failure"));
        JobExecutionSnapshot attempt = await consumer.NextAttemptAsync(fixture);
        JobSubmitted submitted = await fixture.PublishedAsync<JobSubmitted>(message => message.JobId == jobId);
        JobFaulted faulted = await fixture.PublishedAsync<JobFaulted>(message => message.JobId == jobId);
        Fault<InMemoryJob> typedFault = await fixture.PublishedAsync<Fault<InMemoryJob>>(
            message => message.Message.Label == "permanent-failure");
        JobState state = await fixture.GetStateAsync(jobId);

        Assert.Equal(jobId, accepted);
        Assert.Equal(new JobExecutionSnapshot(jobId, attempt.AttemptId, 0, "permanent-failure"), attempt);
        Assert.Equal(jobId, submitted.JobId);
        Assert.Equal(jobId, faulted.JobId);
        Assert.Equal("permanent-failure", typedFault.Message.Label);
        Assert.Equal(JobLifecycleStatus.Faulted, state.Status);
        Assert.NotNull(state.Faulted);
        Assert.Null(state.Completed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-FAULT", "configured-retry-completes-later-attempt")]
    public async Task ConfiguredJobRetry_CompletesOnTheSecondDistinctAttemptAsync()
    {
        var consumer = new FaultingJobConsumer(completeOnRetry: true);
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync(
            consumer,
            options => options.ConfigureRetry(retry => retry.Immediate(1)));
        Guid jobId = NewId.NextGuid();

        await fixture.SubmitAsync(jobId, new InMemoryJob("retry"));
        JobExecutionSnapshot first = await consumer.NextAttemptAsync(fixture);
        JobExecutionSnapshot second = await consumer.NextAttemptAsync(fixture);
        JobCompleted<InMemoryJob> completed = await fixture.PublishedAsync<JobCompleted<InMemoryJob>>(
            message => message.JobId == jobId);
        JobState state = await fixture.GetStateAsync(jobId);

        Assert.Equal(0, first.RetryAttempt);
        Assert.Equal(1, second.RetryAttempt);
        Assert.NotEqual(first.AttemptId, second.AttemptId);
        Assert.Equal(jobId, completed.JobId);
        Assert.Equal("retry", completed.Job.Label);
        Assert.Equal(JobLifecycleStatus.Completed, state.Status);
        Assert.Equal(1, state.LastRetryAttempt);
        Assert.NotNull(state.Completed);
        Assert.NotNull(state.Faulted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CHECKPOINT", "saved-checkpoint-is-restored-to-the-next-attempt-and-query")]
    public async Task SavedCheckpoint_IsRestoredToTheNextAttemptAndTypedStateQueryAsync()
    {
        var consumer = new CheckpointingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync(
            consumer,
            options => options.ConfigureRetry(retry => retry.Immediate(1)));
        Guid jobId = NewId.NextGuid();

        await fixture.SubmitAsync(jobId, new InMemoryJob("checkpoint"));
        CheckpointObservation first = await consumer.NextObservationAsync(fixture);
        CheckpointObservation second = await consumer.NextObservationAsync(fixture);
        JobCompleted<InMemoryJob> completed = await fixture.PublishedAsync<JobCompleted<InMemoryJob>>(
            message => message.JobId == jobId);
        JobState<TestCheckpoint> state = await fixture.GetStateAsync<TestCheckpoint>(jobId);

        Assert.Equal(new CheckpointObservation(jobId, 0, false, null), first);
        Assert.Equal(new CheckpointObservation(jobId, 1, true, "page-42"), second);
        Assert.Equal(jobId, completed.JobId);
        Assert.Equal(JobLifecycleStatus.Completed, state.Status);
        Assert.Equal(new TestCheckpoint("page-42"), state.Checkpoint);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CANCELLATION", "running-cancel-closes-consumer-status-and-slot")]
    public async Task CancelRunningJob_ReachesTheConsumerAndClosesStatusAndSlotWithTheReasonAsync()
    {
        var consumer = new BlockingJobConsumer(completeOnRetry: false);
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync(consumer);
        Guid jobId = NewId.NextGuid();

        Guid accepted = await fixture.SubmitAsync(jobId, new InMemoryJob("cancel"));
        JobExecutionSnapshot attempt = await consumer.NextAttemptAsync(fixture);
        JobState started = await fixture.GetStateAsync(jobId);
        await fixture.Harness.Bus.CancelJobAsync(jobId, "operator-requested", cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        JobCancellationSnapshot cancellation = await consumer.NextCancellationAsync(fixture);
        JobCanceled canceled = await fixture.PublishedAsync<JobCanceled>(message => message.JobId == jobId);
        JobSlotReleased released = await fixture.SentAsync<JobSlotReleased>(message => message.JobId == jobId);
        JobState terminal = await fixture.GetStateAsync(jobId);

        Assert.Equal(jobId, accepted);
        Assert.Equal(new JobExecutionSnapshot(jobId, attempt.AttemptId, 0, "cancel"), attempt);
        Assert.Equal(new JobCancellationSnapshot(jobId, attempt.AttemptId, true), cancellation);
        Assert.Equal(JobLifecycleStatus.Running, started.Status);
        Assert.NotNull(started.Started);
        Assert.Equal(jobId, canceled.JobId);
        Assert.Equal("operator-requested", canceled.Reason);
        Assert.Equal(jobId, released.JobId);
        Assert.Equal(JobLifecycleStatus.Canceled, terminal.Status);
        Assert.Equal("operator-requested", terminal.Reason);
        Assert.NotNull(terminal.Faulted);
        Assert.Null(terminal.Completed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CANCELLATION", "retry-after-cancel-uses-new-attempt")]
    public async Task RetryAfterCancellation_UsesANewAttemptAndCompletesAsync()
    {
        var consumer = new BlockingJobConsumer(completeOnRetry: true);
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync(consumer);
        Guid jobId = NewId.NextGuid();

        await fixture.SubmitAsync(jobId, new InMemoryJob("retry-after-cancel"));
        JobExecutionSnapshot first = await consumer.NextAttemptAsync(fixture);
        await fixture.Harness.Bus.CancelJobAsync(jobId, "retry-requested", cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        await consumer.NextCancellationAsync(fixture);
        await fixture.PublishedAsync<JobCanceled>(message => message.JobId == jobId);

        await fixture.Harness.Bus.RetryJobAsync(jobId, cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        JobExecutionSnapshot second = await consumer.NextAttemptAsync(fixture);
        JobCompleted<InMemoryJob> completed = await fixture.PublishedAsync<JobCompleted<InMemoryJob>>(
            message => message.JobId == jobId);

        Assert.Equal(0, first.RetryAttempt);
        Assert.Equal(1, second.RetryAttempt);
        Assert.NotEqual(first.AttemptId, second.AttemptId);
        Assert.Equal(jobId, completed.JobId);
        Assert.Equal("retry-after-cancel", completed.Job.Label);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CANCELLATION", "waiting-job-cancels-without-consuming-slot")]
    public async Task CancelWaitingJob_PreservesTheRunningSlotAndPublishesItsWaitAndTerminalTransitionsAsync()
    {
        var consumer = new BlockingJobConsumer(completeOnRetry: false);
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync(
            consumer,
            options => options.ConcurrentJobLimit = 1,
            options => options.SlotWaitTime = TimeSpan.FromSeconds(1));
        Guid runningJobId = NewId.NextGuid();
        Guid waitingJobId = NewId.NextGuid();

        await fixture.SubmitAsync(runningJobId, new InMemoryJob("running"));
        JobExecutionSnapshot running = await consumer.NextAttemptAsync(fixture);
        await fixture.SubmitAsync(waitingJobId, new InMemoryJob("waiting"));
        JobSlotWaitElapsed waited = await fixture.SentAsync<JobSlotWaitElapsed>(message => message.JobId == waitingJobId);

        await fixture.Harness.Bus.CancelJobAsync(waitingJobId, "waiting-canceled", cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        JobCanceled waitingCanceled = await fixture.PublishedAsync<JobCanceled>(message => message.JobId == waitingJobId);
        JobState waitingState = await fixture.GetStateAsync(waitingJobId);

        Assert.Equal(runningJobId, running.JobId);
        Assert.Equal(waitingJobId, waited.JobId);
        Assert.Equal(waitingJobId, waitingCanceled.JobId);
        Assert.Equal("waiting-canceled", waitingCanceled.Reason);
        Assert.Equal(JobLifecycleStatus.Canceled, waitingState.Status);
        Assert.Null(waitingState.Started);

        await fixture.Harness.Bus.CancelJobAsync(runningJobId, "fixture-cleanup", cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        await consumer.NextCancellationAsync(fixture);
        await fixture.PublishedAsync<JobCanceled>(message => message.JobId == runningJobId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CANCELLATION", "cancel-during-allocation-preserves-reason-and-publishes-terminal-event")]
    public async Task CancelDuringSlotAllocation_PublishesCancellationAfterTheOutstandingResponseAsync()
    {
        var consumer = new CompletingJobConsumer();
        var distribution = new BlockingDistributionStrategy();
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync(
            consumer,
            distributionStrategy: distribution);
        Guid jobId = NewId.NextGuid();

        try
        {
            await fixture.SubmitAsync(jobId, new InMemoryJob("cancel-during-allocation"));
            await distribution.Entered.WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
            await fixture.Harness.Bus.CancelJobAsync(
                    jobId,
                    "allocation-canceled",
                    cancellationToken: fixture.CancellationToken)
                .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
            await fixture.ConsumedCountAsync<CancelJob>(message => message.JobId == jobId, 1);
        }
        finally
        {
            distribution.Release();
        }

        JobCanceled canceled = await fixture.PublishedAsync<JobCanceled>(message => message.JobId == jobId);
        JobSlotReleased released = await fixture.SentAsync<JobSlotReleased>(message => message.JobId == jobId);
        JobState state = await fixture.GetStateAsync(jobId);

        Assert.Equal("allocation-canceled", canceled.Reason);
        Assert.Equal(JobSlotDisposition.Canceled, released.Disposition);
        Assert.Equal(JobLifecycleStatus.Canceled, state.Status);
        Assert.Equal("allocation-canceled", state.Reason);
        Assert.Null(state.Started);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-COMPLETION", "registered-lifecycle-through-scoped-publish-filter")]
    public async Task RegisteredStateMachines_CompleteTheLifecycleThroughTheScopedPublishFilterAsync()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync(consumer, useScopedFilter: true);
        Guid jobId = NewId.NextGuid();

        Guid accepted = await fixture.SubmitAsync(jobId, new InMemoryJob("complete"));
        JobExecutionSnapshot execution = await consumer.NextAttemptAsync(fixture);
        JobSubmitted submitted = await fixture.PublishedAsync<JobSubmitted>(message => message.JobId == jobId);
        JobStarted started = await fixture.PublishedAsync<JobStarted>(message => message.JobId == jobId);
        JobCompleted completed = await fixture.PublishedAsync<JobCompleted>(message => message.JobId == jobId);
        JobCompleted<InMemoryJob> typedCompleted = await fixture.PublishedAsync<JobCompleted<InMemoryJob>>(
            message => message.JobId == jobId);
        JobState state = await fixture.GetStateAsync(jobId);
        PublishObservation observation = fixture.PublishRecorder.Single(
            typeof(JobCompleted<InMemoryJob>), jobId);

        Assert.Equal(jobId, accepted);
        Assert.Equal(jobId, submitted.JobId);
        Assert.NotEqual(Guid.Empty, submitted.JobTypeId);
        Assert.Equal(new JobExecutionSnapshot(jobId, started.AttemptId, 0, "complete"), execution);
        Assert.Equal(jobId, completed.JobId);
        Assert.Equal(jobId, typedCompleted.JobId);
        Assert.Equal("complete", typedCompleted.Job.Label);
        Assert.Equal(JobLifecycleStatus.Completed, state.Status);
        Assert.NotNull(state.Completed);
        Assert.Null(state.Faulted);
        Assert.True(submitted.Timestamp <= started.Timestamp);
        Assert.True(started.Timestamp <= completed.Timestamp);
        Assert.NotEqual(Guid.Empty, observation.ScopeId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-FINALIZATION", "explicit-finalization-removes-completed-state")]
    public async Task ExplicitFinalization_RemovesACompletedJobWhenAutomaticFinalizationIsDisabledAsync()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync(consumer);
        Guid jobId = NewId.NextGuid();

        await fixture.SubmitAsync(jobId, new InMemoryJob("finalize-completed"));
        await consumer.NextAttemptAsync(fixture);
        await fixture.PublishedAsync<JobCompleted>(message => message.JobId == jobId);
        Assert.Equal(JobLifecycleStatus.Completed, (await fixture.GetStateAsync(jobId)).Status);

        await fixture.Harness.Bus.FinalizeJobAsync(jobId, fixture.CancellationToken)
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        await fixture.ConsumedCountAsync<FinalizeJob>(message => message.JobId == jobId, 1);

        JobState finalized = await fixture.GetStateAsync(jobId);
        Assert.Equal(JobLifecycleStatus.NotFound, finalized.Status);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-IDENTITY", "published-jobs-receive-distinct-nonempty-identities")]
    public async Task PublishingJobs_GeneratesDistinctNonEmptyIdentitiesAcrossEachLifecycleAsync()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync(
            consumer,
            configureSaga: options => options.SlotWaitTime = TimeSpan.FromSeconds(1));

        await fixture.Harness.Bus.PublishAsync(new InMemoryJob("first"), fixture.CancellationToken)
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        JobExecutionSnapshot first = await consumer.NextAttemptAsync(fixture);
        JobCompleted<InMemoryJob> firstCompleted = await fixture.PublishedAsync<JobCompleted<InMemoryJob>>(
            message => message.JobId == first.JobId);

        await fixture.Harness.Bus.PublishAsync(new InMemoryJob("second"), fixture.CancellationToken)
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        JobExecutionSnapshot second = await consumer.NextAttemptAsync(fixture);
        JobCompleted<InMemoryJob> secondCompleted = await fixture.PublishedAsync<JobCompleted<InMemoryJob>>(
            message => message.JobId == second.JobId);

        Assert.NotEqual(Guid.Empty, first.JobId);
        Assert.NotEqual(Guid.Empty, second.JobId);
        Assert.NotEqual(first.JobId, second.JobId);
        Assert.Equal(first.JobId, firstCompleted.JobId);
        Assert.Equal(first.Label, firstCompleted.Job.Label);
        Assert.Equal(second.JobId, secondCompleted.JobId);
        Assert.Equal(second.Label, secondCompleted.Job.Label);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-STATE", "unknown-identity-is-explicitly-not-found")]
    public async Task UnknownJobIdentity_ReturnsTheCompleteNotFoundStateAsync()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync(consumer);
        Guid missingJobId = NewId.NextGuid();

        JobState state = await fixture.GetStateAsync(missingJobId);

        Assert.Equal(missingJobId, state.JobId);
        Assert.Equal(JobLifecycleStatus.NotFound, state.Status);
        Assert.Null(state.Submitted);
        Assert.Null(state.Started);
        Assert.Null(state.Completed);
        Assert.Null(state.Faulted);
        Assert.Null(state.Reason);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-LIFECYCLE", "cancel-readd-and-manual-continuation")]
    public async Task RecurringJob_CancelReAddAndManualRunPreserveItsIdentityAndContinuationAsync()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync(consumer);
        string jobName = $"resumable-{NewId.NextGuid():N}";
        DateTimeOffset start = fixture.Scheduler.UtcNow.AddDays(1).AddSeconds(1);

        Guid initialJobId = await fixture.AddOrUpdateRecurringAsync(
            jobName,
            new InMemoryJob("before-cancel"),
            schedule =>
            {
                schedule.Start = start;
                schedule.EveryHours(1);
            });
        await fixture.ConsumedCountAsync<JobSubmitted>(message => message.JobId == initialJobId, 1);

        await fixture.RunRecurringAsync(jobName);
        JobExecutionSnapshot first = await consumer.NextAttemptAsync(fixture);
        JobCompleted<InMemoryJob> firstCompleted = await fixture.PublishedAsync<JobCompleted<InMemoryJob>>(
            message => message.JobId == initialJobId && message.Job.Label == "before-cancel");
        await fixture.ConsumedCountAsync<JobCompleted>(message => message.JobId == initialJobId, 1);

        Guid canceledJobId = await fixture.CancelRecurringAsync(jobName, "operator-pause");
        JobCanceled canceled = await fixture.PublishedAsync<JobCanceled>(
            message => message.JobId == initialJobId && message.Reason == "operator-pause");

        Guid resumedJobId = await fixture.AddOrUpdateRecurringAsync(
            jobName,
            new InMemoryJob("after-cancel"),
            schedule =>
            {
                schedule.Start = start;
                schedule.EveryHours(1);
            });
        await fixture.ConsumedCountAsync<JobSubmitted>(message => message.JobId == initialJobId, 2);
        await fixture.RunRecurringAsync(jobName);
        JobExecutionSnapshot second = await consumer.NextAttemptAsync(fixture);
        JobCompleted<InMemoryJob> secondCompleted = await fixture.PublishedAsync<JobCompleted<InMemoryJob>>(
            message => message.JobId == initialJobId && message.Job.Label == "after-cancel");

        Assert.Equal(initialJobId, canceledJobId);
        Assert.Equal(initialJobId, resumedJobId);
        Assert.Equal(initialJobId, first.JobId);
        Assert.Equal(initialJobId, second.JobId);
        Assert.Equal(0, first.RetryAttempt);
        Assert.Equal(0, second.RetryAttempt);
        Assert.Equal("before-cancel", first.Label);
        Assert.Equal("after-cancel", second.Label);
        Assert.Equal("before-cancel", firstCompleted.Job.Label);
        Assert.Equal("operator-pause", canceled.Reason);
        Assert.Equal("after-cancel", secondCompleted.Job.Label);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-LIFECYCLE", "named-identities-repeat-update-and-noop")]
    public async Task NamedRecurringJobs_KeepDistinctStableIdentitiesAcrossRunsUpdatesAndNoOpUpdatesAsync()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync(consumer);
        DateTimeOffset start = fixture.Scheduler.UtcNow.AddDays(1).AddSeconds(1);
        (string Name, int Seconds)[] schedules =
        [
            ("one", 2),
            ("two", 3),
            ("three", 4),
            ("four", 5),
        ];
        var jobIds = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach ((string name, int seconds) in schedules)
        {
            Guid jobId = await fixture.AddOrUpdateRecurringAsync(
                name,
                new InMemoryJob(name),
                schedule =>
                {
                    schedule.Start = start;
                    schedule.EverySeconds(seconds);
                });
            jobIds.Add(name, jobId);
            await fixture.ConsumedCountAsync<JobSubmitted>(message => message.JobId == jobId, 1);

            JobState state = await fixture.GetStateAsync(jobId);
            Assert.True(state.IsRecurring);
            Assert.NotNull(state.NextStartDate);
        }

        Assert.Equal(schedules.Length, jobIds.Values.Distinct().Count());

        await fixture.RunRecurringAsync("one");
        JobExecutionSnapshot first = await consumer.NextAttemptAsync(fixture);
        await fixture.PublishedCountAsync<JobCompleted<InMemoryJob>>(message => message.JobId == jobIds["one"], 1);
        await fixture.ConsumedCountAsync<JobCompleted>(message => message.JobId == jobIds["one"], 1);

        await fixture.RunRecurringAsync("one");
        JobExecutionSnapshot second = await consumer.NextAttemptAsync(fixture);
        await fixture.PublishedCountAsync<JobCompleted<InMemoryJob>>(message => message.JobId == jobIds["one"], 2);
        await fixture.ConsumedCountAsync<JobCompleted>(message => message.JobId == jobIds["one"], 2);

        JobState beforeUpdate = await fixture.GetStateAsync(jobIds["one"]);
        DateTimeOffset updatedStart = start.AddHours(12);
        Guid updatedJobId = await fixture.AddOrUpdateRecurringAsync(
            "one",
            new InMemoryJob("one-updated"),
            schedule =>
            {
                schedule.Start = updatedStart;
                schedule.EverySeconds(10);
            });
        await fixture.ConsumedCountAsync<JobSubmitted>(message => message.JobId == jobIds["one"], 2);
        JobState afterUpdate = await fixture.GetStateAsync(jobIds["one"]);

        Guid unchangedJobId = await fixture.AddOrUpdateRecurringAsync(
            "one",
            new InMemoryJob("one-noop"),
            schedule =>
            {
                schedule.Start = updatedStart;
                schedule.EverySeconds(10);
            });
        await fixture.ConsumedCountAsync<JobSubmitted>(message => message.JobId == jobIds["one"], 3);
        JobState afterNoOp = await fixture.GetStateAsync(jobIds["one"]);

        Assert.Equal(jobIds["one"], first.JobId);
        Assert.Equal(jobIds["one"], second.JobId);
        Assert.Equal(0, first.RetryAttempt);
        Assert.Equal(0, second.RetryAttempt);
        Assert.Equal(jobIds["one"], updatedJobId);
        Assert.Equal(jobIds["one"], unchangedJobId);
        Assert.NotNull(beforeUpdate.NextStartDate);
        Assert.NotNull(afterUpdate.NextStartDate);
        Assert.True(beforeUpdate.NextStartDate < updatedStart.UtcDateTime);
        Assert.True(afterUpdate.NextStartDate > updatedStart.UtcDateTime);
        Assert.NotEqual(beforeUpdate.NextStartDate, afterUpdate.NextStartDate);
        Assert.Equal(afterUpdate.NextStartDate, afterNoOp.NextStartDate);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULED-JOB", "provider-timed-one-shot-execution")]
    public async Task OneShotJob_RunsAtTheProviderOwnedScheduledInstantAndThenCompletesAsync()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync(consumer);
        DateTimeOffset dueAt = fixture.Scheduler.UtcNow.AddHours(1);

        Guid jobId = await fixture.ScheduleAsync(dueAt, new InMemoryJob("one-shot"));
        await fixture.ConsumedCountAsync<JobSubmitted>(message => message.JobId == jobId, 1);
        JobState scheduled = await fixture.GetStateAsync(jobId);

        Assert.False(scheduled.IsRecurring);
        Assert.Equal(JobLifecycleStatus.WaitingForSlot, scheduled.Status);

        fixture.Scheduler.Advance(TimeSpan.FromHours(1));

        JobExecutionSnapshot execution = await consumer.NextAttemptAsync(fixture);
        JobCompleted<InMemoryJob> completed = await fixture.PublishedAsync<JobCompleted<InMemoryJob>>(
            message => message.JobId == jobId);
        await fixture.ConsumedCountAsync<JobCompleted>(message => message.JobId == jobId, 1);
        JobState terminal = await fixture.GetStateAsync(jobId);

        Assert.Equal(jobId, execution.JobId);
        Assert.Equal("one-shot", completed.Job.Label);
        Assert.Equal(JobLifecycleStatus.Completed, terminal.Status);
        Assert.NotNull(terminal.Completed);
    }

    public sealed record InMemoryJob(string Label);

    public sealed record TestCheckpoint(string Cursor);

    private sealed class CompletingJobConsumer : IJobConsumer<InMemoryJob>
    {
        private readonly Channel<JobExecutionSnapshot> _attempts = Channel.CreateUnbounded<JobExecutionSnapshot>();

        public Task RunAsync(JobContext<InMemoryJob> context) =>
            _attempts.Writer.WriteAsync(Snapshot(context), context.CancellationToken).AsTask();

        public Task<JobExecutionSnapshot> NextAttemptAsync(JobServiceFixture fixture) =>
            _attempts.Reader.ReadAsync(fixture.CancellationToken).AsTask()
                .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
    }

    private sealed class FaultingJobConsumer(bool completeOnRetry) : IJobConsumer<InMemoryJob>
    {
        private readonly Channel<JobExecutionSnapshot> _attempts = Channel.CreateUnbounded<JobExecutionSnapshot>();

        public async Task RunAsync(JobContext<InMemoryJob> context)
        {
            await _attempts.Writer.WriteAsync(Snapshot(context), context.CancellationToken);
            if (completeOnRetry && context.RetryAttempt > 0)
                return;

            throw new InvalidOperationException($"job {context.Job.Label} failed on attempt {context.RetryAttempt}");
        }

        public Task<JobExecutionSnapshot> NextAttemptAsync(JobServiceFixture fixture) =>
            _attempts.Reader.ReadAsync(fixture.CancellationToken).AsTask()
                .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
    }

    private sealed class CheckpointingJobConsumer : IJobConsumer<InMemoryJob>
    {
        private readonly Channel<CheckpointObservation> _observations = Channel.CreateUnbounded<CheckpointObservation>();

        public async Task RunAsync(JobContext<InMemoryJob> context)
        {
            bool found = context.TryGetCheckpoint<TestCheckpoint>(out TestCheckpoint? checkpoint);
            await _observations.Writer.WriteAsync(
                new CheckpointObservation(context.JobId, context.RetryAttempt, found, checkpoint?.Cursor),
                context.CancellationToken);

            if (context.RetryAttempt == 0)
            {
                await context.SaveCheckpointAsync(new TestCheckpoint("page-42"), context.CancellationToken);
                throw new InvalidOperationException("The first attempt persists a checkpoint before retrying.");
            }
        }

        public Task<CheckpointObservation> NextObservationAsync(JobServiceFixture fixture) =>
            _observations.Reader.ReadAsync(fixture.CancellationToken).AsTask()
                .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
    }

    private sealed class BlockingJobConsumer(bool completeOnRetry) : IJobConsumer<InMemoryJob>
    {
        private readonly Channel<JobExecutionSnapshot> _attempts = Channel.CreateUnbounded<JobExecutionSnapshot>();
        private readonly Channel<JobCancellationSnapshot> _cancellations = Channel.CreateUnbounded<JobCancellationSnapshot>();
        private readonly TaskCompletionSource _never = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task RunAsync(JobContext<InMemoryJob> context)
        {
            JobExecutionSnapshot attempt = Snapshot(context);
            await _attempts.Writer.WriteAsync(attempt, context.CancellationToken);
            if (completeOnRetry && context.RetryAttempt > 0)
                return;

            try
            {
                await _never.Task.WaitAsync(context.CancellationToken);
            }
            finally
            {
                await _cancellations.Writer.WriteAsync(
                    new JobCancellationSnapshot(context.JobId, context.AttemptId, context.CancellationToken.IsCancellationRequested),
                    CancellationToken.None);
            }
        }

        public Task<JobExecutionSnapshot> NextAttemptAsync(JobServiceFixture fixture) =>
            _attempts.Reader.ReadAsync(fixture.CancellationToken).AsTask()
                .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);

        public Task<JobCancellationSnapshot> NextCancellationAsync(JobServiceFixture fixture) =>
            _cancellations.Reader.ReadAsync(fixture.CancellationToken).AsTask()
                .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
    }

    private sealed class BlockingDistributionStrategy : IJobDistributionStrategy
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Entered => _entered.Task;

        public async Task<Uri?> SelectInstanceAsync(
            ConsumeContext<AllocateJobSlot> context,
            JobTypeInfo jobTypeInfo,
            CancellationToken cancellationToken = default)
        {
            _entered.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
            return jobTypeInfo.Instances.Keys.FirstOrDefault();
        }

        public void Release() => _release.TrySetResult();
    }

    private sealed record JobExecutionSnapshot(Guid JobId, Guid AttemptId, int RetryAttempt, string Label);
    private sealed record JobCancellationSnapshot(Guid JobId, Guid AttemptId, bool IsCancellationRequested);
    private sealed record CheckpointObservation(Guid JobId, int RetryAttempt, bool Found, string? Cursor);

    private static JobExecutionSnapshot Snapshot(JobContext<InMemoryJob> context) =>
        new(context.JobId, context.AttemptId, context.RetryAttempt, context.Job.Label);

    private sealed class JobServiceFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;

        private JobServiceFixture(ServiceProvider provider, ITestHarness harness, ScopedPublishRecorder publishRecorder)
        {
            _provider = provider;
            Harness = harness;
            PublishRecorder = publishRecorder;
        }

        public CancellationToken CancellationToken => TestContext.Current.CancellationToken;
        public ITestHarness Harness { get; }
        public IInMemoryDelayProvider Scheduler => _provider.GetRequiredService<IInMemoryDelayProvider>();
        public TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        public ScopedPublishRecorder PublishRecorder { get; }

        public static async Task<JobServiceFixture> StartAsync<TConsumer>(
            TConsumer consumer,
            Action<JobOptions<InMemoryJob>>? configureJob = null,
            Action<JobSagaOptions>? configureSaga = null,
            bool useScopedFilter = false,
            IJobDistributionStrategy? distributionStrategy = null)
            where TConsumer : class, IJobConsumer<InMemoryJob>
        {
            TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
                .GetValidatedOptions()
                .OperationTimeout!.Value;
            var recorder = new ScopedPublishRecorder();
            var services = new ServiceCollection();
            services.AddSingleton(consumer);
            services.AddSingleton(recorder);
            services.AddScoped<JobPublishScope>();
            if (distributionStrategy is not null)
                services.AddSingleton(distributionStrategy);
            services.AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.SetEndpointNameFormatter(
                    new KebabCaseEndpointNameFormatter($"job-native-{NewId.NextGuid():N}"));
                configuration.AddConsumer<TConsumer>(registration =>
                {
                    if (configureJob is not null)
                        registration.Options<JobOptions<InMemoryJob>>(configureJob);
                });
                configuration.AddJobSagaStateMachines(options =>
                {
                    options.FinalizeCompleted = false;
                    configureSaga?.Invoke(options);
                });
                configuration.AddJobService(options => options.HeartbeatInterval = TimeSpan.FromSeconds(10))
                    .ConfigureEndpoint(endpoint => endpoint.PrefetchCount = 100);
                if (useScopedFilter)
                {
                    configuration.AddConfigureEndpointsCallback((context, _, endpoint) =>
                        endpoint.UsePublishFilter(typeof(ScopedJobPublishFilter<>), context));
                }

                configuration.UsingInMemory((context, bus) =>
                {
                    bus.ConfigureDelayedMessageScheduler();
                    bus.ConfigureEndpoints(context);
                });
            });
            ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
            try
            {
                ITestHarness harness = await provider.StartTestHarnessAsync()
                    .WaitAsync(timeout, TestContext.Current.CancellationToken);
                return new JobServiceFixture(provider, harness, recorder);
            }
            catch
            {
                await provider.DisposeAsync();
                throw;
            }
        }

        public Task<Guid> SubmitAsync(Guid jobId, InMemoryJob job)
        {
            IRequestClient<SubmitJob<InMemoryJob>> client = Harness.GetRequestClient<SubmitJob<InMemoryJob>>();
            return client.SubmitJobAsync(jobId, job, cancellationToken: CancellationToken)
                .WaitAsync(OperationTimeout, CancellationToken);
        }

        public Task<Guid> AddOrUpdateRecurringAsync(
            string jobName,
            InMemoryJob job,
            Action<IRecurringJobScheduleConfigurator> configure)
        {
            IRequestClient<SubmitJob<InMemoryJob>> client = Harness.GetRequestClient<SubmitJob<InMemoryJob>>();
            return client.AddOrUpdateRecurringJobAsync(jobName, job, configure, CancellationToken)
                .WaitAsync(OperationTimeout, CancellationToken);
        }

        public Task<Guid> ScheduleAsync(DateTimeOffset start, InMemoryJob job)
        {
            IRequestClient<SubmitJob<InMemoryJob>> client = Harness.GetRequestClient<SubmitJob<InMemoryJob>>();
            return client.ScheduleJobAsync(start, job, CancellationToken)
                .WaitAsync(OperationTimeout, CancellationToken);
        }

        public Task<Guid> CancelRecurringAsync(string jobName, string reason) =>
            Harness.Bus.CancelRecurringJobAsync<InMemoryJob>(jobName, reason, CancellationToken)
                .WaitAsync(OperationTimeout, CancellationToken);

        public Task RunRecurringAsync(string jobName) =>
            Harness.Bus.RunRecurringJobAsync<InMemoryJob>(jobName)
                .WaitAsync(OperationTimeout, CancellationToken);

        public async Task<TMessage> PublishedAsync<TMessage>(Func<TMessage, bool> predicate)
            where TMessage : class
        {
            IPublishedMessage<TMessage> published = await Harness.Published
                .SelectAsync<TMessage>(message => predicate(message.Context.Message), CancellationToken)
                .FirstObservedAsync()
                .WaitAsync(OperationTimeout, CancellationToken);
            return published.Context.Message;
        }

        public async Task<TMessage> SentAsync<TMessage>(Func<TMessage, bool> predicate)
            where TMessage : class
        {
            ISentMessage<TMessage> sent = await Harness.Sent
                .SelectAsync<TMessage>(message => predicate(message.Context.Message), CancellationToken)
                .FirstObservedAsync()
                .WaitAsync(OperationTimeout, CancellationToken);
            return sent.Context.Message;
        }

        public async Task ConsumedCountAsync<TMessage>(Func<TMessage, bool> predicate, int expectedCount)
            where TMessage : class
        {
            int count = await Harness.Consumed
                .SelectAsync<TMessage>(message => predicate(message.Context.Message), CancellationToken)
                .Take(expectedCount)
                .CountObservedAsync(TestContext.Current.CancellationToken)
                .WaitAsync(OperationTimeout, CancellationToken);
            Assert.Equal(expectedCount, count);
        }

        public async Task PublishedCountAsync<TMessage>(Func<TMessage, bool> predicate, int expectedCount)
            where TMessage : class
        {
            int count = await Harness.Published
                .SelectAsync<TMessage>(message => predicate(message.Context.Message), CancellationToken)
                .Take(expectedCount)
                .CountObservedAsync(TestContext.Current.CancellationToken)
                .WaitAsync(OperationTimeout, CancellationToken);
            Assert.Equal(expectedCount, count);
        }

        public Task<JobState> GetStateAsync(Guid jobId)
        {
            IRequestClient<GetJobState> client = Harness.GetRequestClient<GetJobState>();
            return client.GetJobStateAsync(jobId)
                .WaitAsync(OperationTimeout, CancellationToken);
        }

        public Task<JobState<TCheckpoint>> GetStateAsync<TCheckpoint>(Guid jobId)
            where TCheckpoint : class
        {
            IRequestClient<GetJobState> client = Harness.GetRequestClient<GetJobState>();
            return client.GetJobStateAsync<TCheckpoint>(jobId)
                .WaitAsync(OperationTimeout, CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Harness.StopAsync(CancellationToken.None)
                    .WaitAsync(OperationTimeout, CancellationToken.None);
            }
            finally
            {
                await _provider.DisposeAsync();
            }
        }
    }

    private sealed class JobPublishScope
    {
        public Guid Id { get; } = NewId.NextGuid();
    }

    private sealed class ScopedJobPublishFilter<T>(JobPublishScope scope, ScopedPublishRecorder recorder) :
        IFilter<PublishContext<T>>
        where T : class
    {
        public async Task SendAsync(PublishContext<T> context, IPipe<PublishContext<T>> next)
        {
            recorder.Record(typeof(T), JobId(context.Message), scope.Id);
            await next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("jobPublishScope");

        private static Guid? JobId(T message) => message switch
        {
            JobSubmitted submitted => submitted.JobId,
            JobStarted started => started.JobId,
            JobCompleted completed => completed.JobId,
            JobCompleted<InMemoryJob> completed => completed.JobId,
            JobFaulted faulted => faulted.JobId,
            JobCanceled canceled => canceled.JobId,
            _ => null,
        };
    }

    private sealed class ScopedPublishRecorder
    {
        private readonly ConcurrentQueue<PublishObservation> _observations = new();

        public void Record(Type messageType, Guid? jobId, Guid scopeId) =>
            _observations.Enqueue(new PublishObservation(messageType, jobId, scopeId));

        public PublishObservation Single(Type messageType, Guid jobId) =>
            Assert.Single(_observations, observation =>
                observation.MessageType == messageType && observation.JobId == jobId);
    }

    private sealed record PublishObservation(Type MessageType, Guid? JobId, Guid ScopeId);
}
