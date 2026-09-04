using System;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a dynamo db saga repository registration provider implementation.
/// </summary>
public class DynamoDbSagaRepositoryRegistrationProvider :
    ISagaRepositoryRegistrationProvider
{
    readonly Action<IDynamoDbSagaRepositoryConfigurator> _configure;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
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

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
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
