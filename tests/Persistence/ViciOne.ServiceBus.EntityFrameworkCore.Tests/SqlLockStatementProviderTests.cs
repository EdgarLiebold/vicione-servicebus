using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests;

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
        "SELECT TOP 1 * FROM [north]]schema].[Outbox]]Archive] WITH (UPDLOCK, ROWLOCK, READPAST) WHERE [Bus]]Key] = @p0 AND ([Status]]Code] = @p1 OR ([Status]]Code] = @p2 AND ([Next]]At] IS NULL OR [Next]]At] <= @p3)) OR [Status]]Code] = @p4) ORDER BY [Created]]At], [Outbox]]Id]")]
    [InlineData("postgresql",
        "SELECT *, xmin FROM \"north]schema\".\"Outbox]Archive\" WHERE \"Bus]Key\" = @p0 AND (\"Status]Code\" = @p1 OR (\"Status]Code\" = @p2 AND (\"Next]At\" IS NULL OR \"Next]At\" <= @p3)) OR \"Status]Code\" = @p4) ORDER BY \"Created]At\", \"Outbox]Id\" LIMIT 1 FOR UPDATE SKIP LOCKED")]
    [InlineData("sqlite",
        "SELECT * FROM \"Outbox]Archive\" WHERE \"Bus]Key\" = @p0 AND (\"Status]Code\" = @p1 OR (\"Status]Code\" = @p2 AND (\"Next]At\" IS NULL OR \"Next]At\" <= @p3)) OR \"Status]Code\" = @p4) ORDER BY \"Created]At\", \"Outbox]Id\" LIMIT 1")]
    [RequirementCoverage("REQ-VSB-EF-LOCK-SQL", "outbox-model-mapping-and-identifier-quoting")]
    public void OutboxStatement_UsesTheExactModelAndQuotesEveryIdentifier(string provider, string expected)
    {
        ILockStatementProvider statements = CreateProvider(provider);
        using var context = new OutboxMappingContext(CreateOptions<OutboxMappingContext>());

        string statement = statements.GetOutboxStatement(context);

        Assert.Equal(expected, statement);
    }

    [Theory]
    [InlineData("sql-server",
        "DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource = N'ViciOne.ServiceBus:InboxCleanup:north''schema.Inbox''Archive', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 0; SELECT CASE WHEN @result >= 0 THEN 1 ELSE 0 END")]
    [InlineData("postgresql",
        "SELECT CASE WHEN pg_try_advisory_xact_lock(hashtext('ViciOne.ServiceBus:InboxCleanup:north''schema.Inbox''Archive'), 0) THEN 1 ELSE 0 END")]
    [InlineData("sqlite", "SELECT 1")]
    [RequirementCoverage("REQ-VSB-EF-INBOX-CLEANUP", "provider-native-transactional-ownership-sql")]
    public void InboxCleanupStatement_UsesProviderNativeTransactionalOwnership(string provider, string expected)
    {
        ILockStatementProvider statements = CreateProvider(provider);
        using var context = new InboxMappingContext(CreateOptions<InboxMappingContext>());

        string statement = statements.GetInboxCleanupLockStatement(context);

        Assert.Equal(expected, statement);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-LOCK-SQL", "pessimistic-executor-does-not-freeze-first-model")]
    public async Task PessimisticExecutor_RequestsAStatementForEveryContextAsync()
    {
        var statements = new RecordingLockStatementProvider();
        var executor = new PessimisticLoadQueryExecutor<MappingSaga>(statements, null);
        using var north = new NorthContext(CreateOptions<NorthContext>());
        using var south = new SouthContext(CreateOptions<SouthContext>());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            executor.LoadAsync(north, Guid.NewGuid(), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            executor.LoadAsync(south, Guid.NewGuid(), cancellation.Token));

        Assert.Equal([typeof(NorthContext), typeof(SouthContext)], statements.ContextTypes);
    }

    private static ILockStatementProvider CreateProvider(string provider) => provider switch
    {
        "sql-server" => new SqlServerLockStatementProvider(),
        "postgresql" => new PostgreSqlLockStatementProvider(),
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
                entity.Property(state => state.OutboxId).HasColumnName("Outbox]Id");
                entity.Property(state => state.BusKey).HasColumnName("Bus]Key");
                entity.Property(state => state.Status).HasColumnName("Status]Code");
                entity.Property(state => state.NextDeliveryTime).HasColumnName("Next]At");
                entity.Property(state => state.Created).HasColumnName("Created]At");
            });
        }
    }

    private sealed class InboxMappingContext(DbContextOptions<InboxMappingContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InboxState>(entity =>
            {
                entity.ToTable("Inbox'Archive", "north'schema");
                entity.HasKey(state => state.Id);
                entity.Property(state => state.Delivered);
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

        public string GetInboxCleanupLockStatement(DbContext context) => throw new NotSupportedException();
    }
}
