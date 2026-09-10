using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.SqlTransport.PostgreSql;
using ViciOne.ServiceBus.SqlTransport.SqlServer;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Configuration;

public sealed class SqlTransportOptionsStartupTests
{
    [Theory]
    [InlineData(DatabaseProvider.PostgreSql, InvalidOption.Address, "ConnectionString")]
    [InlineData(DatabaseProvider.PostgreSql, InvalidOption.Port, "Port")]
    [InlineData(DatabaseProvider.PostgreSql, InvalidOption.ConnectionLimit, "ConnectionLimit")]
    [InlineData(DatabaseProvider.PostgreSql, InvalidOption.NoOperation, "no migration operation")]
    [InlineData(DatabaseProvider.SqlServer, InvalidOption.Address, "ConnectionString")]
    [InlineData(DatabaseProvider.SqlServer, InvalidOption.Port, "Port")]
    [InlineData(DatabaseProvider.SqlServer, InvalidOption.ConnectionLimit, "ConnectionLimit")]
    [InlineData(DatabaseProvider.SqlServer, InvalidOption.NoOperation, "no migration operation")]
    public void InvalidMigrationOptions_FailAtStartupBeforeDatabaseIo(
        DatabaseProvider databaseProvider,
        InvalidOption invalid,
        string reason)
    {
        using ServiceProvider provider = Provider(databaseProvider, invalid);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains(reason, string.Join(Environment.NewLine, exception.Failures), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(DatabaseProvider.PostgreSql)]
    [InlineData(DatabaseProvider.SqlServer)]
    public void CoherentMigrationOptions_PassTheSameStartupBoundary(DatabaseProvider databaseProvider)
    {
        using ServiceProvider provider = Provider(databaseProvider, null);

        provider.GetRequiredService<IStartupValidator>().Validate();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-MIGRATION-LIFECYCLE", "constructor-rejects-missing-services-and-options")]
    public void MigrationHostedService_RejectsMissingDependenciesAtConstruction()
    {
        var migrator = new NoopMigrator();
        var logger = NullLogger<SqlTransportMigrationHostedService>.Instance;
        IOptions<SqlTransportMigrationOptions> migrationOptions = Options.Create(new SqlTransportMigrationOptions());
        IOptions<SqlTransportOptions> transportOptions = Options.Create(new SqlTransportOptions());

        Assert.Throws<ArgumentNullException>(() => new SqlTransportMigrationHostedService(null!, logger, migrationOptions, transportOptions));
        Assert.Throws<ArgumentNullException>(() => new SqlTransportMigrationHostedService(migrator, null!, migrationOptions, transportOptions));
        Assert.Throws<ArgumentNullException>(() => new SqlTransportMigrationHostedService(migrator, logger, null!, transportOptions));
        Assert.Throws<ArgumentNullException>(() => new SqlTransportMigrationHostedService(migrator, logger, migrationOptions, null!));
    }

    static ServiceProvider Provider(DatabaseProvider provider, InvalidOption? invalid)
    {
        var services = new ServiceCollection();
        services.Configure<SqlTransportOptions>(options =>
        {
            options.ConnectionString = invalid == InvalidOption.Address ? null : "Server=localhost;Database=transport";
            if (invalid == InvalidOption.Port)
                options.Port = 0;
            if (invalid == InvalidOption.ConnectionLimit)
                options.ConnectionLimit = 0;
        });

        Action<SqlTransportMigrationOptions> configure = options =>
        {
            if (invalid != InvalidOption.NoOperation)
                return;

            options.CreateDatabase = false;
            options.CreateSchema = false;
            options.CreateInfrastructure = false;
            options.DeleteDatabase = false;
        };

        if (provider == DatabaseProvider.PostgreSql)
            services.AddPostgreSqlMigrationHostedService(configure);
        else
            services.AddSqlServerMigrationHostedService(configure);

        return services.BuildServiceProvider();
    }

    public enum DatabaseProvider
    {
        PostgreSql,
        SqlServer,
    }

    public enum InvalidOption
    {
        Address,
        Port,
        ConnectionLimit,
        NoOperation,
    }

    private sealed class NoopMigrator : ISqlTransportDatabaseMigrator
    {
        public Task CreateDatabaseAsync(SqlTransportOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task CreateSchemaIfNotExistAsync(SqlTransportOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task CreateInfrastructureAsync(SqlTransportOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteDatabaseAsync(SqlTransportOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
