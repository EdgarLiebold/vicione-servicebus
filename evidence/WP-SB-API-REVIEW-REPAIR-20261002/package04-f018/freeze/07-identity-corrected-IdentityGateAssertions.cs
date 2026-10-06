// Package-only Research draft. Parent must read, restore, build and prove all four cases.
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.EntityFrameworkCore;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Sagas.Configuration;

namespace ViciOneReview.F018.Gate;

public sealed class EntityFrameworkSagaTransactionOwnershipRegressionTests(TimeSpan operationTimeout, CancellationToken testToken)
{
    public Task Gate_WithAnActualCurrentTransactionAsync(bool query, bool foreignMarker) => RunAsync(query, foreignMarker);

    private async Task RunAsync(bool query, bool foreignMarker)
    {
        string root = Path.Combine(Path.GetTempPath(), "vsb-f018-identity-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string connection = $"Data Source={Path.Combine(root, "owned.db")};Pooling=False";
            var evidence = new Evidence(fail: false);
            var options = Options(connection, evidence);
            Guid selectedId = Guid.NewGuid();
            Guid neighborId = Guid.NewGuid();
            Guid selectedVersion = Guid.NewGuid();
            Guid neighborVersion = Guid.NewGuid();
            await using (var init = new TransactionDb(options))
            {
                await init.Database.EnsureCreatedAsync(testToken);
                if (query)
                    init.QuerySagas.AddRange(new QuerySaga { CorrelationId = selectedId, Version = selectedVersion },
                        new QuerySaga { CorrelationId = neighborId, Value = 71, Version = neighborVersion });
                else
                    init.SendSagas.AddRange(new SendSaga { CorrelationId = selectedId, Version = selectedVersion },
                        new SendSaga { CorrelationId = neighborId, Value = 71, Version = neighborVersion });
                await init.SaveChangesAsync(testToken);
            }
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<TransactionDb>(builder => builder.UseSqlite(connection)
                .AddInterceptors(new ObserveSave(evidence), new ObserveCommand(evidence)));
            string queue = "owned-f018-gate-" + Guid.NewGuid().ToString("N");
            services.AddViciOneServiceBus(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.ConfigureEntityFrameworkTransactionalStore<TransactionDb>(outbox =>
                { outbox.UseSqlite(); outbox.DisableInboxCleanupService(); });
                if (query)
                    configuration.AddSaga<QuerySaga>().EntityFrameworkRepository(repository =>
                    {
                        repository.UseSqlite(); repository.SetOptimisticConcurrency(useTransaction: true);
                        repository.IsolationLevel = IsolationLevel.Serializable;
                        repository.UseExistingDbContext<TransactionDb>();
                    });
                else
                    configuration.AddSaga<SendSaga>().EntityFrameworkRepository(repository =>
                    {
                        repository.UseSqlite(); repository.SetOptimisticConcurrency(useTransaction: true);
                        repository.IsolationLevel = IsolationLevel.Serializable;
                        repository.UseExistingDbContext<TransactionDb>();
                    });
                configuration.UsingInMemory((context, transport) => transport.ReceiveEndpoint(queue, endpoint =>
                {
                    // Public SPI alternative to UseEntityFrameworkOutbox for this saga-only endpoint:
                    // one observer appends the same actual outbox filter first and the gate second.
                    var actualOutbox = new OutboxConsumePipeSpecificationObserver<TransactionDb>(endpoint, context);
                    endpoint.ConnectSagaConfigurationObserver(new GateObserver(actualOutbox, evidence, foreignMarker));
                    if (query) endpoint.ConfigureSaga<QuerySaga>(context);
                    else endpoint.ConfigureSaga<SendSaga>(context);
                }));
            });
            await using (ServiceProvider provider = services.BuildServiceProvider(
                new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }))
            {
                IBusControl bus = provider.GetRequiredService<IBusControl>();
                var observer = new ObserveReceive();
                using var handle = bus.ConnectReceiveObserver(observer);
                evidence.Armed = true;
                try
                {
                    await bus.StartAsync(testToken).WaitAsync(operationTimeout, testToken);
                    ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri("queue:" + queue), testToken);
                    await endpoint.SendAsync(new Update(selectedId), testToken).WaitAsync(operationTimeout, testToken);
                    await observer.Done.Task.WaitAsync(operationTimeout, testToken);
                }
                finally
                {
                    try { await bus.StopAsync(CancellationToken.None).WaitAsync(operationTimeout); }
                    finally { evidence.Armed = false; }
                }
                GateEvidence gate = Assert.Single(evidence.Gates);
                Assert.NotNull(gate.BeforeTransaction);
                Assert.Equal(gate.ActualTransactionId, gate.ParentMarkerId);
                Assert.Same(gate.BeforeTransaction, gate.AfterTransaction);
                Assert.Equal(gate.ActualTransactionId, gate.AfterTransactionId);
                Assert.Equal(foreignMarker, gate.LocalMarkerId != gate.ActualTransactionId);
                if (foreignMarker)
                {
                    // Original marker-only / actual-transaction-exists-only implementations
                    // finish the saga normally: GateFailure remains null and this assertion fails.
                    Exception rejection = Assert.IsType<InvalidOperationException>(evidence.GateFailure);
                    Assert.NotNull(rejection.StackTrace);
                    Assert.Contains("BeginTransactionAsync", rejection.StackTrace, StringComparison.Ordinal);
                    Assert.NotEmpty(observer.Faults);
                    Assert.All(observer.Faults, failure => Assert.True(ContainsExact(failure, rejection)));
                    Assert.Empty(evidence.Commands);
                    Assert.Empty(evidence.Saves);
                }
                else
                {
                    Assert.Null(evidence.GateFailure);
                    Assert.Empty(observer.Faults);
                    SaveEvidence saga = Assert.Single(evidence.Saves.Where(x => x.Kind == "saga"));
                    SaveEvidence consumed = Assert.Single(evidence.Saves.Where(x => x.Kind == "consumed"));
                    Assert.Same(gate.Context, saga.Context);
                    Assert.Same(gate.BeforeTransaction, saga.Transaction);
                    Assert.Same(saga.Context, consumed.Context);
                    Assert.Same(saga.Transaction, consumed.Transaction);
                    Assert.Equal(gate.ActualTransactionId, saga.TransactionId);
                    Assert.Equal(gate.ActualTransactionId, consumed.TransactionId);
                    Assert.Single(evidence.Commands.Where(x => x.Kind == "SELECT"));
                    Assert.Single(evidence.Commands.Where(x => x.Kind == "UPDATE"));
                    Assert.All(evidence.Commands, command =>
                    { Assert.Same(gate.Context, command.Context); Assert.Same(gate.BeforeTransaction, command.Transaction); });
                }
                await using var check = new TransactionDb(options);
                (Guid Id, int Value, Guid Version)[] states = query
                    ? (await check.QuerySagas.AsNoTracking().ToArrayAsync(testToken)).Select(x => (x.CorrelationId, x.Value, x.Version)).ToArray()
                    : (await check.SendSagas.AsNoTracking().ToArrayAsync(testToken)).Select(x => (x.CorrelationId, x.Value, x.Version)).ToArray();
                Assert.Equal(2, states.Length);
                var selected = Assert.Single(states.Where(x => x.Id == selectedId));
                var neighbor = Assert.Single(states.Where(x => x.Id == neighborId));
                Assert.Equal(foreignMarker ? 0 : 1, selected.Value);
                if (foreignMarker) Assert.Equal(selectedVersion, selected.Version);
                else Assert.NotEqual(selectedVersion, selected.Version);
                Assert.Equal(71, neighbor.Value); Assert.Equal(neighborVersion, neighbor.Version);
                InboxState inbox = Assert.Single(await check.Set<InboxState>().AsNoTracking().ToArrayAsync(testToken));
                if (foreignMarker) Assert.Null(inbox.Consumed); else Assert.NotNull(inbox.Consumed);
            }
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private sealed class GateObserver(ISagaConfigurationObserver actualOutbox, Evidence evidence, bool foreignMarker)
        : ISagaConfigurationObserver
    {
        public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator) where TSaga : class =>
            actualOutbox.SagaConfigured(configurator);
        public void StateMachineSagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator, object stateMachine) where TSaga : class =>
            actualOutbox.StateMachineSagaConfigured(configurator, stateMachine);
        public void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
            where TSaga : class where TMessage : class
        {
            actualOutbox.SagaMessageConfigured(configurator);
            ISagaMessageConfigurator<TMessage> message = Assert.IsAssignableFrom<ISagaMessageConfigurator<TMessage>>(configurator);
            message.UseFilter(new GateFilter<TMessage>(evidence, foreignMarker));
        }
    }

    private sealed record Marker(Guid TransactionId) : IDbTransactionContext;
    private sealed record GateEvidence(DbContext Context, DbTransaction BeforeTransaction, Guid ActualTransactionId,
        Guid ParentMarkerId, Guid LocalMarkerId, DbTransaction? AfterTransaction, Guid? AfterTransactionId);

    private sealed class GateFilter<TMessage>(Evidence evidence, bool foreignMarker) : IFilter<ConsumeContext<TMessage>>
        where TMessage : class
    {
        public void Probe(ProbeContext context) => context.CreateFilterScope("f018-public-identity-gate-control");
        public async Task SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
        {
            if (!context.TryGetPayload<IServiceScope>(out IServiceScope? serviceScope))
                throw new InvalidOperationException("Gate setup did not receive an actual owned DI service scope.");
            TransactionDb actual = serviceScope.ServiceProvider.GetRequiredService<TransactionDb>();
            IDbContextTransaction active = Assert.IsAssignableFrom<IDbContextTransaction>(actual.Database.CurrentTransaction);
            DbTransaction physical = active.GetDbTransaction();
            if (!context.TryGetPayload<IDbTransactionContext>(out IDbTransactionContext? parentMarker))
                throw new InvalidOperationException("Gate setup did not receive the actual outbox transaction marker.");
            Assert.Equal(active.TransactionId, parentMarker.TransactionId);
            Guid chosen = active.TransactionId;
            if (foreignMarker)
                do { chosen = Guid.NewGuid(); } while (chosen == active.TransactionId);
            var marker = new Marker(chosen);
            var scoped = new ConsumeContextScope<TMessage>(context);
            IDbTransactionContext installed = scoped.AddOrUpdatePayload<IDbTransactionContext>(() => marker, _ => marker);
            Assert.Same(marker, installed);
            if (!scoped.TryGetPayload<IDbTransactionContext>(out IDbTransactionContext? readBack))
                throw new InvalidOperationException("The public scope did not expose its installed local marker.");
            Assert.Same(marker, readBack);
            Assert.True(context.TryGetPayload<IDbTransactionContext>(out IDbTransactionContext? parentAfter));
            Assert.Same(parentMarker, parentAfter);
            Assert.Same(active, actual.Database.CurrentTransaction);
            try { await next.SendAsync(scoped).ConfigureAwait(false); }
            catch (Exception exception) { evidence.GateFailure = exception; throw; }
            finally
            {
                evidence.Gates.Enqueue(new(actual, physical, active.TransactionId, parentMarker.TransactionId,
                    readBack.TransactionId, actual.Database.CurrentTransaction?.GetDbTransaction(), actual.Database.CurrentTransaction?.TransactionId));
            }
        }
    }

    private static DbContextOptions<TransactionDb> Options(string connection, Evidence evidence) =>
        new DbContextOptionsBuilder<TransactionDb>().UseSqlite(connection)
            .AddInterceptors(new ObserveSave(evidence), new ObserveCommand(evidence)).Options;

    private static bool ContainsExact(Exception actual, Exception expected) =>
        ReferenceEquals(actual, expected)
        || actual.InnerException is not null && ContainsExact(actual.InnerException, expected)
        || actual is AggregateException aggregate && aggregate.InnerExceptions.Any(x => ContainsExact(x, expected));

    public sealed record Update(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class SendSaga : ISaga, IOrchestrates<Update>
    {
        public Guid CorrelationId { get; set; }
        public int Value { get; set; }
        public Guid Version { get; set; }
        public Task ConsumeAsync(ConsumeContext<Update> context)
        {
            Assert.Equal(CorrelationId, context.Message.CorrelationId);
            Value++;
            Version = Guid.NewGuid();
            return Task.CompletedTask;
        }
    }

    public sealed class QuerySaga : ISaga, IObserves<Update, QuerySaga>
    {
        public Guid CorrelationId { get; set; }
        public int Value { get; set; }
        public Guid Version { get; set; }
        public Expression<Func<QuerySaga, Update, bool>> CorrelationExpression =>
            (saga, message) => saga.CorrelationId == message.CorrelationId;
        public Task ConsumeAsync(ConsumeContext<Update> context)
        {
            Assert.Equal(CorrelationId, context.Message.CorrelationId);
            Value++;
            Version = Guid.NewGuid();
            return Task.CompletedTask;
        }
    }

    public sealed class TransactionDb(DbContextOptions<TransactionDb> options) : DbContext(options)
    {
        public DbSet<SendSaga> SendSagas => Set<SendSaga>();
        public DbSet<QuerySaga> QuerySagas => Set<QuerySaga>();
        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<SendSaga>().ToTable("F018SendSaga");
            model.Entity<SendSaga>().HasKey(x => x.CorrelationId);
            model.Entity<SendSaga>().Property(x => x.Version).IsConcurrencyToken().ValueGeneratedNever();
            model.Entity<QuerySaga>().ToTable("F018QuerySaga");
            model.Entity<QuerySaga>().HasKey(x => x.CorrelationId);
            model.Entity<QuerySaga>().Property(x => x.Version).IsConcurrencyToken().ValueGeneratedNever();
            model.Entity<QuerySaga>().Ignore(x => x.CorrelationExpression);
            model.AddTransactionalOutboxEntities();
        }
    }

    private sealed record CommandEvidence(string Kind, DbContext Context, DbTransaction? Transaction);
    private sealed record SaveEvidence(string Kind, DbContext Context, DbTransaction? Transaction,
        Guid? TransactionId, IsolationLevel? IsolationLevel);

    private sealed class Evidence(bool fail)
    {
        public bool Armed { get; set; }
        public InvalidOperationException Primary { get; } = new("f018-owned-inbox-consumed-save-failure");
        public ConcurrentQueue<SaveEvidence> Saves { get; } = new();
        public ConcurrentQueue<CommandEvidence> Commands { get; } = new();
        public int Failures;
        public ConcurrentQueue<GateEvidence> Gates { get; } = new();
        public Exception? GateFailure;
        public void Observe(DbContext context)
        {
            if (!Armed) return;
            bool saga = context.ChangeTracker.Entries<SendSaga>().Any(x => x.State == EntityState.Modified)
                || context.ChangeTracker.Entries<QuerySaga>().Any(x => x.State == EntityState.Modified);
            bool consumed = context.ChangeTracker.Entries<InboxState>().Any(x => x.State == EntityState.Modified
                && x.Entity.Consumed.HasValue && x.Property(y => y.Consumed).OriginalValue == null);
            IDbContextTransaction? transaction = context.Database.CurrentTransaction;
            DbTransaction? physical = transaction?.GetDbTransaction();
            if (saga) Saves.Enqueue(new("saga", context, physical, transaction?.TransactionId, physical?.IsolationLevel));
            if (consumed)
            {
                Saves.Enqueue(new("consumed", context, physical, transaction?.TransactionId, physical?.IsolationLevel));
                if (fail) { Interlocked.Increment(ref Failures); throw Primary; }
            }
        }
    }

    private sealed class ObserveSave(Evidence evidence) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data,
            InterceptionResult<int> result, CancellationToken token = default)
        {
            evidence.Observe(data.Context!);
            return ValueTask.FromResult(result);
        }
    }

    private sealed class ObserveCommand(Evidence evidence) : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData data, InterceptionResult<DbDataReader> result, CancellationToken token = default)
        {
            if (evidence.Armed && (command.CommandText.Contains("\"F018SendSaga\"", StringComparison.Ordinal)
                || command.CommandText.Contains("\"F018QuerySaga\"", StringComparison.Ordinal)))
            {
                string kind = command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.Ordinal) ? "SELECT" : "UPDATE";
                evidence.Commands.Enqueue(new(kind, data.Context!, command.Transaction));
            }
            return ValueTask.FromResult(result);
        }
    }

    private sealed class ObserveReceive : IReceiveObserver
    {
        public TaskCompletionSource Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ConcurrentQueue<Exception> Faults { get; } = new();
        public Task PreReceiveAsync(ReceiveContext context) => Task.CompletedTask;
        public Task PostReceiveAsync(ReceiveContext context) { Done.TrySetResult(); return Task.CompletedTask; }
        public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType) where T : class => Task.CompletedTask;
        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception) where T : class
        { Faults.Enqueue(exception); return Task.CompletedTask; }
        public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
        { Faults.Enqueue(exception); Done.TrySetResult(); return Task.CompletedTask; }
    }
}
