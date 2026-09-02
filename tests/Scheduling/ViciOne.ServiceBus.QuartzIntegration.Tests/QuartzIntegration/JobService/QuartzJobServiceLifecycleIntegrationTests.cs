using System.Collections.Concurrent;
using Quartz;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.QuartzIntegration.Tests.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.QuartzIntegration.Tests.QuartzIntegration.JobService;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzJobServiceLifecycleIntegrationTests
{
    private const int JobCount = 10;
    private const int ConcurrentJobLimit = 3;

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-SERVICE-LIFECYCLE", "default-request-client-completes")]
    public Task DefaultRequestClient_ObservesTheCompleteJobLifecycle()
    {
        return AssertSuccessfulLifecycle(useExplicitServiceAddress: false);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-SERVICE-LIFECYCLE", "addressed-request-client-completes")]
    public Task AddressedRequestClient_ObservesTheCompleteJobLifecycle()
    {
        return AssertSuccessfulLifecycle(useExplicitServiceAddress: true);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-SERVICE-LIFECYCLE", "cancellation-reaches-consumer-and-terminal-event")]
    public async Task CancelJob_CancelsTheRunningConsumerAndPublishesTheReason()
    {
        TimeSpan timeout = OperationTimeout();
        Guid jobId = NewId.NextGuid();
        var lifecycle = new JobLifecycleProbe([jobId]);
        var consumer = new CancellationAwareJobConsumer();

        await using QuartzTestBus fixture = await QuartzJobServiceTestBus.Start<CalculationJob, CancellationAwareJobConsumer>(
            timeout,
            consumer,
            static options => options.SetJobTimeout(TimeSpan.FromMinutes(1)),
            lifecycle.Configure);
        IRequestClient<SubmitJob<CalculationJob>> client = fixture.Bus.CreateRequestClient<SubmitJob<CalculationJob>>();

        Guid acceptedJobId = await client.SubmitJob(
                jobId,
                new CalculationJob("cancel-me"),
                cancellationToken: TestContext.Current.CancellationToken)
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        JobExecutionSnapshot execution = await consumer.Started.WaitAsync(timeout, TestContext.Current.CancellationToken);

        await fixture.Bus.CancelJob(jobId, "operator-requested")
            .WaitAsync(timeout, TestContext.Current.CancellationToken);

        bool cancellationRequested = await consumer.CancellationObserved
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        JobSubmittedSnapshot submitted = await lifecycle.For(jobId).Submitted.Task
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        JobStartedSnapshot started = await lifecycle.For(jobId).Started.Task
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        JobCanceledSnapshot canceled = await lifecycle.For(jobId).Canceled.Task
            .WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal(jobId, acceptedJobId);
        Assert.Equal(jobId, execution.JobId);
        Assert.Equal("cancel-me", execution.Label);
        Assert.True(cancellationRequested);
        Assert.Equal(jobId, submitted.JobId);
        Assert.Equal(jobId, started.JobId);
        Assert.Equal(started.AttemptId, execution.AttemptId);
        Assert.Equal(jobId, canceled.JobId);
        Assert.Equal("operator-requested", canceled.Reason);
        Assert.True(submitted.Timestamp <= started.Timestamp);
        Assert.True(started.Timestamp <= canceled.Timestamp);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-SERVICE-LIFECYCLE", "concurrent-limit-is-enforced-and-all-jobs-complete")]
    public async Task ConcurrentJobLimit_BoundsExecutionAndStillCompletesEveryAcceptedJob()
    {
        TimeSpan timeout = OperationTimeout();
        Guid[] jobIds = Enumerable.Range(0, JobCount).Select(_ => NewId.NextGuid()).ToArray();
        var lifecycle = new JobLifecycleProbe(jobIds);
        var consumer = new ConcurrencyTrackingJobConsumer(JobCount, ConcurrentJobLimit);

        await using QuartzTestBus fixture = await QuartzJobServiceTestBus.Start<CalculationJob, ConcurrencyTrackingJobConsumer>(
            timeout,
            consumer,
            options => options
                .SetJobTimeout(TimeSpan.FromMinutes(1))
                .SetConcurrentJobLimit(ConcurrentJobLimit),
            lifecycle.Configure,
            configureJobService: static service => service.SlotWaitTime = TimeSpan.FromSeconds(1));
        IRequestClient<SubmitJob<CalculationJob>> client = fixture.Bus.CreateRequestClient<SubmitJob<CalculationJob>>();

        Guid[] accepted = await Task.WhenAll(jobIds.Select((jobId, index) => client.SubmitJob(
                jobId,
                new CalculationJob($"job-{index}"),
                cancellationToken: TestContext.Current.CancellationToken)))
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        try
        {
            await consumer.LimitReached.WaitAsync(timeout, TestContext.Current.CancellationToken);

            Assert.Equal(ConcurrentJobLimit, consumer.CurrentCount);
            Assert.Equal(ConcurrentJobLimit, consumer.MaximumCount);
        }
        finally
        {
            consumer.ReleaseAll();
        }

        await consumer.AllCompleted.WaitAsync(timeout, TestContext.Current.CancellationToken);
        await Task.WhenAll(jobIds.Select(jobId => lifecycle.For(jobId).Completed.Task))
            .WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal(jobIds.Order(), accepted.Order());
        Assert.Equal(0, consumer.DuplicateExecutionCount);
        Assert.Equal(JobCount, consumer.ExecutedJobIds.Count);
        Assert.Equal(jobIds.Order(), consumer.ExecutedJobIds.Order());
        Assert.Equal(0, consumer.CurrentCount);
        Assert.Equal(ConcurrentJobLimit, consumer.MaximumCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-SERVICE-LIFECYCLE", "running-attempt-answers-scheduled-status-check")]
    public async Task RunningJob_AnswersTheStatusCheckScheduledThroughQuartz()
    {
        TimeSpan timeout = OperationTimeout();
        Guid jobId = NewId.NextGuid();
        var lifecycle = new JobLifecycleProbe([jobId]);
        var consumer = new ReleasableJobConsumer();

        await using QuartzTestBus fixture = await QuartzJobServiceTestBus.Start<CalculationJob, ReleasableJobConsumer>(
            timeout,
            consumer,
            static options => options.SetJobTimeout(TimeSpan.FromMinutes(1)),
            lifecycle.Configure,
            configureJobService: static service => service.StatusCheckInterval = TimeSpan.FromSeconds(30));
        var scheduledStatusCheck = new ScheduledMessageCapture(nameof(JobStatusCheckRequested));
        var statusCheck = new ConsumeCompletionObserver<GetJobAttemptStatus>(message => message.JobId == jobId);
        using ConnectHandle scheduledObserver = fixture.Bus.ConnectConsumeObserver(scheduledStatusCheck);
        using ConnectHandle statusObserver = fixture.Bus.ConnectConsumeObserver(statusCheck);
        IRequestClient<SubmitJob<CalculationJob>> submitClient = fixture.Bus.CreateRequestClient<SubmitJob<CalculationJob>>();

        Guid acceptedJobId = await submitClient.SubmitJob(
                jobId,
                new CalculationJob("status-check"),
                cancellationToken: TestContext.Current.CancellationToken)
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        JobExecutionSnapshot execution = await consumer.Started.WaitAsync(timeout, TestContext.Current.CancellationToken);
        ScheduledMessageSnapshot scheduled = await scheduledStatusCheck.Scheduled
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(await fixture.Scheduler.GetTrigger(
            new TriggerKey(scheduled.TokenId.ToString("N")),
            TestContext.Current.CancellationToken));

        await fixture.Scheduler.TriggerJob(trigger.JobKey, trigger.JobDataMap, TestContext.Current.CancellationToken);
        await statusCheck.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);

        IRequestClient<GetJobState> stateClient = fixture.Bus.CreateRequestClient<GetJobState>();
        JobState state = await stateClient.GetJobState(jobId)
            .WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal(jobId, acceptedJobId);
        Assert.Equal(jobId, execution.JobId);
        Assert.Equal(jobId, state.JobId);
        Assert.Equal("Started", state.CurrentState);
        Assert.Equal(0, state.LastRetryAttempt);
        Assert.NotNull(state.Submitted);
        Assert.NotNull(state.Started);
        Assert.Null(state.Completed);
        Assert.Null(state.Faulted);
        Assert.Contains(scheduled.PayloadTypes, type =>
            type.EndsWith($":{nameof(JobStatusCheckRequested)}", StringComparison.Ordinal));
        Assert.True(statusCheck.ObservedCount >= 1);

        consumer.Release();
        JobCompletedSnapshot completed = await lifecycle.For(jobId).Completed.Task
            .WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal(jobId, completed.JobId);
    }

    private static async Task AssertSuccessfulLifecycle(bool useExplicitServiceAddress)
    {
        TimeSpan timeout = OperationTimeout();
        Guid jobId = NewId.NextGuid();
        var lifecycle = new JobLifecycleProbe([jobId]);
        var consumer = new CompletingJobConsumer();
        Uri? serviceAddress = null;

        await using QuartzTestBus fixture = await QuartzJobServiceTestBus.Start<CalculationJob, CompletingJobConsumer>(
            timeout,
            consumer,
            static options => options.SetJobTimeout(TimeSpan.FromMinutes(1)),
            lifecycle.Configure,
            address => serviceAddress = address);
        IRequestClient<SubmitJob<CalculationJob>> client = useExplicitServiceAddress
            ? fixture.Bus.CreateRequestClient<SubmitJob<CalculationJob>>(Assert.IsType<Uri>(serviceAddress))
            : fixture.Bus.CreateRequestClient<SubmitJob<CalculationJob>>();

        Guid acceptedJobId = await client.SubmitJob(
                jobId,
                new CalculationJob("complete"),
                cancellationToken: TestContext.Current.CancellationToken)
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        JobExecutionSnapshot execution = await consumer.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        JobSubmittedSnapshot submitted = await lifecycle.For(jobId).Submitted.Task
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        JobStartedSnapshot started = await lifecycle.For(jobId).Started.Task
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        JobCompletedSnapshot completed = await lifecycle.For(jobId).Completed.Task
            .WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal(jobId, acceptedJobId);
        Assert.Equal(new JobExecutionSnapshot(jobId, started.AttemptId, 0, "complete"), execution);
        Assert.Equal(jobId, submitted.JobId);
        Assert.NotEqual(Guid.Empty, submitted.JobTypeId);
        Assert.Equal(TimeSpan.FromMinutes(1), submitted.JobTimeout);
        Assert.Equal(jobId, started.JobId);
        Assert.NotEqual(Guid.Empty, started.AttemptId);
        Assert.Equal(0, started.RetryAttempt);
        Assert.Equal(jobId, completed.JobId);
        Assert.True(completed.Duration >= TimeSpan.Zero);
        Assert.True(submitted.Timestamp <= started.Timestamp);
        Assert.True(started.Timestamp <= completed.Timestamp);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class JobLifecycleProbe
    {
        private readonly IReadOnlyDictionary<Guid, JobSignals> _jobs;

        public JobLifecycleProbe(IEnumerable<Guid> jobIds)
        {
            _jobs = jobIds.ToDictionary(jobId => jobId, _ => new JobSignals());
        }

        public JobSignals For(Guid jobId) => _jobs[jobId];

        public void Configure(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint($"job-events-{NewId.NextGuid():N}", endpoint =>
            {
                endpoint.Handler<JobSubmitted>(context =>
                {
                    if (_jobs.TryGetValue(context.Message.JobId, out JobSignals? signals))
                    {
                        signals.Submitted.TrySetResult(new JobSubmittedSnapshot(
                            context.Message.JobId,
                            context.Message.JobTypeId,
                            context.Message.Timestamp,
                            context.Message.JobTimeout));
                    }

                    return Task.CompletedTask;
                });
                endpoint.Handler<JobStarted>(context =>
                {
                    if (_jobs.TryGetValue(context.Message.JobId, out JobSignals? signals))
                    {
                        signals.Started.TrySetResult(new JobStartedSnapshot(
                            context.Message.JobId,
                            context.Message.AttemptId,
                            context.Message.RetryAttempt,
                            context.Message.Timestamp));
                    }

                    return Task.CompletedTask;
                });
                endpoint.Handler<JobCompleted>(context =>
                {
                    if (_jobs.TryGetValue(context.Message.JobId, out JobSignals? signals))
                    {
                        signals.Completed.TrySetResult(new JobCompletedSnapshot(
                            context.Message.JobId,
                            context.Message.Timestamp,
                            context.Message.Duration));
                    }

                    return Task.CompletedTask;
                });
                endpoint.Handler<JobCanceled>(context =>
                {
                    if (_jobs.TryGetValue(context.Message.JobId, out JobSignals? signals))
                    {
                        signals.Canceled.TrySetResult(new JobCanceledSnapshot(
                            context.Message.JobId,
                            context.Message.Timestamp,
                            context.Message.Reason));
                    }

                    return Task.CompletedTask;
                });
            });
        }
    }

    private sealed class JobSignals
    {
        public TaskCompletionSource<JobSubmittedSnapshot> Submitted { get; } = NewSource<JobSubmittedSnapshot>();
        public TaskCompletionSource<JobStartedSnapshot> Started { get; } = NewSource<JobStartedSnapshot>();
        public TaskCompletionSource<JobCompletedSnapshot> Completed { get; } = NewSource<JobCompletedSnapshot>();
        public TaskCompletionSource<JobCanceledSnapshot> Canceled { get; } = NewSource<JobCanceledSnapshot>();

        private static TaskCompletionSource<T> NewSource<T>() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class CompletingJobConsumer : IJobConsumer<CalculationJob>
    {
        private readonly TaskCompletionSource<JobExecutionSnapshot> _completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<JobExecutionSnapshot> Completed => _completed.Task;

        public Task Run(JobContext<CalculationJob> context)
        {
            _completed.TrySetResult(Snapshot(context));
            return Task.CompletedTask;
        }
    }

    private sealed class CancellationAwareJobConsumer : IJobConsumer<CalculationJob>
    {
        private readonly TaskCompletionSource<JobExecutionSnapshot> _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _cancellationObserved =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _neverReleased = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<JobExecutionSnapshot> Started => _started.Task;
        public Task<bool> CancellationObserved => _cancellationObserved.Task;

        public async Task Run(JobContext<CalculationJob> context)
        {
            _started.TrySetResult(Snapshot(context));
            try
            {
                await _neverReleased.Task.WaitAsync(context.CancellationToken);
            }
            finally
            {
                _cancellationObserved.TrySetResult(context.CancellationToken.IsCancellationRequested);
            }
        }
    }

    private sealed class ReleasableJobConsumer : IJobConsumer<CalculationJob>
    {
        private readonly TaskCompletionSource<JobExecutionSnapshot> _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<JobExecutionSnapshot> Started => _started.Task;

        public void Release() => _release.TrySetResult();

        public async Task Run(JobContext<CalculationJob> context)
        {
            _started.TrySetResult(Snapshot(context));
            await _release.Task.WaitAsync(context.CancellationToken);
        }
    }

    private sealed class ConcurrencyTrackingJobConsumer(int expectedJobs, int expectedLimit) : IJobConsumer<CalculationJob>
    {
        private readonly ConcurrentDictionary<Guid, byte> _executedJobIds = new();
        private readonly TaskCompletionSource _allCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _limitReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _completedCount;
        private int _currentCount;
        private int _duplicateExecutionCount;
        private int _maximumCount;

        public Task AllCompleted => _allCompleted.Task;
        public IReadOnlyCollection<Guid> ExecutedJobIds => _executedJobIds.Keys.ToArray();
        public Task LimitReached => _limitReached.Task;
        public int CurrentCount => Volatile.Read(ref _currentCount);
        public int DuplicateExecutionCount => Volatile.Read(ref _duplicateExecutionCount);
        public int MaximumCount => Volatile.Read(ref _maximumCount);

        public void ReleaseAll() => _release.TrySetResult();

        public async Task Run(JobContext<CalculationJob> context)
        {
            if (!_executedJobIds.TryAdd(context.JobId, 0))
                Interlocked.Increment(ref _duplicateExecutionCount);

            int current = Interlocked.Increment(ref _currentCount);
            UpdateMaximum(current);
            if (current == expectedLimit)
                _limitReached.TrySetResult();

            try
            {
                await _release.Task.WaitAsync(context.CancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref _currentCount);
                if (Interlocked.Increment(ref _completedCount) == expectedJobs)
                    _allCompleted.TrySetResult();
            }
        }

        private void UpdateMaximum(int current)
        {
            int maximum = Volatile.Read(ref _maximumCount);
            while (current > maximum)
            {
                int observed = Interlocked.CompareExchange(ref _maximumCount, current, maximum);
                if (observed == maximum)
                    return;

                maximum = observed;
            }
        }
    }

    private static JobExecutionSnapshot Snapshot(JobContext<CalculationJob> context) =>
        new(context.JobId, context.AttemptId, context.RetryAttempt, context.Job.Label);

    private sealed class CalculationJob
    {
        public CalculationJob()
        {
        }

        public CalculationJob(string label)
        {
            Label = label;
        }

        public string Label { get; init; } = string.Empty;
    }
    private sealed record JobExecutionSnapshot(Guid JobId, Guid AttemptId, int RetryAttempt, string Label);
    private sealed record JobSubmittedSnapshot(Guid JobId, Guid JobTypeId, DateTime Timestamp, TimeSpan JobTimeout);
    private sealed record JobStartedSnapshot(Guid JobId, Guid AttemptId, int RetryAttempt, DateTime Timestamp);
    private sealed record JobCompletedSnapshot(Guid JobId, DateTime Timestamp, TimeSpan Duration);
    private sealed record JobCanceledSnapshot(Guid JobId, DateTime Timestamp, string? Reason);
}
