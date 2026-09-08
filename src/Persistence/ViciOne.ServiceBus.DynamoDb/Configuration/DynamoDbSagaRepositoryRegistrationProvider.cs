using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DynamoDb;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Sagas;

namespace ViciOne.ServiceBus.DynamoDb.Configuration;

/// <summary>Applies one Amazon DynamoDB repository configuration to compatible versioned saga types.</summary>
internal sealed class DynamoDbSagaRepositoryRegistrationProvider(
    Action<IDynamoDbSagaRepositoryConfigurator> configure) :
    ISagaRepositoryRegistrationProvider
{
    readonly Action<IDynamoDbSagaRepositoryConfigurator> _configure =
        configure ?? throw new ArgumentNullException(nameof(configure));

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
    /// <typeparam name="TSaga">The versioned saga state configured by the provider.</typeparam>
    /// <param name="configurator">The saga registration to update.</param>
    private void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISagaVersion
    {
        configurator.UseDynamoDb(repository => _configure(repository));
    }

    interface IProxy
    {
        void Configure(DynamoDbSagaRepositoryRegistrationProvider provider);
    }

    private sealed class Proxy<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator) :
        IProxy
        where TSaga : class, ISagaVersion
    {
        readonly ISagaRegistrationConfigurator<TSaga> _configurator =
            configurator ?? throw new ArgumentNullException(nameof(configurator));

        public void Configure(DynamoDbSagaRepositoryRegistrationProvider provider)
        {
            provider.Configure(_configurator);
        }
    }
}
