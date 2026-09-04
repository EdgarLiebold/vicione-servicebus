using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests.EntityFrameworkCoreIntegration;

public sealed class BusOutboxDeliveryServiceTests
{
    private static readonly DateTimeOffset FrozenTime =
        new(2042, 3, 4, 5, 6, 7, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-DELIVERY", "commit-delivers-exact-envelope-without-poll-advance")]
    public async Task CommittedBatch_DeliversTheExactEnvelopeWithoutAdvancingThePollClockAsync()
    {
        await using BusOutboxFixture fixture = await BusOutboxFixture.CreateAsync(useRawJson: false);
        Guid messageId = Guid.NewGuid();
        Guid correlationId = Guid.NewGuid();
        Guid conversationId = Guid.NewGuid();

        await using (AsyncServiceScope scope = fixture.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BusOutboxDbContext>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            await publishEndpoint.PublishAsync(
                new OutboxProbe(1),
                context =>
                {
                    context.MessageId = messageId;
                    context.CorrelationId = correlationId;
                    context.ConversationId = conversationId;
                },
                fixture.CancellationToken);

            OutboxMessage pending = Assert.Single(dbContext.ChangeTracker.Entries<OutboxMessage>()).Entity;
            Assert.Equal(EntityState.Added, dbContext.Entry(pending).State);
            Assert.Equal(messageId, pending.MessageId);
            Assert.Equal(correlationId, pending.CorrelationId);
            Assert.Equal(conversationId, pending.ConversationId);
            await using BusOutboxDbContext independent = fixture.CreateContext();
            Assert.Empty(await independent.Set<OutboxMessage>().AsNoTracking().ToListAsync(fixture.CancellationToken));

            await dbContext.SaveChangesAsync(fixture.CancellationToken);
        }

        DeliveryObservation delivered = await fixture.Deliveries.ReadAsync(fixture.OperationTimeout, fixture.CancellationToken);
        Guid drainedOutbox = await fixture.Drains.ReadAsync(fixture.OperationTimeout, fixture.CancellationToken);

        Assert.Equal(1, delivered.Sequence);
        Assert.Equal(messageId, delivered.MessageId);
        Assert.Equal(correlationId, delivered.CorrelationId);
        Assert.Equal(conversationId, delivered.ConversationId);
        Assert.NotEqual(Guid.Empty, drainedOutbox);
        Assert.Equal(FrozenTime, fixture.TimeProvider.GetUtcNow());
        await fixture.AssertStoreIsEmptyAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-DELIVERY", "raw-json-preserves-user-header")]
    public async Task RawJsonDelivery_PreservesTheUserHeaderAndEnvelopeIdentityAsync()
    {
        await using BusOutboxFixture fixture = await BusOutboxFixture.CreateAsync(useRawJson: true);
        Guid messageId = Guid.NewGuid();
        Guid correlationId = Guid.NewGuid();

        await fixture.PublishAndCommitAsync(
            new OutboxProbe(2),
            context =>
            {
                context.MessageId = messageId;
                context.CorrelationId = correlationId;
                context.Headers.Set("tenant", "factory-a");
            });

        DeliveryObservation delivered = await fixture.Deliveries.ReadAsync(fixture.OperationTimeout, fixture.CancellationToken);
        await fixture.Drains.ReadAsync(fixture.OperationTimeout, fixture.CancellationToken);

        Assert.Equal(2, delivered.Sequence);
        Assert.Equal(messageId, delivered.MessageId);
        Assert.Equal(correlationId, delivered.CorrelationId);
        Assert.Equal("factory-a", delivered.Tenant);
        await fixture.AssertStoreIsEmptyAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-DELIVERY", "successive-commits-use-distinct-batches-without-loss")]
    public async Task SuccessiveCommits_DrainDistinctBatchesWithoutLossOrDuplicationAsync()
    {
        await using BusOutboxFixture fixture = await BusOutboxFixture.CreateAsync(useRawJson: false);
        Guid firstMessageId = Guid.NewGuid();
        Guid secondMessageId = Guid.NewGuid();

        await using (AsyncServiceScope scope = fixture.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BusOutboxDbContext>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            await publishEndpoint.PublishAsync(
                new OutboxProbe(10),
                context => context.MessageId = firstMessageId,
                fixture.CancellationToken);
            await dbContext.SaveChangesAsync(fixture.CancellationToken);

            await publishEndpoint.PublishAsync(
                new OutboxProbe(20),
                context => context.MessageId = secondMessageId,
                fixture.CancellationToken);
            await dbContext.SaveChangesAsync(fixture.CancellationToken);
        }

        DeliveryObservation[] deliveries =
        [
            await fixture.Deliveries.ReadAsync(fixture.OperationTimeout, fixture.CancellationToken),
            await fixture.Deliveries.ReadAsync(fixture.OperationTimeout, fixture.CancellationToken),
        ];
        Guid[] drainedOutboxes =
        [
            await fixture.Drains.ReadAsync(fixture.OperationTimeout, fixture.CancellationToken),
            await fixture.Drains.ReadAsync(fixture.OperationTimeout, fixture.CancellationToken),
        ];

        Assert.Equal([10, 20], deliveries.Select(delivery => delivery.Sequence).Order().ToArray());
        Assert.All(deliveries, delivery => Assert.True(delivery.MessageId.HasValue));
        Assert.Equal(
            new[] { firstMessageId, secondMessageId }.Order().ToArray(),
            deliveries.Select(delivery => delivery.MessageId!.Value).Order().ToArray());
        Assert.Equal(2, drainedOutboxes.Distinct().Count());
        await fixture.AssertStoreIsEmptyAsync();
        Assert.Equal(2, fixture.Deliveries.RecordedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-RETRY", "retry-waits-for-persisted-due-time")]
    public async Task FailedSend_RetriesOnlyAfterThePersistedDueTimeAndANewSignalAsync()
    {
        var notification = new ControlledOutboxNotification();
        var failingSend = new FailingSendFilter();
        await using BusOutboxFixture fixture = await BusOutboxFixture.CreateAsync(
            useRawJson: false,
            notification,
            failingSend);

        Assert.Equal(1, await notification.ReadWaitEntryAsync(fixture.OperationTimeout, fixture.CancellationToken));
        await fixture.PublishAndCommitAsync(
            new OutboxProbe(30),
            context => context.MessageId = Guid.NewGuid());

        Assert.Equal(1, await failingSend.ReadAttemptAsync(fixture.OperationTimeout, fixture.CancellationToken));
        Assert.Equal(2, await notification.ReadWaitEntryAsync(fixture.OperationTimeout, fixture.CancellationToken));
        Assert.Equal(1, failingSend.AttemptCount);
        OutboxState firstFailure = await fixture.LoadSingleOutboxStateAsync();
        Assert.Equal(OutboxDeliveryStatus.RetryScheduled, firstFailure.Status);
        Assert.Equal(1, firstFailure.DeliveryAttempts);
        Assert.Equal(OutboxFailureKind.Unclassified, firstFailure.LastFailureKind);
        Assert.Equal((FrozenTime + TimeSpan.FromSeconds(1)).UtcDateTime, firstFailure.NextDeliveryTime);

        notification.Delivered();
        Assert.Equal(3, await notification.ReadWaitEntryAsync(fixture.OperationTimeout, fixture.CancellationToken));
        Assert.Equal(1, failingSend.AttemptCount);

        fixture.TimeProvider.Advance(TimeSpan.FromSeconds(1));
        notification.Delivered();
        Assert.Equal(2, await failingSend.ReadAttemptAsync(fixture.OperationTimeout, fixture.CancellationToken));
        Assert.Equal(4, await notification.ReadWaitEntryAsync(fixture.OperationTimeout, fixture.CancellationToken));
        Assert.Equal(2, failingSend.AttemptCount);
        OutboxState secondFailure = await fixture.LoadSingleOutboxStateAsync();
        Assert.Equal(OutboxDeliveryStatus.RetryScheduled, secondFailure.Status);
        Assert.Equal(2, secondFailure.DeliveryAttempts);
        Assert.Equal((FrozenTime + TimeSpan.FromSeconds(3)).UtcDateTime, secondFailure.NextDeliveryTime);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-DELIVERY", "otel-baggage-round-trip")]
    public async Task Delivery_RestoresOpenTelemetryBaggageFromTheStoredEnvelopeAsync()
    {
        await using BusOutboxFixture fixture = await BusOutboxFixture.CreateAsync(useRawJson: false);
        using var publishingActivity = new Activity("bus-outbox-publisher");
        publishingActivity.SetIdFormat(ActivityIdFormat.W3C);
        publishingActivity.AddBaggage("suitcase", "calibration-data");
        publishingActivity.Start();

        try
        {
            await fixture.PublishAndCommitAsync(
                new OutboxProbe(40),
                context => context.MessageId = Guid.NewGuid());
        }
        finally
        {
            publishingActivity.Stop();
        }

        DeliveryObservation delivered = await fixture.Deliveries.ReadAsync(fixture.OperationTimeout, fixture.CancellationToken);
        await fixture.Drains.ReadAsync(fixture.OperationTimeout, fixture.CancellationToken);

        Assert.Equal(40, delivered.Sequence);
        Assert.Equal("calibration-data", delivered.Baggage);
        await fixture.AssertStoreIsEmptyAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-DELIVERY", "concurrent-batches-cross-delivery-limit-without-loss-or-duplication")]
    public async Task ConcurrentBatches_DeliverEveryMessageExactlyOnceAcrossDeliveryWindowsAsync()
    {
        const int batchCount = 12;
        const int messagesPerBatch = 12;
        const int firstSequence = 1000;
        await using BusOutboxFixture fixture = await BusOutboxFixture.CreateAsync(useRawJson: false);

        await Task.WhenAll(Enumerable.Range(0, batchCount).Select(batch =>
            fixture.PublishBatchAndCommitAsync(
                Enumerable.Range(firstSequence + batch * messagesPerBatch, messagesPerBatch)
                    .Select(sequence => new OutboxProbe(sequence)))));

        int expectedMessageCount = batchCount * messagesPerBatch;
        DeliveryObservation[] delivered;
        try
        {
            delivered = await fixture.Deliveries.ReadManyAsync(
                expectedMessageCount,
                fixture.OperationTimeout,
                fixture.CancellationToken);
        }
        catch (TimeoutException exception)
        {
            throw new InvalidOperationException(await fixture.DescribeDeliveryTimeoutAsync(expectedMessageCount), exception);
        }
        Guid[] drainedOutboxes = await fixture.Drains.ReadManyAsync(
            batchCount,
            fixture.OperationTimeout,
            fixture.CancellationToken);

        Assert.Equal(
            Enumerable.Range(firstSequence, expectedMessageCount),
            delivered.Select(observation => observation.Sequence).Order());
        Assert.Equal(expectedMessageCount, delivered.Select(observation => observation.MessageId).Distinct().Count());
        Assert.Equal(batchCount, drainedOutboxes.Distinct().Count());
        Assert.Equal(expectedMessageCount, fixture.Deliveries.RecordedCount);
        await fixture.AssertStoreIsEmptyAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-DELIVERY", "scheduled-publish-keeps-delay-after-outbox-commit")]
    public async Task ScheduledPublish_RemainsDeferredUntilTheExactTransportDeadlineAsync()
    {
        TimeSpan delay = TimeSpan.FromHours(3);
        await using BusOutboxFixture fixture = await BusOutboxFixture.CreateAsync(useRawJson: false);
        IInMemoryDelayProvider delayProvider = fixture.Services.GetRequiredService<IInMemoryDelayProvider>();

        await fixture.SchedulePublishAndCommitAsync(new OutboxProbe(50), delay);
        await fixture.Drains.ReadAsync(fixture.OperationTimeout, fixture.CancellationToken);

        Assert.Equal(0, fixture.Deliveries.RecordedCount);
        delayProvider.Advance(delay - TimeSpan.FromTicks(1));
        Assert.Equal(0, fixture.Deliveries.RecordedCount);
        delayProvider.Advance(TimeSpan.FromTicks(1));

        DeliveryObservation delivered = await fixture.Deliveries.ReadAsync(
            fixture.OperationTimeout,
            fixture.CancellationToken);
        Assert.Equal(50, delivered.Sequence);
        Assert.Equal(1, fixture.Deliveries.RecordedCount);
        await fixture.AssertStoreIsEmptyAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-WRITE", "persists-without-started-bus-or-delivery-service")]
    public async Task PersistedBatch_DoesNotRequireAStartedBusOrDeliveryServiceAsync()
    {
        Guid messageId = Guid.NewGuid();
        await using BusOutboxFixture fixture = await BusOutboxFixture.CreateAsync(
            useRawJson: false,
            startHarness: false,
            disableDeliveryService: true);

        await fixture.PublishAndCommitAsync(
            new OutboxProbe(60),
            context => context.MessageId = messageId);

        await using BusOutboxDbContext verification = fixture.CreateContext();
        OutboxMessage storedMessage = Assert.Single(await verification.Set<OutboxMessage>()
            .AsNoTracking()
            .ToListAsync(fixture.CancellationToken));
        OutboxState storedState = Assert.Single(await verification.Set<OutboxState>()
            .AsNoTracking()
            .ToListAsync(fixture.CancellationToken));

        Assert.Equal(messageId, storedMessage.MessageId);
        Assert.Equal(storedState.OutboxId, storedMessage.OutboxId);
        Assert.NotNull(storedMessage.DestinationAddress);
        Assert.Equal(0, fixture.Deliveries.RecordedCount);
    }

    public sealed record OutboxProbe(int Sequence);

    public sealed class OutboxProbeConsumer(DeliveryProbe deliveries) : IConsumer<OutboxProbe>
    {
        public Task ConsumeAsync(ConsumeContext<OutboxProbe> context)
        {
            context.Headers.TryGetHeader("tenant", out object? tenant);
            deliveries.Record(new DeliveryObservation(
                context.Message.Sequence,
                context.MessageId,
                context.CorrelationId,
                context.ConversationId,
                tenant?.ToString(),
                Activity.Current?.GetBaggageItem("suitcase")));
            return Task.CompletedTask;
        }
    }

    public sealed class BusOutboxDbContext(DbContextOptions<BusOutboxDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddTransactionalOutboxEntities();
        }
    }

    public sealed record DeliveryObservation(
        int Sequence,
        Guid? MessageId,
        Guid? CorrelationId,
        Guid? ConversationId,
        string? Tenant,
        string? Baggage);

    public sealed class DeliveryProbe
    {
        private readonly Channel<DeliveryObservation> _channel = Channel.CreateUnbounded<DeliveryObservation>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        private int _recordedCount;

        public int RecordedCount => Volatile.Read(ref _recordedCount);

        public void Record(DeliveryObservation observation)
        {
            if (!_channel.Writer.TryWrite(observation))
                throw new InvalidOperationException("The delivery observation channel rejected an outbox message.");

            Interlocked.Increment(ref _recordedCount);
        }

        public Task<DeliveryObservation> ReadAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            _channel.Reader.ReadAsync(cancellationToken).AsTask().WaitAsync(timeout, cancellationToken);

        public Task<DeliveryObservation[]> ReadManyAsync(int count, TimeSpan timeout, CancellationToken cancellationToken) =>
            ReadManyCoreAsync(count, cancellationToken).WaitAsync(timeout, cancellationToken);

        private async Task<DeliveryObservation[]> ReadManyCoreAsync(int count, CancellationToken cancellationToken)
        {
            var observations = new DeliveryObservation[count];
            for (var index = 0; index < observations.Length; index++)
                observations[index] = await _channel.Reader.ReadAsync(cancellationToken);

            return observations;
        }
    }

    public sealed class OutboxDrainObserver : SaveChangesInterceptor, IDbTransactionInterceptor
    {
        private readonly Channel<Guid> _drained = Channel.CreateUnbounded<Guid>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        private readonly ConcurrentDictionary<DbContext, Guid[]> _pending = new();

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is { } context)
            {
                Guid[] deletedOutboxes = context.ChangeTracker.Entries<OutboxState>()
                    .Where(entry => entry.State == EntityState.Deleted)
                    .Select(entry => entry.Entity.OutboxId)
                    .Distinct()
                    .ToArray();
                if (deletedOutboxes.Length > 0)
                    _pending[context] = deletedOutboxes;
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            Forget(eventData.Context);

            return base.SaveChangesFailedAsync(eventData, cancellationToken);
        }

        public override Task SaveChangesCanceledAsync(DbContextEventData eventData, CancellationToken cancellationToken = default)
        {
            Forget(eventData.Context);

            return base.SaveChangesCanceledAsync(eventData, cancellationToken);
        }

        public Task TransactionCommittedAsync(
            DbTransaction transaction,
            TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (eventData.Context is { } context && _pending.TryRemove(context, out Guid[]? outboxIds))
            {
                foreach (Guid outboxId in outboxIds)
                {
                    if (!_drained.Writer.TryWrite(outboxId))
                        throw new InvalidOperationException("The outbox-drain observation channel rejected a committed batch.");
                }
            }

            return Task.CompletedTask;
        }

        public Task TransactionRolledBackAsync(
            DbTransaction transaction,
            TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); Forget(eventData.Context);
            return Task.CompletedTask;
        }

        public Task TransactionFailedAsync(
            DbTransaction transaction,
            TransactionErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); Forget(eventData.Context);
            return Task.CompletedTask;
        }

        public Task<Guid> ReadAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            _drained.Reader.ReadAsync(cancellationToken).AsTask().WaitAsync(timeout, cancellationToken);

        public Task<Guid[]> ReadManyAsync(int count, TimeSpan timeout, CancellationToken cancellationToken) =>
            ReadManyCoreAsync(count, cancellationToken).WaitAsync(timeout, cancellationToken);

        private async Task<Guid[]> ReadManyCoreAsync(int count, CancellationToken cancellationToken)
        {
            var outboxIds = new Guid[count];
            for (var index = 0; index < outboxIds.Length; index++)
                outboxIds[index] = await _drained.Reader.ReadAsync(cancellationToken);

            return outboxIds;
        }

        private void Forget(DbContext? context)
        {
            if (context is not null)
                _pending.TryRemove(context, out _);
        }
    }

    public sealed class ControlledOutboxNotification :
        IBusOutboxNotification<EntityFrameworkBusOutboxScope<IBus, BusOutboxDbContext>>
    {
        private readonly Channel<int> _waitEntries = Channel.CreateUnbounded<int>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        private readonly Channel<bool> _wakeUps = Channel.CreateUnbounded<bool>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        private int _waitCount;

        public void Delivered()
        {
            if (!_wakeUps.Writer.TryWrite(true))
                throw new InvalidOperationException("The controlled outbox notification rejected a delivery signal.");
        }

        public async Task WaitForDeliveryAsync(CancellationToken cancellationToken)
        {
            int waitCount = Interlocked.Increment(ref _waitCount);
            if (!_waitEntries.Writer.TryWrite(waitCount))
                throw new InvalidOperationException("The controlled outbox notification rejected a wait observation.");

            await _wakeUps.Reader.ReadAsync(cancellationToken);
        }

        public Task<int> ReadWaitEntryAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            _waitEntries.Reader.ReadAsync(cancellationToken).AsTask().WaitAsync(timeout, cancellationToken);
    }

    public sealed class FailingSendFilter : IFilter<SendContext>
    {
        private readonly Channel<int> _attempts = Channel.CreateUnbounded<int>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        private int _attemptCount;

        public int AttemptCount => Volatile.Read(ref _attemptCount);

        public Task SendAsync(SendContext context, IPipe<SendContext> next)
        {
            int attempt = Interlocked.Increment(ref _attemptCount);
            if (!_attempts.Writer.TryWrite(attempt))
                throw new InvalidOperationException("The failing-send observation channel rejected an attempt.");

            throw new InvalidOperationException("Injected outbox transport failure.");
        }

        public void Probe(ProbeContext context)
        {
        }

        public Task<int> ReadAttemptAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            _attempts.Reader.ReadAsync(cancellationToken).AsTask().WaitAsync(timeout, cancellationToken);
    }

    private sealed class BusOutboxFixture : IAsyncDisposable
    {
        private readonly PostgreSqlTestDatabase _database;
        private readonly bool _harnessStarted;

        private BusOutboxFixture(
            PostgreSqlTestDatabase database,
            ServiceProvider services,
            ITestHarness harness,
            FakeTimeProvider timeProvider,
            DeliveryProbe deliveries,
            OutboxDrainObserver drains,
            TimeSpan operationTimeout,
            CancellationToken cancellationToken,
            bool harnessStarted)
        {
            _database = database;
            Services = services;
            Harness = harness;
            TimeProvider = timeProvider;
            Deliveries = deliveries;
            Drains = drains;
            OperationTimeout = operationTimeout;
            CancellationToken = cancellationToken;
            _harnessStarted = harnessStarted;
        }

        public CancellationToken CancellationToken { get; }

        public DeliveryProbe Deliveries { get; }

        public OutboxDrainObserver Drains { get; }

        public ITestHarness Harness { get; }

        public TimeSpan OperationTimeout { get; }

        public ServiceProvider Services { get; }

        public FakeTimeProvider TimeProvider { get; }

        public static async Task<BusOutboxFixture> CreateAsync(
            bool useRawJson,
            IBusOutboxNotification<EntityFrameworkBusOutboxScope<IBus, BusOutboxDbContext>>? notification = null,
            IFilter<SendContext>? sendFilter = null,
            bool startHarness = true,
            bool disableDeliveryService = false)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
                .GetValidatedOptions()
                .OperationTimeout!.Value;
            PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync("bus-outbox-delivery", cancellationToken);
            var timeProvider = new FakeTimeProvider(FrozenTime);
            var deliveries = new DeliveryProbe();
            var drains = new OutboxDrainObserver();
            var services = new ServiceCollection();
            services.AddSingleton<TimeProvider>(timeProvider);
            services.AddSingleton(deliveries);
            services.AddSingleton(drains);
            services.AddDbContext<BusOutboxDbContext>(builder => builder
                .UseNpgsql(database.ConnectionString, options => options.EnableRetryOnFailure())
                .AddInterceptors(drains));
            services.AddTelemetryListener(TextWriter.Null);
            services.AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
            {
                configuration.SetTestTimeouts(operationTimeout, operationTimeout);
                configuration.AddEntityFrameworkOutbox<BusOutboxDbContext>(outbox =>
                {
                    outbox.UsePostgres();
                    outbox.DisableInboxCleanupService();
                    outbox.QueryDelay = TimeSpan.FromHours(1);
                    outbox.UseBusOutbox(busOutbox =>
                    {
                        busOutbox.MessageDeliveryLimit = 10;
                        if (disableDeliveryService)
                            busOutbox.DisableDeliveryService();
                    });
                });
                configuration.AddConsumer<OutboxProbeConsumer>();
                configuration.UsingInMemory((context, bus) =>
                {
                    if (useRawJson)
                        bus.UseRawJsonSerializer();
                    if (sendFilter is not null)
                        bus.ConfigureSend(send => send.UseFilter(sendFilter));

                    bus.ConfigureEndpoints(context);
                });
            });
            if (notification is not null)
                services.Replace(ServiceDescriptor.Singleton(notification));

            ServiceProvider? provider = null;
            try
            {
                await using (var context = new BusOutboxDbContext(
                    new DbContextOptionsBuilder<BusOutboxDbContext>()
                        .UseNpgsql(database.ConnectionString)
                        .Options))
                {
                    Assert.True(await context.Database.EnsureCreatedAsync(cancellationToken));
                }

                provider = services.BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true,
                });
                ITestHarness harness = provider.GetTestHarness();
                if (startHarness)
                    await harness.StartAsync().WaitAsync(operationTimeout, cancellationToken);

                return new BusOutboxFixture(
                    database,
                    provider,
                    harness,
                    timeProvider,
                    deliveries,
                    drains,
                    operationTimeout,
                    cancellationToken,
                    startHarness);
            }
            catch
            {
                if (provider is not null)
                    await provider.DisposeAsync();

                await database.DisposeAsync();
                throw;
            }
        }

        public BusOutboxDbContext CreateContext() => new(
            new DbContextOptionsBuilder<BusOutboxDbContext>()
                .UseNpgsql(_database.ConnectionString)
                .Options);

        public async Task PublishAndCommitAsync(OutboxProbe message, Action<PublishContext<OutboxProbe>> configure)
        {
            await using AsyncServiceScope scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BusOutboxDbContext>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            await publishEndpoint.PublishAsync(message, configure, CancellationToken);
            await dbContext.SaveChangesAsync(CancellationToken);
        }

        public async Task PublishBatchAndCommitAsync(IEnumerable<OutboxProbe> messages)
        {
            await using AsyncServiceScope scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BusOutboxDbContext>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            await Task.WhenAll(messages.Select(message =>
                publishEndpoint.PublishAsync(
                    message,
                    context => context.MessageId = Guid.NewGuid(),
                    CancellationToken)));
            await dbContext.SaveChangesAsync(CancellationToken);
        }

        public async Task SchedulePublishAndCommitAsync(OutboxProbe message, TimeSpan delay)
        {
            await using AsyncServiceScope scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BusOutboxDbContext>();
            var scheduler = scope.ServiceProvider.GetRequiredService<IMessageScheduler>();
            await scheduler.SchedulePublishAsync(delay, message, CancellationToken);
            await dbContext.SaveChangesAsync(CancellationToken);
        }

        public async Task AssertStoreIsEmptyAsync()
        {
            await using BusOutboxDbContext context = CreateContext();
            Assert.Empty(await context.Set<OutboxMessage>().AsNoTracking().ToListAsync(CancellationToken));
            Assert.Empty(await context.Set<OutboxState>().AsNoTracking().ToListAsync(CancellationToken));
        }

        public async Task<OutboxState> LoadSingleOutboxStateAsync()
        {
            await using BusOutboxDbContext context = CreateContext();
            return await context.Set<OutboxState>().AsNoTracking().SingleAsync(CancellationToken);
        }

        public async Task<string> DescribeDeliveryTimeoutAsync(int expectedMessageCount)
        {
            await using BusOutboxDbContext context = CreateContext();
            OutboxState[] states = await context.Set<OutboxState>()
                .AsNoTracking()
                .OrderBy(x => x.OutboxId)
                .ToArrayAsync(CancellationToken.None);
            var remainingByOutbox = await context.Set<OutboxMessage>()
                .AsNoTracking()
                .Where(x => x.OutboxId.HasValue)
                .GroupBy(x => x.OutboxId)
                .Select(group => new { OutboxId = group.Key!.Value, Count = group.Count() })
                .ToDictionaryAsync(x => x.OutboxId, x => x.Count, CancellationToken.None);
            int messageCount = remainingByOutbox.Values.Sum();
            string stateSummary = string.Join(", ", states.Select(state =>
                $"{state.OutboxId}:{state.Status}:remaining={remainingByOutbox.GetValueOrDefault(state.OutboxId)}:"
                + $"last={state.LastSequenceNumber?.ToString() ?? "null"}:lock={state.LockId}:attempts={state.DeliveryAttempts}"));

            return $"Expected {expectedMessageCount} deliveries but observed {Deliveries.RecordedCount}; "
                + $"the store retained {messageCount} messages across {states.Length} states [{stateSummary}].";
        }

        public async ValueTask DisposeAsync()
        {
            if (_harnessStarted)
                await Harness.StopAsync(CancellationToken.None).WaitAsync(OperationTimeout, CancellationToken.None);

            await Services.DisposeAsync();
            await _database.DisposeAsync();
        }
    }
}
