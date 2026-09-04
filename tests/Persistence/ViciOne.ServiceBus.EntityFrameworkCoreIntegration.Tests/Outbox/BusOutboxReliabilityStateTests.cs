namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.Outbox;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.ProviderAbstractions;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class BusOutboxReliabilityStateTests
{
    private static readonly DateTimeOffset Now =
        new(2042, 3, 4, 5, 6, 7, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-RETRY", "unknown-failure-is-persisted-as-unclassified")]
    public void UnknownFailure_SchedulesABoundedRetryWithoutGuessingTransientClassification()
    {
        using ServiceProvider provider = CreateProvider();
        var service = CreateService(provider, maximumAttempts: 3, initialDelay: TimeSpan.FromSeconds(2));
        var state = CreateState();
        var message = CreateMessage(state.OutboxId, 17);

        service.ApplyDeliveryFailure(state, message, new InvalidOperationException("unknown"));

        Assert.Equal(OutboxDeliveryStatus.RetryScheduled, state.Status);
        Assert.Equal(1, state.DeliveryAttempts);
        Assert.Equal(OutboxFailureKind.Unclassified, state.LastFailureKind);
        Assert.Equal(Now.UtcDateTime, state.LastFailureTime);
        Assert.Equal((Now + TimeSpan.FromSeconds(2)).UtcDateTime, state.NextDeliveryTime);
        Assert.Equal(message.SequenceNumber, state.FailedSequenceNumber);
        Assert.Equal(message.MessageId, state.FailedMessageId);
        Assert.Contains("unknown", state.LastFailure, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-RETRY", "retry-budget-exhaustion-is-terminal")]
    public void RetryBudgetExhaustion_TransitionsToTerminalQuarantine()
    {
        using ServiceProvider provider = CreateProvider();
        var service = CreateService(provider, maximumAttempts: 3);
        var state = CreateState();
        state.DeliveryAttempts = 2;
        var message = CreateMessage(state.OutboxId, 23);

        service.ApplyDeliveryFailure(state, message, new TimeoutException("still unavailable"));

        Assert.Equal(OutboxDeliveryStatus.Quarantined, state.Status);
        Assert.Equal(3, state.DeliveryAttempts);
        Assert.Equal(OutboxFailureKind.RetryLimitExceeded, state.LastFailureKind);
        Assert.Null(state.NextDeliveryTime);
        Assert.Equal(message.SequenceNumber, state.FailedSequenceNumber);
        Assert.Equal(message.MessageId, state.FailedMessageId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-RETRY", "corrupt-attempt-count-is-quarantined")]
    public void CorruptAttemptCount_IsQuarantinedInsteadOfOverflowingIntoAnOperationalLoop()
    {
        using ServiceProvider provider = CreateProvider();
        var service = CreateService(provider);
        var state = CreateState();
        state.DeliveryAttempts = int.MaxValue;
        var message = CreateMessage(state.OutboxId, 29);

        service.ApplyDeliveryFailure(state, message, new TimeoutException("timeout"));

        Assert.Equal(OutboxDeliveryStatus.Quarantined, state.Status);
        Assert.Equal(OutboxFailureKind.InvariantViolation, state.LastFailureKind);
        Assert.Contains(int.MaxValue.ToString(), state.LastFailure, StringComparison.Ordinal);
        Assert.Null(state.NextDeliveryTime);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-FAILURE-CLASSIFICATION", "classifier-fault-does-not-own-delivery")]
    public void FaultingClassifier_IsolatedAndTheNextClassifierRemainsAuthoritative()
    {
        using ServiceProvider provider = CreateProvider();
        var service = CreateService(provider, classifiers: [new ThrowingClassifier(), new PermanentClassifier()]);
        var failure = new TestTransportException();

        TransportSendFailureKind kind = service.Classify(failure);

        Assert.Equal(TransportSendFailureKind.Permanent, kind);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-FAILURE-CLASSIFICATION", "fallback-classification-is-explicit")]
    public void BuiltInClassification_DistinguishesTransientPermanentAndUnclassifiedFailures()
    {
        using ServiceProvider provider = CreateProvider();
        var service = CreateService(provider);

        Assert.Equal(TransportSendFailureKind.Transient, service.Classify(new TimeoutException()));
        Assert.Equal(TransportSendFailureKind.Transient, service.Classify(new OperationCanceledException()));
        Assert.Equal(TransportSendFailureKind.Permanent, service.Classify(new ConfigurationException("invalid")));
        Assert.Equal(TransportSendFailureKind.Permanent, service.Classify(new UnauthorizedAccessException()));
        Assert.Equal(TransportSendFailureKind.Unclassified, service.Classify(new TestTransportException()));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(4, 8)]
    [InlineData(7, 60)]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-RETRY", "exponential-delay-is-capped")]
    public void RetryDelay_IsExponentialAndCappedWithoutOverflow(int attempt, int expectedSeconds)
    {
        using ServiceProvider provider = CreateProvider();
        var service = CreateService(provider);

        TimeSpan delay = service.CalculateRetryDelay(attempt);

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), delay);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-QUARANTINE", "missing-destination-is-retained")]
    public async Task MissingDestination_IsPersistedAsQuarantinedAndNeverDeletedAsDelivered()
    {
        await using DeliveryFixture fixture = await DeliveryFixture.Create();
        OutboxState state = CreateState();
        OutboxMessage message = CreatePersistableMessage(state.OutboxId, destinationAddress: null);
        fixture.DbContext.AddRange(state, message);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();
        state = await fixture.DbContext.Set<OutboxState>().SingleAsync(TestContext.Current.CancellationToken);
        using ServiceProvider provider = CreateProvider();
        var service = CreateService(provider);

        int delivered = await service.DeliverOutboxMessages(fixture.DbContext, state, TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        OutboxState persisted = await fixture.DbContext.Set<OutboxState>().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, delivered);
        Assert.Equal(OutboxDeliveryStatus.Quarantined, persisted.Status);
        Assert.Equal(OutboxFailureKind.InvariantViolation, persisted.LastFailureKind);
        Assert.Null(persisted.Delivered);
        Assert.Single(await fixture.DbContext.Set<OutboxMessage>().ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-QUARANTINE", "corrupt-metadata-is-retained")]
    public async Task CorruptPersistedMetadata_IsQuarantinedInsteadOfHotLooping()
    {
        await using DeliveryFixture fixture = await DeliveryFixture.Create();
        OutboxState state = CreateState();
        OutboxMessage message = CreatePersistableMessage(state.OutboxId, new Uri("loopback://localhost/valid"));
        message.Headers = "{";
        fixture.DbContext.AddRange(state, message);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();
        state = await fixture.DbContext.Set<OutboxState>().SingleAsync(TestContext.Current.CancellationToken);
        using ServiceProvider provider = CreateProvider();
        var service = CreateService(provider);

        int delivered = await service.DeliverOutboxMessages(fixture.DbContext, state, TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        OutboxState persisted = await fixture.DbContext.Set<OutboxState>().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, delivered);
        Assert.Equal(OutboxDeliveryStatus.Quarantined, persisted.Status);
        Assert.Equal(OutboxFailureKind.InvariantViolation, persisted.LastFailureKind);
        Assert.Contains("metadata", persisted.LastFailure, StringComparison.OrdinalIgnoreCase);
        Assert.Single(await fixture.DbContext.Set<OutboxMessage>().ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-DELIVERY", "empty-final-window-is-progress")]
    public async Task EmptyFinalDeliveryWindow_MarksTheOutboxDeliveredAndReportsProgress()
    {
        await using DeliveryFixture fixture = await DeliveryFixture.Create();
        OutboxState state = CreateState();
        state.LastSequenceNumber = 42;
        fixture.DbContext.Add(state);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();
        state = await fixture.DbContext.Set<OutboxState>().SingleAsync(TestContext.Current.CancellationToken);
        using ServiceProvider provider = CreateProvider();
        var service = CreateService(provider);

        int progress = await service.DeliverOutboxMessages(fixture.DbContext, state, TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        OutboxState persisted = await fixture.DbContext.Set<OutboxState>().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, progress);
        Assert.Equal(OutboxDeliveryStatus.Delivered, persisted.Status);
        Assert.Equal(Now.UtcDateTime, persisted.Delivered);
        Assert.Equal(42, persisted.LastSequenceNumber);
    }

    private static BusOutboxDeliveryService<IBus, DeliveryDbContext> CreateService(
        IServiceProvider provider,
        int maximumAttempts = 10,
        TimeSpan? initialDelay = null,
        IReadOnlyList<ITransportSendFailureClassifier>? classifiers = null)
    {
        var options = new OutboxDeliveryServiceOptions<EntityFrameworkBusOutboxScope<IBus, DeliveryDbContext>>
        {
            MaximumDeliveryAttempts = maximumAttempts,
            InitialDeliveryRetryDelay = initialDelay ?? TimeSpan.FromSeconds(1),
            MaximumDeliveryRetryDelay = TimeSpan.FromMinutes(1)
        };
        var outboxOptions = new EntityFrameworkOutboxOptions<DeliveryDbContext>
        {
            LockStatementProvider = new SqliteLockStatementProvider()
        };

        return new BusOutboxDeliveryService<IBus, DeliveryDbContext>(
            Options.Create(options),
            Options.Create(outboxOptions),
            new RecordingNotification(),
            classifiers ?? [],
            NullLogger<BusOutboxDeliveryService<IBus, DeliveryDbContext>>.Instance,
            provider,
            new FakeTimeProvider(Now),
            BusPersistenceIdentity<IBus>.Create("default"));
    }

    private static ServiceProvider CreateProvider()
    {
        IBusControl bus = global::ViciOne.ServiceBus.Bus.Factory.CreateUsingInMemory(_ => { });
        return new ServiceCollection()
            .AddSingleton<IBus>(bus)
            .AddSingleton(bus)
            .BuildServiceProvider();
    }

    private static OutboxState CreateState() => new()
    {
        OutboxId = Guid.NewGuid(),
        BusKey = "default",
        Created = Now.UtcDateTime,
        Status = OutboxDeliveryStatus.Pending
    };

    private static OutboxMessage CreateMessage(Guid outboxId, long sequenceNumber) => new()
    {
        OutboxId = outboxId,
        SequenceNumber = sequenceNumber,
        MessageId = Guid.NewGuid()
    };

    private static OutboxMessage CreatePersistableMessage(Guid outboxId, Uri? destinationAddress)
    {
        var context = new MessageSendContext<DeliveryProbe>(new DeliveryProbe())
        {
            MessageId = Guid.NewGuid(),
            DestinationAddress = destinationAddress,
            Serializer = ServiceBusMetadataJson.MessageSerializer
        };

        return OutboxMessageFactory.Create(
            context,
            ServiceBusMetadataJson.ObjectDeserializer,
            new FakeTimeProvider(Now),
            outboxId: outboxId);
    }

    private sealed record DeliveryProbe;
    private sealed class TestTransportException : Exception;

    private sealed class ThrowingClassifier : ITransportSendFailureClassifier
    {
        public bool TryClassify(Exception exception, out TransportSendFailureKind failureKind)
        {
            failureKind = default;
            throw new InvalidOperationException("classifier bug");
        }
    }

    private sealed class PermanentClassifier : ITransportSendFailureClassifier
    {
        public bool TryClassify(Exception exception, out TransportSendFailureKind failureKind)
        {
            failureKind = TransportSendFailureKind.Permanent;
            return exception is TestTransportException;
        }
    }

    private sealed class RecordingNotification : IBusOutboxNotification<EntityFrameworkBusOutboxScope<IBus, DeliveryDbContext>>
    {
        public Task WaitForDelivery(CancellationToken cancellationToken) => Task.CompletedTask;
        public void Delivered()
        {
        }
    }

    private sealed class DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddTransactionalOutboxEntities();
    }

    private sealed class DeliveryFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private DeliveryFixture(SqliteConnection connection, DeliveryDbContext dbContext)
        {
            _connection = connection;
            DbContext = dbContext;
        }

        public DeliveryDbContext DbContext { get; }

        public static async Task<DeliveryFixture> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var context = new DeliveryDbContext(new DbContextOptionsBuilder<DeliveryDbContext>().UseSqlite(connection).Options);
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            return new DeliveryFixture(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
