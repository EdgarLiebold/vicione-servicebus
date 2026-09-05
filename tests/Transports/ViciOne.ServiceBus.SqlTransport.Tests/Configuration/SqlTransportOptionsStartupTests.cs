using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.SqlTransport.PostgreSql;
using ViciOne.ServiceBus.SqlTransport.SqlServer;
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
            services.AddPostgresMigrationHostedService(configure);
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
}
