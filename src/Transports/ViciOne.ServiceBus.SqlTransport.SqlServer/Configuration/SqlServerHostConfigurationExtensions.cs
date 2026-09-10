using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.SqlTransport.SqlServer;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>Provides SQL Server host configuration extensions for the SQL transport.</summary>
public static class SqlServerHostConfigurationExtensions
{
    /// <summary>Configures the SQL transport from an absolute SQL Server host address.</summary>
    /// <param name="configurator">The SQL bus factory configurator.</param>
    /// <param name="hostAddress">The SQL Server host address.</param>
    /// <param name="configure">An optional callback that configures the host.</param>
    public static void UseSqlServer(this ISqlBusFactoryConfigurator configurator, Uri hostAddress, Action<ISqlHostConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(hostAddress);

        var hostConfigurator = new SqlServerHostConfigurator(hostAddress);

        configure?.Invoke(hostConfigurator);

        configurator.Host(hostConfigurator.Settings);
    }

    /// <summary>Configures the SQL transport from options registered in dependency injection.</summary>
    /// <param name="configurator">The SQL bus factory configurator.</param>
    /// <param name="context">The registration context from which <see cref="SqlTransportOptions" /> are resolved.</param>
    /// <param name="configure">An optional callback that configures the host.</param>
    public static void UseSqlServer(this ISqlBusFactoryConfigurator configurator, IBusRegistrationContext context,
        Action<ISqlHostConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

        var hostConfigurator = new SqlServerHostConfigurator(context.GetRequiredService<IOptions<SqlTransportOptions>>().Value);

        configure?.Invoke(hostConfigurator);

        configurator.Host(hostConfigurator.Settings);
    }

    /// <summary>Configures the SQL transport from a SQL Server connection string.</summary>
    /// <param name="configurator">The SQL bus factory configurator.</param>
    /// <param name="connectionString">A valid SQL Server connection string.</param>
    /// <param name="configure">An optional callback that configures the host.</param>
    public static void UseSqlServer(this ISqlBusFactoryConfigurator configurator, string connectionString, Action<ISqlHostConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var hostConfigurator = new SqlServerHostConfigurator(connectionString);

        configure?.Invoke(hostConfigurator);

        configurator.Host(hostConfigurator.Settings);
    }
}
