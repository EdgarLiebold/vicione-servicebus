using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests;

public sealed class ActiveMqJobServiceTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-BRK-0426", "running-job-cancel-reaches-consumer-and-terminal-event")]
    public async Task CancelJob_CancelsTheRunningConsumerAndPublishesTheReasonAsync()
    {
        var consumer = new BlockingJobConsumer(completeOnRetry: false);
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync("job-cancel", consumer);
        Guid jobId = NewId.NextGuid();

        Guid accepted = await fixture.SubmitAsync(jobId, new ActiveMqJob("cancel"));
        JobExecutionSnapshot attempt = await consumer.NextAttemptAsync(fixture);
        await fixture.Harness.Bus.CancelJobAsync(jobId, "operator-requested", cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        JobCancellationSnapshot cancellation = await consumer.NextCancellationAsync(fixture);
        JobCanceled canceled = await fixture.PublishedAsync<JobCanceled>(message => message.JobId == jobId);
        JobSlotReleased released = await fixture.SentAsync<JobSlotReleased>(message => message.JobId == jobId);

        Assert.Equal(jobId, accepted);
        Assert.Equal(new JobExecutionSnapshot(jobId, attempt.AttemptId, 0, "cancel"), attempt);
        Assert.Equal(new JobCancellationSnapshot(jobId, attempt.AttemptId, true), cancellation);
        Assert.Equal(jobId, canceled.JobId);
        Assert.Equal("operator-requested", canceled.Reason);
        Assert.Equal(jobId, released.JobId);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-BRK-0427", "cancel-updates-started-status-to-canceled-with-reason")]
    public async Task CancelJob_UpdatesStartedStatusToCanceledWithReasonAsync()
    {
        var consumer = new BlockingJobConsumer(completeOnRetry: false);
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync("job-status", consumer);
        Guid jobId = NewId.NextGuid();

        await fixture.SubmitAsync(jobId, new ActiveMqJob("status"));
        JobExecutionSnapshot attempt = await consumer.NextAttemptAsync(fixture);
        JobState started = await fixture.GetStateAsync(jobId);
        await fixture.Harness.Bus.CancelJobAsync(jobId, "status-canceled", cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        await consumer.NextCancellationAsync(fixture);
        await fixture.PublishedAsync<JobCanceled>(message => message.JobId == jobId);
        JobState canceled = await fixture.GetStateAsync(jobId);

        Assert.Equal("Started", started.CurrentState);
        Assert.Equal(0, started.LastRetryAttempt);
        Assert.NotNull(started.Submitted);
        Assert.NotNull(started.Started);
        Assert.Null(started.Completed);
        Assert.Null(started.Faulted);
        Assert.Equal(jobId, attempt.JobId);
        Assert.Equal("Canceled", canceled.CurrentState);
        Assert.Equal("status-canceled", canceled.Reason);
        Assert.NotNull(canceled.Faulted);
        Assert.Null(canceled.Completed);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-BRK-0428", "canceled-job-retry-uses-new-attempt-and-completes")]
    public async Task RetryJob_AfterCancellationUsesANewAttemptAndCompletesAsync()
    {
        var consumer = new BlockingJobConsumer(completeOnRetry: true);
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync("job-retry", consumer);
        Guid jobId = NewId.NextGuid();

        await fixture.SubmitAsync(jobId, new ActiveMqJob("retry"));
        JobExecutionSnapshot first = await consumer.NextAttemptAsync(fixture);
        await fixture.Harness.Bus.CancelJobAsync(jobId, "retry-requested", cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        await consumer.NextCancellationAsync(fixture);
        await fixture.PublishedAsync<JobCanceled>(message => message.JobId == jobId);

        await fixture.Harness.Bus.RetryJobAsync(jobId, cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        JobExecutionSnapshot second = await consumer.NextAttemptAsync(fixture);
        JobCompleted completed = await fixture.PublishedAsync<JobCompleted>(message => message.JobId == jobId);
        JobCompleted<ActiveMqJob> typedCompleted = await fixture.PublishedAsync<JobCompleted<ActiveMqJob>>(
            message => message.JobId == jobId);

        Assert.Equal(0, first.RetryAttempt);
        Assert.Equal(1, second.RetryAttempt);
        Assert.NotEqual(first.AttemptId, second.AttemptId);
        Assert.Equal(jobId, completed.JobId);
        Assert.Equal(jobId, typedCompleted.JobId);
        Assert.Equal("retry", typedCompleted.Job.Label);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-BRK-0429", "waiting-job-can-be-canceled-without-consuming-a-slot")]
    public async Task CancelJob_WhileWaitingPublishesTheWaitAndCanceledTransitionsAsync()
    {
        var consumer = new BlockingJobConsumer(completeOnRetry: false);
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync(
            "job-waiting",
            consumer,
            options => options.SetConcurrentJobLimit(1),
            options => options.SlotWaitTime = TimeSpan.FromSeconds(1));
        Guid runningJobId = NewId.NextGuid();
        Guid waitingJobId = NewId.NextGuid();

        await fixture.SubmitAsync(runningJobId, new ActiveMqJob("running"));
        JobExecutionSnapshot running = await consumer.NextAttemptAsync(fixture);
        await fixture.SubmitAsync(waitingJobId, new ActiveMqJob("waiting"));
        JobSlotWaitElapsed waited = await fixture.SentAsync<JobSlotWaitElapsed>(message => message.JobId == waitingJobId);

        await fixture.Harness.Bus.CancelJobAsync(waitingJobId, "waiting-canceled", cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        JobCanceled waitingCanceled = await fixture.PublishedAsync<JobCanceled>(message => message.JobId == waitingJobId);
        JobState waitingState = await fixture.GetStateAsync(waitingJobId);

        Assert.Equal(runningJobId, running.JobId);
        Assert.Equal(waitingJobId, waited.JobId);
        Assert.Equal(waitingJobId, waitingCanceled.JobId);
        Assert.Equal("waiting-canceled", waitingCanceled.Reason);
        Assert.Equal("Canceled", waitingState.CurrentState);
        Assert.Null(waitingState.Started);

        await fixture.Harness.Bus.CancelJobAsync(runningJobId, "fixture-cleanup", cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        await consumer.NextCancellationAsync(fixture);
        await fixture.PublishedAsync<JobCanceled>(message => message.JobId == runningJobId);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-BRK-0430", "accepted-job-publishes-submitted-started-and-completed")]
    public async Task SubmitJob_CompletesTheAcceptedLifecycleExactlyOnceAsync()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync("job-complete", consumer);
        Guid jobId = NewId.NextGuid();

        Guid accepted = await fixture.SubmitAsync(jobId, new ActiveMqJob("complete"));
        JobExecutionSnapshot execution = await consumer.NextAttemptAsync(fixture);
        JobSubmitted submitted = await fixture.PublishedAsync<JobSubmitted>(message => message.JobId == jobId);
        JobStarted started = await fixture.PublishedAsync<JobStarted>(message => message.JobId == jobId);
        JobCompleted completed = await fixture.PublishedAsync<JobCompleted>(message => message.JobId == jobId);
        JobState state = await fixture.GetStateAsync(jobId);

        Assert.Equal(jobId, accepted);
        Assert.Equal(jobId, submitted.JobId);
        Assert.NotEqual(Guid.Empty, submitted.JobTypeId);
        Assert.Equal(new JobExecutionSnapshot(jobId, started.AttemptId, 0, "complete"), execution);
        Assert.Equal(jobId, completed.JobId);
        Assert.Equal("Completed", state.CurrentState);
        Assert.NotNull(state.Completed);
        Assert.Null(state.Faulted);
        Assert.True(submitted.Timestamp <= started.Timestamp);
        Assert.True(started.Timestamp <= completed.Timestamp);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-BRK-0431", "published-job-receives-one-nonempty-generated-identity")]
    public async Task PublishJob_GeneratesOneNonEmptyIdentityAcrossTheLifecycleAsync()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync("job-generated-id", consumer);

        await fixture.Harness.Bus.PublishAsync(new ActiveMqJob("generated"), fixture.CancellationToken)
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        JobExecutionSnapshot execution = await consumer.NextAttemptAsync(fixture);
        JobSubmitted submitted = await fixture.PublishedAsync<JobSubmitted>(message => message.JobId == execution.JobId);
        JobCompleted completed = await fixture.PublishedAsync<JobCompleted>(message => message.JobId == execution.JobId);
        JobCompleted<ActiveMqJob> typedCompleted = await fixture.PublishedAsync<JobCompleted<ActiveMqJob>>(
            message => message.JobId == execution.JobId);

        Assert.NotEqual(Guid.Empty, execution.JobId);
        Assert.Equal(execution.JobId, submitted.JobId);
        Assert.Equal(execution.JobId, completed.JobId);
        Assert.Equal(execution.JobId, typedCompleted.JobId);
        Assert.Equal("generated", typedCompleted.Job.Label);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-BRK-0432", "unknown-job-state-is-explicitly-not-found")]
    public async Task GetJobState_ForUnknownIdentityReturnsNotFoundAsync()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.StartAsync("job-not-found", consumer);
        Guid missingJobId = NewId.NextGuid();

        JobState state = await fixture.GetStateAsync(missingJobId);

        Assert.Equal(missingJobId, state.JobId);
        Assert.Equal("NotFound", state.CurrentState);
        Assert.Null(state.Submitted);
        Assert.Null(state.Started);
        Assert.Null(state.Completed);
        Assert.Null(state.Faulted);
    }

    public sealed record ActiveMqJob(string Label);

    private sealed class CompletingJobConsumer : IJobConsumer<ActiveMqJob>
    {
        private readonly Channel<JobExecutionSnapshot> _attempts = Channel.CreateUnbounded<JobExecutionSnapshot>();

        public Task RunAsync(JobContext<ActiveMqJob> context) =>
            _attempts.Writer.WriteAsync(Snapshot(context), context.CancellationToken).AsTask();

        public Task<JobExecutionSnapshot> NextAttemptAsync(JobServiceFixture fixture) =>
            _attempts.Reader.ReadAsync(fixture.CancellationToken).AsTask()
                .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
    }

    private sealed class BlockingJobConsumer(bool completeOnRetry) : IJobConsumer<ActiveMqJob>
    {
        private readonly Channel<JobExecutionSnapshot> _attempts = Channel.CreateUnbounded<JobExecutionSnapshot>();
        private readonly Channel<JobCancellationSnapshot> _cancellations = Channel.CreateUnbounded<JobCancellationSnapshot>();
        private readonly TaskCompletionSource _never = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task RunAsync(JobContext<ActiveMqJob> context)
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

    private sealed record JobExecutionSnapshot(Guid JobId, Guid AttemptId, int RetryAttempt, string Label);
    private sealed record JobCancellationSnapshot(Guid JobId, Guid AttemptId, bool IsCancellationRequested);

    private static JobExecutionSnapshot Snapshot(JobContext<ActiveMqJob> context) =>
        new(context.JobId, context.AttemptId, context.RetryAttempt, context.Job.Label);

    private sealed class JobServiceFixture : IAsyncDisposable
    {
        private readonly ActiveMqBroker _broker;
        private readonly ServiceProvider _provider;

        private JobServiceFixture(ActiveMqBroker broker, ServiceProvider provider, ITestHarness harness)
        {
            _broker = broker;
            _provider = provider;
            Harness = harness;
        }

        public CancellationToken CancellationToken => TestContext.Current.CancellationToken;
        public ITestHarness Harness { get; }
        public TimeSpan OperationTimeout => _broker.OperationTimeout;

        public static async Task<JobServiceFixture> StartAsync<TConsumer>(
            string purpose,
            TConsumer consumer,
            Action<JobOptions<ActiveMqJob>>? configureJob = null,
            Action<JobSagaOptions>? configureSaga = null)
            where TConsumer : class, IJobConsumer<ActiveMqJob>
        {
            var broker = ActiveMqBroker.Create(ActiveMqBroker.OpenWireFlavor, purpose);
            try
            {
                var services = new ServiceCollection();
                services.AddSingleton(consumer);
                services.AddViciOneServiceBusTestHarness(configuration =>
                {
                    configuration.SetTestTimeouts(testInactivityTimeout: broker.OperationTimeout);
                    configuration.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(broker.Prefix));
                    configuration.AddConsumer<TConsumer>(registration =>
                    {
                        if (configureJob is not null)
                            registration.Options<JobOptions<ActiveMqJob>>(configureJob);
                    });
                    configuration.AddJobSagaStateMachines(options =>
                    {
                        options.FinalizeCompleted = false;
                        configureSaga?.Invoke(options);
                    });
                    configuration.SetJobConsumerOptions(options => options.HeartbeatInterval = TimeSpan.FromSeconds(10))
                        .Endpoint(endpoint => endpoint.PrefetchCount = 100);
                    configuration.UsingActiveMq((context, bus) =>
                    {
                        broker.ConfigureHost(bus);
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
                    ITestHarness harness = await provider.StartTestHarnessAsync()
                        .WaitAsync(broker.OperationTimeout, TestContext.Current.CancellationToken);
                    return new JobServiceFixture(broker, provider, harness);
                }
                catch
                {
                    await provider.DisposeAsync();
                    throw;
                }
            }
            catch
            {
                broker.Dispose();
                throw;
            }
        }

        public Task<Guid> SubmitAsync(Guid jobId, ActiveMqJob job)
        {
            IRequestClient<SubmitJob<ActiveMqJob>> client = Harness.GetRequestClient<SubmitJob<ActiveMqJob>>();
            return client.SubmitJobAsync(jobId, job, cancellationToken: CancellationToken)
                .WaitAsync(OperationTimeout, CancellationToken);
        }

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

        public Task<JobState> GetStateAsync(Guid jobId)
        {
            IRequestClient<GetJobState> client = Harness.GetRequestClient<GetJobState>();
            return client.GetJobStateAsync(jobId)
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
                _broker.Dispose();
            }
        }
    }
}
