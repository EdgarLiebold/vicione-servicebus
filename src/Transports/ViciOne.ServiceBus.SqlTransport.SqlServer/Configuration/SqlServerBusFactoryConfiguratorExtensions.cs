using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.SqlTransport.SqlServer;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides SQL Server transport registration extensions.</summary>
public static class SqlServerBusFactoryConfiguratorExtensions
{
    /// <summary>Configures the bus to use SQL Server settings resolved from dependency injection.</summary>
    /// <param name="configurator">The bus registration configurator.</param>
    /// <param name="configure">An optional callback that configures the SQL bus factory.</param>
    public static void UsingSqlServer(this IBusRegistrationConfigurator configurator,
        Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? configure = null)
    {
        configurator.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, SqlServerSendFailureClassifier>());
        configurator.SetBusFactory(new SqlRegistrationBusFactory((context, cfg) =>
        {
            cfg.UseSqlServer(context);

            configure?.Invoke(context, cfg);
        }));
    }

    /// <summary>Configures the bus to use SQL Server with the specified connection string.</summary>
    /// <param name="configurator">The bus registration configurator.</param>
    /// <param name="connectionString">
    /// Connection string to be used/parsed by the transport. <see cref="SqlTransportOptions" /> are not
    /// used with this overload.
    /// </param>
    /// <param name="configure">An optional callback that configures the SQL bus factory.</param>
    public static void UsingSqlServer(this IBusRegistrationConfigurator configurator, string connectionString,
        Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? configure = null)
    {
        configurator.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, SqlServerSendFailureClassifier>());
        configurator.SetBusFactory(new SqlRegistrationBusFactory((context, cfg) =>
        {
            cfg.UseSqlServer(connectionString);

            configure?.Invoke(context, cfg);
        }));
    }
}
