namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests.EntityFrameworkCoreIntegration;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class QuartzTransactionalOutboxTests
{
    private static readonly DateTime ScheduledTime = new(2100, 2, 3, 4, 5, 6, DateTimeKind.Utc);

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-QUARTZ", "schedule-command-is-committed-before-quartz-registration")]
    public async Task ScheduledPublish_ReachesQuartzOnlyAfterTheEntityFrameworkTransactionCommits()
    {
        await using QuartzOutboxFixture fixture = await QuartzOutboxFixture.CreateAsync();
        var command = new ScheduleThroughOutbox(NewId.NextGuid());
        var scheduleObserver = new ScheduleMessageObserver();
        using ConnectHandle observer = fixture.Harness.Bus.ConnectConsumeObserver(scheduleObserver);

        await fixture.Harness.Bus.Publish(command, fixture.CancellationToken);
        ScheduledMessage<ScheduledOutboxPayload> scheduled = await fixture.Gate.Scheduled
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        var triggerKey = new TriggerKey(scheduled.TokenId.ToString("N"));

        try
        {
            Assert.False(await fixture.Scheduler.CheckExists(triggerKey, fixture.CancellationToken)
                .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken));
            Assert.Equal(0, scheduleObserver.ObservedCount);
        }
        finally
        {
            fixture.Gate.Release();
        }

        IReceivedMessage<ScheduleThroughOutbox> consumed = await fixture.Harness.Consumed
            .SelectAsync<ScheduleThroughOutbox>(
                context => context.Context.Message.CorrelationId == command.CorrelationId,
                fixture.CancellationToken)
            .First()
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        ScheduleMessage observedSchedule = await scheduleObserver.Completed
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(
            await fixture.Scheduler.GetTrigger(triggerKey, fixture.CancellationToken)
                .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken));

        Assert.Null(consumed.Exception);
        Assert.Equal(1, scheduleObserver.ObservedCount);
        Assert.Equal(scheduled.TokenId, observedSchedule.CorrelationId);
        Assert.Equal(ScheduledTime, trigger.GetNextFireTimeUtc()?.UtcDateTime);

        await fixture.Scheduler.TriggerJob(trigger.JobKey, trigger.JobDataMap, fixture.CancellationToken)
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        ScheduledOutboxPayload delivered = await fixture.Deliveries.Delivered
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);

        Assert.Equal(command.CorrelationId, delivered.CorrelationId);
        Assert.Equal(1, fixture.Deliveries.DeliveryCount);
    }

    public sealed record ScheduleThroughOutbox(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record ScheduledOutboxPayload(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class QuartzOutboxDbContext(DbContextOptions<QuartzOutboxDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddTransactionalOutboxEntities();
    }

    public sealed class ScheduleThroughOutboxConsumer(QuartzOutboxCommitGate gate) : IConsumer<ScheduleThroughOutbox>
    {
        public async Task Consume(ConsumeContext<ScheduleThroughOutbox> context)
        {
            ScheduledMessage<ScheduledOutboxPayload> scheduled = await context.SchedulePublish(
                ScheduledTime,
                new ScheduledOutboxPayload(context.Message.CorrelationId),
                context.CancellationToken);
            gate.Record(scheduled);
            await gate.WaitForReleaseAsync(context.CancellationToken);
        }
    }

    public sealed class ScheduledOutboxPayloadConsumer(ScheduledOutboxDeliveryProbe deliveries)
        : IConsumer<ScheduledOutboxPayload>
    {
        public Task Consume(ConsumeContext<ScheduledOutboxPayload> context)
        {
            deliveries.Record(context.Message);
            return Task.CompletedTask;
        }
    }

    private sealed class ScheduleThroughOutboxConsumerDefinition : ConsumerDefinition<ScheduleThroughOutboxConsumer>
    {
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<ScheduleThroughOutboxConsumer> consumerConfigurator,
            IRegistrationContext context) =>
            endpointConfigurator.UseEntityFrameworkOutbox<QuartzOutboxDbContext>(context);
    }

    public sealed class QuartzOutboxCommitGate(TimeSpan operationTimeout)
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<ScheduledMessage<ScheduledOutboxPayload>> _scheduled =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<ScheduledMessage<ScheduledOutboxPayload>> Scheduled => _scheduled.Task;

        public void Record(ScheduledMessage<ScheduledOutboxPayload> scheduled)
        {
            if (!_scheduled.TrySetResult(scheduled))
                throw new InvalidOperationException("The outbox schedule was recorded more than once.");
        }

        public void Release() => _release.TrySetResult();

        public Task WaitForReleaseAsync(CancellationToken cancellationToken) =>
            _release.Task.WaitAsync(operationTimeout, cancellationToken);
    }

    public sealed class ScheduledOutboxDeliveryProbe
    {
        private readonly TaskCompletionSource<ScheduledOutboxPayload> _delivered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _deliveryCount;

        public Task<ScheduledOutboxPayload> Delivered => _delivered.Task;

        public int DeliveryCount => Volatile.Read(ref _deliveryCount);

        public void Record(ScheduledOutboxPayload message)
        {
            if (Interlocked.Increment(ref _deliveryCount) == 1)
                _delivered.TrySetResult(message);
        }
    }

    private sealed class ScheduleMessageObserver : IConsumeObserver
    {
        private readonly TaskCompletionSource<ScheduleMessage> _completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _observedCount;

        public Task<ScheduleMessage> Completed => _completed.Task;

        public int ObservedCount => Volatile.Read(ref _observedCount);

        public Task PreConsume<T>(ConsumeContext<T> context) where T : class => Task.CompletedTask;

        public Task PostConsume<T>(ConsumeContext<T> context) where T : class
        {
            if (context.Message is ScheduleMessage schedule && Interlocked.Increment(ref _observedCount) == 1)
                _completed.TrySetResult(schedule);

            return Task.CompletedTask;
        }

        public Task ConsumeFault<T>(ConsumeContext<T> context, Exception exception) where T : class
        {
            if (context.Message is ScheduleMessage)
                _completed.TrySetException(exception);

            return Task.CompletedTask;
        }
    }

    private sealed class QuartzOutboxFixture : IAsyncDisposable
    {
        private readonly PostgreSqlTestDatabase _database;

        private QuartzOutboxFixture(
            PostgreSqlTestDatabase database,
            ServiceProvider services,
            ITestHarness harness,
            IScheduler scheduler,
            QuartzOutboxCommitGate gate,
            ScheduledOutboxDeliveryProbe deliveries,
            TimeSpan operationTimeout,
            CancellationToken cancellationToken)
        {
            _database = database;
            Services = services;
            Harness = harness;
            Scheduler = scheduler;
            Gate = gate;
            Deliveries = deliveries;
            OperationTimeout = operationTimeout;
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }

        public ScheduledOutboxDeliveryProbe Deliveries { get; }

        public QuartzOutboxCommitGate Gate { get; }

        public ITestHarness Harness { get; }

        public TimeSpan OperationTimeout { get; }

        public IScheduler Scheduler { get; }

        public ServiceProvider Services { get; }

        public static async Task<QuartzOutboxFixture> CreateAsync()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
                .GetValidatedOptions()
                .OperationTimeout!.Value;
            PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
                "transactional-outbox-quartz",
                cancellationToken);
            await using (var setup = new QuartzOutboxDbContext(
                             new DbContextOptionsBuilder<QuartzOutboxDbContext>()
                                 .UseNpgsql(database.ConnectionString)
                                 .Options))
            {
                if (!await setup.Database.EnsureCreatedAsync(cancellationToken))
                    throw new InvalidOperationException("The Quartz outbox test database was not created.");
            }

            var gate = new QuartzOutboxCommitGate(operationTimeout);
            var deliveries = new ScheduledOutboxDeliveryProbe();
            var services = new ServiceCollection();
            services.AddSingleton(gate);
            services.AddSingleton(deliveries);
            services.AddQuartz();
            services.AddDbContext<QuartzOutboxDbContext>(builder => builder
                .UseNpgsql(database.ConnectionString, options => options.EnableRetryOnFailure()));
            services.AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
            {
                configuration.SetTestTimeouts(operationTimeout, operationTimeout);
                configuration.AddPublishMessageScheduler();
                configuration.AddQuartzConsumers();
                configuration.AddEntityFrameworkOutbox<QuartzOutboxDbContext>(outbox =>
                {
                    outbox.UsePostgres();
                    outbox.DisableInboxCleanupService();
                });
                configuration.AddConsumer<ScheduleThroughOutboxConsumer, ScheduleThroughOutboxConsumerDefinition>();
                configuration.AddConsumer<ScheduledOutboxPayloadConsumer>();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.UsePublishMessageScheduler();
                    bus.ConfigureEndpoints(context);
                });
            });

            ServiceProvider? provider = null;
            try
            {
                provider = services.BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true,
                });
                ITestHarness harness = await provider.StartTestHarness().WaitAsync(operationTimeout, cancellationToken);
                IScheduler scheduler = await provider.GetRequiredService<ISchedulerFactory>()
                    .GetScheduler(cancellationToken)
                    .WaitAsync(operationTimeout, cancellationToken);
                return new QuartzOutboxFixture(
                    database,
                    provider,
                    harness,
                    scheduler,
                    gate,
                    deliveries,
                    operationTimeout,
                    cancellationToken);
            }
            catch
            {
                if (provider is not null)
                    await provider.DisposeAsync();
                await database.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            Gate.Release();
            await Harness.Stop(CancellationToken.None).WaitAsync(OperationTimeout, CancellationToken.None);
            await Services.DisposeAsync();
            await _database.DisposeAsync();
        }
    }
}
