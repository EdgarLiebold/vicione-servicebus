using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests.EntityFrameworkCoreIntegration;

public sealed class TransactionalOutboxFaultTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-TRANSACTION", "database-constraint-fault-does-not-poison-endpoint")]
    public async Task DuplicateKeyFailure_PublishesTheTypedDatabaseFaultAndTheEndpointRecoversAsync()
    {
        await using TransactionalOutboxFixture fixture = await TransactionalOutboxFixture.CreateAsync();
        Guid duplicatedEntityId = Guid.NewGuid();
        var first = new PersistEntityCommand(Guid.NewGuid(), duplicatedEntityId);
        var duplicate = new PersistEntityCommand(Guid.NewGuid(), duplicatedEntityId);
        var recovery = new PersistEntityCommand(Guid.NewGuid(), Guid.NewGuid());

        await fixture.Harness.Bus.PublishAsync(first, fixture.CancellationToken);
        PersistedEntityEvent firstCommitted = await fixture.Deliveries.ReadPersistedAsync(
            fixture.OperationTimeout,
            fixture.CancellationToken);

        Task<IPublishedMessage<Fault<PersistEntityCommand>>> faultTask = fixture.Harness.Published
            .SelectAsync<Fault<PersistEntityCommand>>(
                context => context.Context.Message.Message.CommandId == duplicate.CommandId,
                fixture.CancellationToken)
            .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
        await fixture.Harness.Bus.PublishAsync(duplicate, fixture.CancellationToken);
        IPublishedMessage<Fault<PersistEntityCommand>> fault = await faultTask.WaitAsync(
            fixture.OperationTimeout,
            fixture.CancellationToken);

        await fixture.Harness.Bus.PublishAsync(recovery, fixture.CancellationToken);
        PersistedEntityEvent recoveryCommitted = await fixture.Deliveries.ReadPersistedAsync(
            fixture.OperationTimeout,
            fixture.CancellationToken);

        await using AsyncServiceScope verificationScope = fixture.Services.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<TransactionalOutboxDbContext>();
        PersistedEntity[] entities = await dbContext.PersistedEntities
            .AsNoTracking()
            .OrderBy(entity => entity.Id)
            .ToArrayAsync(fixture.CancellationToken);
        using var snapshot = new CancellationTokenSource();
        snapshot.Cancel();
        IPublishedMessage<Fault<PersistEntityCommand>>[] faults = fixture.Harness.Published
            .Select<Fault<PersistEntityCommand>>(snapshot.Token)
            .Where(message => message.Context.Message.Message.CommandId == duplicate.CommandId)
            .ToArray();

        Assert.Equal(first.CommandId, firstCommitted.CommandId);
        Assert.Equal(recovery.CommandId, recoveryCommitted.CommandId);
        Assert.Equal(
            new[] { duplicatedEntityId, recovery.EntityId }.Order().ToArray(),
            entities.Select(entity => entity.Id).Order().ToArray());
        Assert.Single(faults);
        Assert.Same(fault, faults[0]);
        Assert.Contains(
            fault.Context.Message.Exceptions,
            exception => exception.ExceptionType == TypeCache<DbUpdateException>.ShortName);
        Assert.DoesNotContain(
            fixture.Deliveries.Persisted,
            message => message.CommandId == duplicate.CommandId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-TRANSACTION", "failed-attempt-rolls-back-before-single-successful-retry")]
    public async Task FirstAttemptFailure_RollsBackItsOutboxAndTheRetryPublishesEachEffectOnceAsync()
    {
        await using TransactionalOutboxFixture fixture = await TransactionalOutboxFixture.CreateAsync();
        var command = new RetryOutboxCommand(Guid.NewGuid());

        await fixture.Harness.Bus.PublishAsync(command, fixture.CancellationToken);
        RetryCommittedEvent first = await fixture.Deliveries.ReadRetryAsync(
            fixture.OperationTimeout,
            fixture.CancellationToken);
        RetryCommittedEvent second = await fixture.Deliveries.ReadRetryAsync(
            fixture.OperationTimeout,
            fixture.CancellationToken);

        Assert.Equal(2, fixture.Attempts.For(command.CommandId));
        Assert.Equal([1, 2], new[] { first.Ordinal, second.Ordinal }.Order().ToArray());
        Assert.Equal(
            [1, 2],
            fixture.Deliveries.RetryEvents
                .Where(message => message.CommandId == command.CommandId)
                .Select(message => message.Ordinal)
                .Order()
                .ToArray());
        Assert.Equal(2, fixture.Deliveries.RetryCount);
        using var snapshot = new CancellationTokenSource();
        snapshot.Cancel();
        Assert.Empty(fixture.Harness.Published.Select<Fault<RetryOutboxCommand>>(snapshot.Token));
    }

    public sealed record PersistEntityCommand(Guid CommandId, Guid EntityId);

    public sealed record PersistedEntityEvent(Guid CommandId, Guid EntityId);

    public sealed record RetryOutboxCommand(Guid CommandId);

    public sealed record RetryCommittedEvent(Guid CommandId, int Ordinal);

    public sealed class ExpectedFirstAttemptFailure : Exception;

    public sealed class PersistedEntity
    {
        public Guid Id { get; set; }
    }

    public sealed class TransactionalOutboxDbContext(DbContextOptions<TransactionalOutboxDbContext> options) : DbContext(options)
    {
        public DbSet<PersistedEntity> PersistedEntities => Set<PersistedEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PersistedEntity>().HasKey(entity => entity.Id);
            modelBuilder.AddTransactionalOutboxEntities();
        }
    }

    public sealed class PersistEntityConsumer(TransactionalOutboxDbContext dbContext) : IConsumer<PersistEntityCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<PersistEntityCommand> context)
        {
            dbContext.PersistedEntities.Add(new PersistedEntity { Id = context.Message.EntityId });
            await context.Advanced().PublishAsync(
                new PersistedEntityEvent(context.Message.CommandId, context.Message.EntityId),
                context.CancellationToken);
        }
    }

    public sealed class RetryOutboxConsumer(RetryAttemptProbe attempts) : IConsumer<RetryOutboxCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<RetryOutboxCommand> context)
        {
            int attempt = attempts.Increment(context.Message.CommandId);
            await context.Advanced().PublishAsync(new RetryCommittedEvent(context.Message.CommandId, 1), context.CancellationToken);
            await context.Advanced().PublishAsync(new RetryCommittedEvent(context.Message.CommandId, 2), context.CancellationToken);

            if (attempt == 1)
                throw new ExpectedFirstAttemptFailure();
        }
    }

    public sealed class PersistedEntityEventConsumer(TransactionalDeliveryProbe deliveries) : IConsumer<PersistedEntityEvent>
    {
        public Task ConsumeAsync(ConsumeContext<PersistedEntityEvent> context)
        {
            deliveries.Record(context.Message);
            return Task.CompletedTask;
        }
    }

    public sealed class RetryCommittedEventConsumer(TransactionalDeliveryProbe deliveries) : IConsumer<RetryCommittedEvent>
    {
        public Task ConsumeAsync(ConsumeContext<RetryCommittedEvent> context)
        {
            deliveries.Record(context.Message);
            return Task.CompletedTask;
        }
    }

    private sealed class PersistEntityConsumerDefinition : ConsumerDefinition<PersistEntityConsumer>
    {
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<PersistEntityConsumer> consumerConfigurator,
            IRegistrationContext context) => endpointConfigurator.UseEntityFrameworkOutbox<TransactionalOutboxDbContext>(context);
    }

    private sealed class RetryOutboxConsumerDefinition : ConsumerDefinition<RetryOutboxConsumer>
    {
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<RetryOutboxConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageRetry(retry => retry.Immediate(1));
            endpointConfigurator.UseEntityFrameworkOutbox<TransactionalOutboxDbContext>(context);
        }
    }

    public sealed class RetryAttemptProbe
    {
        private readonly Dictionary<Guid, int> _attempts = [];
        private readonly Lock _lock = new();

        public int For(Guid commandId)
        {
            lock (_lock)
                return _attempts.GetValueOrDefault(commandId);
        }

        public int Increment(Guid commandId)
        {
            lock (_lock)
            {
                int attempt = _attempts.GetValueOrDefault(commandId) + 1;
                _attempts[commandId] = attempt;
                return attempt;
            }
        }
    }

    public sealed class TransactionalDeliveryProbe
    {
        private readonly Channel<PersistedEntityEvent> _persisted = Channel.CreateUnbounded<PersistedEntityEvent>();
        private readonly Channel<RetryCommittedEvent> _retry = Channel.CreateUnbounded<RetryCommittedEvent>();
        private readonly List<PersistedEntityEvent> _persistedSnapshot = [];
        private readonly List<RetryCommittedEvent> _retrySnapshot = [];
        private readonly Lock _lock = new();
        private int _retryCount;

        public IReadOnlyList<PersistedEntityEvent> Persisted
        {
            get
            {
                lock (_lock)
                    return _persistedSnapshot.ToArray();
            }
        }

        public int RetryCount => Volatile.Read(ref _retryCount);

        public IReadOnlyList<RetryCommittedEvent> RetryEvents
        {
            get
            {
                lock (_lock)
                    return _retrySnapshot.ToArray();
            }
        }

        public void Record(PersistedEntityEvent message)
        {
            lock (_lock)
                _persistedSnapshot.Add(message);
            if (!_persisted.Writer.TryWrite(message))
                throw new InvalidOperationException("The persisted-entity observation channel rejected an event.");
        }

        public void Record(RetryCommittedEvent message)
        {
            lock (_lock)
                _retrySnapshot.Add(message);
            Interlocked.Increment(ref _retryCount);
            if (!_retry.Writer.TryWrite(message))
                throw new InvalidOperationException("The retry observation channel rejected an event.");
        }

        public Task<PersistedEntityEvent> ReadPersistedAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            _persisted.Reader.ReadAsync(cancellationToken).AsTask().WaitAsync(timeout, cancellationToken);

        public Task<RetryCommittedEvent> ReadRetryAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            _retry.Reader.ReadAsync(cancellationToken).AsTask().WaitAsync(timeout, cancellationToken);
    }

    private sealed class TransactionalOutboxFixture : IAsyncDisposable
    {
        private readonly PostgreSqlTestDatabase _database;

        private TransactionalOutboxFixture(
            PostgreSqlTestDatabase database,
            ServiceProvider services,
            ITestHarness harness,
            RetryAttemptProbe attempts,
            TransactionalDeliveryProbe deliveries,
            TimeSpan operationTimeout,
            CancellationToken cancellationToken)
        {
            _database = database;
            Services = services;
            Harness = harness;
            Attempts = attempts;
            Deliveries = deliveries;
            OperationTimeout = operationTimeout;
            CancellationToken = cancellationToken;
        }

        public RetryAttemptProbe Attempts { get; }

        public CancellationToken CancellationToken { get; }

        public TransactionalDeliveryProbe Deliveries { get; }

        public ITestHarness Harness { get; }

        public TimeSpan OperationTimeout { get; }

        public ServiceProvider Services { get; }

        public static async Task<TransactionalOutboxFixture> CreateAsync()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
                .GetValidatedOptions()
                .OperationTimeout!.Value;
            PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
                "transactional-outbox-faults",
                cancellationToken);
            var attempts = new RetryAttemptProbe();
            var deliveries = new TransactionalDeliveryProbe();
            var services = new ServiceCollection();
            services.AddSingleton(attempts);
            services.AddSingleton(deliveries);
            services.AddDbContext<TransactionalOutboxDbContext>(builder => builder
                .UseNpgsql(database.ConnectionString, options => options.EnableRetryOnFailure()));
            services.AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
            {
                configuration.SetTestTimeouts(operationTimeout, operationTimeout);
                configuration.AddEntityFrameworkOutbox<TransactionalOutboxDbContext>(outbox =>
                {
                    outbox.UsePostgres();
                    outbox.DisableInboxCleanupService();
                    outbox.QueryDelay = TimeSpan.FromHours(1);
                });
                configuration.AddConsumer<PersistEntityConsumer, PersistEntityConsumerDefinition>();
                configuration.AddConsumer<RetryOutboxConsumer, RetryOutboxConsumerDefinition>();
                configuration.AddConsumer<PersistedEntityEventConsumer>();
                configuration.AddConsumer<RetryCommittedEventConsumer>();
                configuration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
            });

            ServiceProvider? provider = null;
            try
            {
                await using (var context = new TransactionalOutboxDbContext(
                                 new DbContextOptionsBuilder<TransactionalOutboxDbContext>()
                                     .UseNpgsql(database.ConnectionString)
                                     .Options))
                {
                    if (!await context.Database.EnsureCreatedAsync(cancellationToken))
                        throw new InvalidOperationException("The transactional-outbox test database was not created.");
                }

                provider = services.BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true,
                });
                ITestHarness harness = await provider.StartTestHarnessAsync().WaitAsync(operationTimeout, cancellationToken);
                return new TransactionalOutboxFixture(
                    database,
                    provider,
                    harness,
                    attempts,
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
            await Harness.StopAsync(CancellationToken.None).WaitAsync(OperationTimeout, CancellationToken.None);
            await Services.DisposeAsync();
            await _database.DisposeAsync();
        }
    }
}
