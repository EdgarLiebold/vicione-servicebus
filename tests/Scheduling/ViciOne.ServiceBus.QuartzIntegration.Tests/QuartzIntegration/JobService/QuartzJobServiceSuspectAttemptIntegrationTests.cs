using System.Collections.Concurrent;
using Quartz;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.QuartzIntegration.Tests.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.QuartzIntegration.Tests.QuartzIntegration.JobService;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzJobServiceSuspectAttemptIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-SERVICE-SUSPECT", "silent-attempt-faults-without-retry")]
    public async Task SilentAttempt_UsesQuartzStatusChecksAndFaultsWithoutASuspectRetry()
    {
        await using SuspectAttemptFixture fixture = await SuspectAttemptFixture.Start(suspectRetryCount: 0);

        Guid acceptedJobId = await fixture.Submit();
        JobAttemptSnapshot firstAttempt = await fixture.Consumer.FirstAttempt
            .WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);
        SuppressedAttemptFault suppressed = await fixture.Suppression.Completed
            .WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);

        await fixture.TriggerAllStatusChecks();

        JobFaultSnapshot faulted = await fixture.Events.Faulted
            .WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);
        await fixture.StatusChecks.Completed.WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);
        JobState state = await fixture.GetState();

        Assert.Equal(fixture.JobId, acceptedJobId);
        Assert.Equal(fixture.JobId, firstAttempt.JobId);
        Assert.Equal(0, firstAttempt.RetryAttempt);
        Assert.Equal(firstAttempt.AttemptId, suppressed.AttemptId);
        Assert.Equal(["job", "job-attempt"], suppressed.Endpoints.Order());
        Assert.Equal(2, suppressed.DeliveryCount);
        Assert.Equal(3, fixture.StatusSchedules.ObservedCount);
        Assert.Equal(2, fixture.StatusChecks.ObservedCount);
        Assert.Equal(fixture.JobId, faulted.JobId);
        Assert.Contains("status check timed out", faulted.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Faulted", state.CurrentState);
        Assert.Equal(0, state.LastRetryAttempt);
        Assert.NotNull(state.Faulted);
        Assert.Null(state.Completed);
        Assert.Equal(1, fixture.Consumer.AttemptCount);
        Assert.Equal(0, fixture.Events.CompletedCount);
        Assert.Equal(0, fixture.Events.CanceledCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-JOB-SERVICE-SUSPECT", "retry-ignores-stale-attempt-and-completes")]
    public async Task SuspectRetry_IgnoresThePreviousAttemptCompletionAndCompletesTheCurrentAttempt()
    {
        await using SuspectAttemptFixture fixture = await SuspectAttemptFixture.Start(suspectRetryCount: 1);

        Guid acceptedJobId = await fixture.Submit();
        JobAttemptSnapshot firstAttempt = await fixture.Consumer.FirstAttempt
            .WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);
        SuppressedAttemptFault suppressed = await fixture.Suppression.Completed
            .WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);

        await fixture.TriggerAllStatusChecks();

        ScheduledMessageSnapshot retrySchedule = await fixture.RetrySchedule.Scheduled
            .WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);
        await fixture.Trigger(retrySchedule);
        JobAttemptSnapshot retryAttempt = await fixture.Consumer.RetryAttempt
            .WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);

        await fixture.SendStaleCompletion(firstAttempt);
        JobState stateWhileRetryRuns = await fixture.GetState();

        Assert.Equal(fixture.JobId, acceptedJobId);
        Assert.Equal(firstAttempt.AttemptId, suppressed.AttemptId);
        Assert.Equal(0, firstAttempt.RetryAttempt);
        Assert.Equal(1, retryAttempt.RetryAttempt);
        Assert.NotEqual(firstAttempt.AttemptId, retryAttempt.AttemptId);
        Assert.Equal("Started", stateWhileRetryRuns.CurrentState);
        Assert.Equal(1, stateWhileRetryRuns.LastRetryAttempt);
        Assert.Null(stateWhileRetryRuns.Completed);
        Assert.Equal(0, fixture.Events.CompletedCount);
        Assert.Equal(0, fixture.Events.FaultedCount);

        fixture.Consumer.ReleaseRetry();
        JobCompletionSnapshot completed = await fixture.Events.Completed
            .WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);
        await fixture.StatusChecks.Completed.WaitAsync(fixture.Timeout, TestContext.Current.CancellationToken);

        Assert.Equal(fixture.JobId, completed.JobId);
        Assert.Equal(2, fixture.Consumer.AttemptCount);
        Assert.Equal(2, fixture.Events.StartedAttempts.Count);
        Assert.Equal([0, 1], fixture.Events.StartedAttempts.Select(attempt => attempt.RetryAttempt).Order());
        Assert.Equal(1, fixture.Events.CompletedCount);
        Assert.Equal(0, fixture.Events.FaultedCount);
        Assert.Equal(0, fixture.Events.CanceledCount);
        Assert.Contains(retrySchedule.PayloadTypes, type =>
            type.EndsWith($":{nameof(JobRetryDelayElapsed)}", StringComparison.Ordinal));
    }

    private sealed class SuspectAttemptFixture : IAsyncDisposable
    {
        private readonly QuartzTestBus _bus;
        private readonly ConnectHandle _retryScheduleObserver;
        private readonly ConnectHandle _statusScheduleObserver;
        private readonly ConnectHandle _statusObserver;

        private SuspectAttemptFixture(
            QuartzTestBus bus,
            TimeSpan timeout,
            Guid jobId,
            SilentAttemptConsumer consumer,
            AttemptFaultSuppression suppression,
            SuspectTerminalProbe events,
            ScheduledMessageSequenceCapture statusSchedules,
            ScheduledMessageCapture retrySchedule,
            ConsumeCompletionObserver<GetJobAttemptStatus> statusChecks,
            ConnectHandle statusScheduleObserver,
            ConnectHandle retryScheduleObserver,
            ConnectHandle statusObserver)
        {
            _bus = bus;
            Timeout = timeout;
            JobId = jobId;
            Consumer = consumer;
            Suppression = suppression;
            Events = events;
            StatusSchedules = statusSchedules;
            RetrySchedule = retrySchedule;
            StatusChecks = statusChecks;
            _statusScheduleObserver = statusScheduleObserver;
            _retryScheduleObserver = retryScheduleObserver;
            _statusObserver = statusObserver;
        }

        public TimeSpan Timeout { get; }
        public Guid JobId { get; }
        public SilentAttemptConsumer Consumer { get; }
        public AttemptFaultSuppression Suppression { get; }
        public SuspectTerminalProbe Events { get; }
        public ScheduledMessageSequenceCapture StatusSchedules { get; }
        public ScheduledMessageCapture RetrySchedule { get; }
        public ConsumeCompletionObserver<GetJobAttemptStatus> StatusChecks { get; }

        public static async Task<SuspectAttemptFixture> Start(int suspectRetryCount)
        {
            TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
                .GetValidatedOptions()
                .OperationTimeout!.Value;
            Guid jobId = NewId.NextGuid();
            var consumer = new SilentAttemptConsumer();
            var suppression = new AttemptFaultSuppression(jobId);
            var events = new SuspectTerminalProbe(jobId);

            QuartzTestBus bus = await QuartzJobServiceTestBus.Start<SuspectJob, SilentAttemptConsumer>(
                timeout,
                consumer,
                static options => options.SetJobTimeout(TimeSpan.FromMinutes(1)),
                configurator =>
                {
                    configurator.UseFilter(suppression);
                    events.Configure(configurator);
                },
                configureJobService: service =>
                {
                    service.StatusCheckInterval = TimeSpan.FromSeconds(30);
                    service.SuspectJobRetryCount = suspectRetryCount;
                    service.SuspectJobRetryDelay = TimeSpan.FromMinutes(1);
                });
            var statusSchedules = new ScheduledMessageSequenceCapture(nameof(JobStatusCheckRequested), expectedCount: 3);
            var retrySchedule = new ScheduledMessageCapture(nameof(JobRetryDelayElapsed));
            var statusChecks = new ConsumeCompletionObserver<GetJobAttemptStatus>(message => message.JobId == jobId, expectedCount: 2);
            ConnectHandle statusScheduleObserver = bus.Bus.ConnectConsumeObserver(statusSchedules);
            ConnectHandle retryScheduleObserver = bus.Bus.ConnectConsumeObserver(retrySchedule);
            ConnectHandle statusObserver = bus.Bus.ConnectConsumeObserver(statusChecks);

            return new SuspectAttemptFixture(
                bus,
                timeout,
                jobId,
                consumer,
                suppression,
                events,
                statusSchedules,
                retrySchedule,
                statusChecks,
                statusScheduleObserver,
                retryScheduleObserver,
                statusObserver);
        }

        public async Task<Guid> Submit()
        {
            IRequestClient<SubmitJob<SuspectJob>> client = _bus.Bus.CreateRequestClient<SubmitJob<SuspectJob>>();
            return await client.SubmitJob(
                    JobId,
                    new SuspectJob("silent-worker"),
                    cancellationToken: TestContext.Current.CancellationToken)
                .WaitAsync(Timeout, TestContext.Current.CancellationToken);
        }

        public async Task TriggerAllStatusChecks()
        {
            for (int index = 0; index < 3; index++)
            {
                ScheduledMessageSnapshot schedule = await StatusSchedules.At(index)
                    .WaitAsync(Timeout, TestContext.Current.CancellationToken);
                await Trigger(schedule);
            }
        }

        public async Task Trigger(ScheduledMessageSnapshot schedule)
        {
            ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(await _bus.Scheduler.GetTrigger(
                new TriggerKey(schedule.TokenId.ToString("N")),
                TestContext.Current.CancellationToken));
            await _bus.Scheduler.TriggerJob(trigger.JobKey, trigger.JobDataMap, TestContext.Current.CancellationToken);
        }

        public async Task<JobState> GetState()
        {
            IRequestClient<GetJobState> client = _bus.Bus.CreateRequestClient<GetJobState>();
            return await client.GetJobState(JobId).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        }

        public async Task SendStaleCompletion(JobAttemptSnapshot attempt)
        {
            ISendEndpoint endpoint = await _bus.Bus.GetSendEndpoint(new Uri("loopback://localhost/job"))
                .WaitAsync(Timeout, TestContext.Current.CancellationToken);
            await endpoint.Send<JobAttemptCompleted>(new
            {
                JobId,
                AttemptId = attempt.AttemptId,
                RetryAttempt = attempt.RetryAttempt,
                Timestamp = new DateTime(2042, 2, 3, 4, 5, 6, DateTimeKind.Utc),
                Duration = TimeSpan.FromSeconds(1),
                InstanceProperties = (Dictionary<string, object>?)null,
                JobTypeProperties = (Dictionary<string, object>?)null,
            }, TestContext.Current.CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            Consumer.ReleaseRetry();
            _statusScheduleObserver.Dispose();
            _retryScheduleObserver.Dispose();
            _statusObserver.Dispose();
            await _bus.DisposeAsync();
        }
    }

    private sealed class SilentAttemptConsumer : IJobConsumer<SuspectJob>
    {
        private readonly TaskCompletionSource<JobAttemptSnapshot> _firstAttempt =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<JobAttemptSnapshot> _retryAttempt =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _retryRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _attemptCount;

        public int AttemptCount => Volatile.Read(ref _attemptCount);
        public Task<JobAttemptSnapshot> FirstAttempt => _firstAttempt.Task;
        public Task<JobAttemptSnapshot> RetryAttempt => _retryAttempt.Task;

        public void ReleaseRetry() => _retryRelease.TrySetResult();

        public Task Run(JobContext<SuspectJob> context)
        {
            Interlocked.Increment(ref _attemptCount);
            var snapshot = new JobAttemptSnapshot(context.JobId, context.AttemptId, context.RetryAttempt);
            if (context.RetryAttempt == 0)
            {
                _firstAttempt.TrySetResult(snapshot);
                return Task.FromException(new OperationCanceledException("worker stopped reporting"));
            }

            _retryAttempt.TrySetResult(snapshot);
            return _retryRelease.Task.WaitAsync(context.CancellationToken);
        }
    }

    private sealed class AttemptFaultSuppression(Guid jobId) : IFilter<ConsumeContext<JobAttemptFaulted>>
    {
        private static readonly string[] ExpectedEndpoints = ["job", "job-attempt"];
        private readonly TaskCompletionSource<SuppressedAttemptFault> _completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object _lock = new();
        private readonly HashSet<string> _endpoints = [];
        private Guid? _messageId;
        private Guid _attemptId;

        public Task<SuppressedAttemptFault> Completed => _completed.Task;

        public Task Send(ConsumeContext<JobAttemptFaulted> context, IPipe<ConsumeContext<JobAttemptFaulted>> next)
        {
            string endpoint = context.ReceiveContext.InputAddress.AbsolutePath.Trim('/');
            if (TrySuppress(context, endpoint))
                return Task.CompletedTask;

            return next.Send(context);
        }

        public void Probe(ProbeContext context) => context.CreateScope("job-attempt-fault-suppression");

        private bool TrySuppress(ConsumeContext<JobAttemptFaulted> context, string endpoint)
        {
            if (!ExpectedEndpoints.Contains(endpoint, StringComparer.Ordinal)
                || context.Message.JobId != jobId
                || context.Message.RetryAttempt != 0
                || context.Message.RetryDelay.HasValue
                || !context.MessageId.HasValue)
            {
                return false;
            }

            lock (_lock)
            {
                if (!_messageId.HasValue)
                {
                    _messageId = context.MessageId.Value;
                    _attemptId = context.Message.AttemptId;
                }

                if (_messageId.Value != context.MessageId.Value)
                    return false;

                _endpoints.Add(endpoint);
                if (_endpoints.Count == ExpectedEndpoints.Length)
                {
                    _completed.TrySetResult(new SuppressedAttemptFault(
                        _messageId.Value,
                        _attemptId,
                        [.. _endpoints],
                        _endpoints.Count));
                }

                return true;
            }
        }
    }

    private sealed class SuspectTerminalProbe(Guid jobId)
    {
        private readonly TaskCompletionSource<JobCompletionSnapshot> _completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<JobFaultSnapshot> _faulted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ConcurrentQueue<JobStartedSnapshot> _startedAttempts = new();
        private int _canceledCount;
        private int _completedCount;
        private int _faultedCount;

        public int CanceledCount => Volatile.Read(ref _canceledCount);
        public Task<JobCompletionSnapshot> Completed => _completed.Task;
        public int CompletedCount => Volatile.Read(ref _completedCount);
        public Task<JobFaultSnapshot> Faulted => _faulted.Task;
        public int FaultedCount => Volatile.Read(ref _faultedCount);
        public IReadOnlyCollection<JobStartedSnapshot> StartedAttempts => _startedAttempts.ToArray();

        public void Configure(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint($"suspect-job-events-{NewId.NextGuid():N}", endpoint =>
            {
                endpoint.Handler<JobStarted>(context =>
                {
                    if (context.Message.JobId == jobId)
                        _startedAttempts.Enqueue(new JobStartedSnapshot(context.Message.AttemptId, context.Message.RetryAttempt));
                    return Task.CompletedTask;
                });
                endpoint.Handler<JobCompleted>(context =>
                {
                    if (context.Message.JobId == jobId)
                    {
                        Interlocked.Increment(ref _completedCount);
                        _completed.TrySetResult(new JobCompletionSnapshot(context.Message.JobId));
                    }
                    return Task.CompletedTask;
                });
                endpoint.Handler<JobFaulted>(context =>
                {
                    if (context.Message.JobId == jobId)
                    {
                        Interlocked.Increment(ref _faultedCount);
                        _faulted.TrySetResult(new JobFaultSnapshot(context.Message.JobId, context.Message.Exceptions.Message));
                    }
                    return Task.CompletedTask;
                });
                endpoint.Handler<JobCanceled>(context =>
                {
                    if (context.Message.JobId == jobId)
                        Interlocked.Increment(ref _canceledCount);
                    return Task.CompletedTask;
                });
            });
        }
    }

    private sealed class SuspectJob
    {
        public SuspectJob()
        {
        }

        public SuspectJob(string label)
        {
            Label = label;
        }

        public string Label { get; init; } = string.Empty;
    }

    private sealed record JobAttemptSnapshot(Guid JobId, Guid AttemptId, int RetryAttempt);
    private sealed record SuppressedAttemptFault(Guid MessageId, Guid AttemptId, string[] Endpoints, int DeliveryCount);
    private sealed record JobStartedSnapshot(Guid AttemptId, int RetryAttempt);
    private sealed record JobCompletionSnapshot(Guid JobId);
    private sealed record JobFaultSnapshot(Guid JobId, string Reason);
}
