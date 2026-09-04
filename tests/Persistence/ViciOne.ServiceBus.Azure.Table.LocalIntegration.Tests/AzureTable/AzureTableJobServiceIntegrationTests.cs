using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.AzureTable.Saga;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.AzureTable;

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
        JobStarted started = await fixture.PublishedAsync<JobStarted>(jobId, message => message.JobId);
        JobCompleted completed = await fixture.PublishedAsync<JobCompleted>(jobId, message => message.JobId);
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
        JobSubmitted submitted = await fixture.PublishedAsync<JobSubmitted>(jobId, message => message.JobId);
        JobStarted started = await fixture.PublishedAsync<JobStarted>(jobId, message => message.JobId);
        JobCompleted completed = await fixture.PublishedAsync<JobCompleted>(jobId, message => message.JobId);
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
        JobSubmitted submitted = await fixture.PublishedAsync<JobSubmitted>(jobId, message => message.JobId);
        JobStarted started = await fixture.PublishedAsync<JobStarted>(jobId, message => message.JobId);
        JobCanceled canceled = await fixture.PublishedAsync<JobCanceled>(jobId, message => message.JobId);
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
        JobSubmitted submitted = await fixture.PublishedAsync<JobSubmitted>(jobId, message => message.JobId);
        JobStarted started = await fixture.PublishedAsync<JobStarted>(jobId, message => message.JobId);
        JobFaulted faulted = await fixture.PublishedAsync<JobFaulted>(jobId, message => message.JobId);
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

    public sealed record PersistentJob(string Label);

    private sealed class CompletingJobConsumer : IJobConsumer<PersistentJob>
    {
        private readonly TaskCompletionSource<JobExecutionSnapshot> _completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<JobExecutionSnapshot> Completed => _completed.Task;

        public Task RunAsync(JobContext<PersistentJob> context)
        {
            _completed.TrySetResult(Snapshot(context));
            return Task.CompletedTask;
        }
    }

    private sealed class TimedJobConsumer(FakeTimeProvider timeProvider, TimeSpan executionTime) : IJobConsumer<PersistentJob>
    {
        public Task RunAsync(JobContext<PersistentJob> context)
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

        public async Task RunAsync(JobContext<PersistentJob> context)
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

        public Task RunAsync(JobContext<PersistentJob> context)
        {
            _attempted.TrySetResult(Snapshot(context));
            return Task.FromException(new ExpectedJobFailure());
        }
    }

    private sealed class ExpectedJobFailure : Exception
    {
        public const string FailureMessage = "The persistent job failed as requested.";

        public ExpectedJobFailure() : base(FailureMessage)
        {
        }
    }

    private sealed record JobExecutionSnapshot(Guid JobId, Guid AttemptId, int RetryAttempt, string Label);

    private static JobExecutionSnapshot Snapshot(JobContext<PersistentJob> context) =>
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
            TimeProvider? timeProvider = null)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            AzureTableTestTable table = await AzureTableTestTable.CreateAsync(purpose, cancellationToken);
            try
            {
                var services = new ServiceCollection();
                services.AddSingleton(consumer);
                services.AddViciOneServiceBusTestHarness(configuration =>
                {
                    configuration.SetKebabCaseEndpointNameFormatter();
                    configuration.AddConsumer<TConsumer>()
                        .Endpoint(endpoint => endpoint.Name = $"persistent-job-{NewId.NextGuid():N}");
                    configuration.SetJobConsumerOptions(options => options.HeartbeatInterval = TimeSpan.FromSeconds(10));
                    configuration.AddJobSagaStateMachines(options => options.FinalizeCompleted = false)
                        .AzureTableRepository(repository => repository.TableClientFactory(() => table.Table));
                    configuration.UsingInMemory((context, bus) =>
                    {
                        if (timeProvider is not null)
                            bus.UseExecute(consumeContext => consumeContext.SetTimeProvider(timeProvider));

                        bus.UseDelayedMessageScheduler();
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
            IRequestClient<SubmitJob<PersistentJob>> client = Harness.GetRequestClient<SubmitJob<PersistentJob>>();
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
            var repository = (ILoadSagaRepository<JobSaga>)AzureTableSagaRepository<JobSaga>
                .Create(() => _table.Table);
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
