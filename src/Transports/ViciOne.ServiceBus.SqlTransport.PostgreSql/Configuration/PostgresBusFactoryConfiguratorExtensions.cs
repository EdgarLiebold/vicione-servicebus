namespace ViciOne.ServiceBus
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Npgsql;
    using SqlTransport.Configuration;
    using SqlTransport.PostgreSql;


    public static class PostgresBusFactoryConfiguratorExtensions
    {
        /// <summary>
        /// Configure the bus to use the PostgreSQL database transport
        /// </summary>
        /// <param name="configurator">The registration configurator (configured via AddViciOneServiceBus)</param>
        /// <param name="configure">The configuration callback for the bus factory</param>
        public static void UsingPostgres(this IBusRegistrationConfigurator configurator,
            Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? configure = null)
        {
            configurator.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, PostgresSendFailureClassifier>());
            configurator.SetBusFactory(new SqlRegistrationBusFactory((context, cfg) =>
            {
                cfg.UsePostgres(context);

                configure?.Invoke(context, cfg);
            }));
        }

        /// <summary>
        /// Configure the bus to use the PostgreSQL database transport
        /// </summary>
        /// <param name="configurator">The registration configurator (configured via AddViciOneServiceBus)</param>
        /// <param name="connectionString">
        /// Connection string to be used/parsed by the transport. <see cref="SqlTransportOptions" /> are not
        /// used with this overload
        /// </param>
        /// <param name="configure">The configuration callback for the bus factory</param>
        public static void UsingPostgres(this IBusRegistrationConfigurator configurator, string connectionString,
            Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? configure = null)
        {
            configurator.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, PostgresSendFailureClassifier>());
            configurator.SetBusFactory(new SqlRegistrationBusFactory((context, cfg) =>
            {
                cfg.UsePostgres(connectionString);

                configure?.Invoke(context, cfg);
            }));
        }

        /// <summary>
        /// Configure the bus to use the PostgreSQL database transport
        /// </summary>
        /// <param name="configurator">The registration configurator (configured via AddViciOneServiceBus)</param>
        /// <param name="dataSource">A preconfigured data source used to create connections</param>
        /// <param name="configure">The configuration callback for the bus factory</param>
        public static void UsingPostgres(this IBusRegistrationConfigurator configurator, NpgsqlDataSource dataSource,
            Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? configure = null)
        {
            configurator.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, PostgresSendFailureClassifier>());
            configurator.SetBusFactory(new SqlRegistrationBusFactory((context, cfg) =>
            {
                cfg.UsePostgres(dataSource);

                configure?.Invoke(context, cfg);
            }));
        }

        /// <summary>
        /// Configure the bus to use the PostgreSQL database transport
        /// </summary>
        /// <param name="configurator">The registration configurator (configured via AddViciOneServiceBus)</param>
        /// <param name="dataSourceProvider">Resolve the data source from the container</param>
        /// <param name="configure">The configuration callback for the bus factory</param>
        public static void UsingPostgres(this IBusRegistrationConfigurator configurator, Func<IBusRegistrationContext, NpgsqlDataSource> dataSourceProvider,
            Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? configure = null)
        {
            configurator.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, PostgresSendFailureClassifier>());
            configurator.SetBusFactory(new SqlRegistrationBusFactory((context, cfg) =>
            {
                cfg.UsePostgres(dataSourceProvider(context));

                configure?.Invoke(context, cfg);
            }));
        }
    }
}
