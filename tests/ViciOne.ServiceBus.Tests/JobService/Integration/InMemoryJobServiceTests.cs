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
    public async Task PermanentFailure_PublishesTheSubmittedFaultedAndTypedFaultContracts()
    {
        var consumer = new FaultingJobConsumer(completeOnRetry: false);
        await using JobServiceFixture fixture = await JobServiceFixture.Start(consumer);
        Guid jobId = NewId.NextGuid();

        Guid accepted = await fixture.Submit(jobId, new InMemoryJob("permanent-failure"));
        JobExecutionSnapshot attempt = await consumer.NextAttempt(fixture);
        JobSubmitted submitted = await fixture.Published<JobSubmitted>(message => message.JobId == jobId);
        JobFaulted faulted = await fixture.Published<JobFaulted>(message => message.JobId == jobId);
        Fault<InMemoryJob> typedFault = await fixture.Published<Fault<InMemoryJob>>(
            message => message.Message.Label == "permanent-failure");
        JobState state = await fixture.GetState(jobId);

        Assert.Equal(jobId, accepted);
        Assert.Equal(new JobExecutionSnapshot(jobId, attempt.AttemptId, 0, "permanent-failure"), attempt);
        Assert.Equal(jobId, submitted.JobId);
        Assert.Equal(jobId, faulted.JobId);
        Assert.Equal("permanent-failure", typedFault.Message.Label);
        Assert.Equal("Faulted", state.CurrentState);
        Assert.NotNull(state.Faulted);
        Assert.Null(state.Completed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-FAULT", "configured-retry-completes-later-attempt")]
    public async Task ConfiguredJobRetry_CompletesOnTheSecondDistinctAttempt()
    {
        var consumer = new FaultingJobConsumer(completeOnRetry: true);
        await using JobServiceFixture fixture = await JobServiceFixture.Start(
            consumer,
            options => options.SetRetry(retry => retry.Immediate(1)));
        Guid jobId = NewId.NextGuid();

        await fixture.Submit(jobId, new InMemoryJob("retry"));
        JobExecutionSnapshot first = await consumer.NextAttempt(fixture);
        JobExecutionSnapshot second = await consumer.NextAttempt(fixture);
        JobCompleted<InMemoryJob> completed = await fixture.Published<JobCompleted<InMemoryJob>>(
            message => message.JobId == jobId);
        JobState state = await fixture.GetState(jobId);

        Assert.Equal(0, first.RetryAttempt);
        Assert.Equal(1, second.RetryAttempt);
        Assert.NotEqual(first.AttemptId, second.AttemptId);
        Assert.Equal(jobId, completed.JobId);
        Assert.Equal("retry", completed.Job.Label);
        Assert.Equal("Completed", state.CurrentState);
        Assert.Equal(1, state.LastRetryAttempt);
        Assert.NotNull(state.Completed);
        Assert.NotNull(state.Faulted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CANCELLATION", "running-cancel-closes-consumer-status-and-slot")]
    public async Task CancelRunningJob_ReachesTheConsumerAndClosesStatusAndSlotWithTheReason()
    {
        var consumer = new BlockingJobConsumer(completeOnRetry: false);
        await using JobServiceFixture fixture = await JobServiceFixture.Start(consumer);
        Guid jobId = NewId.NextGuid();

        Guid accepted = await fixture.Submit(jobId, new InMemoryJob("cancel"));
        JobExecutionSnapshot attempt = await consumer.NextAttempt(fixture);
        JobState started = await fixture.GetState(jobId);
        await fixture.Harness.Bus.CancelJob(jobId, "operator-requested")
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        JobCancellationSnapshot cancellation = await consumer.NextCancellation(fixture);
        JobCanceled canceled = await fixture.Published<JobCanceled>(message => message.JobId == jobId);
        JobSlotReleased released = await fixture.Sent<JobSlotReleased>(message => message.JobId == jobId);
        JobState terminal = await fixture.GetState(jobId);

        Assert.Equal(jobId, accepted);
        Assert.Equal(new JobExecutionSnapshot(jobId, attempt.AttemptId, 0, "cancel"), attempt);
        Assert.Equal(new JobCancellationSnapshot(jobId, attempt.AttemptId, true), cancellation);
        Assert.Equal("Started", started.CurrentState);
        Assert.NotNull(started.Started);
        Assert.Equal(jobId, canceled.JobId);
        Assert.Equal("operator-requested", canceled.Reason);
        Assert.Equal(jobId, released.JobId);
        Assert.Equal("Canceled", terminal.CurrentState);
        Assert.Equal("operator-requested", terminal.Reason);
        Assert.NotNull(terminal.Faulted);
        Assert.Null(terminal.Completed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CANCELLATION", "retry-after-cancel-uses-new-attempt")]
    public async Task RetryAfterCancellation_UsesANewAttemptAndCompletes()
    {
        var consumer = new BlockingJobConsumer(completeOnRetry: true);
        await using JobServiceFixture fixture = await JobServiceFixture.Start(consumer);
        Guid jobId = NewId.NextGuid();

        await fixture.Submit(jobId, new InMemoryJob("retry-after-cancel"));
        JobExecutionSnapshot first = await consumer.NextAttempt(fixture);
        await fixture.Harness.Bus.CancelJob(jobId, "retry-requested")
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        await consumer.NextCancellation(fixture);
        await fixture.Published<JobCanceled>(message => message.JobId == jobId);

        await fixture.Harness.Bus.RetryJob(jobId)
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        JobExecutionSnapshot second = await consumer.NextAttempt(fixture);
        JobCompleted<InMemoryJob> completed = await fixture.Published<JobCompleted<InMemoryJob>>(
            message => message.JobId == jobId);

        Assert.Equal(0, first.RetryAttempt);
        Assert.Equal(1, second.RetryAttempt);
        Assert.NotEqual(first.AttemptId, second.AttemptId);
        Assert.Equal(jobId, completed.JobId);
        Assert.Equal("retry-after-cancel", completed.Job.Label);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CANCELLATION", "waiting-job-cancels-without-consuming-slot")]
    public async Task CancelWaitingJob_PreservesTheRunningSlotAndPublishesItsWaitAndTerminalTransitions()
    {
        var consumer = new BlockingJobConsumer(completeOnRetry: false);
        await using JobServiceFixture fixture = await JobServiceFixture.Start(
            consumer,
            options => options.SetConcurrentJobLimit(1),
            options => options.SlotWaitTime = TimeSpan.FromSeconds(1));
        Guid runningJobId = NewId.NextGuid();
        Guid waitingJobId = NewId.NextGuid();

        await fixture.Submit(runningJobId, new InMemoryJob("running"));
        JobExecutionSnapshot running = await consumer.NextAttempt(fixture);
        await fixture.Submit(waitingJobId, new InMemoryJob("waiting"));
        JobSlotWaitElapsed waited = await fixture.Sent<JobSlotWaitElapsed>(message => message.JobId == waitingJobId);

        await fixture.Harness.Bus.CancelJob(waitingJobId, "waiting-canceled")
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        JobCanceled waitingCanceled = await fixture.Published<JobCanceled>(message => message.JobId == waitingJobId);
        JobState waitingState = await fixture.GetState(waitingJobId);

        Assert.Equal(runningJobId, running.JobId);
        Assert.Equal(waitingJobId, waited.JobId);
        Assert.Equal(waitingJobId, waitingCanceled.JobId);
        Assert.Equal("waiting-canceled", waitingCanceled.Reason);
        Assert.Equal("Canceled", waitingState.CurrentState);
        Assert.Null(waitingState.Started);

        await fixture.Harness.Bus.CancelJob(runningJobId, "fixture-cleanup")
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        await consumer.NextCancellation(fixture);
        await fixture.Published<JobCanceled>(message => message.JobId == runningJobId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-COMPLETION", "registered-lifecycle-through-scoped-publish-filter")]
    public async Task RegisteredStateMachines_CompleteTheLifecycleThroughTheScopedPublishFilter()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.Start(consumer, useScopedFilter: true);
        Guid jobId = NewId.NextGuid();

        Guid accepted = await fixture.Submit(jobId, new InMemoryJob("complete"));
        JobExecutionSnapshot execution = await consumer.NextAttempt(fixture);
        JobSubmitted submitted = await fixture.Published<JobSubmitted>(message => message.JobId == jobId);
        JobStarted started = await fixture.Published<JobStarted>(message => message.JobId == jobId);
        JobCompleted completed = await fixture.Published<JobCompleted>(message => message.JobId == jobId);
        JobCompleted<InMemoryJob> typedCompleted = await fixture.Published<JobCompleted<InMemoryJob>>(
            message => message.JobId == jobId);
        JobState state = await fixture.GetState(jobId);
        PublishObservation observation = fixture.PublishRecorder.Single(
            typeof(JobCompleted<InMemoryJob>), jobId);

        Assert.Equal(jobId, accepted);
        Assert.Equal(jobId, submitted.JobId);
        Assert.NotEqual(Guid.Empty, submitted.JobTypeId);
        Assert.Equal(new JobExecutionSnapshot(jobId, started.AttemptId, 0, "complete"), execution);
        Assert.Equal(jobId, completed.JobId);
        Assert.Equal(jobId, typedCompleted.JobId);
        Assert.Equal("complete", typedCompleted.Job.Label);
        Assert.Equal("Completed", state.CurrentState);
        Assert.NotNull(state.Completed);
        Assert.Null(state.Faulted);
        Assert.True(submitted.Timestamp <= started.Timestamp);
        Assert.True(started.Timestamp <= completed.Timestamp);
        Assert.NotEqual(Guid.Empty, observation.ScopeId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-IDENTITY", "published-jobs-receive-distinct-nonempty-identities")]
    public async Task PublishingJobs_GeneratesDistinctNonEmptyIdentitiesAcrossEachLifecycle()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.Start(
            consumer,
            configureSaga: options => options.SlotWaitTime = TimeSpan.FromSeconds(1));

        await fixture.Harness.Bus.Publish(new InMemoryJob("first"), fixture.CancellationToken)
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        JobExecutionSnapshot first = await consumer.NextAttempt(fixture);
        JobCompleted<InMemoryJob> firstCompleted = await fixture.Published<JobCompleted<InMemoryJob>>(
            message => message.JobId == first.JobId);

        await fixture.Harness.Bus.Publish(new InMemoryJob("second"), fixture.CancellationToken)
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        JobExecutionSnapshot second = await consumer.NextAttempt(fixture);
        JobCompleted<InMemoryJob> secondCompleted = await fixture.Published<JobCompleted<InMemoryJob>>(
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
    public async Task UnknownJobIdentity_ReturnsTheCompleteNotFoundState()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.Start(consumer);
        Guid missingJobId = NewId.NextGuid();

        JobState state = await fixture.GetState(missingJobId);

        Assert.Equal(missingJobId, state.JobId);
        Assert.Equal("NotFound", state.CurrentState);
        Assert.Null(state.Submitted);
        Assert.Null(state.Started);
        Assert.Null(state.Completed);
        Assert.Null(state.Faulted);
        Assert.Null(state.Reason);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-LIFECYCLE", "cancel-readd-and-manual-continuation")]
    public async Task RecurringJob_CancelReAddAndManualRunPreserveItsIdentityAndContinuation()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.Start(consumer);
        string jobName = $"resumable-{NewId.NextGuid():N}";
        DateTimeOffset start = fixture.Scheduler.UtcNow.AddDays(1).AddSeconds(1);

        Guid initialJobId = await fixture.AddOrUpdateRecurring(
            jobName,
            new InMemoryJob("before-cancel"),
            schedule =>
            {
                schedule.Start = start;
                schedule.Every(hours: 1);
            });
        await fixture.ConsumedCount<JobSubmitted>(message => message.JobId == initialJobId, 1);

        await fixture.RunRecurring(jobName);
        JobExecutionSnapshot first = await consumer.NextAttempt(fixture);
        JobCompleted<InMemoryJob> firstCompleted = await fixture.Published<JobCompleted<InMemoryJob>>(
            message => message.JobId == initialJobId && message.Job.Label == "before-cancel");
        await fixture.ConsumedCount<JobCompleted>(message => message.JobId == initialJobId, 1);

        Guid canceledJobId = await fixture.CancelRecurring(jobName, "operator-pause");
        JobCanceled canceled = await fixture.Published<JobCanceled>(
            message => message.JobId == initialJobId && message.Reason == "operator-pause");

        Guid resumedJobId = await fixture.AddOrUpdateRecurring(
            jobName,
            new InMemoryJob("after-cancel"),
            schedule =>
            {
                schedule.Start = start;
                schedule.Every(hours: 1);
            });
        await fixture.ConsumedCount<JobSubmitted>(message => message.JobId == initialJobId, 2);
        await fixture.RunRecurring(jobName);
        JobExecutionSnapshot second = await consumer.NextAttempt(fixture);
        JobCompleted<InMemoryJob> secondCompleted = await fixture.Published<JobCompleted<InMemoryJob>>(
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
    public async Task NamedRecurringJobs_KeepDistinctStableIdentitiesAcrossRunsUpdatesAndNoOpUpdates()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.Start(consumer);
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
            Guid jobId = await fixture.AddOrUpdateRecurring(
                name,
                new InMemoryJob(name),
                schedule =>
                {
                    schedule.Start = start;
                    schedule.Every(seconds: seconds);
                });
            jobIds.Add(name, jobId);
            await fixture.ConsumedCount<JobSubmitted>(message => message.JobId == jobId, 1);

            JobState state = await fixture.GetState(jobId);
            Assert.True(state.IsRecurring);
            Assert.NotNull(state.NextStartDate);
        }

        Assert.Equal(schedules.Length, jobIds.Values.Distinct().Count());

        await fixture.RunRecurring("one");
        JobExecutionSnapshot first = await consumer.NextAttempt(fixture);
        await fixture.PublishedCount<JobCompleted<InMemoryJob>>(message => message.JobId == jobIds["one"], 1);
        await fixture.ConsumedCount<JobCompleted>(message => message.JobId == jobIds["one"], 1);

        await fixture.RunRecurring("one");
        JobExecutionSnapshot second = await consumer.NextAttempt(fixture);
        await fixture.PublishedCount<JobCompleted<InMemoryJob>>(message => message.JobId == jobIds["one"], 2);
        await fixture.ConsumedCount<JobCompleted>(message => message.JobId == jobIds["one"], 2);

        JobState beforeUpdate = await fixture.GetState(jobIds["one"]);
        DateTimeOffset updatedStart = start.AddHours(12);
        Guid updatedJobId = await fixture.AddOrUpdateRecurring(
            "one",
            new InMemoryJob("one-updated"),
            schedule =>
            {
                schedule.Start = updatedStart;
                schedule.Every(seconds: 10);
            });
        await fixture.ConsumedCount<JobSubmitted>(message => message.JobId == jobIds["one"], 2);
        JobState afterUpdate = await fixture.GetState(jobIds["one"]);

        Guid unchangedJobId = await fixture.AddOrUpdateRecurring(
            "one",
            new InMemoryJob("one-noop"),
            schedule =>
            {
                schedule.Start = updatedStart;
                schedule.Every(seconds: 10);
            });
        await fixture.ConsumedCount<JobSubmitted>(message => message.JobId == jobIds["one"], 3);
        JobState afterNoOp = await fixture.GetState(jobIds["one"]);

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
    public async Task OneShotJob_RunsAtTheProviderOwnedScheduledInstantAndThenCompletes()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.Start(consumer);
        DateTimeOffset scheduledTime = fixture.Scheduler.UtcNow.AddHours(1);

        Guid jobId = await fixture.Schedule(scheduledTime, new InMemoryJob("one-shot"));
        await fixture.ConsumedCount<JobSubmitted>(message => message.JobId == jobId, 1);
        JobState scheduled = await fixture.GetState(jobId);

        fixture.Scheduler.Advance(TimeSpan.FromHours(1));

        JobExecutionSnapshot execution = await consumer.NextAttempt(fixture);
        JobCompleted<InMemoryJob> completed = await fixture.Published<JobCompleted<InMemoryJob>>(
            message => message.JobId == jobId);
        await fixture.ConsumedCount<JobCompleted>(message => message.JobId == jobId, 1);
        JobState terminal = await fixture.GetState(jobId);

        Assert.False(scheduled.IsRecurring);
        Assert.Equal("WaitingForSlot", scheduled.CurrentState);
        Assert.Equal(jobId, execution.JobId);
        Assert.Equal("one-shot", completed.Job.Label);
        Assert.Equal("Completed", terminal.CurrentState);
        Assert.NotNull(terminal.Completed);
    }

    public sealed record InMemoryJob(string Label);

    private sealed class CompletingJobConsumer : IJobConsumer<InMemoryJob>
    {
        private readonly Channel<JobExecutionSnapshot> _attempts = Channel.CreateUnbounded<JobExecutionSnapshot>();

        public Task Run(JobContext<InMemoryJob> context) =>
            _attempts.Writer.WriteAsync(Snapshot(context), context.CancellationToken).AsTask();

        public Task<JobExecutionSnapshot> NextAttempt(JobServiceFixture fixture) =>
            _attempts.Reader.ReadAsync(fixture.CancellationToken).AsTask()
                .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
    }

    private sealed class FaultingJobConsumer(bool completeOnRetry) : IJobConsumer<InMemoryJob>
    {
        private readonly Channel<JobExecutionSnapshot> _attempts = Channel.CreateUnbounded<JobExecutionSnapshot>();

        public async Task Run(JobContext<InMemoryJob> context)
        {
            await _attempts.Writer.WriteAsync(Snapshot(context), context.CancellationToken);
            if (completeOnRetry && context.RetryAttempt > 0)
                return;

            throw new InvalidOperationException($"job {context.Job.Label} failed on attempt {context.RetryAttempt}");
        }

        public Task<JobExecutionSnapshot> NextAttempt(JobServiceFixture fixture) =>
            _attempts.Reader.ReadAsync(fixture.CancellationToken).AsTask()
                .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
    }

    private sealed class BlockingJobConsumer(bool completeOnRetry) : IJobConsumer<InMemoryJob>
    {
        private readonly Channel<JobExecutionSnapshot> _attempts = Channel.CreateUnbounded<JobExecutionSnapshot>();
        private readonly Channel<JobCancellationSnapshot> _cancellations = Channel.CreateUnbounded<JobCancellationSnapshot>();
        private readonly TaskCompletionSource _never = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task Run(JobContext<InMemoryJob> context)
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

        public Task<JobExecutionSnapshot> NextAttempt(JobServiceFixture fixture) =>
            _attempts.Reader.ReadAsync(fixture.CancellationToken).AsTask()
                .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);

        public Task<JobCancellationSnapshot> NextCancellation(JobServiceFixture fixture) =>
            _cancellations.Reader.ReadAsync(fixture.CancellationToken).AsTask()
                .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
    }

    private sealed record JobExecutionSnapshot(Guid JobId, Guid AttemptId, int RetryAttempt, string Label);
    private sealed record JobCancellationSnapshot(Guid JobId, Guid AttemptId, bool IsCancellationRequested);

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

        public static async Task<JobServiceFixture> Start<TConsumer>(
            TConsumer consumer,
            Action<JobOptions<InMemoryJob>>? configureJob = null,
            Action<JobSagaOptions>? configureSaga = null,
            bool useScopedFilter = false)
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
                configuration.SetJobConsumerOptions(options => options.HeartbeatInterval = TimeSpan.FromSeconds(10))
                    .Endpoint(endpoint => endpoint.PrefetchCount = 100);
                if (useScopedFilter)
                {
                    configuration.AddConfigureEndpointsCallback((context, _, endpoint) =>
                        endpoint.UsePublishFilter(typeof(ScopedJobPublishFilter<>), context));
                }

                configuration.UsingInMemory((context, bus) =>
                {
                    bus.UseDelayedMessageScheduler();
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
                ITestHarness harness = await provider.StartTestHarness()
                    .WaitAsync(timeout, TestContext.Current.CancellationToken);
                return new JobServiceFixture(provider, harness, recorder);
            }
            catch
            {
                await provider.DisposeAsync();
                throw;
            }
        }

        public Task<Guid> Submit(Guid jobId, InMemoryJob job)
        {
            IRequestClient<SubmitJob<InMemoryJob>> client = Harness.GetRequestClient<SubmitJob<InMemoryJob>>();
            return client.SubmitJob(jobId, job, cancellationToken: CancellationToken)
                .WaitAsync(OperationTimeout, CancellationToken);
        }

        public Task<Guid> AddOrUpdateRecurring(
            string jobName,
            InMemoryJob job,
            Action<IRecurringJobScheduleConfigurator> configure)
        {
            IRequestClient<SubmitJob<InMemoryJob>> client = Harness.GetRequestClient<SubmitJob<InMemoryJob>>();
            return client.AddOrUpdateRecurringJob(jobName, job, configure, CancellationToken)
                .WaitAsync(OperationTimeout, CancellationToken);
        }

        public Task<Guid> Schedule(DateTimeOffset start, InMemoryJob job)
        {
            IRequestClient<SubmitJob<InMemoryJob>> client = Harness.GetRequestClient<SubmitJob<InMemoryJob>>();
            return client.ScheduleJob(start, job, CancellationToken)
                .WaitAsync(OperationTimeout, CancellationToken);
        }

        public Task<Guid> CancelRecurring(string jobName, string reason) =>
            Harness.Bus.CancelRecurringJob<InMemoryJob>(jobName, reason, CancellationToken)
                .WaitAsync(OperationTimeout, CancellationToken);

        public Task RunRecurring(string jobName) =>
            Harness.Bus.RunRecurringJob<InMemoryJob>(jobName)
                .WaitAsync(OperationTimeout, CancellationToken);

        public async Task<TMessage> Published<TMessage>(Func<TMessage, bool> predicate)
            where TMessage : class
        {
            IPublishedMessage<TMessage> published = await Harness.Published
                .SelectAsync<TMessage>(message => predicate(message.Context.Message), CancellationToken)
                .First()
                .WaitAsync(OperationTimeout, CancellationToken);
            return published.Context.Message;
        }

        public async Task<TMessage> Sent<TMessage>(Func<TMessage, bool> predicate)
            where TMessage : class
        {
            ISentMessage<TMessage> sent = await Harness.Sent
                .SelectAsync<TMessage>(message => predicate(message.Context.Message), CancellationToken)
                .First()
                .WaitAsync(OperationTimeout, CancellationToken);
            return sent.Context.Message;
        }

        public async Task ConsumedCount<TMessage>(Func<TMessage, bool> predicate, int expectedCount)
            where TMessage : class
        {
            int count = await Harness.Consumed
                .SelectAsync<TMessage>(message => predicate(message.Context.Message), CancellationToken)
                .Take(expectedCount)
                .Count()
                .WaitAsync(OperationTimeout, CancellationToken);
            Assert.Equal(expectedCount, count);
        }

        public async Task PublishedCount<TMessage>(Func<TMessage, bool> predicate, int expectedCount)
            where TMessage : class
        {
            int count = await Harness.Published
                .SelectAsync<TMessage>(message => predicate(message.Context.Message), CancellationToken)
                .Take(expectedCount)
                .Count()
                .WaitAsync(OperationTimeout, CancellationToken);
            Assert.Equal(expectedCount, count);
        }

        public Task<JobState> GetState(Guid jobId)
        {
            IRequestClient<GetJobState> client = Harness.GetRequestClient<GetJobState>();
            return client.GetJobState(jobId)
                .WaitAsync(OperationTimeout, CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Harness.Stop(CancellationToken.None)
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
        public async Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
        {
            recorder.Record(typeof(T), JobId(context.Message), scope.Id);
            await next.Send(context);
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
