using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides registered consumers, sagas, activities, futures, and services while receive endpoints are configured.
/// </summary>
public interface IRegistrationContext :
    IServiceProvider
{
    /// <summary>Configures a registered consumer on a receive endpoint.</summary>
    /// <param name="consumerType">The registered consumer implementation type.</param>
    /// <param name="configurator">The receive endpoint to configure.</param>
    void ConfigureConsumer(Type consumerType, IReceiveEndpointConfigurator configurator);

    /// <summary>Configures a registered consumer on a receive endpoint and optionally customizes its pipeline.</summary>
    /// <typeparam name="TConsumer">The registered consumer implementation.</typeparam>
    /// <param name="configurator">The receive endpoint to configure.</param>
    /// <param name="configure">The optional callback that configures the consumer pipeline.</param>
    void ConfigureConsumer<TConsumer>(IReceiveEndpointConfigurator configurator, Action<IConsumerConfigurator<TConsumer>>? configure = null)
        where TConsumer : class, IConsumer;

    /// <summary>Configures every registered consumer on a receive endpoint.</summary>
    /// <param name="configurator">The receive endpoint to configure.</param>
    void ConfigureConsumers(IReceiveEndpointConfigurator configurator);

    /// <summary>Configures every registered handler category on the receive endpoint.</summary>
    /// <param name="configurator">The receive endpoint to configure.</param>
    void ConfigureConsumerKinds(IReceiveEndpointConfigurator configurator);

    /// <summary>Configures a registered saga on a receive endpoint.</summary>
    /// <param name="sagaType">The registered saga state type.</param>
    /// <param name="configurator">The receive endpoint to configure.</param>
    void ConfigureSaga(Type sagaType, IReceiveEndpointConfigurator configurator);

    /// <summary>Configures a registered saga on a receive endpoint and optionally customizes its pipeline.</summary>
    /// <typeparam name="TSaga">The registered saga state type.</typeparam>
    /// <param name="configurator">The receive endpoint to configure.</param>
    /// <param name="configure">The optional callback that configures the saga pipeline.</param>
    void ConfigureSaga<TSaga>(IReceiveEndpointConfigurator configurator, Action<ISagaConfigurator<TSaga>>? configure = null)
        where TSaga : class;

    /// <summary>Configures every registered saga on a receive endpoint.</summary>
    /// <param name="configurator">The receive endpoint to configure.</param>
    void ConfigureSagas(IReceiveEndpointConfigurator configurator);

    /// <summary>Configures the execution endpoint for a registered routing-slip activity.</summary>
    /// <param name="activityType">The registered activity implementation type.</param>
    /// <param name="configurator">The execution endpoint to configure.</param>
    void ConfigureExecuteActivity(Type activityType, IReceiveEndpointConfigurator configurator);

    /// <summary>Configures both execution and compensation endpoints for a registered routing-slip activity.</summary>
    /// <param name="activityType">The registered activity implementation type.</param>
    /// <param name="executeEndpointConfigurator">The execution endpoint to configure.</param>
    /// <param name="compensateEndpointConfigurator">The compensation endpoint to configure.</param>
    void ConfigureActivity(Type activityType, IReceiveEndpointConfigurator executeEndpointConfigurator,
        IReceiveEndpointConfigurator compensateEndpointConfigurator);

    /// <summary>Configures an activity execution endpoint that sends compensation to an existing endpoint.</summary>
    /// <param name="activityType">The registered activity implementation type.</param>
    /// <param name="executeEndpointConfigurator">The execution endpoint to configure.</param>
    /// <param name="compensateAddress">The endpoint that processes compensation requests.</param>
    void ConfigureActivityExecute(Type activityType, IReceiveEndpointConfigurator executeEndpointConfigurator, Uri compensateAddress);

    /// <summary>Configures the compensation endpoint for a registered routing-slip activity.</summary>
    /// <param name="activityType">The registered activity implementation type.</param>
    /// <param name="compensateEndpointConfigurator">The compensation endpoint to configure.</param>
    void ConfigureActivityCompensate(Type activityType, IReceiveEndpointConfigurator compensateEndpointConfigurator);

    /// <summary>Configures a registered future on a receive endpoint.</summary>
    /// <param name="futureType">The registered future implementation type.</param>
    /// <param name="configurator">The receive endpoint to configure.</param>
    void ConfigureFuture(Type futureType, IReceiveEndpointConfigurator configurator);

    /// <summary>Configures a registered future on a receive endpoint.</summary>
    /// <typeparam name="TFuture">The registered future implementation type.</typeparam>
    /// <param name="configurator">The receive endpoint to configure.</param>
    void ConfigureFuture<TFuture>(IReceiveEndpointConfigurator configurator)
        where TFuture : class;
}
