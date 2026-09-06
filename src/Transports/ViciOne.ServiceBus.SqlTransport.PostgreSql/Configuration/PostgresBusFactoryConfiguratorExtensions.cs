using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.SqlTransport.PostgreSql;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides PostgreSQL transport registration extensions.</summary>
public static class PostgresBusFactoryConfiguratorExtensions
{
    /// <summary>Configures the bus to use PostgreSQL settings resolved from dependency injection.</summary>
    /// <param name="configurator">The bus registration configurator.</param>
    /// <param name="configure">An optional callback that configures the SQL bus factory.</param>
    public static void UsingPostgres(this IBusRegistrationConfigurator configurator,
        Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? configure = null)
    {
        configurator.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, PostgresSendFailureClassifier>());
        configurator.SetBusFactory(new SqlRegistrationBusFactory((context, cfg) =>
        {
            cfg.UsePostgres(context);

            configure?.Invoke(context, cfg);
        }));
    }

    /// <summary>Configures the bus to use PostgreSQL with the specified connection string.</summary>
    /// <param name="configurator">The bus registration configurator.</param>
    /// <param name="connectionString">
    /// Connection string to be used/parsed by the transport. <see cref="SqlTransportOptions" /> are not
    /// used with this overload.
    /// </param>
    /// <param name="configure">An optional callback that configures the SQL bus factory.</param>
    public static void UsingPostgres(this IBusRegistrationConfigurator configurator, string connectionString,
        Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? configure = null)
    {
        configurator.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, PostgresSendFailureClassifier>());
        configurator.SetBusFactory(new SqlRegistrationBusFactory((context, cfg) =>
        {
            cfg.UsePostgres(connectionString);

            configure?.Invoke(context, cfg);
        }));
    }

    /// <summary>Configures the bus to use the specified PostgreSQL data source.</summary>
    /// <param name="configurator">The bus registration configurator.</param>
    /// <param name="dataSource">The preconfigured data source used to open PostgreSQL connections.</param>
    /// <param name="configure">An optional callback that configures the SQL bus factory.</param>
    public static void UsingPostgres(this IBusRegistrationConfigurator configurator, NpgsqlDataSource dataSource,
        Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? configure = null)
    {
        configurator.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, PostgresSendFailureClassifier>());
        configurator.SetBusFactory(new SqlRegistrationBusFactory((context, cfg) =>
        {
            cfg.UsePostgres(dataSource);

            configure?.Invoke(context, cfg);
        }));
    }

    /// <summary>Configures the bus to use a PostgreSQL data source resolved for the bus registration.</summary>
    /// <param name="configurator">The bus registration configurator.</param>
    /// <param name="dataSourceProvider">A delegate that resolves the data source from the registration context.</param>
    /// <param name="configure">An optional callback that configures the SQL bus factory.</param>
    public static void UsingPostgres(this IBusRegistrationConfigurator configurator, Func<IBusRegistrationContext, NpgsqlDataSource> dataSourceProvider,
        Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? configure = null)
    {
        configurator.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, PostgresSendFailureClassifier>());
        configurator.SetBusFactory(new SqlRegistrationBusFactory((context, cfg) =>
        {
            cfg.UsePostgres(dataSourceProvider(context));

            configure?.Invoke(context, cfg);
        }));
    }
}
