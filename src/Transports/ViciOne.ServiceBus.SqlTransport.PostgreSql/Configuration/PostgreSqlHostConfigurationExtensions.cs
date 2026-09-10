using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>Provides PostgreSQL host configuration extensions for the SQL transport.</summary>
public static class PostgreSqlHostConfigurationExtensions
{
    /// <summary>Configures the database transport to use PostgreSQL as the storage engine.</summary>
    /// <param name="configurator">The SQL bus factory configurator.</param>
    /// <param name="hostAddress">The PostgreSQL host address.</param>
    /// <param name="configure">An optional callback that configures the host.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configurator" /> or <paramref name="hostAddress" /> is <see langword="null" />.</exception>
    public static void UsePostgreSql(this ISqlBusFactoryConfigurator configurator, Uri hostAddress, Action<ISqlHostConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(hostAddress);

        var hostConfigurator = new PostgreSqlHostConfigurator(hostAddress);

        configure?.Invoke(hostConfigurator);

        configurator.Host(hostConfigurator.Settings);
    }

    /// <summary>Configures the database transport to use PostgreSQL as the storage engine.</summary>
    /// <param name="configurator">The SQL bus factory configurator.</param>
    /// <param name="connectionString">A valid PostgreSQL connection string.</param>
    /// <param name="configure">An optional callback that configures the host.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configurator" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="connectionString" /> is empty or contains only white-space characters.</exception>
    public static void UsePostgreSql(this ISqlBusFactoryConfigurator configurator, string connectionString, Action<ISqlHostConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var hostConfigurator = new PostgreSqlHostConfigurator(connectionString);

        configure?.Invoke(hostConfigurator);

        configurator.Host(hostConfigurator.Settings);
    }

    /// <summary>Configures the database transport to use PostgreSQL as the storage engine.</summary>
    /// <param name="configurator">The SQL bus factory configurator.</param>
    /// <param name="dataSource">The preconfigured data source used to open PostgreSQL connections.</param>
    /// <param name="configure">An optional callback that configures the host.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configurator" /> or <paramref name="dataSource" /> is <see langword="null" />.</exception>
    public static void UsePostgreSql(this ISqlBusFactoryConfigurator configurator, NpgsqlDataSource dataSource,
        Action<ISqlHostConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(dataSource);

        var hostConfigurator = new PostgreSqlHostConfigurator(dataSource);

        configure?.Invoke(hostConfigurator);

        configurator.Host(hostConfigurator.Settings);
    }

    /// <summary>Configures the database transport to use PostgreSQL as the storage engine.</summary>
    /// <param name="configurator">The SQL bus factory configurator.</param>
    /// <param name="context">The registration context from which <see cref="SqlTransportOptions" /> are resolved.</param>
    /// <param name="configure">An optional callback that configures the host.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configurator" /> or <paramref name="context" /> is <see langword="null" />.</exception>
    public static void UsePostgreSql(this ISqlBusFactoryConfigurator configurator, IBusRegistrationContext context,
        Action<ISqlHostConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

        var hostConfigurator = new PostgreSqlHostConfigurator(context.GetRequiredService<IOptions<SqlTransportOptions>>().Value);

        configure?.Invoke(hostConfigurator);

        configurator.Host(hostConfigurator.Settings);
    }
}
