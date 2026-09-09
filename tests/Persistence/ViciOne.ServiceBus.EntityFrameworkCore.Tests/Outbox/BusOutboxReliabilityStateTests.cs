using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Outbox;

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
        Assert.Equal(OutboxFailureCode.TransportSendFailed, state.LastFailureCode);
        Assert.Equal(Now.UtcDateTime, state.LastFailureTime);
        Assert.Equal((Now + TimeSpan.FromSeconds(2)).UtcDateTime, state.NextDeliveryTime);
        Assert.Equal(message.SequenceNumber, state.FailedSequenceNumber);
        Assert.Equal(message.MessageId, state.FailedMessageId);
        Assert.Equal(typeof(InvalidOperationException).FullName, state.LastExceptionType);
        Assert.DoesNotContain("unknown", state.LastExceptionType, StringComparison.Ordinal);
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
        Assert.Equal(OutboxFailureCode.TransportSendFailed, state.LastFailureCode);
        Assert.Equal(typeof(TimeoutException).FullName, state.LastExceptionType);
        Assert.Null(state.NextDeliveryTime);
        Assert.Equal(message.SequenceNumber, state.FailedSequenceNumber);
        Assert.Equal(message.MessageId, state.FailedMessageId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-RETRY", "permanent-failure-counts-terminal-attempt")]
    public void PermanentFailure_CountsTheTerminalDeliveryAttempt()
    {
        using ServiceProvider provider = CreateProvider();
        var service = CreateService(provider, classifiers: [new PermanentClassifier()]);
        var state = CreateState();
        state.DeliveryAttempts = 4;
        var message = CreateMessage(state.OutboxId, 25);

        service.ApplyDeliveryFailure(state, message, new TestTransportException());

        Assert.Equal(OutboxDeliveryStatus.Quarantined, state.Status);
        Assert.Equal(5, state.DeliveryAttempts);
        Assert.Equal(OutboxFailureKind.Permanent, state.LastFailureKind);
        Assert.Equal(OutboxFailureCode.TransportSendFailed, state.LastFailureCode);
        Assert.Equal(typeof(TestTransportException).FullName, state.LastExceptionType);
        Assert.Equal(message.SequenceNumber, state.FailedSequenceNumber);
        Assert.Equal(message.MessageId, state.FailedMessageId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-RETRY", "failure-description-cannot-break-state-persistence")]
    public void FaultingExceptionDescription_CannotPreventBoundedFailurePersistence()
    {
        using ServiceProvider provider = CreateProvider();
        var service = CreateService(provider);
        var state = CreateState();
        var message = CreateMessage(state.OutboxId, 27);

        service.ApplyDeliveryFailure(state, message, new FaultingDescriptionException());

        Assert.Equal(OutboxDeliveryStatus.RetryScheduled, state.Status);
        Assert.Equal(1, state.DeliveryAttempts);
        Assert.Equal(typeof(FaultingDescriptionException).FullName, state.LastExceptionType);
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
        Assert.Equal(OutboxFailureCode.InvalidDeliveryAttemptCount, state.LastFailureCode);
        Assert.Null(state.LastExceptionType);
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
    public async Task MissingDestination_IsPersistedAsQuarantinedAndNeverDeletedAsDeliveredAsync()
    {
        await using DeliveryFixture fixture = await DeliveryFixture.CreateAsync();
        OutboxState state = CreateState();
        OutboxMessage message = CreatePersistableMessage(state.OutboxId, destinationAddress: null);
        fixture.DbContext.AddRange(state, message);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();
        state = await fixture.DbContext.Set<OutboxState>().SingleAsync(TestContext.Current.CancellationToken);
        using ServiceProvider provider = CreateProvider();
        var service = CreateService(provider);

        int delivered = await service.DeliverOutboxMessagesAsync(fixture.DbContext, state, TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        OutboxState persisted = await fixture.DbContext.Set<OutboxState>().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, delivered);
        Assert.Equal(OutboxDeliveryStatus.Quarantined, persisted.Status);
        Assert.Equal(1, persisted.DeliveryAttempts);
        Assert.Equal(OutboxFailureKind.InvariantViolation, persisted.LastFailureKind);
        Assert.Equal(OutboxFailureCode.MissingDestinationAddress, persisted.LastFailureCode);
        Assert.Null(persisted.LastExceptionType);
        Assert.Null(persisted.Delivered);
        Assert.Single(await fixture.DbContext.Set<OutboxMessage>().ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-QUARANTINE", "corrupt-metadata-is-retained")]
    public async Task CorruptPersistedMetadata_IsQuarantinedInsteadOfHotLoopingAsync()
    {
        await using DeliveryFixture fixture = await DeliveryFixture.CreateAsync();
        OutboxState state = CreateState();
        OutboxMessage message = CreatePersistableMessage(state.OutboxId, new Uri("loopback://localhost/valid"));
        message.Headers = "{";
        fixture.DbContext.AddRange(state, message);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();
        state = await fixture.DbContext.Set<OutboxState>().SingleAsync(TestContext.Current.CancellationToken);
        using ServiceProvider provider = CreateProvider();
        var service = CreateService(provider);

        int delivered = await service.DeliverOutboxMessagesAsync(fixture.DbContext, state, TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        OutboxState persisted = await fixture.DbContext.Set<OutboxState>().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, delivered);
        Assert.Equal(OutboxDeliveryStatus.Quarantined, persisted.Status);
        Assert.Equal(1, persisted.DeliveryAttempts);
        Assert.Equal(OutboxFailureKind.InvariantViolation, persisted.LastFailureKind);
        Assert.Equal(OutboxFailureCode.MetadataDeserializationFailed, persisted.LastFailureCode);
        Assert.Equal(typeof(System.Text.Json.JsonException).FullName, persisted.LastExceptionType);
        Assert.Single(await fixture.DbContext.Set<OutboxMessage>().ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-DELIVERY", "empty-final-window-is-progress")]
    public async Task EmptyFinalDeliveryWindow_MarksTheOutboxDeliveredAndReportsProgressAsync()
    {
        await using DeliveryFixture fixture = await DeliveryFixture.CreateAsync();
        OutboxState state = CreateState();
        state.LastSequenceNumber = 42;
        fixture.DbContext.Add(state);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();
        state = await fixture.DbContext.Set<OutboxState>().SingleAsync(TestContext.Current.CancellationToken);
        using ServiceProvider provider = CreateProvider();
        var service = CreateService(provider);

        int progress = await service.DeliverOutboxMessagesAsync(fixture.DbContext, state, TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        OutboxState persisted = await fixture.DbContext.Set<OutboxState>().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, progress);
        Assert.Equal(OutboxDeliveryStatus.Delivered, persisted.Status);
        Assert.Equal(Now.UtcDateTime, persisted.Delivered);
        Assert.Equal(42, persisted.LastSequenceNumber);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-DELIVERY", "endpoint-resolution-observes-delivery-timeout")]
    public async Task EndpointResolution_UsesTheBoundedDeliveryTokenAsync()
    {
        await using DeliveryFixture fixture = await DeliveryFixture.CreateAsync();
        OutboxState state = CreateState();
        OutboxMessage message = CreatePersistableMessage(state.OutboxId, new Uri("loopback://localhost/delivery"));
        fixture.DbContext.AddRange(state, message);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();
        state = await fixture.DbContext.Set<OutboxState>().SingleAsync(TestContext.Current.CancellationToken);
        IBus bus = DispatchProxy.Create<IBus, RecordingBusProxy>();
        var busProxy = (RecordingBusProxy)(object)bus;
        using ServiceProvider provider = CreateProvider(bus);
        var service = CreateService(provider);

        int delivered = await service.DeliverOutboxMessagesAsync(fixture.DbContext, state, CancellationToken.None);

        Assert.Equal(1, delivered);
        Assert.True(busProxy.EndpointResolutionToken.CanBeCanceled);
    }

    private static EntityFrameworkTransactionalOutboxSource<IBus, DeliveryDbContext> CreateService(
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

        return new EntityFrameworkTransactionalOutboxSource<IBus, DeliveryDbContext>(
            Options.Create(options),
            Options.Create(outboxOptions),
            new RecordingNotification(),
            classifiers ?? [],
            NullLogger<EntityFrameworkTransactionalOutboxSource<IBus, DeliveryDbContext>>.Instance,
            provider,
            new FakeTimeProvider(Now),
            BusPersistenceIdentity<IBus>.Create("default"));
    }

    private static ServiceProvider CreateProvider(IBus? bus = null)
    {
        IBusControl busControl = global::ViciOne.ServiceBus.Advanced.Bus.Factory.CreateUsingInMemory(_ => { });
        return new ServiceCollection()
            .AddSingleton(bus ?? busControl)
            .AddSingleton(busControl)
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

    private sealed class FaultingDescriptionException : Exception
    {
        public override string ToString() => throw new InvalidOperationException("Description unavailable.");
    }

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

    private class RecordingBusProxy : DispatchProxy
    {
        private static readonly ISendEndpoint Endpoint = DispatchProxy.Create<TestSendEndpoint, SuccessfulSendEndpointProxy>();

        public CancellationToken EndpointResolutionToken { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(ISendEndpointProvider.GetSendEndpointAsync))
            {
                EndpointResolutionToken = (CancellationToken)args![1]!;
                return Task.FromResult(Endpoint);
            }

            throw new InvalidOperationException($"Unexpected bus member: {targetMethod?.Name ?? "<null>"}.");
        }
    }

    private interface TestSendEndpoint : ISendEndpoint, ViciOne.ServiceBus.Advanced.IAdvancedSendEndpoint;

    private class SuccessfulSendEndpointProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "SendAsync" && targetMethod.ReturnType == typeof(Task))
                return Task.CompletedTask;

            throw new InvalidOperationException($"Unexpected send-endpoint member: {targetMethod?.Name ?? "<null>"}.");
        }
    }

    private sealed class RecordingNotification : IBusOutboxNotification<EntityFrameworkBusOutboxScope<IBus, DeliveryDbContext>>
    {
        public Task WaitForDeliveryAsync(CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested
                ? Task.FromCanceled(cancellationToken)
                : Task.CompletedTask;

        public void SignalDelivery()
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

        public static async Task<DeliveryFixture> CreateAsync()
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
