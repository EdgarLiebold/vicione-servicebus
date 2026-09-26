using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ViciOne.ServiceBus.EntityFrameworkCore;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Saga;

public sealed class EntityFrameworkSagaRollbackFailureTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EF-SAGA-FACTORY", "rollback-failure-preserves-primary-error-and-releases-context")]
    public async Task FailedRollback_PreservesPrimaryErrorAndReleasesOwnedContextAsync(bool query)
    {
        CancellationToken testToken = TestContext.Current.CancellationToken;
        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(testToken);
        var primary = new InvalidOperationException("The Saga SELECT failed.");
        var secondary = new InvalidOperationException("The provider rejected rollback.");
        var evidence = new FailureEvidence(primary, secondary);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(testToken);
        var options = new DbContextOptionsBuilder<TrackedContext>()
            .UseSqlite(connection)
            .AddInterceptors(new SelectFailureInterceptor(evidence), new RollbackFailureInterceptor(evidence))
            .Options;
        Guid firstId = Guid.Parse("71290000-0000-0000-0000-000000000001");
        Guid secondId = Guid.Parse("71290000-0000-0000-0000-000000000002");
        await using (var seed = new TrackedContext(options))
        {
            await seed.Database.EnsureCreatedAsync(testToken);
            seed.AddRange(new RollbackSaga { CorrelationId = firstId, Value = "Grüße", Revision = 17 },
                new RollbackSaga { CorrelationId = secondId, Value = "neighbor", Revision = 29 });
            await seed.SaveChangesAsync(testToken);
        }

        var contexts = new List<TrackedContext>();
        ISagaRepository<RollbackSaga> repository = EntityFrameworkSagaRepository<RollbackSaga>.CreateOptimistic(() =>
        {
            var context = new TrackedContext(options);
            contexts.Add(context);
            return context;
        }, isTransactionEnabled: true);
        evidence.Armed = true;

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ExecuteAsync().WaitAsync(Timeout, testToken));

        Assert.Same(primary, actual);
        TrackedContext failedContext = Assert.Single(contexts);
        Assert.Equal(1, failedContext.DisposeCalls);
        TransactionObservation started = Assert.Single(evidence.Started);
        Assert.Same(failedContext, started.Context);
        CommandObservation failedSelect = Assert.Single(evidence.FailedSelects);
        Assert.Same(failedContext, failedSelect.Context);
        Assert.Same(started.Transaction, failedSelect.Transaction);
        Assert.Contains("RollbackSagas", failedSelect.Sql, StringComparison.Ordinal);
        Assert.Equal(operationCancellation.Token, failedSelect.Token);
        TransactionObservation rollback = Assert.Single(evidence.Rollbacks);
        Assert.Same(failedContext, rollback.Context);
        Assert.Same(started.Transaction, rollback.Transaction);
        Assert.Equal(started.TransactionId, rollback.TransactionId);
        Assert.False(rollback.Token.CanBeCanceled);
        Assert.Empty(evidence.Commits);

        evidence.Armed = false;
        Guid[] ids = (await ((IQuerySagaRepository<RollbackSaga>)repository)
            .FindAsync(new SagaQuery<RollbackSaga>(_ => true), testToken).WaitAsync(Timeout, testToken)).ToArray();
        Assert.Equal(new[] { firstId, secondId }, ids.Order());
        RollbackSaga? first = await ((ILoadSagaRepository<RollbackSaga>)repository).LoadAsync(firstId, testToken)
            .WaitAsync(Timeout, testToken);
        RollbackSaga? second = await ((ILoadSagaRepository<RollbackSaga>)repository).LoadAsync(secondId, testToken)
            .WaitAsync(Timeout, testToken);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal((firstId, "Grüße", 17), (first.CorrelationId, first.Value, first.Revision));
        Assert.Equal((secondId, "neighbor", 29), (second.CorrelationId, second.Value, second.Revision));
        Assert.Equal(4, contexts.Count);
        Assert.All(contexts, context => Assert.Equal(1, context.DisposeCalls));

        async Task ExecuteAsync()
        {
            if (query)
                await ((IQuerySagaRepository<RollbackSaga>)repository).FindAsync(
                    new SagaQuery<RollbackSaga>(_ => true), operationCancellation.Token);
            else
                await ((ILoadSagaRepository<RollbackSaga>)repository).LoadAsync(firstId, operationCancellation.Token);
        }
    }

    public sealed class RollbackSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
        public string Value { get; set; } = "";
        public int Revision { get; set; }
    }

    private sealed class TrackedContext(DbContextOptions<TrackedContext> options) : DbContext(options)
    {
        public int DisposeCalls { get; private set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RollbackSaga>().ToTable("RollbackSagas").HasKey(x => x.CorrelationId);
        }

        public override async ValueTask DisposeAsync()
        {
            DisposeCalls++;
            await base.DisposeAsync();
        }
    }

    private sealed record TransactionObservation(DbContext? Context, DbTransaction Transaction,
        Guid TransactionId, CancellationToken Token);

    private sealed record CommandObservation(DbContext? Context, DbTransaction? Transaction,
        string Sql, CancellationToken Token);

    private sealed class FailureEvidence(Exception primary, Exception secondary)
    {
        public bool Armed { get; set; }
        public Exception Primary { get; } = primary;
        public Exception Secondary { get; } = secondary;
        public List<TransactionObservation> Started { get; } = [];
        public List<TransactionObservation> Rollbacks { get; } = [];
        public List<TransactionObservation> Commits { get; } = [];
        public List<CommandObservation> FailedSelects { get; } = [];
    }

    private sealed class SelectFailureInterceptor(FailureEvidence evidence) : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (evidence.Armed && command.CommandText.Contains("RollbackSagas", StringComparison.Ordinal))
            {
                evidence.FailedSelects.Add(new CommandObservation(eventData.Context, command.Transaction, command.CommandText, cancellationToken));
                return ValueTask.FromException<InterceptionResult<DbDataReader>>(evidence.Primary);
            }
            return ValueTask.FromResult(result);
        }
    }

    private sealed class RollbackFailureInterceptor(FailureEvidence evidence) : DbTransactionInterceptor
    {
        public override ValueTask<DbTransaction> TransactionStartedAsync(DbConnection connection,
            TransactionEndEventData eventData, DbTransaction result, CancellationToken cancellationToken = default)
        {
            if (evidence.Armed)
                evidence.Started.Add(new TransactionObservation(eventData.Context, result, eventData.TransactionId, cancellationToken));
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult> TransactionRollingBackAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!evidence.Armed)
                return ValueTask.FromResult(result);
            evidence.Rollbacks.Add(new TransactionObservation(eventData.Context, transaction, eventData.TransactionId, cancellationToken));
            return ValueTask.FromException<InterceptionResult>(evidence.Secondary);
        }

        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (evidence.Armed)
                evidence.Commits.Add(new TransactionObservation(eventData.Context, transaction, eventData.TransactionId, cancellationToken));
            return ValueTask.FromResult(result);
        }
    }
}
