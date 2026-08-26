namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests.Saga;

using System.Data;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class PostgreSqlPessimisticSagaQueryCustomizationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-POSTGRES-PESSIMISTIC-QUERY", "locked-load-includes-and-updates-two-level-navigation")]
    public async Task LockedLoad_AppliesTheConfiguredNavigationGraphInsideTheTransaction()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
            "pessimistic-saga-query",
            cancellationToken);
        DbContextOptions<NavigationSagaDbContext> options = new DbContextOptionsBuilder<NavigationSagaDbContext>()
            .UseNpgsql(database.ConnectionString)
            .Options;
        Guid sagaId = Guid.NewGuid();
        await using (var setup = new NavigationSagaDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync(cancellationToken);
            setup.Sagas.Add(new NavigationSaga
            {
                CorrelationId = sagaId,
                Dependency = new SagaDependency
                {
                    InnerDependency = new SagaInnerDependency { Name = "before" },
                },
            });
            await setup.SaveChangesAsync(cancellationToken);
        }

        var executor = new PessimisticLoadQueryExecutor<NavigationSaga>(
            new PostgresLockStatementProvider(),
            query => query
                .Include(saga => saga.Dependency)
                .ThenInclude(dependency => dependency.InnerDependency));
        await using (var context = new NavigationSagaDbContext(options))
        {
            await context.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

            NavigationSaga loaded = Assert.IsType<NavigationSaga>(
                await executor.Load(context, sagaId, cancellationToken));
            Assert.Equal("before", loaded.Dependency.InnerDependency.Name);
            loaded.Dependency.InnerDependency.Name = "after";
            Assert.Equal(1, await context.SaveChangesAsync(cancellationToken));
            await transaction.CommitAsync(cancellationToken);
        }

        await using var verification = new NavigationSagaDbContext(options);
        string persistedName = await verification.InnerDependencies
            .AsNoTracking()
            .Where(inner => inner.Dependency!.Saga!.CorrelationId == sagaId)
            .Select(inner => inner.Name)
            .SingleAsync(cancellationToken);
        Assert.Equal("after", persistedName);
    }

    public sealed class NavigationSaga : ISaga
    {
        public Guid CorrelationId { get; set; }

        public SagaDependency Dependency { get; set; } = null!;
    }

    public sealed class SagaDependency
    {
        public int Id { get; set; }

        public NavigationSaga? Saga { get; set; }

        public Guid SagaCorrelationId { get; set; }

        public SagaInnerDependency InnerDependency { get; set; } = null!;
    }

    public sealed class SagaInnerDependency
    {
        public int Id { get; set; }

        public SagaDependency? Dependency { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class NavigationSagaDbContext(DbContextOptions<NavigationSagaDbContext> options) : DbContext(options)
    {
        public DbSet<SagaInnerDependency> InnerDependencies => Set<SagaInnerDependency>();

        public DbSet<NavigationSaga> Sagas => Set<NavigationSaga>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NavigationSaga>(entity =>
            {
                entity.ToTable("NavigationSagas");
                entity.HasKey(saga => saga.CorrelationId);
                entity.HasOne(saga => saga.Dependency)
                    .WithOne(dependency => dependency.Saga)
                    .HasForeignKey<SagaDependency>(dependency => dependency.SagaCorrelationId)
                    .IsRequired();
            });
            modelBuilder.Entity<SagaDependency>(entity =>
            {
                entity.ToTable("SagaDependencies");
                entity.HasKey(dependency => dependency.Id);
                entity.HasOne(dependency => dependency.InnerDependency)
                    .WithOne(inner => inner.Dependency)
                    .HasForeignKey<SagaInnerDependency>(inner => inner.Id)
                    .IsRequired();
            });
            modelBuilder.Entity<SagaInnerDependency>(entity =>
            {
                entity.ToTable("SagaInnerDependencies");
                entity.HasKey(inner => inner.Id);
                entity.Property(inner => inner.Name).HasMaxLength(40);
            });
        }
    }
}
