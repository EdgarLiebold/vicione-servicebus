using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Registration contains the consumers and sagas that have been registered, allowing them to be configured on one or more
/// receive endpoints.
/// </summary>
public interface IRegistrationContext :
    IServiceProvider
{
    /// <summary>Configure a consumer on the receive endpoint.</summary>
    /// <param name="consumerType">The consumer type.</param>
    /// <param name="configurator">The configurator to update.</param>
    void ConfigureConsumer(Type consumerType, IReceiveEndpointConfigurator configurator);

    /// <summary>Configure a consumer on the receive endpoint, with an optional configuration action.</summary>
    /// <typeparam name="T">The consumer type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    void ConfigureConsumer<T>(IReceiveEndpointConfigurator configurator, Action<IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer;

    /// <summary>Configure all registered consumers on the receive endpoint.</summary>
    /// <param name="configurator">The configurator to update.</param>
    void ConfigureConsumers(IReceiveEndpointConfigurator configurator);

    /// <summary>Configures every registered handler category on the receive endpoint.</summary>
    /// <param name="configurator">The configurator to update.</param>
    void ConfigureConsumerKinds(IReceiveEndpointConfigurator configurator);

    /// <summary>Configure a saga on the receive endpoint.</summary>
    /// <param name="sagaType">The saga type.</param>
    /// <param name="configurator">The configurator to update.</param>
    void ConfigureSaga(Type sagaType, IReceiveEndpointConfigurator configurator);

    /// <summary>Configure a saga on the receive endpoint, with an optional configuration action.</summary>
    /// <typeparam name="T">The saga type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    void ConfigureSaga<T>(IReceiveEndpointConfigurator configurator, Action<ISagaConfigurator<T>>? configure = null)
        where T : class;

    /// <summary>Configure all registered sagas on the receive endpoint.</summary>
    /// <param name="configurator">The configurator to update.</param>
    void ConfigureSagas(IReceiveEndpointConfigurator configurator);

    /// <summary>Configure the specified execute activity type.</summary>
    /// <param name="activityType">The runtime activity type used by the operation.</param>
    /// <param name="configurator">The configurator to update.</param>
    void ConfigureExecuteActivity(Type activityType, IReceiveEndpointConfigurator configurator);

    /// <summary>Configure the specified activity type.</summary>
    /// <param name="activityType">The runtime activity type used by the operation.</param>
    /// <param name="executeEndpointConfigurator">The configurator for the execute activity endpoint.</param>
    /// <param name="compensateEndpointConfigurator">The configurator for the compensate activity endpoint.</param>
    void ConfigureActivity(Type activityType, IReceiveEndpointConfigurator executeEndpointConfigurator,
        IReceiveEndpointConfigurator compensateEndpointConfigurator);

    /// <summary>Configure the specified activity type.</summary>
    /// <param name="activityType">The runtime activity type used by the operation.</param>
    /// <param name="executeEndpointConfigurator">The configurator for the execute activity endpoint.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    void ConfigureActivityExecute(Type activityType, IReceiveEndpointConfigurator executeEndpointConfigurator, Uri compensateAddress);

    /// <summary>Configure the specified activity type.</summary>
    /// <param name="activityType">The runtime activity type used by the operation.</param>
    /// <param name="compensateEndpointConfigurator">The configurator for the compensate activity endpoint.</param>
    void ConfigureActivityCompensate(Type activityType, IReceiveEndpointConfigurator compensateEndpointConfigurator);

    /// <summary>Configure a future on the receive endpoint.</summary>
    /// <param name="futureType">The saga type.</param>
    /// <param name="configurator">The configurator to update.</param>
    void ConfigureFuture(Type futureType, IReceiveEndpointConfigurator configurator);

    /// <summary>Configure a future on the receive endpoint, with an optional configuration action.</summary>
    /// <typeparam name="T">The saga type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    void ConfigureFuture<T>(IReceiveEndpointConfigurator configurator)
        where T : class;
}
