using System.Data.Common;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.Saga;

public sealed class PostgreSqlSagaConcurrencyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-EXECUTION-STRATEGY", "retry-reloads-state-after-a-rolled-back-attempt")]
    public async Task TransientRetry_ReloadsTheSagaBeforeApplyingTheOperationAgainAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
            "saga-execution-strategy-retry",
            cancellationToken);
        await using (var setup = CreateContext(database.ConnectionString))
            await setup.Database.EnsureCreatedAsync(cancellationToken);

        var handler = new HandlerProbe();
        var transientFailure = new TransientSagaSaveProbe();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(handler)
            .AddSingleton(transientFailure)
            .AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddSagaStateMachine<SerializedStateMachine, SerializedState, SerializedStateDefinition>()
                    .EntityFrameworkRepository(repository =>
                    {
                        repository.UsePostgreSql();
                        repository.AddDbContext<DbContext, SerializedSagaDbContext>((services, builder) =>
                            builder.UseNpgsql(database.ConnectionString, options => options.EnableRetryOnFailure())
                                .AddInterceptors(services.GetRequiredService<TransientSagaSaveProbe>()));
                    });
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid sagaId = Guid.NewGuid();
            ISendEndpoint endpoint = await harness.GetSagaEndpointAsync<SerializedState>(TestContext.Current.CancellationToken);
            await endpoint.SendAsync(new BeginSerializedSaga(sagaId), cancellationToken);
            await harness.Published
                .SelectAsync<SerializedSagaStarted>(
                    observation => observation.Context.Message.CorrelationId == sagaId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            handler.Release();
            transientFailure.StartObserving();

            await endpoint.SendAsync(new IncrementSerializedSaga(sagaId), cancellationToken);
            IPublishedMessage<SerializedSagaIncremented> incremented = await harness.Published
                .SelectAsync<SerializedSagaIncremented>(
                    observation => observation.Context.Message.CorrelationId == sagaId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await using var verification = CreateContext(database.ConnectionString);
            SerializedState persisted = await verification.States.AsNoTracking()
                .SingleAsync(state => state.CorrelationId == sagaId, cancellationToken);

            Assert.Equal(1, incremented.Context.Message.Counter);
            Assert.Equal(1, persisted.Counter);
            Assert.Equal(2, handler.InvocationCount);
            Assert.Equal(2, transientFailure.SaveAttempts);
            Assert.Single(harness.Published.Snapshot<SerializedSagaIncremented>());
        }
        finally
        {
            handler.Release();
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-CONCURRENCY", "same-correlation-is-serialized-by-the-database-row-lock")]
    public async Task SameCorrelation_EntersOneHandlerAtATimeAndPersistsBothUpdatesAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
            "saga-serialization",
            cancellationToken);
        await using (var setup = CreateContext(database.ConnectionString))
            await setup.Database.EnsureCreatedAsync(cancellationToken);

        var handler = new HandlerProbe();
        var rowLocks = new RowLockProbe();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(handler)
            .AddSingleton(rowLocks)
            .AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddSagaStateMachine<SerializedStateMachine, SerializedState, SerializedStateDefinition>()
                    .EntityFrameworkRepository(repository =>
                    {
                        repository.UsePostgreSql();
                        repository.AddDbContext<DbContext, SerializedSagaDbContext>((services, builder) =>
                            builder.UseNpgsql(database.ConnectionString)
                                .AddInterceptors(services.GetRequiredService<RowLockProbe>()));
                    });
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid sagaId = Guid.NewGuid();
            ISendEndpoint endpoint = await harness.GetSagaEndpointAsync<SerializedState>(TestContext.Current.CancellationToken);
            await endpoint.SendAsync(new BeginSerializedSaga(sagaId), cancellationToken);
            await harness.Published
                .SelectAsync<SerializedSagaStarted>(
                    observation => observation.Context.Message.CorrelationId == sagaId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            rowLocks.StartObserving();

            await endpoint.SendAsync(new IncrementSerializedSaga(sagaId), cancellationToken);
            await handler.FirstInvocationEntered.WaitAsync(timeout, cancellationToken);
            await endpoint.SendAsync(new IncrementSerializedSaga(sagaId), cancellationToken);
            await rowLocks.WaitForAttemptsAsync(expectedCount: 2, timeout, cancellationToken);

            Assert.Equal(1, handler.InvocationCount);
            handler.Release();
            IPublishedMessage<SerializedSagaIncremented> completed = await harness.Published
                .SelectAsync<SerializedSagaIncremented>(
                    observation => observation.Context.Message.CorrelationId == sagaId
                        && observation.Context.Message.Counter == 2,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await using var verification = CreateContext(database.ConnectionString);
            SerializedState persisted = await verification.States.AsNoTracking()
                .SingleAsync(state => state.CorrelationId == sagaId, cancellationToken);
            Assert.Equal(2, completed.Context.Message.Counter);
            Assert.Equal(2, persisted.Counter);
            Assert.Equal(2, handler.InvocationCount);
            Assert.True(rowLocks.AttemptCount >= 2);
        }
        finally
        {
            handler.Release();
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static SerializedSagaDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<SerializedSagaDbContext>()
            .UseNpgsql(connectionString)
            .Options);

    public sealed record BeginSerializedSaga(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record IncrementSerializedSaga(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record SerializedSagaStarted(Guid CorrelationId);

    public sealed record SerializedSagaIncremented(Guid CorrelationId, int Counter);

    public sealed class SerializedState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public int Counter { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed class SerializedStateMachine : ViciOneServiceBusStateMachine<SerializedState>
    {
        public SerializedStateMachine(HandlerProbe handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            InstanceState(state => state.CurrentState);
            Event(() => Begin, configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            Event(() => Increment, configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            Initially(
                When(Begin)
                    .Publish(context => new SerializedSagaStarted(context.Saga.CorrelationId))
                    .TransitionTo(Active));
            During(Active,
                When(Increment)
                    .ThenAwaited(context => handler.EnterAsync(context.CancellationToken))
                    .Then(context => context.Saga.Counter++)
                    .Publish(context => new SerializedSagaIncremented(context.Saga.CorrelationId, context.Saga.Counter)));
        }

        public IState Active { get; private set; } = null!;

        public IEvent<BeginSerializedSaga> Begin { get; private set; } = null!;

        public IEvent<IncrementSerializedSaga> Increment { get; private set; } = null!;
    }

    private sealed class SerializedStateDefinition : SagaDefinition<SerializedState>
    {
        public SerializedStateDefinition()
        {
            ConcurrentMessageLimit = 2;
        }

        protected override void ConfigureSaga(
            IReceiveEndpointConfigurator endpointConfigurator,
            ISagaConfigurator<SerializedState> sagaConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.ConcurrentMessageLimit = 2;
            sagaConfigurator.UseMessageRetry(retry => retry.Immediate(2));
            sagaConfigurator.UseVolatileOutbox(context);
        }
    }

    public sealed class SerializedSagaDbContext(DbContextOptions<SerializedSagaDbContext> options) : DbContext(options)
    {
        public DbSet<SerializedState> States => Set<SerializedState>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SerializedState>(entity =>
            {
                entity.ToTable("SerializedSagas");
                entity.HasKey(state => state.CorrelationId);
                entity.Property(state => state.CurrentState).HasMaxLength(64);
            });
        }
    }

    public sealed class HandlerProbe
    {
        private readonly TaskCompletionSource _firstInvocationEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _invocationCount;

        public Task FirstInvocationEntered => _firstInvocationEntered.Task;

        public int InvocationCount => Volatile.Read(ref _invocationCount);

        public async Task EnterAsync(CancellationToken cancellationToken)
        {
            int invocation = Interlocked.Increment(ref _invocationCount);
            if (invocation != 1)
                return;

            _firstInvocationEntered.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
        }

        public void Release() => _release.TrySetResult();
    }

    public sealed class RowLockProbe : DbCommandInterceptor
    {
        private readonly Channel<int> _attempts = Channel.CreateUnbounded<int>();
        private int _attemptCount;
        private int _isObserving;

        public int AttemptCount => Volatile.Read(ref _attemptCount);

        public void StartObserving() => Volatile.Write(ref _isObserving, 1);

        public async Task WaitForAttemptsAsync(int expectedCount, TimeSpan timeout, CancellationToken cancellationToken)
        {
            while (Volatile.Read(ref _attemptCount) < expectedCount)
                await _attempts.Reader.ReadAsync(cancellationToken).AsTask().WaitAsync(timeout, cancellationToken);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled<global::Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<global::System.Data.Common.DbDataReader>>(cancellationToken); if (Volatile.Read(ref _isObserving) == 1
                            && command.CommandText.Contains("SerializedSagas", StringComparison.Ordinal)
                            && command.CommandText.Contains("FOR UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                int attempt = Interlocked.Increment(ref _attemptCount);
                if (!_attempts.Writer.TryWrite(attempt))
                    throw new InvalidOperationException("The row-lock observation channel rejected an attempt.");
            }

            return ValueTask.FromResult(result);
        }
    }

    public sealed class TransientSagaSaveProbe : SaveChangesInterceptor
    {
        private int _isObserving;
        private int _saveAttempts;

        public int SaveAttempts => Volatile.Read(ref _saveAttempts);

        public void StartObserving() => Volatile.Write(ref _isObserving, 1);

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled<global::Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>>(cancellationToken); if (Volatile.Read(ref _isObserving) == 0
                            || eventData.Context?.ChangeTracker.Entries<SerializedState>()
                                .Any(entry => entry.State == EntityState.Modified) != true)
                return ValueTask.FromResult(result);

            if (Interlocked.Increment(ref _saveAttempts) == 1)
            {
                throw new PostgresException(
                    "Test-owned serialization failure after the saga handler mutated tracked state.",
                    "ERROR",
                    "ERROR",
                    PostgresErrorCodes.SerializationFailure);
            }

            return ValueTask.FromResult(result);
        }
    }
}
