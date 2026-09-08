using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.EntityFrameworkCoreIntegration;

public sealed class QuartzTransactionalOutboxTests
{
    private static readonly DateTimeOffset DueAt = new(2100, 2, 3, 4, 5, 6, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-QUARTZ", "schedule-command-is-committed-before-quartz-registration")]
    public async Task ScheduledPublish_ReachesQuartzOnlyAfterTheEntityFrameworkTransactionCommitsAsync()
    {
        await using QuartzOutboxFixture fixture = await QuartzOutboxFixture.CreateAsync();
        var command = new ScheduleThroughOutbox(NewId.NextGuid());
        var scheduleObserver = new ScheduleMessageObserver();
        using ConnectHandle sendObserver = fixture.Harness.Bus.ConnectSendObserver(scheduleObserver);
        using ConnectHandle publishObserver = fixture.Harness.Bus.ConnectPublishObserver(scheduleObserver);

        await fixture.Harness.Bus.PublishAsync(command, fixture.CancellationToken);
        ScheduledMessage<ScheduledOutboxPayload> scheduled = await fixture.Gate.Scheduled
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        try
        {
            IReadOnlyCollection<TriggerKey> uncommittedTriggers = await fixture.Scheduler
                .GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup(), fixture.CancellationToken).AsTask()
                .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
            Assert.DoesNotContain(uncommittedTriggers, key => key.Name == scheduled.TokenId.ToString("N"));
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
            .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        Guid? observedScheduleCorrelationId = await scheduleObserver.Completed
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        IReadOnlyCollection<TriggerKey> committedTriggers = await fixture.Scheduler
            .GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup(), fixture.CancellationToken).AsTask()
            .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        TriggerKey triggerKey = Assert.Single(
            committedTriggers,
            key => key.Name == scheduled.TokenId.ToString("N"));
        ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(
            await fixture.Scheduler.GetTrigger(triggerKey, fixture.CancellationToken).AsTask()
                .WaitAsync(fixture.OperationTimeout, fixture.CancellationToken));

        Assert.Null(consumed.Exception);
        Assert.Equal(1, scheduleObserver.ObservedCount);
        Assert.Equal(scheduled.TokenId, observedScheduleCorrelationId);
        Assert.Equal(DueAt, trigger.NextFireTimeUtc);

        await fixture.Scheduler.TriggerJob(trigger.JobKey, trigger.JobDataMap, fixture.CancellationToken).AsTask()
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
        public async Task ConsumeAsync(ConsumeContext<ScheduleThroughOutbox> context)
        {
            ScheduledMessage<ScheduledOutboxPayload> scheduled = await context.Advanced().SchedulePublishAsync(
                DueAt,
                new ScheduledOutboxPayload(context.Message.CorrelationId),
                context.CancellationToken);
            gate.Record(scheduled);
            await gate.WaitForReleaseAsync(context.CancellationToken);
        }
    }

    public sealed class ScheduledOutboxPayloadConsumer(ScheduledOutboxDeliveryProbe deliveries)
        : IConsumer<ScheduledOutboxPayload>
    {
        public Task ConsumeAsync(ConsumeContext<ScheduledOutboxPayload> context)
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

    private sealed class ScheduleMessageObserver : ISendObserver, IPublishObserver
    {
        private readonly TaskCompletionSource<Guid?> _completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _observedCount;

        public Task<Guid?> Completed => _completed.Task;

        public int ObservedCount => Volatile.Read(ref _observedCount);

        public Task PreSendAsync<T>(SendContext<T> context) where T : class
        {
            if (context.Message is ScheduleMessage or SerializedMessageBody)
                Interlocked.Increment(ref _observedCount);

            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context) where T : class
        {
            if (context.Message is ScheduleMessage or SerializedMessageBody)
                _completed.TrySetResult(context.CorrelationId);

            return Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception) where T : class
        {
            if (context.Message is ScheduleMessage or SerializedMessageBody)
                _completed.TrySetException(exception);

            return Task.CompletedTask;
        }

        public Task PrePublishAsync<T>(PublishContext<T> context) where T : class
        {
            if (context.Message is ScheduleMessage)
                Interlocked.Increment(ref _observedCount);

            return Task.CompletedTask;
        }

        public Task PostPublishAsync<T>(PublishContext<T> context) where T : class
        {
            if (context.Message is ScheduleMessage)
                _completed.TrySetResult(context.CorrelationId);

            return Task.CompletedTask;
        }

        public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception) where T : class
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
                configuration.Contracts(contracts => contracts
                    .Register<ScheduleThroughOutbox>("vicione.tests.ef.schedule-through-outbox")
                    .Register<ScheduleMessage>("vicione.scheduler.schedule")
                    .Register<ScheduledOutboxPayload>("vicione.tests.ef.scheduled-outbox-payload"));
                configuration.AddPublishMessageScheduler();
                configuration.AddQuartzScheduling(provider => provider.GetRequiredService<ISchedulerFactory>());
                configuration.ConfigureEntityFrameworkTransactionalStore<QuartzOutboxDbContext>(outbox =>
                {
                    outbox.UsePostgres();
                    outbox.DisableInboxCleanupService();
                });
                configuration.AddConsumer<ScheduleThroughOutboxConsumer, ScheduleThroughOutboxConsumerDefinition>();
                configuration.AddConsumer<ScheduledOutboxPayloadConsumer>();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.ConfigurePublishMessageScheduler();
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
                ITestHarness harness = await provider.StartTestHarnessAsync().WaitAsync(operationTimeout, cancellationToken);
                IScheduler scheduler = await provider.GetRequiredService<ISchedulerFactory>()
                    .GetScheduler(cancellationToken).AsTask()
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
            await Harness.StopAsync(CancellationToken.None).WaitAsync(OperationTimeout, CancellationToken.None);
            await Services.DisposeAsync();
            await _database.DisposeAsync();
        }
    }
}
