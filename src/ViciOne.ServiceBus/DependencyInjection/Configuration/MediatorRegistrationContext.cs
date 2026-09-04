using System;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a mediator registration context implementation.
/// </summary>
public class MediatorRegistrationContext :
    IMediatorRegistrationContext,
    ISetScopedConsumeContext
{
    readonly RegistrationContext _registration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="registration">The registration value.</param>
    public MediatorRegistrationContext(RegistrationContext registration)
    {
        _registration = registration;
    }

    /// <summary>
    /// Gets service.
    /// </summary>
    /// <param name="serviceType">The service type value.</param>
    /// <returns>The result of the operation.</returns>
    public object? GetService(Type serviceType)
    {
        return _registration.GetService(serviceType);
    }

    /// <summary>
    /// Configures consumer.
    /// </summary>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="configurator">The configurator value.</param>
    public void ConfigureConsumer(Type consumerType, IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureConsumer(consumerType, configurator);
    }

    /// <summary>
    /// Configures consumer.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void ConfigureConsumer<T>(IReceiveEndpointConfigurator configurator, Action<IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
    {
        _registration.ConfigureConsumer(configurator, configure);
    }

    /// <summary>
    /// Configures consumers.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    public void ConfigureConsumers(IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureConsumers(configurator);
    }

    /// <summary>
    /// Configures saga.
    /// </summary>
    /// <param name="sagaType">The saga type value.</param>
    /// <param name="configurator">The configurator value.</param>
    public void ConfigureSaga(Type sagaType, IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureSaga(sagaType, configurator);
    }

    /// <summary>
    /// Configures saga.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void ConfigureSaga<T>(IReceiveEndpointConfigurator configurator, Action<ISagaConfigurator<T>>? configure = null)
        where T : class, ISaga
    {
        _registration.ConfigureSaga(configurator, configure);
    }

    /// <summary>
    /// Configures sagas.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    public void ConfigureSagas(IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureSagas(configurator);
    }

    /// <summary>
    /// Configures execute activity.
    /// </summary>
    /// <param name="activityType">The activity type value.</param>
    /// <param name="configurator">The configurator value.</param>
    public void ConfigureExecuteActivity(Type activityType, IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureExecuteActivity(activityType, configurator);
    }

    /// <summary>
    /// Configures activity.
    /// </summary>
    /// <param name="activityType">The activity type value.</param>
    /// <param name="executeEndpointConfigurator">The execute endpoint configurator value.</param>
    /// <param name="compensateEndpointConfigurator">The compensate endpoint configurator value.</param>
    public void ConfigureActivity(Type activityType, IReceiveEndpointConfigurator executeEndpointConfigurator,
        IReceiveEndpointConfigurator compensateEndpointConfigurator)
    {
        _registration.ConfigureActivity(activityType, executeEndpointConfigurator, compensateEndpointConfigurator);
    }

    /// <summary>
    /// Configures activity execute.
    /// </summary>
    /// <param name="activityType">The activity type value.</param>
    /// <param name="executeEndpointConfigurator">The execute endpoint configurator value.</param>
    /// <param name="compensateAddress">The compensate address value.</param>
    public void ConfigureActivityExecute(Type activityType, IReceiveEndpointConfigurator executeEndpointConfigurator, Uri compensateAddress)
    {
        _registration.ConfigureActivityExecute(activityType, executeEndpointConfigurator, compensateAddress);
    }

    /// <summary>
    /// Configures activity compensate.
    /// </summary>
    /// <param name="activityType">The activity type value.</param>
    /// <param name="compensateEndpointConfigurator">The compensate endpoint configurator value.</param>
    public void ConfigureActivityCompensate(Type activityType, IReceiveEndpointConfigurator compensateEndpointConfigurator)
    {
        _registration.ConfigureActivityCompensate(activityType, compensateEndpointConfigurator);
    }

    /// <summary>
    /// Configures future.
    /// </summary>
    /// <param name="futureType">The future type value.</param>
    /// <param name="configurator">The configurator value.</param>
    public void ConfigureFuture(Type futureType, IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureFuture(futureType, configurator);
    }

    /// <summary>
    /// Configures future.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void ConfigureFuture<T>(IReceiveEndpointConfigurator configurator)
        where T : class, ISaga
    {
        _registration.ConfigureFuture<T>(configurator);
    }

    /// <summary>
    /// Performs the push context operation.
    /// </summary>
    /// <param name="scope">The scope value.</param>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public IDisposable PushContext(IServiceScope scope, ConsumeContext context)
    {
        return _registration.PushContext(scope, context);
    }
}
