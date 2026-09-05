using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.DurableSend;

public sealed class EntityFrameworkDurableSendDurabilityTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-COMMIT", "sqlserver-delayed-durability-policy")]
    public void SqlServerPreflight_AcceptsOnlyModesWithFullyDurableDefaultCommits()
    {
        EntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>.EnsureSqlServerDurability("DISABLED");
        EntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>.EnsureSqlServerDurability("allowed");

        ConfigurationException forced = Assert.Throws<ConfigurationException>(() =>
            EntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>.EnsureSqlServerDurability("FORCED"));
        Assert.Contains("FORCED", forced.Message, StringComparison.Ordinal);
        Assert.Throws<ConfigurationException>(() =>
            EntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>.EnsureSqlServerDurability(null));
        Assert.Throws<ConfigurationException>(() =>
            EntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>.EnsureSqlServerDurability("UNKNOWN"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-COMMIT", "postgresql-wal-flush-and-fsync-policy")]
    public void PostgreSqlPreflight_RequiresFsyncAndALocalWalFlushMode()
    {
        foreach (string mode in new[] { "on", "local", "remote_write", "remote_apply" })
            EntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>.EnsurePostgreSqlDurability(mode, "on");

        ConfigurationException asynchronous = Assert.Throws<ConfigurationException>(() =>
            EntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>.EnsurePostgreSqlDurability("off", "on"));
        Assert.Contains("synchronous_commit='off'", asynchronous.Message, StringComparison.Ordinal);
        ConfigurationException fsyncOff = Assert.Throws<ConfigurationException>(() =>
            EntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>.EnsurePostgreSqlDurability("on", "off"));
        Assert.Contains("fsync='off'", fsyncOff.Message, StringComparison.Ordinal);
        Assert.Throws<ConfigurationException>(() =>
            EntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>.EnsurePostgreSqlDurability(null, null));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-COMMIT", "sqlite-journal-and-synchronous-policy")]
    public void SqlitePreflight_RequiresTheJournalSpecificStrictSynchronousLevel()
    {
        EntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>.EnsureSqliteDurability(2, "wal");
        EntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>.EnsureSqliteDurability(3, "WAL");
        foreach (string mode in new[] { "delete", "truncate", "persist" })
            EntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>.EnsureSqliteDurability(3, mode);

        Assert.Throws<ConfigurationException>(() =>
            EntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>.EnsureSqliteDurability(1, "wal"));
        Assert.Throws<ConfigurationException>(() =>
            EntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>.EnsureSqliteDurability(2, "delete"));
        Assert.Throws<ConfigurationException>(() =>
            EntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>.EnsureSqliteDurability(3, "memory"));
        Assert.Throws<ConfigurationException>(() =>
            EntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>.EnsureSqliteDurability(-1, null));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-COMMIT", "real-sqlite-preflight-and-connection-ownership")]
    public async Task SqlitePreflight_QueriesARealSessionAndRestoresItsConnectionStateAsync()
    {
        string path = Path.Combine(Path.GetTempPath(), $"vicione-durable-{Guid.NewGuid():N}.db");
        string connectionString = $"Data Source={path}";
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            await using (var setup = new SqliteConnection(connectionString))
            {
                await setup.OpenAsync(cancellationToken);
                await using SqliteCommand command = setup.CreateCommand();
                command.CommandText = "PRAGMA journal_mode=WAL;";
                Assert.Equal("wal", Convert.ToString(await command.ExecuteScalarAsync(cancellationToken))?.ToLowerInvariant());
            }

            var connection = new SqliteConnection(connectionString);
            var options = new DbContextOptionsBuilder<DurableDbContext>().UseSqlite(connection).Options;
            await using var context = new DurableDbContext(options);
            var validator = new EntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>();

            Assert.Equal(ConnectionState.Closed, connection.State);
            await validator.ValidateAsync(context, cancellationToken);
            Assert.Equal(ConnectionState.Closed, connection.State);
        }
        finally
        {
            File.Delete(path);
            File.Delete(path + "-wal");
            File.Delete(path + "-shm");
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-COMMIT", "volatile-and-unidentified-providers-fail-closed")]
    public async Task ProviderPreflight_FailsClosedForVolatileSqliteAndMissingProviderIdentityAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);
        var sqliteOptions = new DbContextOptionsBuilder<DurableDbContext>().UseSqlite(connection).Options;
        await using var sqlite = new DurableDbContext(sqliteOptions);
        var validator = new EntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>();

        ConfigurationException volatileStore = await Assert.ThrowsAsync<ConfigurationException>(() =>
            validator.ValidateAsync(sqlite, cancellationToken));
        Assert.Contains("journal_mode='memory'", volatileStore.Message, StringComparison.OrdinalIgnoreCase);

        await using var unidentified = new DurableDbContext(new DbContextOptionsBuilder<DurableDbContext>().Options);
        ConfigurationException noProvider = await Assert.ThrowsAsync<ConfigurationException>(() =>
            validator.ValidateAsync(unidentified, cancellationToken));
        Assert.Contains("provider name is unavailable", noProvider.Message, StringComparison.OrdinalIgnoreCase);
    }

    private interface ITestBus : IBus;

    private sealed class DurableDbContext(DbContextOptions<DurableDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddViciOneReliableMessaging();
    }
}
