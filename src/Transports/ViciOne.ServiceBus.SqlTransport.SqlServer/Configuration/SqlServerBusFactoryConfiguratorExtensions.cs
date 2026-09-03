namespace ViciOne.ServiceBus
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using SqlTransport.Configuration;
    using SqlTransport.SqlServer;


    public static class SqlServerBusFactoryConfiguratorExtensions
    {
        /// <summary>
        /// Configure the bus to use the SQL Server transport
        /// </summary>
        /// <param name="configurator">The registration configurator (configured via AddViciOneServiceBus)</param>
        /// <param name="configure">The configuration callback for the bus factory</param>
        public static void UsingSqlServer(this IBusRegistrationConfigurator configurator,
            Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? configure = null)
        {
            configurator.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, SqlServerSendFailureClassifier>());
            configurator.SetBusFactory(new SqlRegistrationBusFactory((context, cfg) =>
            {
                cfg.UseSqlServer(context);

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
        public static void UsingSqlServer(this IBusRegistrationConfigurator configurator, string connectionString,
            Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>? configure = null)
        {
            configurator.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, SqlServerSendFailureClassifier>());
            configurator.SetBusFactory(new SqlRegistrationBusFactory((context, cfg) =>
            {
                cfg.UseSqlServer(connectionString);

                configure?.Invoke(context, cfg);
            }));
        }
    }
}
