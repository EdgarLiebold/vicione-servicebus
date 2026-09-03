namespace ViciOne.ServiceBus.Configuration
{
    using System;


    public class AzureTableSagaRepositoryRegistrationProvider :
        ISagaRepositoryRegistrationProvider
    {
        readonly Action<IAzureTableSagaRepositoryConfigurator> _configure;

        public AzureTableSagaRepositoryRegistrationProvider(Action<IAzureTableSagaRepositoryConfigurator> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            _configure = configure;
        }

        public virtual void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
            where TSaga : class, ISaga
        {
            configurator.AzureTableRepository(_configure);
        }
    }
}
