using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>Provides PostgreSQL host configuration extensions for the SQL transport.</summary>
public static class PostgresHostConfigurationExtensions
{
    /// <summary>Configures the database transport to use PostgreSQL as the storage engine.</summary>
    /// <param name="configurator">The SQL bus factory configurator.</param>
    /// <param name="hostAddress">The PostgreSQL host address.</param>
    /// <param name="configure">An optional callback that configures the host.</param>
    public static void UsePostgres(this ISqlBusFactoryConfigurator configurator, Uri hostAddress, Action<ISqlHostConfigurator>? configure = null)
    {
        var hostConfigurator = new PostgresSqlHostConfigurator(hostAddress);

        configure?.Invoke(hostConfigurator);

        configurator.Host(hostConfigurator.Settings);
    }

    /// <summary>Configures the database transport to use PostgreSQL as the storage engine.</summary>
    /// <param name="configurator">The SQL bus factory configurator.</param>
    /// <param name="connectionString">A valid PostgreSQL connection string.</param>
    /// <param name="configure">An optional callback that configures the host.</param>
    public static void UsePostgres(this ISqlBusFactoryConfigurator configurator, string connectionString, Action<ISqlHostConfigurator>? configure = null)
    {
        var hostConfigurator = new PostgresSqlHostConfigurator(connectionString);

        configure?.Invoke(hostConfigurator);

        configurator.Host(hostConfigurator.Settings);
    }

    /// <summary>Configures the database transport to use PostgreSQL as the storage engine.</summary>
    /// <param name="configurator">The SQL bus factory configurator.</param>
    /// <param name="dataSource">The preconfigured data source used to open PostgreSQL connections.</param>
    /// <param name="configure">An optional callback that configures the host.</param>
    public static void UsePostgres(this ISqlBusFactoryConfigurator configurator, NpgsqlDataSource dataSource,
        Action<ISqlHostConfigurator>? configure = null)
    {
        var hostConfigurator = new PostgresSqlHostConfigurator(dataSource);

        configure?.Invoke(hostConfigurator);

        configurator.Host(hostConfigurator.Settings);
    }

    /// <summary>Configures the database transport to use PostgreSQL as the storage engine.</summary>
    /// <param name="configurator">The SQL bus factory configurator.</param>
    /// <param name="context">The registration context from which <see cref="SqlTransportOptions" /> are resolved.</param>
    /// <param name="configure">An optional callback that configures the host.</param>
    public static void UsePostgres(this ISqlBusFactoryConfigurator configurator, IBusRegistrationContext context,
        Action<ISqlHostConfigurator>? configure = null)
    {
        var hostConfigurator = new PostgresSqlHostConfigurator(context.GetRequiredService<IOptions<SqlTransportOptions>>().Value);

        configure?.Invoke(hostConfigurator);

        configurator.Host(hostConfigurator.Settings);
    }
}
