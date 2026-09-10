using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.SqlTransport.PostgreSql;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides PostgreSQL transport registration extensions.</summary>
public static class PostgreSqlBusFactoryConfiguratorExtensions
{
    /// <summary>Configures the bus to use PostgreSQL settings resolved from dependency injection.</summary>
    /// <param name="configurator">The bus registration configurator.</param>
    /// <param name="configure">An optional callback that configures the SQL bus factory.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configurator" /> is <see langword="null" />.</exception>
    public static void UsingPostgreSql(this IBusRegistrationConfigurator configurator,
        Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, PostgreSqlSendFailureClassifier>());
        configurator.SetBusFactory(new SqlRegistrationBusFactory((context, cfg) =>
        {
            cfg.UsePostgreSql(context);

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
    /// <exception cref="ArgumentNullException"><paramref name="configurator" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="connectionString" /> is empty or contains only white-space characters.</exception>
    public static void UsingPostgreSql(this IBusRegistrationConfigurator configurator, string connectionString,
        Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        configurator.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, PostgreSqlSendFailureClassifier>());
        configurator.SetBusFactory(new SqlRegistrationBusFactory((context, cfg) =>
        {
            cfg.UsePostgreSql(connectionString);

            configure?.Invoke(context, cfg);
        }));
    }

    /// <summary>Configures the bus to use the specified PostgreSQL data source.</summary>
    /// <param name="configurator">The bus registration configurator.</param>
    /// <param name="dataSource">The preconfigured data source used to open PostgreSQL connections.</param>
    /// <param name="configure">An optional callback that configures the SQL bus factory.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configurator" /> or <paramref name="dataSource" /> is <see langword="null" />.</exception>
    public static void UsingPostgreSql(this IBusRegistrationConfigurator configurator, NpgsqlDataSource dataSource,
        Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(dataSource);

        configurator.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, PostgreSqlSendFailureClassifier>());
        configurator.SetBusFactory(new SqlRegistrationBusFactory((context, cfg) =>
        {
            cfg.UsePostgreSql(dataSource);

            configure?.Invoke(context, cfg);
        }));
    }

    /// <summary>Configures the bus to use a PostgreSQL data source resolved for the bus registration.</summary>
    /// <param name="configurator">The bus registration configurator.</param>
    /// <param name="dataSourceProvider">A delegate that resolves the data source from the registration context.</param>
    /// <param name="configure">An optional callback that configures the SQL bus factory.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configurator" /> or <paramref name="dataSourceProvider" /> is <see langword="null" />.</exception>
    public static void UsingPostgreSql(this IBusRegistrationConfigurator configurator, Func<IBusRegistrationContext, NpgsqlDataSource> dataSourceProvider,
        Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(dataSourceProvider);

        configurator.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, PostgreSqlSendFailureClassifier>());
        configurator.SetBusFactory(new SqlRegistrationBusFactory((context, cfg) =>
        {
            NpgsqlDataSource dataSource = dataSourceProvider(context)
                ?? throw new InvalidOperationException("The PostgreSQL data-source provider returned null.");
            cfg.UsePostgreSql(dataSource);

            configure?.Invoke(context, cfg);
        }));
    }
}
