using System.Collections.Concurrent;
using Quartz;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.QuartzIntegration.Tests.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.QuartzIntegration.Tests.QuartzIntegration.JobService;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzJobServiceFaultIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-SERVICE-FAULT", "unhandled-failure-faults-the-job")]
    public async Task UnhandledConsumerFailure_PublishesOneTerminalJobFaultAsync()
    {
        TimeSpan timeout = OperationTimeout();
        Guid jobId = NewId.NextGuid();
        var events = new JobTerminalProbe(jobId, expectedStarts: 1);
        var consumer = new PermanentlyFaultingJobConsumer();

        await using QuartzTestBus fixture = await QuartzJobServiceTestBus.StartAsync<FaultingJob, PermanentlyFaultingJobConsumer>(
            timeout,
            consumer,
            static options => options.SetJobTimeout(TimeSpan.FromMinutes(1)),
            events.Configure);
        IRequestClient<SubmitJob<FaultingJob>> submitClient = fixture.Bus.CreateRequestClient<SubmitJob<FaultingJob>>();

        Guid acceptedJobId = await submitClient.SubmitJobAsync(
                jobId,
                new FaultingJob("permanent"),
                cancellationToken: TestContext.Current.CancellationToken)
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        JobAttemptSnapshot attempted = await consumer.Attempted.WaitAsync(timeout, TestContext.Current.CancellationToken);
        JobFaultSnapshot faulted = await events.Faulted.WaitAsync(timeout, TestContext.Current.CancellationToken);
        await events.ExpectedStartsObserved.WaitAsync(timeout, TestContext.Current.CancellationToken);

        IRequestClient<GetJobState> stateClient = fixture.Bus.CreateRequestClient<GetJobState>();
        JobState state = await stateClient.GetJobStateAsync(jobId, cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal(jobId, acceptedJobId);
        Assert.Equal(new JobAttemptSnapshot(jobId, attempted.AttemptId, 0, "permanent"), attempted);
        Assert.NotEqual(Guid.Empty, attempted.AttemptId);
        Assert.Equal(jobId, faulted.JobId);
        Assert.Contains(nameof(PermanentJobException), faulted.ExceptionType, StringComparison.Ordinal);
        Assert.Contains("permanent failure", faulted.Reason, StringComparison.Ordinal);
        Assert.Equal(1, events.FaultedCount);
        Assert.Single(events.StartedAttempts);
        Assert.Equal("Faulted", state.CurrentState);
        Assert.NotNull(state.Faulted);
        Assert.Null(state.Completed);
        Assert.Contains("permanent failure", state.Reason, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-SERVICE-FAULT", "configured-retry-completes-through-quartz")]
    public async Task ConfiguredRetry_IsScheduledThroughQuartzAndCompletesOnTheNextAttemptAsync()
    {
        TimeSpan timeout = OperationTimeout();
        Guid jobId = NewId.NextGuid();
        var events = new JobTerminalProbe(jobId, expectedStarts: 2);
        var consumer = new TransientlyFaultingJobConsumer();

        await using QuartzTestBus fixture = await QuartzJobServiceTestBus.StartAsync<FaultingJob, TransientlyFaultingJobConsumer>(
            timeout,
            consumer,
            options => options
                .SetJobTimeout(TimeSpan.FromMinutes(1))
                .SetRetry(retry => retry.Interval(1, TimeSpan.FromMinutes(1))),
            events.Configure);
        var retrySchedule = new ScheduledMessageCapture(nameof(JobRetryDelayElapsed));
        using ConnectHandle scheduleObserver = fixture.Bus.ConnectConsumeObserver(retrySchedule);
        IRequestClient<SubmitJob<FaultingJob>> submitClient = fixture.Bus.CreateRequestClient<SubmitJob<FaultingJob>>();

        Guid acceptedJobId = await submitClient.SubmitJobAsync(
                jobId,
                new FaultingJob("transient"),
                cancellationToken: TestContext.Current.CancellationToken)
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        JobAttemptSnapshot firstAttempt = await consumer.FirstAttempt.WaitAsync(timeout, TestContext.Current.CancellationToken);
        ScheduledMessageSnapshot scheduled = await retrySchedule.Scheduled
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(await fixture.Scheduler.GetTrigger(
            new TriggerKey(scheduled.TokenId.ToString("N")),
            TestContext.Current.CancellationToken));

        await fixture.Scheduler.TriggerJob(trigger.JobKey, trigger.JobDataMap, TestContext.Current.CancellationToken);

        JobAttemptSnapshot secondAttempt = await consumer.SecondAttempt.WaitAsync(timeout, TestContext.Current.CancellationToken);
        JobCompletionSnapshot completed = await events.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        await events.ExpectedStartsObserved.WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal(jobId, acceptedJobId);
        Assert.Equal(jobId, firstAttempt.JobId);
        Assert.Equal(0, firstAttempt.RetryAttempt);
        Assert.Equal(jobId, secondAttempt.JobId);
        Assert.Equal(1, secondAttempt.RetryAttempt);
        Assert.NotEqual(firstAttempt.AttemptId, secondAttempt.AttemptId);
        Assert.Equal("transient", firstAttempt.Label);
        Assert.Equal("transient", secondAttempt.Label);
        Assert.Equal(jobId, completed.JobId);
        Assert.Equal(2, events.StartedAttempts.Count);
        Assert.Equal([0, 1], events.StartedAttempts.Select(attempt => attempt.RetryAttempt).Order());
        Assert.Equal(0, events.FaultedCount);
        Assert.Contains(scheduled.PayloadTypes, type =>
            type.EndsWith($":{nameof(JobRetryDelayElapsed)}", StringComparison.Ordinal));
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class JobTerminalProbe(Guid jobId, int expectedStarts)
    {
        private readonly TaskCompletionSource<JobCompletionSnapshot> _completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _expectedStartsObserved =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<JobFaultSnapshot> _faulted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ConcurrentQueue<JobStartedSnapshot> _startedAttempts = new();
        private int _faultedCount;

        public Task<JobCompletionSnapshot> Completed => _completed.Task;
        public Task ExpectedStartsObserved => _expectedStartsObserved.Task;
        public Task<JobFaultSnapshot> Faulted => _faulted.Task;
        public int FaultedCount => Volatile.Read(ref _faultedCount);
        public IReadOnlyCollection<JobStartedSnapshot> StartedAttempts => _startedAttempts.ToArray();

        public void Configure(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint($"job-terminal-events-{NewId.NextGuid():N}", endpoint =>
            {
                endpoint.Handler<JobStarted>(context =>
                {
                    if (context.Message.JobId == jobId)
                    {
                        _startedAttempts.Enqueue(new JobStartedSnapshot(
                            context.Message.JobId,
                            context.Message.AttemptId,
                            context.Message.RetryAttempt));
                        if (_startedAttempts.Count == expectedStarts)
                            _expectedStartsObserved.TrySetResult();
                    }

                    return Task.CompletedTask;
                });
                endpoint.Handler<JobCompleted>(context =>
                {
                    if (context.Message.JobId == jobId)
                        _completed.TrySetResult(new JobCompletionSnapshot(context.Message.JobId, context.Message.Duration));

                    return Task.CompletedTask;
                });
                endpoint.Handler<JobFaulted>(context =>
                {
                    if (context.Message.JobId == jobId)
                    {
                        Interlocked.Increment(ref _faultedCount);
                        _faulted.TrySetResult(new JobFaultSnapshot(
                            context.Message.JobId,
                            context.Message.Exceptions.ExceptionType,
                            context.Message.Exceptions.Message));
                    }

                    return Task.CompletedTask;
                });
            });
        }
    }

    private sealed class PermanentlyFaultingJobConsumer : IJobConsumer<FaultingJob>
    {
        private readonly TaskCompletionSource<JobAttemptSnapshot> _attempted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<JobAttemptSnapshot> Attempted => _attempted.Task;

        public Task RunAsync(JobContext<FaultingJob> context)
        {
            _attempted.TrySetResult(Snapshot(context));
            return Task.FromException(new PermanentJobException("permanent failure"));
        }
    }

    private sealed class TransientlyFaultingJobConsumer : IJobConsumer<FaultingJob>
    {
        private readonly TaskCompletionSource<JobAttemptSnapshot> _firstAttempt =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<JobAttemptSnapshot> _secondAttempt =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _attempts;

        public Task<JobAttemptSnapshot> FirstAttempt => _firstAttempt.Task;
        public Task<JobAttemptSnapshot> SecondAttempt => _secondAttempt.Task;

        public Task RunAsync(JobContext<FaultingJob> context)
        {
            JobAttemptSnapshot snapshot = Snapshot(context);
            if (Interlocked.Increment(ref _attempts) == 1)
            {
                _firstAttempt.TrySetResult(snapshot);
                return Task.FromException(new TransientJobException("transient failure"));
            }

            _secondAttempt.TrySetResult(snapshot);
            return Task.CompletedTask;
        }
    }

    private static JobAttemptSnapshot Snapshot(JobContext<FaultingJob> context) =>
        new(context.JobId, context.AttemptId, context.RetryAttempt, context.Job.Label);

    private sealed class FaultingJob
    {
        public FaultingJob()
        {
        }

        public FaultingJob(string label)
        {
            Label = label;
        }

        public string Label { get; init; } = string.Empty;
    }

    private sealed class PermanentJobException(string message) : Exception(message);
    private sealed class TransientJobException(string message) : Exception(message);
    private sealed record JobAttemptSnapshot(Guid JobId, Guid AttemptId, int RetryAttempt, string Label);
    private sealed record JobStartedSnapshot(Guid JobId, Guid AttemptId, int RetryAttempt);
    private sealed record JobCompletionSnapshot(Guid JobId, TimeSpan Duration);
    private sealed record JobFaultSnapshot(Guid JobId, string ExceptionType, string Reason);
}
