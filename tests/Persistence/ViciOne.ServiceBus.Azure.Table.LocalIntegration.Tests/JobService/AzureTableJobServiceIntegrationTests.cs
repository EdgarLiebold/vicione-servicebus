using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Azure.Table;
using ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.JobService;

public sealed class AzureTableJobServiceIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-JOB-SERVICE-TIME", "context-clock-controls-lifecycle-and-duration")]
    public async Task JobLifecycle_UsesTheContextClockForTimestampsTimeoutAndElapsedDurationAsync()
    {
        // The in-memory transport supplies real SentTime headers while the context clock controls
        // lifecycle decisions. Capture one common epoch before virtual time starts so heartbeat
        // freshness and lifecycle timestamps remain in the same time domain.
        DateTimeOffset startedAt = TimeProvider.System.GetUtcNow();
        TimeSpan executionTime = TimeSpan.FromSeconds(37);
        var timeProvider = new FakeTimeProvider(startedAt);
        var consumer = new TimedJobConsumer(timeProvider, executionTime);
        await using JobServiceFixture<TimedJobConsumer> fixture =
            await JobServiceFixture<TimedJobConsumer>.StartAsync("job-time-source", consumer, timeProvider);
        Guid jobId = NewId.NextGuid();

        Guid acceptedJobId = await fixture.SubmitAsync(jobId, new PersistentJob("timed"));
        IJobStarted started = await fixture.PublishedAsync<IJobStarted>(jobId, message => message.JobId);
        IJobCompleted completed = await fixture.PublishedAsync<IJobCompleted>(jobId, message => message.JobId);
        JobSaga persisted = await fixture.ReadJobAsync(jobId);

        Assert.Equal(jobId, acceptedJobId);
        Assert.Equal(startedAt.UtcDateTime, started.Timestamp);
        Assert.Equal(startedAt.Add(executionTime).UtcDateTime, completed.Timestamp);
        Assert.Equal(executionTime, completed.Duration);
        Assert.Equal(started.Timestamp, persisted.Started);
        Assert.Equal(completed.Timestamp, persisted.Completed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-JOB-SERVICE-LIFECYCLE", "completed-job-persists-terminal-state")]
    public async Task CompletedJob_PersistsTheAcceptedSubmittedStartedAndCompletedLifecycleAsync()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture<CompletingJobConsumer> fixture =
            await JobServiceFixture<CompletingJobConsumer>.StartAsync("job-completed", consumer);
        Guid jobId = NewId.NextGuid();

        Guid acceptedJobId = await fixture.SubmitAsync(jobId, new PersistentJob("complete"));
        IJobSubmitted submitted = await fixture.PublishedAsync<IJobSubmitted>(jobId, message => message.JobId);
        IJobStarted started = await fixture.PublishedAsync<IJobStarted>(jobId, message => message.JobId);
        IJobCompleted completed = await fixture.PublishedAsync<IJobCompleted>(jobId, message => message.JobId);
        JobExecutionSnapshot execution = await consumer.Completed
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        JobSaga persisted = await fixture.ReadJobAsync(jobId);

        Assert.Equal(jobId, acceptedJobId);
        Assert.Equal(jobId, submitted.JobId);
        Assert.Equal(jobId, started.JobId);
        Assert.Equal(jobId, completed.JobId);
        Assert.Equal(new JobExecutionSnapshot(jobId, started.AttemptId, 0, "complete"), execution);
        Assert.Equal(jobId, persisted.CorrelationId);
        Assert.Equal(started.AttemptId, persisted.AttemptId);
        Assert.Equal(0, persisted.RetryAttempt);
        Assert.Equal(submitted.Timestamp, persisted.Submitted);
        Assert.Equal(started.Timestamp, persisted.Started);
        Assert.Equal(completed.Timestamp, persisted.Completed);
        Assert.Null(persisted.Faulted);
        Assert.Null(persisted.Reason);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-JOB-SERVICE-LIFECYCLE", "canceled-job-persists-reason-and-cancels-consumer")]
    public async Task CanceledJob_CancelsTheConsumerAndPersistsTheTerminalReasonAsync()
    {
        var consumer = new CancellationAwareJobConsumer();
        await using JobServiceFixture<CancellationAwareJobConsumer> fixture =
            await JobServiceFixture<CancellationAwareJobConsumer>.StartAsync("job-canceled", consumer);
        Guid jobId = NewId.NextGuid();

        Guid acceptedJobId = await fixture.SubmitAsync(jobId, new PersistentJob("cancel"));
        JobExecutionSnapshot execution = await consumer.Started
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        await fixture.Harness.Bus.CancelJobAsync(jobId, "operator-requested", cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        bool cancellationObserved = await consumer.CancellationObserved
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        IJobSubmitted submitted = await fixture.PublishedAsync<IJobSubmitted>(jobId, message => message.JobId);
        IJobStarted started = await fixture.PublishedAsync<IJobStarted>(jobId, message => message.JobId);
        IJobCanceled canceled = await fixture.PublishedAsync<IJobCanceled>(jobId, message => message.JobId);
        JobSaga persisted = await fixture.ReadJobAsync(jobId);

        Assert.Equal(jobId, acceptedJobId);
        Assert.Equal(jobId, submitted.JobId);
        Assert.Equal(jobId, started.JobId);
        Assert.Equal(new JobExecutionSnapshot(jobId, started.AttemptId, 0, "cancel"), execution);
        Assert.True(cancellationObserved);
        Assert.Equal(jobId, canceled.JobId);
        Assert.Equal("operator-requested", canceled.Reason);
        Assert.Equal(jobId, persisted.CorrelationId);
        Assert.Equal(started.AttemptId, persisted.AttemptId);
        Assert.Null(persisted.Completed);
        Assert.NotNull(persisted.Faulted);
        Assert.Equal("operator-requested", persisted.Reason);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-JOB-SERVICE-LIFECYCLE", "faulted-job-persists-original-failure")]
    public async Task FaultedJob_PublishesAndPersistsTheOriginalFailureAsync()
    {
        var consumer = new FaultingJobConsumer();
        await using JobServiceFixture<FaultingJobConsumer> fixture =
            await JobServiceFixture<FaultingJobConsumer>.StartAsync("job-faulted", consumer);
        Guid jobId = NewId.NextGuid();

        Guid acceptedJobId = await fixture.SubmitAsync(jobId, new PersistentJob("fault"));
        JobExecutionSnapshot execution = await consumer.Attempted
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        IJobSubmitted submitted = await fixture.PublishedAsync<IJobSubmitted>(jobId, message => message.JobId);
        IJobStarted started = await fixture.PublishedAsync<IJobStarted>(jobId, message => message.JobId);
        IJobFaulted faulted = await fixture.PublishedAsync<IJobFaulted>(jobId, message => message.JobId);
        JobSaga persisted = await fixture.ReadJobAsync(jobId);

        Assert.Equal(jobId, acceptedJobId);
        Assert.Equal(jobId, submitted.JobId);
        Assert.Equal(jobId, started.JobId);
        Assert.Equal(new JobExecutionSnapshot(jobId, started.AttemptId, 0, "fault"), execution);
        Assert.Equal(jobId, faulted.JobId);
        Assert.Contains(nameof(ExpectedJobFailure), faulted.Exceptions.ExceptionType, StringComparison.Ordinal);
        Assert.Contains(ExpectedJobFailure.FailureMessage, faulted.Exceptions.Message, StringComparison.Ordinal);
        Assert.Equal(jobId, persisted.CorrelationId);
        Assert.Equal(started.AttemptId, persisted.AttemptId);
        Assert.Null(persisted.Completed);
        Assert.NotNull(persisted.Faulted);
        Assert.Contains(ExpectedJobFailure.FailureMessage, persisted.Reason, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-JOB-SERVICE-LIFECYCLE", "concurrent-terminal-outcomes-remain-isolated")]
    public async Task ConcurrentJobs_PersistOnlyTheirOwnCompletionOrFailureAsync()
    {
        var consumer = new MixedOutcomeJobConsumer();
        await using JobServiceFixture<MixedOutcomeJobConsumer> fixture =
            await JobServiceFixture<MixedOutcomeJobConsumer>.StartAsync(
                "job-mixed-outcomes", consumer, configureJob: options => options.ConcurrentJobLimit = 2);
        Guid completedJobId = NewId.NextGuid();
        Guid faultedJobId = NewId.NextGuid();

        Guid[] accepted;
        try
        {
            accepted = await Task.WhenAll(
                fixture.SubmitAsync(completedJobId, new PersistentJob("complete")),
                fixture.SubmitAsync(faultedJobId, new PersistentJob("fault")));
            await consumer.BothStarted.WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
            JobExecutionSnapshot[] simultaneous = consumer.Attempts.ToArray();
            Assert.Equal(2, simultaneous.Length);
            Assert.Equal("complete", Assert.Single(simultaneous, attempt => attempt.JobId == completedJobId).Label);
            Assert.Equal("fault", Assert.Single(simultaneous, attempt => attempt.JobId == faultedJobId).Label);
        }
        finally
        {
            consumer.Release();
        }

        IJobCompleted completed = await fixture.PublishedAsync<IJobCompleted>(completedJobId, message => message.JobId);
        IJobFaulted faulted = await fixture.PublishedAsync<IJobFaulted>(faultedJobId, message => message.JobId);
        JobSaga completedState = await fixture.ReadJobAsync(completedJobId);
        JobSaga faultedState = await fixture.ReadJobAsync(faultedJobId);
        await fixture.Harness.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);

        Assert.Equal([completedJobId, faultedJobId], accepted);
        Assert.Equal(2, consumer.Attempts.Count);
        Assert.Equal(completedJobId, completed.JobId);
        Assert.Equal(faultedJobId, faulted.JobId);
        Assert.Contains(nameof(ExpectedJobFailure), faulted.Exceptions.ExceptionType, StringComparison.Ordinal);
        Assert.Contains(ExpectedJobFailure.FailureMessage, faulted.Exceptions.Message, StringComparison.Ordinal);
        Assert.Equal(completedJobId, completedState.CorrelationId);
        Assert.NotNull(completedState.Completed);
        Assert.Null(completedState.Faulted);
        Assert.Null(completedState.Reason);
        Assert.Equal(faultedJobId, faultedState.CorrelationId);
        Assert.Null(faultedState.Completed);
        Assert.NotNull(faultedState.Faulted);
        Assert.Contains(ExpectedJobFailure.FailureMessage, faultedState.Reason, StringComparison.Ordinal);
        Assert.Single(fixture.Harness.Published.Snapshot<IJobCompleted>(),
            publication => publication.Context.Message.JobId == completedJobId);
        Assert.Single(fixture.Harness.Published.Snapshot<IJobFaulted>(),
            publication => publication.Context.Message.JobId == faultedJobId);
        Assert.DoesNotContain(fixture.Harness.Published.Snapshot<IJobFaulted>(),
            publication => publication.Context.Message.JobId == completedJobId);
        Assert.DoesNotContain(fixture.Harness.Published.Snapshot<IJobCompleted>(),
            publication => publication.Context.Message.JobId == faultedJobId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-JOB-SERVICE-LIFECYCLE", "completed-running-job-releases-slot-for-waiting-job")]
    public async Task CompletedRunningJob_ReleasesTheSlotForAWaitingJobAsync()
    {
        var consumer = new GatedCompletionJobConsumer();
        await using JobServiceFixture<GatedCompletionJobConsumer> fixture =
            await JobServiceFixture<GatedCompletionJobConsumer>.StartAsync(
                "job-slot-after-completion", consumer, configureSaga: options => options.SlotWaitTime = TimeSpan.FromSeconds(1));
        Guid runningJobId = NewId.NextGuid();
        Guid waitingJobId = NewId.NextGuid();

        try
        {
            Assert.Equal(runningJobId, await fixture.SubmitAsync(runningJobId, new PersistentJob("running")));
            await consumer.RunningStarted.WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
            Assert.Equal(waitingJobId, await fixture.SubmitAsync(waitingJobId, new PersistentJob("waiting")));
            ISentMessage<IJobSlotUnavailable> unavailable = await fixture.Harness.Sent
                .SelectAsync<IJobSlotUnavailable>(message => message.Context.Message.JobId == waitingJobId,
                    fixture.CancellationToken)
                .FirstObservedAsync(fixture.CancellationToken);
            Assert.Equal(waitingJobId, unavailable.Context.Message.JobId);
            JobExecutionSnapshot firstAttempt = Assert.Single(consumer.Attempts);
            Assert.Equal(runningJobId, firstAttempt.JobId);
            Assert.Equal("running", firstAttempt.Label);
        }
        finally
        {
            consumer.ReleaseRunning();
        }

        IJobCompleted runningCompleted = await fixture.PublishedAsync<IJobCompleted>(runningJobId, message => message.JobId);
        IJobCompleted waitingCompleted = await fixture.PublishedAsync<IJobCompleted>(waitingJobId, message => message.JobId);
        JobSaga runningState = await fixture.ReadJobAsync(runningJobId);
        JobSaga waitingState = await fixture.ReadJobAsync(waitingJobId);
        await fixture.Harness.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);

        Assert.Equal(runningJobId, runningCompleted.JobId);
        Assert.Equal(waitingJobId, waitingCompleted.JobId);
        JobExecutionSnapshot[] attempts = consumer.Attempts.ToArray();
        Assert.Equal(2, attempts.Length);
        Assert.Equal("waiting", Assert.Single(attempts, attempt => attempt.JobId == waitingJobId).Label);
        Assert.False(consumer.WaitingStartedBeforeRelease);
        Assert.NotNull(runningState.Completed);
        Assert.Null(runningState.Faulted);
        Assert.NotNull(waitingState.Completed);
        Assert.Null(waitingState.Faulted);
        Assert.Single(fixture.Harness.Published.Snapshot<IJobCompleted>(),
            publication => publication.Context.Message.JobId == runningJobId);
        Assert.Single(fixture.Harness.Published.Snapshot<IJobCompleted>(),
            publication => publication.Context.Message.JobId == waitingJobId);
        Assert.DoesNotContain(fixture.Harness.Published.Snapshot<IJobFaulted>(),
            publication => publication.Context.Message.JobId == runningJobId
                || publication.Context.Message.JobId == waitingJobId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-JOB-SERVICE-LIFECYCLE", "faulted-running-job-releases-slot-for-waiting-job")]
    public async Task FaultedRunningJob_ReleasesTheSlotForAWaitingJobAsync()
    {
        var consumer = new GatedFaultJobConsumer();
        await using JobServiceFixture<GatedFaultJobConsumer> fixture =
            await JobServiceFixture<GatedFaultJobConsumer>.StartAsync(
                "job-slot-after-fault", consumer, configureSaga: options => options.SlotWaitTime = TimeSpan.FromSeconds(1));
        Guid faultedJobId = NewId.NextGuid();
        Guid waitingJobId = NewId.NextGuid();

        try
        {
            Assert.Equal(faultedJobId, await fixture.SubmitAsync(faultedJobId, new PersistentJob("fault")));
            await consumer.FaultStarted.WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
            Assert.Equal(waitingJobId, await fixture.SubmitAsync(waitingJobId, new PersistentJob("waiting")));
            ISentMessage<IJobSlotUnavailable> unavailable = await fixture.Harness.Sent
                .SelectAsync<IJobSlotUnavailable>(message => message.Context.Message.JobId == waitingJobId,
                    fixture.CancellationToken)
                .FirstObservedAsync(fixture.CancellationToken);

            Assert.Equal(waitingJobId, unavailable.Context.Message.JobId);
            JobExecutionSnapshot firstAttempt = Assert.Single(consumer.Attempts);
            Assert.Equal(faultedJobId, firstAttempt.JobId);
            Assert.Equal("fault", firstAttempt.Label);
        }
        finally
        {
            consumer.ReleaseFault();
        }

        IJobFaulted faulted = await fixture.PublishedAsync<IJobFaulted>(faultedJobId, message => message.JobId);
        IJobCompleted completed = await fixture.PublishedAsync<IJobCompleted>(waitingJobId, message => message.JobId);
        JobSaga faultedState = await fixture.ReadJobAsync(faultedJobId);
        JobSaga waitingState = await fixture.ReadJobAsync(waitingJobId);
        await fixture.Harness.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);

        Assert.Equal(faultedJobId, faulted.JobId);
        Assert.Contains(ExpectedJobFailure.FailureMessage, faulted.Exceptions.Message, StringComparison.Ordinal);
        Assert.Equal(waitingJobId, completed.JobId);
        JobExecutionSnapshot[] attempts = consumer.Attempts.ToArray();
        Assert.Equal(2, attempts.Length);
        Assert.Equal("waiting", Assert.Single(attempts, attempt => attempt.JobId == waitingJobId).Label);
        Assert.False(consumer.WaitingStartedBeforeRelease);
        Assert.Null(faultedState.Completed);
        Assert.NotNull(faultedState.Faulted);
        Assert.Null(waitingState.Faulted);
        Assert.NotNull(waitingState.Completed);
        Assert.Single(fixture.Harness.Published.Snapshot<IJobFaulted>(),
            publication => publication.Context.Message.JobId == faultedJobId);
        Assert.Single(fixture.Harness.Published.Snapshot<IJobCompleted>(),
            publication => publication.Context.Message.JobId == waitingJobId);
        Assert.DoesNotContain(fixture.Harness.Published.Snapshot<IJobCompleted>(),
            publication => publication.Context.Message.JobId == faultedJobId);
        Assert.DoesNotContain(fixture.Harness.Published.Snapshot<IJobFaulted>(),
            publication => publication.Context.Message.JobId == waitingJobId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-JOB-SERVICE-LIFECYCLE", "canceled-running-job-releases-slot-for-waiting-job")]
    public async Task CanceledRunningJob_ReleasesTheSlotForAWaitingJobAsync()
    {
        var consumer = new GatedCancellationJobConsumer();
        await using JobServiceFixture<GatedCancellationJobConsumer> fixture =
            await JobServiceFixture<GatedCancellationJobConsumer>.StartAsync(
                "job-slot-after-cancel", consumer, configureSaga: options => options.SlotWaitTime = TimeSpan.FromSeconds(1));
        Guid canceledJobId = NewId.NextGuid();
        Guid waitingJobId = NewId.NextGuid();

        try
        {
            Assert.Equal(canceledJobId, await fixture.SubmitAsync(canceledJobId, new PersistentJob("cancel")));
            await consumer.CancellationTargetStarted.WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
            Assert.Equal(waitingJobId, await fixture.SubmitAsync(waitingJobId, new PersistentJob("waiting")));
            ISentMessage<IJobSlotUnavailable> unavailable = await fixture.Harness.Sent
                .SelectAsync<IJobSlotUnavailable>(message => message.Context.Message.JobId == waitingJobId,
                    fixture.CancellationToken)
                .FirstObservedAsync(fixture.CancellationToken);
            Assert.Equal(waitingJobId, unavailable.Context.Message.JobId);
            JobExecutionSnapshot firstAttempt = Assert.Single(consumer.Attempts);
            Assert.Equal(canceledJobId, firstAttempt.JobId);
            Assert.Equal("cancel", firstAttempt.Label);

            await fixture.Harness.Bus.CancelJobAsync(canceledJobId, "release-slot",
                    cancellationToken: fixture.CancellationToken)
                .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
            Assert.True(await consumer.CancellationObserved.WaitAsync(fixture.OperationTimeout, fixture.CancellationToken));
            IJobCanceled canceled = await fixture.PublishedAsync<IJobCanceled>(canceledJobId, message => message.JobId);
            IJobCompleted completed = await fixture.PublishedAsync<IJobCompleted>(waitingJobId, message => message.JobId);
            JobSaga canceledState = await fixture.ReadJobAsync(canceledJobId);
            JobSaga waitingState = await fixture.ReadJobAsync(waitingJobId);
            await fixture.Harness.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);

            Assert.Equal("release-slot", canceled.Reason);
            Assert.Equal(canceledJobId, canceled.JobId);
            Assert.Equal(waitingJobId, completed.JobId);
            JobExecutionSnapshot[] attempts = consumer.Attempts.ToArray();
            Assert.Equal(2, attempts.Length);
            Assert.Equal("waiting", Assert.Single(attempts, attempt => attempt.JobId == waitingJobId).Label);
            Assert.False(consumer.WaitingStartedBeforeCancellation);
            Assert.Null(canceledState.Completed);
            Assert.NotNull(canceledState.Faulted);
            Assert.Equal("release-slot", canceledState.Reason);
            Assert.Null(waitingState.Faulted);
            Assert.NotNull(waitingState.Completed);
            Assert.Single(fixture.Harness.Published.Snapshot<IJobCanceled>(),
                publication => publication.Context.Message.JobId == canceledJobId);
            Assert.Single(fixture.Harness.Published.Snapshot<IJobCompleted>(),
                publication => publication.Context.Message.JobId == waitingJobId);
            Assert.DoesNotContain(fixture.Harness.Published.Snapshot<IJobCompleted>(),
                publication => publication.Context.Message.JobId == canceledJobId);
            Assert.DoesNotContain(fixture.Harness.Published.Snapshot<IJobCanceled>(),
                publication => publication.Context.Message.JobId == waitingJobId);
        }
        finally
        {
            consumer.Release();
        }
    }

    public sealed record PersistentJob(string Label);

    private sealed class CompletingJobConsumer : IJobConsumer<PersistentJob>
    {
        private readonly TaskCompletionSource<JobExecutionSnapshot> _completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<JobExecutionSnapshot> Completed => _completed.Task;

        public Task RunAsync(IJobContext<PersistentJob> context)
        {
            _completed.TrySetResult(Snapshot(context));
            return Task.CompletedTask;
        }
    }

    private sealed class TimedJobConsumer(FakeTimeProvider timeProvider, TimeSpan executionTime) : IJobConsumer<PersistentJob>
    {
        public Task RunAsync(IJobContext<PersistentJob> context)
        {
            timeProvider.Advance(executionTime);
            return Task.CompletedTask;
        }
    }

    private sealed class CancellationAwareJobConsumer : IJobConsumer<PersistentJob>
    {
        private readonly TaskCompletionSource<bool> _cancellationObserved =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<JobExecutionSnapshot> _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _work = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<bool> CancellationObserved => _cancellationObserved.Task;
        public Task<JobExecutionSnapshot> Started => _started.Task;

        public async Task RunAsync(IJobContext<PersistentJob> context)
        {
            _started.TrySetResult(Snapshot(context));
            try
            {
                await _work.Task.WaitAsync(context.CancellationToken);
            }
            finally
            {
                _cancellationObserved.TrySetResult(context.CancellationToken.IsCancellationRequested);
            }
        }
    }

    private sealed class FaultingJobConsumer : IJobConsumer<PersistentJob>
    {
        private readonly TaskCompletionSource<JobExecutionSnapshot> _attempted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<JobExecutionSnapshot> Attempted => _attempted.Task;

        public Task RunAsync(IJobContext<PersistentJob> context)
        {
            _attempted.TrySetResult(Snapshot(context));
            return Task.FromException(new ExpectedJobFailure());
        }
    }

    private sealed class MixedOutcomeJobConsumer : IJobConsumer<PersistentJob>
    {
        private readonly TaskCompletionSource _bothStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _entryCount;

        public ConcurrentQueue<JobExecutionSnapshot> Attempts { get; } = new();
        public Task BothStarted => _bothStarted.Task;

        public async Task RunAsync(IJobContext<PersistentJob> context)
        {
            Attempts.Enqueue(Snapshot(context));
            if (Interlocked.Increment(ref _entryCount) == 2)
                _bothStarted.TrySetResult();

            await _release.Task.WaitAsync(context.CancellationToken);
            if (context.Job.Label == "fault")
                throw new ExpectedJobFailure();
        }

        public void Release() => _release.TrySetResult();
    }

    private sealed class GatedFaultJobConsumer : IJobConsumer<PersistentJob>
    {
        private readonly TaskCompletionSource _faultStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseFault = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _waitingStartedBeforeRelease;

        public ConcurrentQueue<JobExecutionSnapshot> Attempts { get; } = new();
        public Task FaultStarted => _faultStarted.Task;
        public bool WaitingStartedBeforeRelease => Volatile.Read(ref _waitingStartedBeforeRelease) != 0;

        public async Task RunAsync(IJobContext<PersistentJob> context)
        {
            Attempts.Enqueue(Snapshot(context));
            if (context.Job.Label != "fault")
            {
                if (!_releaseFault.Task.IsCompleted)
                    Interlocked.Exchange(ref _waitingStartedBeforeRelease, 1);
                return;
            }

            _faultStarted.TrySetResult();
            await _releaseFault.Task.WaitAsync(context.CancellationToken);
            throw new ExpectedJobFailure();
        }

        public void ReleaseFault() => _releaseFault.TrySetResult();
    }

    private sealed class GatedCompletionJobConsumer : IJobConsumer<PersistentJob>
    {
        private readonly TaskCompletionSource _runningStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseRunning = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _waitingStartedBeforeRelease;

        public ConcurrentQueue<JobExecutionSnapshot> Attempts { get; } = new();
        public Task RunningStarted => _runningStarted.Task;
        public bool WaitingStartedBeforeRelease => Volatile.Read(ref _waitingStartedBeforeRelease) != 0;

        public async Task RunAsync(IJobContext<PersistentJob> context)
        {
            Attempts.Enqueue(Snapshot(context));
            if (context.Job.Label != "running")
            {
                if (!_releaseRunning.Task.IsCompleted)
                    Interlocked.Exchange(ref _waitingStartedBeforeRelease, 1);
                return;
            }

            _runningStarted.TrySetResult();
            await _releaseRunning.Task.WaitAsync(context.CancellationToken);
        }

        public void ReleaseRunning() => _releaseRunning.TrySetResult();
    }

    private sealed class GatedCancellationJobConsumer : IJobConsumer<PersistentJob>
    {
        private readonly TaskCompletionSource _cancellationTargetStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _cancellationObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _work = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _waitingStartedBeforeCancellation;

        public ConcurrentQueue<JobExecutionSnapshot> Attempts { get; } = new();
        public Task CancellationTargetStarted => _cancellationTargetStarted.Task;
        public Task<bool> CancellationObserved => _cancellationObserved.Task;
        public bool WaitingStartedBeforeCancellation => Volatile.Read(ref _waitingStartedBeforeCancellation) != 0;

        public async Task RunAsync(IJobContext<PersistentJob> context)
        {
            Attempts.Enqueue(Snapshot(context));
            if (context.Job.Label != "cancel")
            {
                if (!_cancellationObserved.Task.IsCompleted)
                    Interlocked.Exchange(ref _waitingStartedBeforeCancellation, 1);
                return;
            }

            _cancellationTargetStarted.TrySetResult();
            try
            {
                await _work.Task.WaitAsync(context.CancellationToken);
            }
            finally
            {
                _cancellationObserved.TrySetResult(context.CancellationToken.IsCancellationRequested);
            }
        }

        public void Release() => _work.TrySetResult();
    }

    private sealed class ExpectedJobFailure : Exception
    {
        public const string FailureMessage = "The persistent job failed as requested.";

        public ExpectedJobFailure() : base(FailureMessage)
        {
        }
    }

    private sealed record JobExecutionSnapshot(Guid JobId, Guid AttemptId, int RetryAttempt, string Label);

    private static JobExecutionSnapshot Snapshot(IJobContext<PersistentJob> context) =>
        new(context.JobId, context.AttemptId, context.RetryAttempt, context.Job.Label);

    private sealed class JobServiceFixture<TConsumer> : IAsyncDisposable
        where TConsumer : class, IJobConsumer<PersistentJob>
    {
        private readonly AzureTableTestTable _table;
        private readonly ServiceProvider _provider;

        private JobServiceFixture(
            AzureTableTestTable table,
            ServiceProvider provider,
            ITestHarness harness)
        {
            _table = table;
            _provider = provider;
            Harness = harness;
        }

        public CancellationToken CancellationToken => TestContext.Current.CancellationToken;
        public ITestHarness Harness { get; }
        public TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.AzureTable)
            .OperationTimeout!.Value;

        public static async Task<JobServiceFixture<TConsumer>> StartAsync(
            string purpose,
            TConsumer consumer,
            TimeProvider? timeProvider = null,
            Action<JobOptions<PersistentJob>>? configureJob = null,
            Action<JobSagaOptions>? configureSaga = null)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            AzureTableTestTable table = await AzureTableTestTable.CreateAsync(purpose, cancellationToken);
            try
            {
                var services = new ServiceCollection();
                services.AddSingleton(consumer);
                services.AddViciOneServiceBusTestHarness(configuration =>
                {
                    TimeSpan timeout = OperationTimeoutForCurrentRun();
                    configuration.SetTestTimeouts(timeout, timeout);
                    configuration.SetKebabCaseEndpointNameFormatter();
                    configuration.AddConsumer<TConsumer>(registration =>
                    {
                        if (configureJob is not null)
                            registration.Options<JobOptions<PersistentJob>>(configureJob);
                    })
                        .Endpoint(endpoint => endpoint.Name = $"persistent-job-{NewId.NextGuid():N}");
                    configuration.AddJobService(options => options.HeartbeatInterval = TimeSpan.FromSeconds(10));
                    configuration.AddJobSagaStateMachines(options =>
                        {
                            options.FinalizeCompleted = false;
                            configureSaga?.Invoke(options);
                        })
                        .UseAzureTable(repository => repository.UseTableClientFactory(() => table.Table));
                    configuration.UsingInMemory((context, bus) =>
                    {
                        if (timeProvider is not null)
                            bus.UseExecute(consumeContext => consumeContext.SetTimeProvider(timeProvider));

                        bus.ConfigureDelayedMessageScheduler();
                        bus.ConfigureEndpoints(context);
                    });
                });
                ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
                try
                {
                    ITestHarness harness = await provider.StartTestHarnessAsync()
                        .WaitAsync(OperationTimeoutForCurrentRun(), cancellationToken);
                    return new JobServiceFixture<TConsumer>(table, provider, harness);
                }
                catch
                {
                    await provider.DisposeAsync();
                    throw;
                }
            }
            catch
            {
                await table.DisposeAsync();
                throw;
            }
        }

        public Task<Guid> SubmitAsync(Guid jobId, PersistentJob job)
        {
            IRequestClient<ISubmitJob<PersistentJob>> client = Harness.CreateRequestClient<ISubmitJob<PersistentJob>>();
            return client.SubmitJobAsync(jobId, job, cancellationToken: CancellationToken)
                .WaitAsync(OperationTimeout, CancellationToken);
        }

        public async Task<TMessage> PublishedAsync<TMessage>(Guid jobId, Func<TMessage, Guid> getJobId)
            where TMessage : class
        {
            IPublishedMessage<TMessage> published = await Harness.Published
                .SelectAsync<TMessage>(message => getJobId(message.Context.Message) == jobId, CancellationToken)
                .FirstObservedAsync()
                .WaitAsync(OperationTimeout, CancellationToken);
            return published.Context.Message;
        }

        public async Task<JobSaga> ReadJobAsync(Guid jobId)
        {
            var repository = (ILoadSagaRepository<JobSaga>)AzureTableSagaRepository
                .Create<JobSaga>(() => _table.Table);
            return await repository.LoadAsync(jobId)
                ?? throw new InvalidOperationException($"Job saga '{jobId:D}' was not persisted.");
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
                await _table.DisposeAsync();
            }
        }

        private static TimeSpan OperationTimeoutForCurrentRun() => TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.AzureTable)
            .OperationTimeout!.Value;
    }
}
