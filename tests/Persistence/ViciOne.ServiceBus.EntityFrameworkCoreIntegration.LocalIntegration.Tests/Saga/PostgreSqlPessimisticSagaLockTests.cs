using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests.Saga;

public sealed class PostgreSqlPessimisticSagaLockTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-POSTGRES-PESSIMISTIC-LOCK", "competing-session-blocks-until-owner-releases")]
    public async Task RowLock_BlocksACompetingSessionAndReleasesWithTheOwningTransactionAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
            "pessimistic-saga-lock",
            cancellationToken);
        DbContextOptions<PessimisticSagaDbContext> options = new DbContextOptionsBuilder<PessimisticSagaDbContext>()
            .UseNpgsql(database.ConnectionString)
            .Options;
        Guid sagaId = Guid.NewGuid();
        await using (var setup = new PessimisticSagaDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync(cancellationToken);
            setup.Sagas.Add(new PessimisticSaga { CorrelationId = sagaId, Value = 7 });
            await setup.SaveChangesAsync(cancellationToken);
        }

        var executor = new PessimisticLoadQueryExecutor<PessimisticSaga>(
            new PostgresLockStatementProvider(),
            queryCustomization: null);
        await using var owner = new PessimisticSagaDbContext(options);
        await owner.Database.OpenConnectionAsync(cancellationToken);
        await using var ownerTransaction = await owner.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        PessimisticSaga ownedSaga = Assert.IsType<PessimisticSaga>(
            await executor.LoadAsync(owner, sagaId, cancellationToken));

        InvalidOperationException blocked;
        await using (var competitor = new PessimisticSagaDbContext(options))
        {
            await competitor.Database.OpenConnectionAsync(cancellationToken);
            Assert.NotEqual(
                ((NpgsqlConnection)owner.Database.GetDbConnection()).ProcessID,
                ((NpgsqlConnection)competitor.Database.GetDbConnection()).ProcessID);
            await using var competitorTransaction = await competitor.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);
            await competitor.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '500ms'", cancellationToken);

            blocked = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                executor.LoadAsync(competitor, sagaId, cancellationToken));
        }

        PostgresException providerFailure = Assert.IsType<PostgresException>(blocked.InnerException);
        Assert.Equal(PostgresErrorCodes.LockNotAvailable, providerFailure.SqlState);
        Assert.Equal(7, ownedSaga.Value);
        await ownerTransaction.CommitAsync(cancellationToken);

        await using var afterRelease = new PessimisticSagaDbContext(options);
        await afterRelease.Database.OpenConnectionAsync(cancellationToken);
        await using var afterReleaseTransaction = await afterRelease.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        PessimisticSaga releasedSaga = Assert.IsType<PessimisticSaga>(
            await executor.LoadAsync(afterRelease, sagaId, cancellationToken));

        Assert.Equal(sagaId, releasedSaga.CorrelationId);
        Assert.Equal(7, releasedSaga.Value);
    }

    public sealed class PessimisticSaga : ISaga
    {
        public Guid CorrelationId { get; set; }

        public int Value { get; set; }
    }

    private sealed class PessimisticSagaDbContext(DbContextOptions<PessimisticSagaDbContext> options) : DbContext(options)
    {
        public DbSet<PessimisticSaga> Sagas => Set<PessimisticSaga>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PessimisticSaga>(entity =>
            {
                entity.ToTable("PessimisticSagas");
                entity.HasKey(saga => saga.CorrelationId);
            });
        }
    }
}
