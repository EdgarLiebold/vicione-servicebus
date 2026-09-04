using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

public sealed class PostgreSqlJobServiceTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0070", "postgresql-native-owner")]
    public async Task CancelJob_CancelsTheRunningConsumerAndPublishesTheReason()
    {
        var consumer = new BlockingJobConsumer(completeOnRetry: false);
        await using JobServiceFixture fixture = await JobServiceFixture.Start("job-cancel", consumer);
        Guid jobId = NewId.NextGuid();

        Guid accepted = await fixture.Submit(jobId, new PostgreSqlJob("cancel"));
        JobExecutionSnapshot attempt = await consumer.NextAttempt(fixture);
        await fixture.Harness.Bus.CancelJob(jobId, "operator-requested")
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        JobCancellationSnapshot cancellation = await consumer.NextCancellation(fixture);
        JobCanceled canceled = await fixture.Published<JobCanceled>(message => message.JobId == jobId);
        JobSlotReleased released = await fixture.Sent<JobSlotReleased>(message => message.JobId == jobId);

        Assert.Equal(jobId, accepted);
        Assert.Equal(new JobExecutionSnapshot(jobId, attempt.AttemptId, 0, "cancel"), attempt);
        Assert.Equal(new JobCancellationSnapshot(jobId, attempt.AttemptId, true), cancellation);
        Assert.Equal(jobId, canceled.JobId);
        Assert.Equal("operator-requested", canceled.Reason);
        Assert.Equal(jobId, released.JobId);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0072", "postgresql-native-owner")]
    public async Task CancelJob_UpdatesStartedStatusToCanceledWithReason()
    {
        var consumer = new BlockingJobConsumer(completeOnRetry: false);
        await using JobServiceFixture fixture = await JobServiceFixture.Start("job-status", consumer);
        Guid jobId = NewId.NextGuid();

        await fixture.Submit(jobId, new PostgreSqlJob("status"));
        JobExecutionSnapshot attempt = await consumer.NextAttempt(fixture);
        JobState started = await fixture.GetState(jobId);
        await fixture.Harness.Bus.CancelJob(jobId, "status-canceled")
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        await consumer.NextCancellation(fixture);
        await fixture.Published<JobCanceled>(message => message.JobId == jobId);
        JobState canceled = await fixture.GetState(jobId);

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
    [RequirementCoverage("OBL-R0-SQL-0074", "postgresql-native-owner")]
    public async Task RetryJob_AfterCancellationUsesANewAttemptAndCompletes()
    {
        var consumer = new BlockingJobConsumer(completeOnRetry: true);
        await using JobServiceFixture fixture = await JobServiceFixture.Start("job-retry", consumer);
        Guid jobId = NewId.NextGuid();

        await fixture.Submit(jobId, new PostgreSqlJob("retry"));
        JobExecutionSnapshot first = await consumer.NextAttempt(fixture);
        await fixture.Harness.Bus.CancelJob(jobId, "retry-requested")
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        await consumer.NextCancellation(fixture);
        await fixture.Published<JobCanceled>(message => message.JobId == jobId);

        await fixture.Harness.Bus.RetryJob(jobId)
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        JobExecutionSnapshot second = await consumer.NextAttempt(fixture);
        JobCompleted completed = await fixture.Published<JobCompleted>(message => message.JobId == jobId);
        JobCompleted<PostgreSqlJob> typedCompleted = await fixture.Published<JobCompleted<PostgreSqlJob>>(
            message => message.JobId == jobId);

        Assert.Equal(0, first.RetryAttempt);
        Assert.Equal(1, second.RetryAttempt);
        Assert.NotEqual(first.AttemptId, second.AttemptId);
        Assert.Equal(jobId, completed.JobId);
        Assert.Equal(jobId, typedCompleted.JobId);
        Assert.Equal("retry", typedCompleted.Job.Label);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0076", "postgresql-native-owner")]
    public async Task CancelJob_WhileWaitingPublishesTheWaitAndCanceledTransitions()
    {
        var consumer = new BlockingJobConsumer(completeOnRetry: false);
        await using JobServiceFixture fixture = await JobServiceFixture.Start(
            "job-waiting",
            consumer,
            options => options.SetConcurrentJobLimit(1),
            options => options.SlotWaitTime = TimeSpan.FromMinutes(2));
        Guid runningJobId = NewId.NextGuid();
        Guid waitingJobId = NewId.NextGuid();

        await fixture.Submit(runningJobId, new PostgreSqlJob("running"));
        JobExecutionSnapshot running = await consumer.NextAttempt(fixture);
        await fixture.Submit(waitingJobId, new PostgreSqlJob("waiting"));
        JobSlotWaitElapsed waited = await fixture.Sent<JobSlotWaitElapsed>(message => message.JobId == waitingJobId);
        JobState waitingBeforeCancel = await fixture.GetState(waitingJobId);
        Assert.Equal("WaitingForSlot", waitingBeforeCancel.CurrentState);
        Assert.Null(waitingBeforeCancel.Started);

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
    [RequirementCoverage("OBL-R0-SQL-0078", "postgresql-native-owner")]
    public async Task SubmitJob_CompletesTheAcceptedLifecycle()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.Start("job-complete", consumer);
        Guid jobId = NewId.NextGuid();

        Guid accepted = await fixture.Submit(jobId, new PostgreSqlJob("complete"));
        JobExecutionSnapshot execution = await consumer.NextAttempt(fixture);
        JobSubmitted submitted = await fixture.Published<JobSubmitted>(message => message.JobId == jobId);
        JobStarted started = await fixture.Published<JobStarted>(message => message.JobId == jobId);
        JobCompleted completed = await fixture.Published<JobCompleted>(message => message.JobId == jobId);
        JobState state = await fixture.GetState(jobId);

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
    [RequirementCoverage("OBL-R0-SQL-0080", "postgresql-native-owner")]
    public async Task PublishJob_GeneratesOneNonEmptyIdentityAcrossTheLifecycle()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.Start("job-generated-id", consumer);

        await fixture.Harness.Bus.Publish(new PostgreSqlJob("generated"), fixture.CancellationToken)
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        JobExecutionSnapshot execution = await consumer.NextAttempt(fixture);
        JobSubmitted submitted = await fixture.Published<JobSubmitted>(message => message.JobId == execution.JobId);
        JobCompleted completed = await fixture.Published<JobCompleted>(message => message.JobId == execution.JobId);
        JobCompleted<PostgreSqlJob> typedCompleted = await fixture.Published<JobCompleted<PostgreSqlJob>>(
            message => message.JobId == execution.JobId);

        Assert.NotEqual(Guid.Empty, execution.JobId);
        Assert.Equal(execution.JobId, submitted.JobId);
        Assert.Equal(execution.JobId, completed.JobId);
        Assert.Equal(execution.JobId, typedCompleted.JobId);
        Assert.Equal("generated", typedCompleted.Job.Label);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0082", "postgresql-native-owner")]
    public async Task GetJobState_ForUnknownIdentityReturnsNotFound()
    {
        var consumer = new CompletingJobConsumer();
        await using JobServiceFixture fixture = await JobServiceFixture.Start("job-not-found", consumer);
        Guid missingJobId = NewId.NextGuid();

        JobState state = await fixture.GetState(missingJobId);

        Assert.Equal(missingJobId, state.JobId);
        Assert.Equal("NotFound", state.CurrentState);
        Assert.Null(state.Submitted);
        Assert.Null(state.Started);
        Assert.Null(state.Completed);
        Assert.Null(state.Faulted);
    }

    public sealed record PostgreSqlJob(string Label);

    private sealed class CompletingJobConsumer : IJobConsumer<PostgreSqlJob>
    {
        private readonly Channel<JobExecutionSnapshot> _attempts = Channel.CreateUnbounded<JobExecutionSnapshot>();

        public Task Run(JobContext<PostgreSqlJob> context) =>
            _attempts.Writer.WriteAsync(Snapshot(context), context.CancellationToken).AsTask();

        public Task<JobExecutionSnapshot> NextAttempt(JobServiceFixture fixture) =>
            _attempts.Reader.ReadAsync(fixture.CancellationToken).AsTask()
                .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
    }

    private sealed class BlockingJobConsumer(bool completeOnRetry) : IJobConsumer<PostgreSqlJob>
    {
        private readonly Channel<JobExecutionSnapshot> _attempts = Channel.CreateUnbounded<JobExecutionSnapshot>();
        private readonly Channel<JobCancellationSnapshot> _cancellations = Channel.CreateUnbounded<JobCancellationSnapshot>();
        private readonly TaskCompletionSource _never = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task Run(JobContext<PostgreSqlJob> context)
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
                    new JobCancellationSnapshot(
                        context.JobId,
                        context.AttemptId,
                        context.CancellationToken.IsCancellationRequested),
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

    private static JobExecutionSnapshot Snapshot(JobContext<PostgreSqlJob> context) =>
        new(context.JobId, context.AttemptId, context.RetryAttempt, context.Job.Label);

    private sealed class JobServiceFixture : IAsyncDisposable
    {
        private readonly PostgreSqlTestDatabase _database;
        private readonly ServiceProvider _provider;

        private JobServiceFixture(
            PostgreSqlTestDatabase database,
            ServiceProvider provider,
            ITestHarness harness)
        {
            _database = database;
            _provider = provider;
            Harness = harness;
        }

        public CancellationToken CancellationToken => TestContext.Current.CancellationToken;
        public ITestHarness Harness { get; }
        public TimeSpan OperationTimeout => _database.OperationTimeout;

        public static async Task<JobServiceFixture> Start<TConsumer>(
            string purpose,
            TConsumer consumer,
            Action<JobOptions<PostgreSqlJob>>? configureJob = null,
            Action<JobSagaOptions>? configureSaga = null)
            where TConsumer : class, IJobConsumer<PostgreSqlJob>
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(purpose, cancellationToken);
            try
            {
                await using (var schema = CreateDbContext(database.ConnectionString))
                {
                    await schema.Database.ExecuteSqlRawAsync(
                        schema.Database.GenerateCreateScript(),
                        cancellationToken);
                }

                var services = new ServiceCollection();
                services.AddSingleton(consumer);
                services.AddDbContext<JobServiceSagaDbContext>(options => options.UseNpgsql(database.ConnectionString));
                services.AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
                {
                    configuration.SetTestTimeouts(database.OperationTimeout, database.OperationTimeout);
                    configuration.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(database.Prefix));
                    configuration.AddConsumer<TConsumer>(registration =>
                    {
                        if (configureJob is not null)
                            registration.Options<JobOptions<PostgreSqlJob>>(configureJob);
                    });
                    configuration.AddJobSagaStateMachines(options =>
                    {
                        options.FinalizeCompleted = false;
                        configureSaga?.Invoke(options);
                    })
                        .SetPartitionedReceiveMode()
                        .EntityFrameworkRepository(repository =>
                        {
                            repository.ExistingDbContext<JobServiceSagaDbContext>();
                            repository.UsePostgres();
                        });
                    configuration.SetJobConsumerOptions(options => options.HeartbeatInterval = TimeSpan.FromSeconds(10))
                        .Endpoint(endpoint => endpoint.PrefetchCount = 100);
                    configuration.UsingPostgres(database.ConnectionString, (context, bus) =>
                    {
                        bus.UseSqlMessageScheduler();
                        bus.UseJobSagaPartitionKeyFormatters();
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
                        .WaitAsync(database.OperationTimeout, cancellationToken);
                    return new JobServiceFixture(database, provider, harness);
                }
                catch
                {
                    await provider.DisposeAsync();
                    throw;
                }
            }
            catch
            {
                await database.DisposeAsync();
                throw;
            }
        }

        public Task<Guid> Submit(Guid jobId, PostgreSqlJob job)
        {
            IRequestClient<SubmitJob<PostgreSqlJob>> client = Harness.GetRequestClient<SubmitJob<PostgreSqlJob>>();
            return client.SubmitJob(jobId, job, cancellationToken: CancellationToken)
                .WaitAsync(OperationTimeout, CancellationToken);
        }

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

        public Task<JobState> GetState(Guid jobId)
        {
            IRequestClient<GetJobState> client = Harness.GetRequestClient<GetJobState>();
            return client.GetJobState(jobId).WaitAsync(OperationTimeout, CancellationToken);
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
                await _database.DisposeAsync();
            }
        }

        private static JobServiceSagaDbContext CreateDbContext(string connectionString) =>
            new(new DbContextOptionsBuilder<JobServiceSagaDbContext>()
                .UseNpgsql(connectionString)
                .Options);
    }
}
