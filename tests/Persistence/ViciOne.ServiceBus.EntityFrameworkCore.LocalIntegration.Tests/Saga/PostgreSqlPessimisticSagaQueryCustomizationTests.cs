using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.EntityFrameworkCore.Saga;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.Saga;

public sealed class PostgreSqlPessimisticSagaQueryCustomizationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-POSTGRES-PESSIMISTIC-QUERY", "locked-load-includes-and-updates-two-level-navigation")]
    public async Task LockedLoad_AppliesTheConfiguredNavigationGraphInsideTheTransactionAsync()
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
            new PostgreSqlLockStatementProvider(),
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
                await executor.LoadAsync(context, sagaId, cancellationToken));
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

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-POSTGRES-PESSIMISTIC-QUERY", "repository-inserts-and-updates-required-navigation-graph")]
    public async Task Repository_PersistsAndReloadsTheRequiredNavigationGraphAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
            "pessimistic-saga-graph",
            cancellationToken);
        DbContextOptions<NavigationSagaDbContext> options = new DbContextOptionsBuilder<NavigationSagaDbContext>()
            .UseNpgsql(database.ConnectionString)
            .Options;
        await using (var setup = new NavigationSagaDbContext(options))
            await setup.Database.EnsureCreatedAsync(cancellationToken);

        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddSaga<NavigationSaga, NavigationSagaDefinition>()
                    .EntityFrameworkRepository(repository =>
                    {
                        repository.UsePostgreSql();
                        repository.CustomizeQuery(query => query
                            .Include(saga => saga.Dependency)
                            .ThenInclude(dependency => dependency.InnerDependency));
                        repository.AddDbContext<DbContext, NavigationSagaDbContext>((_, builder) =>
                            builder.UseNpgsql(database.ConnectionString));
                    });
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid sagaId = Guid.NewGuid();
            ISendEndpoint endpoint = await harness.GetSagaEndpointAsync<NavigationSaga>(TestContext.Current.CancellationToken);
            await endpoint.SendAsync(new CreateNavigationSaga(sagaId, "created"), cancellationToken);
            await harness.Published.SelectAsync<NavigationSagaCreated>(
                    observation => observation.Context.Message.CorrelationId == sagaId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await using (var inserted = new NavigationSagaDbContext(options))
            {
                NavigationSaga graph = await inserted.Sagas.AsNoTracking()
                    .Include(saga => saga.Dependency)
                    .ThenInclude(dependency => dependency.InnerDependency)
                    .SingleAsync(saga => saga.CorrelationId == sagaId, cancellationToken);
                Assert.Equal("created", graph.Dependency.InnerDependency.Name);
            }

            await endpoint.SendAsync(new UpdateNavigationSaga(sagaId, "updated"), cancellationToken);
            IPublishedMessage<NavigationSagaUpdated> updated = await harness.Published
                .SelectAsync<NavigationSagaUpdated>(
                    observation => observation.Context.Message.CorrelationId == sagaId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await using var verification = new NavigationSagaDbContext(options);
            string persistedName = await verification.InnerDependencies.AsNoTracking()
                .Where(inner => inner.Dependency!.Saga!.CorrelationId == sagaId)
                .Select(inner => inner.Name)
                .SingleAsync(cancellationToken);

            Assert.Equal("updated", updated.Context.Message.Name);
            Assert.Equal("updated", persistedName);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    public sealed record CreateNavigationSaga(Guid CorrelationId, string Name) : CorrelatedBy<Guid>;

    public sealed record UpdateNavigationSaga(Guid CorrelationId, string Name) : CorrelatedBy<Guid>;

    public sealed record NavigationSagaCreated(Guid CorrelationId);

    public sealed record NavigationSagaUpdated(Guid CorrelationId, string Name);

    public sealed class NavigationSaga :
        ISaga,
        InitiatedBy<CreateNavigationSaga>,
        Orchestrates<UpdateNavigationSaga>
    {
        public Guid CorrelationId { get; set; }

        public SagaDependency Dependency { get; set; } = null!;

        public Task ConsumeAsync(ConsumeContext<CreateNavigationSaga> context)
        {
            Dependency = new SagaDependency
            {
                InnerDependency = new SagaInnerDependency { Name = context.Message.Name },
            };
            return context.Advanced().PublishAsync(new NavigationSagaCreated(CorrelationId), context.CancellationToken);
        }

        public Task ConsumeAsync(ConsumeContext<UpdateNavigationSaga> context)
        {
            Dependency.InnerDependency.Name = context.Message.Name;
            return context.Advanced().PublishAsync(new NavigationSagaUpdated(CorrelationId, context.Message.Name), context.CancellationToken);
        }
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

    private sealed class NavigationSagaDefinition : SagaDefinition<NavigationSaga>
    {
        protected override void ConfigureSaga(
            IReceiveEndpointConfigurator endpointConfigurator,
            ISagaConfigurator<NavigationSaga> sagaConfigurator,
            IRegistrationContext context)
        {
            sagaConfigurator.UseMessageRetry(retry => retry.Immediate(2));
            sagaConfigurator.UseVolatileOutbox(context);
        }
    }

    public sealed class NavigationSagaDbContext(DbContextOptions<NavigationSagaDbContext> options) : DbContext(options)
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
