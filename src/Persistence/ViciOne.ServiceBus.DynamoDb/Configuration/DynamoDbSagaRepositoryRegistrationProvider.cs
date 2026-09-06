using System;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies one Amazon DynamoDB repository configuration to compatible versioned saga types.</summary>
public class DynamoDbSagaRepositoryRegistrationProvider :
    ISagaRepositoryRegistrationProvider
{
    readonly Action<IDynamoDbSagaRepositoryConfigurator> _configure;

    /// <summary>Creates a provider from the configuration callback applied to each compatible saga type.</summary>
    /// <param name="configure">The Amazon DynamoDB repository configuration callback.</param>
    public DynamoDbSagaRepositoryRegistrationProvider(Action<IDynamoDbSagaRepositoryConfigurator> configure)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    void ISagaRepositoryRegistrationProvider.Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class
    {
        if (typeof(TSaga).ImplementsInterface<ISagaVersion>())
        {
            var proxy = Activator.CreateInstance(typeof(Proxy<>).MakeGenericType(typeof(TSaga)), configurator) as IProxy
                ?? throw new InvalidOperationException($"Unable to create a DynamoDB saga repository proxy for {typeof(TSaga).FullName}.");

            proxy.Configure(this);
        }
    }

    /// <summary>Registers an Amazon DynamoDB repository for the specified versioned saga type.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="configurator">The saga registration to update.</param>
    protected virtual void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISagaVersion
    {
        configurator.DynamoDbRepository(r => _configure(r));
    }


    interface IProxy
    {
        public void Configure<T>(T provider)
            where T : DynamoDbSagaRepositoryRegistrationProvider;
    }


    class Proxy<TSaga> :
        IProxy
        where TSaga : class, ISagaVersion
    {
        readonly ISagaRegistrationConfigurator<TSaga> _configurator;

        public Proxy(ISagaRegistrationConfigurator<TSaga> configurator)
        {
            _configurator = configurator;
        }

        public void Configure<T>(T provider)
            where T : DynamoDbSagaRepositoryRegistrationProvider
        {
            provider.Configure(_configurator);
        }
    }
}
