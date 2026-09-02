namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests;

using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class SqlLockStatementProviderTests
{
    [Theory]
    [InlineData("sql-server",
        "SELECT * FROM [north]]schema].[Order]]Archive] WITH (UPDLOCK, ROWLOCK) WHERE [Correlation]]Id] = @p0",
        "SELECT * FROM [south].[SouthOrder] WITH (UPDLOCK, ROWLOCK) WHERE [SagaId] = @p0")]
    [InlineData("postgresql",
        "SELECT *, xmin FROM \"north]schema\".\"Order]Archive\" WHERE \"Correlation]Id\" = @p0 FOR UPDATE",
        "SELECT *, xmin FROM \"south\".\"SouthOrder\" WHERE \"SagaId\" = @p0 FOR UPDATE")]
    [InlineData("sqlite",
        "SELECT * FROM \"Order]Archive\" WHERE \"Correlation]Id\" = @p0",
        "SELECT * FROM \"SouthOrder\" WHERE \"SagaId\" = @p0")]
    [RequirementCoverage("REQ-VSB-EF-LOCK-SQL", "provider-and-model-specific-mapping")]
    public void LockStatement_IsResolvedFromEveryExactModel(
        string provider,
        string expectedNorth,
        string expectedSouth)
    {
        ILockStatementProvider statements = CreateProvider(provider);
        using var north = new NorthContext(CreateOptions<NorthContext>());
        using var south = new SouthContext(CreateOptions<SouthContext>());

        string northStatement = statements.GetRowLockStatement<MappingSaga>(north);
        string southStatement = statements.GetRowLockStatement<MappingSaga>(south);

        Assert.Equal(expectedNorth, northStatement);
        Assert.Equal(expectedSouth, southStatement);
        Assert.NotSame(north.Model, south.Model);
    }

    [Theory]
    [InlineData("sql-server",
        "SELECT * FROM [north]]schema].[Order]]Archive] WITH (UPDLOCK, ROWLOCK) WHERE [Correlation]]Id] = @p0 AND [Tenant]]Id] = @p1")]
    [InlineData("postgresql",
        "SELECT *, xmin FROM \"north]schema\".\"Order]Archive\" WHERE \"Correlation]Id\" = @p0 AND \"Tenant]Id\" = @p1 FOR UPDATE")]
    [InlineData("sqlite",
        "SELECT * FROM \"Order]Archive\" WHERE \"Correlation]Id\" = @p0 AND \"Tenant]Id\" = @p1")]
    [RequirementCoverage("REQ-VSB-EF-LOCK-SQL", "ordered-property-set-and-identifier-quoting")]
    public void LockStatement_PreservesTheOrderedPropertySet(string provider, string expected)
    {
        ILockStatementProvider statements = CreateProvider(provider);
        using var context = new NorthContext(CreateOptions<NorthContext>());

        string statement = statements.GetRowLockStatement<MappingSaga>(
            context,
            nameof(MappingSaga.CorrelationId),
            nameof(MappingSaga.TenantId));

        Assert.Equal(expected, statement);
    }

    [Theory]
    [InlineData("sql-server",
        "SELECT TOP 1 * FROM [north]]schema].[Outbox]]Archive] WITH (UPDLOCK, ROWLOCK, READPAST) ORDER BY [Created]]At]")]
    [InlineData("postgresql",
        "SELECT *, xmin FROM \"north]schema\".\"Outbox]Archive\" ORDER BY \"Created]At\" LIMIT 1 FOR UPDATE SKIP LOCKED")]
    [InlineData("sqlite",
        "SELECT * FROM \"Outbox]Archive\" ORDER BY \"Created]At\" LIMIT 1")]
    [RequirementCoverage("REQ-VSB-EF-LOCK-SQL", "outbox-model-mapping-and-identifier-quoting")]
    public void OutboxStatement_UsesTheExactModelAndQuotesEveryIdentifier(string provider, string expected)
    {
        ILockStatementProvider statements = CreateProvider(provider);
        using var context = new OutboxMappingContext(CreateOptions<OutboxMappingContext>());

        string statement = statements.GetOutboxStatement(context);

        Assert.Equal(expected, statement);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-LOCK-SQL", "pessimistic-executor-does-not-freeze-first-model")]
    public async Task PessimisticExecutor_RequestsAStatementForEveryContext()
    {
        var statements = new RecordingLockStatementProvider();
        var executor = new PessimisticLoadQueryExecutor<MappingSaga>(statements, null);
        using var north = new NorthContext(CreateOptions<NorthContext>());
        using var south = new SouthContext(CreateOptions<SouthContext>());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            executor.Load(north, Guid.NewGuid(), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            executor.Load(south, Guid.NewGuid(), cancellation.Token));

        Assert.Equal([typeof(NorthContext), typeof(SouthContext)], statements.ContextTypes);
    }

    private static ILockStatementProvider CreateProvider(string provider) => provider switch
    {
        "sql-server" => new SqlServerLockStatementProvider(),
        "postgresql" => new PostgresLockStatementProvider(),
        "sqlite" => new SqliteLockStatementProvider(),
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null),
    };

    private static DbContextOptions<TContext> CreateOptions<TContext>()
        where TContext : DbContext =>
        new DbContextOptionsBuilder<TContext>()
            .UseSqlServer("Server=localhost;Database=not-opened;User Id=unused;Password=unused;Encrypt=False")
            .Options;

    public sealed class MappingSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
        public string TenantId { get; set; } = "";
    }

    private sealed class NorthContext(DbContextOptions<NorthContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MappingSaga>(entity =>
            {
                entity.ToTable("Order]Archive", "north]schema");
                entity.HasKey(saga => saga.CorrelationId);
                entity.Property(saga => saga.CorrelationId).HasColumnName("Correlation]Id");
                entity.Property(saga => saga.TenantId).HasColumnName("Tenant]Id");
            });
        }
    }

    private sealed class SouthContext(DbContextOptions<SouthContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MappingSaga>(entity =>
            {
                entity.ToTable("SouthOrder", "south");
                entity.HasKey(saga => saga.CorrelationId);
                entity.Property(saga => saga.CorrelationId).HasColumnName("SagaId");
            });
        }
    }

    private sealed class OutboxMappingContext(DbContextOptions<OutboxMappingContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxState>(entity =>
            {
                entity.ToTable("Outbox]Archive", "north]schema");
                entity.HasKey(state => state.OutboxId);
                entity.Property(state => state.Created).HasColumnName("Created]At");
            });
        }
    }

    private sealed class RecordingLockStatementProvider : ILockStatementProvider
    {
        public List<Type> ContextTypes { get; } = [];

        public string GetRowLockStatement<T>(DbContext context)
            where T : class
        {
            ContextTypes.Add(context.GetType());
            return "SELECT * FROM [MappingSaga] WHERE [CorrelationId] = @p0";
        }

        public string GetRowLockStatement<T>(DbContext context, params string[] propertyNames)
            where T : class => GetRowLockStatement<T>(context);

        public string GetOutboxStatement(DbContext context) => throw new NotSupportedException();
    }
}
