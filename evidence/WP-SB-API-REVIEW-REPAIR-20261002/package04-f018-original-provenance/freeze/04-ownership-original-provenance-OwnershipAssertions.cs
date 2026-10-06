// Package-only Research draft. Parent must read, restore, build and prove all twelve cases.
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.EntityFrameworkCore;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Sagas.Configuration;

namespace ViciOneReview.F018;

public sealed class EntityFrameworkSagaTransactionOwnershipRegressionTests(TimeSpan operationTimeout, CancellationToken testToken)
{
    public Task Send_WithAnOutboxMarker_UsesTheActualSagaTransactionAsync(bool separate, bool failInboxSave) =>
        RunAsync(query: false, separate, failInboxSave, useTransaction: true);

    public Task Query_WithAnOutboxMarker_UsesTheActualSagaTransactionAsync(bool separate, bool failInboxSave) =>
        RunAsync(query: true, separate, failInboxSave, useTransaction: true);

    public Task SeparateContext_WithOwnTransactionDisabled_PreservesTheExplicitOptOutAsync(bool query, bool failInboxSave) =>
        RunAsync(query, separate: true, failInboxSave, useTransaction: false);

    private async Task RunAsync(bool query, bool separate, bool failInboxSave, bool useTransaction)
    {
        string root = Path.Combine(Path.GetTempPath(), "vsb-f018-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string inboxConnection = $"Data Source={Path.Combine(root, "inbox.db")};Pooling=False";
            string sagaConnection = separate ? $"Data Source={Path.Combine(root, "saga.db")};Pooling=False" : inboxConnection;
            var evidence = new Evidence(failInboxSave);
            var inboxOptions = Options(inboxConnection, evidence);
            var sagaOptions = Options(sagaConnection, evidence);
            Guid selectedId = Guid.NewGuid();
            Guid neighborId = Guid.NewGuid();
            Guid selectedVersion = Guid.NewGuid();
            Guid neighborVersion = Guid.NewGuid();
            await using (var init = new TransactionDb(inboxOptions))
                await init.Database.EnsureCreatedAsync(testToken);
            await using (var init = new TransactionDb(sagaOptions))
            {
                if (separate)
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
            services.AddDbContext<TransactionDb>(builder =>
                builder.UseSqlite(inboxConnection).AddInterceptors(new ObserveSave(evidence), new ObserveCommand(evidence)));
            string queue = "owned-f018-" + Guid.NewGuid().ToString("N");
            services.AddViciOneServiceBus(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.ConfigureEntityFrameworkTransactionalStore<TransactionDb>(outbox =>
                {
                    outbox.UseSqlite();
                    outbox.DisableInboxCleanupService();
                });
                if (query)
                    configuration.AddSaga<QuerySaga>().EntityFrameworkRepository(repository =>
                    {
                        repository.UseSqlite();
                        repository.SetOptimisticConcurrency(useTransaction);
                        repository.IsolationLevel = IsolationLevel.Serializable;
                        if (separate) repository.UseDbContextFactory(() => new TransactionDb(sagaOptions));
                        else repository.UseExistingDbContext<TransactionDb>();
                    });
                else
                    configuration.AddSaga<SendSaga>().EntityFrameworkRepository(repository =>
                    {
                        repository.UseSqlite();
                        repository.SetOptimisticConcurrency(useTransaction);
                        repository.IsolationLevel = IsolationLevel.Serializable;
                        if (separate) repository.UseDbContextFactory(() => new TransactionDb(sagaOptions));
                        else repository.UseExistingDbContext<TransactionDb>();
                    });
                configuration.UsingInMemory((context, transport) => transport.ReceiveEndpoint(queue, endpoint =>
                {
                    endpoint.UseEntityFrameworkOutbox<TransactionDb>(context);
                    if (query) endpoint.ConfigureSaga<QuerySaga>(context);
                    else endpoint.ConfigureSaga<SendSaga>(context);
                }));
            });

            await using (ServiceProvider provider = services.BuildServiceProvider(
                new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }))
            {
                IBusControl bus = provider.GetRequiredService<IBusControl>();
                var observer = new ObserveReceive();
                using var observerHandle = bus.ConnectReceiveObserver(observer);
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

                SaveEvidence sagaSave = Assert.Single(evidence.Saves.Where(x => x.Kind == "saga"));
                SaveEvidence consumedSave = Assert.Single(evidence.Saves.Where(x => x.Kind == "consumed"));
                Assert.NotNull(consumedSave.Transaction);
                Assert.NotNull(consumedSave.TransactionId);
                Assert.Equal(failInboxSave ? 1 : 0, Volatile.Read(ref evidence.Failures));
                if (failInboxSave)
                {
                    Assert.NotEmpty(observer.Faults);
                    Assert.All(observer.Faults, exception => Assert.True(ContainsExact(exception, evidence.Primary)));
                }
                else
                    Assert.Empty(observer.Faults);
                if (separate)
                    Assert.NotSame(consumedSave.Context, sagaSave.Context);
                else
                {
                    Assert.Same(consumedSave.Context, sagaSave.Context);
                    Assert.Same(consumedSave.Transaction, sagaSave.Transaction);
                    Assert.Equal(consumedSave.TransactionId, sagaSave.TransactionId);
                }
                CommandEvidence[] commands = evidence.Commands.ToArray();
                Assert.Single(commands.Where(x => x.Kind == "SELECT"));
                Assert.Single(commands.Where(x => x.Kind == "UPDATE"));
                Assert.All(commands, command =>
                {
                    Assert.Same(sagaSave.Context, command.Context);
                    Assert.Same(sagaSave.Transaction, command.Transaction);
                });
                // An independent saga transaction commits before the later inbox failure.
                // This asserts local ownership, without claiming distributed atomicity.
                await using var checkSaga = new TransactionDb(sagaOptions);
                (Guid Id, int Value, Guid Version)[] states = query
                    ? (await checkSaga.QuerySagas.AsNoTracking().ToArrayAsync(testToken))
                        .Select(x => (x.CorrelationId, x.Value, x.Version)).ToArray()
                    : (await checkSaga.SendSagas.AsNoTracking().ToArrayAsync(testToken))
                        .Select(x => (x.CorrelationId, x.Value, x.Version)).ToArray();
                Assert.Equal(2, states.Length);
                var selected = Assert.Single(states.Where(x => x.Id == selectedId));
                var neighbor = Assert.Single(states.Where(x => x.Id == neighborId));
                Assert.Equal(failInboxSave && !separate ? 0 : 1, selected.Value);
                if (failInboxSave && !separate) Assert.Equal(selectedVersion, selected.Version);
                else Assert.NotEqual(selectedVersion, selected.Version);
                Assert.Equal(71, neighbor.Value);
                Assert.Equal(neighborVersion, neighbor.Version);
                await using var checkInbox = new TransactionDb(inboxOptions);
                InboxState[] inboxRows = await checkInbox.Set<InboxState>().AsNoTracking().ToArrayAsync(testToken);
                InboxState inbox = Assert.Single(inboxRows);
                if (failInboxSave) Assert.Null(inbox.Consumed);
                else Assert.NotNull(inbox.Consumed);

                // Last assertions isolate F018: the original separate context remains
                // operational but runs SELECT and UPDATE without an actual transaction.
                if (useTransaction)
                {
                    Assert.NotNull(sagaSave.Transaction);
                    Assert.NotNull(sagaSave.TransactionId);
                    Assert.Equal(IsolationLevel.Serializable, sagaSave.IsolationLevel);
                    Assert.All(commands, command => Assert.NotNull(command.Transaction));
                    if (separate)
                    {
                        Assert.NotSame(consumedSave.Transaction, sagaSave.Transaction);
                        Assert.NotEqual(consumedSave.TransactionId, sagaSave.TransactionId);
                    }
                }
                else
                {
                    Assert.Null(sagaSave.Transaction);
                    Assert.Null(sagaSave.TransactionId);
                    Assert.All(commands, command => Assert.Null(command.Transaction));
                }
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
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
