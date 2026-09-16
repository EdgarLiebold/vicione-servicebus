using Microsoft.Data.Sqlite;
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

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-LOCK-SQL", "cache-key-preserves-property-sequence-boundaries")]
    public void LockStatementCache_DistinguishesAPropertySequenceFromASeparatorBearingProperty()
    {
        var statements = new SqliteLockStatementProvider();
        using var context = new CacheKeyCollisionContext(CreateOptions<CacheKeyCollisionContext>());

        string twoPropertyStatement = statements.GetRowLockStatement<CacheKeyCollisionEntity>(
            context,
            "Tenant",
            "Region");
        string separatorBearingPropertyStatement = statements.GetRowLockStatement<CacheKeyCollisionEntity>(
            context,
            "Tenant\u001fRegion");

        Assert.Equal(
            "SELECT * FROM \"CacheKeyCollision\" WHERE \"TenantColumn\" = @p0 AND \"RegionColumn\" = @p1",
            twoPropertyStatement);
        Assert.Equal(
            "SELECT * FROM \"CacheKeyCollision\" WHERE \"CombinedColumn\" = @p0",
            separatorBearingPropertyStatement);
    }

    [Theory]
    [InlineData("sql-server", "SELECT * FROM [fallback].[UnqualifiedOrder] WITH (UPDLOCK, ROWLOCK) WHERE [CorrelationId] = @p0")]
    [InlineData("postgresql", "SELECT *, xmin FROM \"fallback\".\"UnqualifiedOrder\" WHERE \"CorrelationId\" = @p0 FOR UPDATE")]
    [InlineData("sqlite", "SELECT * FROM \"UnqualifiedOrder\" WHERE \"CorrelationId\" = @p0")]
    [RequirementCoverage("REQ-VSB-EF-LOCK-SQL", "explicit-fallback-schema-is-provider-specific")]
    public void ExplicitFallbackSchema_IsAppliedOnlyBySchemaAwareProviders(string provider, string expected)
    {
        ILockStatementProvider statements = CreateProvider(provider, "fallback");
        using var context = new UnqualifiedContext(CreateOptions<UnqualifiedContext>(provider));

        string statement = statements.GetRowLockStatement<MappingSaga>(context);

        Assert.Equal(expected, statement);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-LOCK-SQL", "fallback-schema-obeys-portable-identifier-boundary")]
    public void ExplicitFallbackSchema_RejectsUnsafeOrOversizedIdentifiers()
    {
        Func<string, ILockStatementProvider>[] factories =
        [
            schema => new SqlServerLockStatementProvider(schema),
            schema => new PostgreSqlLockStatementProvider(schema),
            schema => new SqliteLockStatementProvider(schema),
        ];
        string[] invalidSchemas = ["unsafe\nschema", new string('\u00e9', 32)];

        foreach (Func<string, ILockStatementProvider> factory in factories)
        {
            foreach (string invalidSchema in invalidSchemas)
            {
                ArgumentException exception = Assert.ThrowsAny<ArgumentException>(() => factory(invalidSchema));
                Assert.Equal("defaultSchema", exception.ParamName);
            }
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-LOCK-SQL", "mapped-identifiers-obey-portable-safety-boundary")]
    public void MappedIdentifiers_RejectUnsafeSchemaTableAndColumnNames()
    {
        var statements = new SqliteLockStatementProvider();
        using var unsafeSchema = new UnsafeSchemaContext(CreateOptions<UnsafeSchemaContext>());
        using var unsafeTable = new UnsafeTableContext(CreateOptions<UnsafeTableContext>());
        using var oversizedColumn = new OversizedColumnContext(CreateOptions<OversizedColumnContext>());

        Assert.Equal(
            "schema",
            Assert.ThrowsAny<ArgumentException>(() => statements.GetRowLockStatement<MappingSaga>(unsafeSchema)).ParamName);
        Assert.Equal(
            "tableName",
            Assert.ThrowsAny<ArgumentException>(() => statements.GetRowLockStatement<MappingSaga>(unsafeTable)).ParamName);
        Assert.Equal(
            "columnName",
            Assert.ThrowsAny<ArgumentException>(() => statements.GetRowLockStatement<MappingSaga>(oversizedColumn)).ParamName);
    }

    [Theory]
    [InlineData("sql-server")]
    [InlineData("postgresql")]
    [InlineData("sqlite")]
    [RequirementCoverage("REQ-VSB-EF-LOCK-SQL", "runtime-provider-selection-delegates-every-statement")]
    public void ReliableProvider_DelegatesEveryStatementToTheDbContextProvider(string provider)
    {
        ILockStatementProvider expectedProvider = CreateReliableExpectedProvider(provider);
        var actualProvider = new ReliableLockStatementProvider();
        using var rowContext = new NorthContext(CreateOptions<NorthContext>(provider));
        using var outboxContext = new OutboxMappingContext(CreateOptions<OutboxMappingContext>(provider));
        using var inboxContext = new InboxMappingContext(CreateOptions<InboxMappingContext>(provider));

        Assert.Equal(
            expectedProvider.GetRowLockStatement<MappingSaga>(rowContext),
            actualProvider.GetRowLockStatement<MappingSaga>(rowContext));
        Assert.Equal(
            expectedProvider.GetRowLockStatement<MappingSaga>(rowContext, nameof(MappingSaga.TenantId)),
            actualProvider.GetRowLockStatement<MappingSaga>(rowContext, nameof(MappingSaga.TenantId)));
        Assert.Equal(expectedProvider.GetOutboxStatement(outboxContext), actualProvider.GetOutboxStatement(outboxContext));
        Assert.Equal(
            expectedProvider.GetInboxCleanupLockStatement(inboxContext),
            actualProvider.GetInboxCleanupLockStatement(inboxContext));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-LOCK-SQL", "required-inputs-and-mappings-fail-at-owner-boundary")]
    public void LockStatementProvider_RejectsEveryMissingRequiredInputOrMapping()
    {
        var formatter = new SqliteLockStatementFormatter();
        Assert.Equal("defaultSchema", Assert.Throws<ArgumentException>(() => new SqlLockStatementProvider(" ", formatter)).ParamName);
        Assert.Equal(
            "formatter",
            Assert.Throws<ArgumentNullException>(() => new SqlLockStatementProvider((ILockStatementFormatter)null!)).ParamName);
        Assert.Equal(
            "formatter",
            Assert.Throws<ArgumentNullException>(() => new SqlLockStatementProvider("fallback", null!)).ParamName);

        var statements = new SqliteLockStatementProvider();
        using var context = new NorthContext(CreateOptions<NorthContext>());
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => statements.GetRowLockStatement<MappingSaga>(null!)).ParamName);
        Assert.Equal(
            "propertyNames",
            Assert.Throws<ArgumentNullException>(() => statements.GetRowLockStatement<MappingSaga>(context, null!)).ParamName);
        Assert.Equal(
            "propertyNames",
            Assert.Throws<ArgumentException>(() => statements.GetRowLockStatement<MappingSaga>(context, [])).ParamName);
        Assert.Equal(
            "propertyNames",
            Assert.Throws<ArgumentException>(() => statements.GetRowLockStatement<MappingSaga>(context, " ")).ParamName);
        Assert.Contains(
            nameof(UnmappedEntity),
            Assert.Throws<InvalidOperationException>(() => statements.GetRowLockStatement<UnmappedEntity>(context)).Message);
        Assert.Contains(
            "MissingProperty",
            Assert.Throws<InvalidOperationException>(() =>
                statements.GetRowLockStatement<MappingSaga>(context, "MissingProperty")).Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-LOCK-SQL", "runtime-provider-selection-fails-without-provider-identity")]
    public void ReliableProvider_RejectsMissingContextAndProviderIdentity()
    {
        var statements = new ReliableLockStatementProvider();
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => statements.GetRowLockStatement<MappingSaga>(null!)).ParamName);
        using var context = new DbContext(new DbContextOptionsBuilder().Options);

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            statements.GetRowLockStatement<MappingSaga>(context));

        Assert.Contains("provider name is unavailable", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
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

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-LOCK-SQL", "outbox-lock-never-crosses-bus-boundary")]
    public async Task OutboxStatement_DoesNotSelectADeliveredRowOwnedByAnotherBusAsync()
    {
        const string ownedBusKey = "owned-bus-v1";
        const string foreignBusKey = "foreign-bus-v1";
        DateTimeOffset now = new(2042, 3, 4, 5, 6, 7, TimeSpan.Zero);
        Guid ownedOutboxId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<OutboxMappingContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new OutboxMappingContext(options);
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        context.AddRange(
            new OutboxState
            {
                OutboxId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                BusKey = foreignBusKey,
                Created = now.AddMinutes(-1),
                Status = OutboxDeliveryStatus.Delivered,
                Delivered = now.AddMinutes(-1),
            },
            new OutboxState
            {
                OutboxId = ownedOutboxId,
                BusKey = ownedBusKey,
                Created = now,
                Status = OutboxDeliveryStatus.Pending,
            });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        string statement = new SqliteLockStatementProvider().GetOutboxStatement(context);

        OutboxState selected = Assert.Single(await context.Set<OutboxState>()
            .FromSqlRaw(
                statement,
                ownedBusKey,
                OutboxDeliveryStatus.Pending,
                OutboxDeliveryStatus.RetryScheduled,
                now,
                OutboxDeliveryStatus.Delivered)
            .AsNoTracking()
            .ToListAsync(TestContext.Current.CancellationToken));

        Assert.Equal(ownedOutboxId, selected.OutboxId);
        Assert.Equal(ownedBusKey, selected.BusKey);
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

    private static ILockStatementProvider CreateProvider(string provider, string schema) => provider switch
    {
        "sql-server" => new SqlServerLockStatementProvider(schema),
        "postgresql" => new PostgreSqlLockStatementProvider(schema),
        "sqlite" => new SqliteLockStatementProvider(schema),
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null),
    };

    private static ILockStatementProvider CreateReliableExpectedProvider(string provider) => provider switch
    {
        "sql-server" => new SqlServerLockStatementProvider(serializable: true),
        "postgresql" => new PostgreSqlLockStatementProvider(),
        "sqlite" => new SqliteLockStatementProvider(),
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null),
    };

    private static DbContextOptions<TContext> CreateOptions<TContext>()
        where TContext : DbContext => CreateOptions<TContext>("sql-server");

    private static DbContextOptions<TContext> CreateOptions<TContext>(string provider)
        where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>();
        return provider switch
        {
            "sql-server" => builder
                .UseSqlServer("Server=localhost;Database=not-opened;User Id=unused;Password=unused;Encrypt=False")
                .Options,
            "postgresql" => builder.UseNpgsql("Host=localhost;Database=not-opened;Username=unused;Password=unused").Options,
            "sqlite" => builder.UseSqlite("Data Source=:memory:").Options,
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null),
        };
    }

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

    private sealed class UnqualifiedContext(DbContextOptions<UnqualifiedContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MappingSaga>(entity =>
            {
                entity.ToTable("UnqualifiedOrder");
                entity.HasKey(saga => saga.CorrelationId);
            });
        }
    }

    private sealed class UnsafeSchemaContext(DbContextOptions<UnsafeSchemaContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MappingSaga>(entity =>
            {
                entity.ToTable("MappingSaga", "unsafe\nschema");
                entity.HasKey(saga => saga.CorrelationId);
            });
        }
    }

    private sealed class UnsafeTableContext(DbContextOptions<UnsafeTableContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MappingSaga>(entity =>
            {
                entity.ToTable("unsafe\ntable");
                entity.HasKey(saga => saga.CorrelationId);
            });
        }
    }

    private sealed class OversizedColumnContext(DbContextOptions<OversizedColumnContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MappingSaga>(entity =>
            {
                entity.ToTable("MappingSaga");
                entity.HasKey(saga => saga.CorrelationId);
                entity.Property(saga => saga.CorrelationId).HasColumnName(new string('\u00e9', 32));
            });
        }
    }

    private sealed class UnmappedEntity
    {
        public Guid CorrelationId { get; set; }
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

    private sealed class CacheKeyCollisionContext(DbContextOptions<CacheKeyCollisionContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CacheKeyCollisionEntity>(entity =>
            {
                entity.ToTable("CacheKeyCollision");
                entity.HasKey(value => value.Id);
                entity.Property<string>("Tenant").HasColumnName("TenantColumn");
                entity.Property<string>("Region").HasColumnName("RegionColumn");
                entity.Property<string>("Tenant\u001fRegion").HasColumnName("CombinedColumn");
            });
        }
    }

    private sealed class CacheKeyCollisionEntity
    {
        public Guid Id { get; set; }
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
