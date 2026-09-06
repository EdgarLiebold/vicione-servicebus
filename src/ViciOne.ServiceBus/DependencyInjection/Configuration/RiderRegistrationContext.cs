using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Carries state for rider registration operations.</summary>
public class RiderRegistrationContext :
    ISetScopedConsumeContext,
    IRiderRegistrationContext
{
    readonly RegistrationContext _registration;

    readonly IContainerSelector _selector;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="registration">The registration.</param>
    /// <param name="selector">The selector.</param>
    public RiderRegistrationContext(RegistrationContext registration, IContainerSelector selector)
    {
        _registration = registration;
        _selector = selector;
    }

    /// <summary>Gets registrations.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The registrations.</returns>
    public IEnumerable<T> GetRegistrations<T>()
        where T : class, IRegistration
    {
        return _selector.GetRegistrations<T>(_registration);
    }

    /// <summary>Gets service.</summary>
    /// <param name="serviceType">The runtime service type used by the operation.</param>
    /// <returns>The service.</returns>
    public object? GetService(Type serviceType)
    {
        return _registration.GetService(serviceType);
    }

    /// <summary>Configures consumer.</summary>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="configurator">The configurator to update.</param>
    public void ConfigureConsumer(Type consumerType, IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureConsumer(consumerType, configurator);
    }

    /// <summary>Configures consumer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public void ConfigureConsumer<T>(IReceiveEndpointConfigurator configurator, Action<IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
    {
        _registration.ConfigureConsumer(configurator, configure);
    }

    /// <summary>Configures consumers.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public void ConfigureConsumers(IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureConsumers(configurator);
    }

    /// <summary>Configures every registered handler category on the receive endpoint.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public void ConfigureConsumerKinds(IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureConsumerKinds(configurator);
    }

    /// <summary>Configures saga.</summary>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="configurator">The configurator to update.</param>
    public void ConfigureSaga(Type sagaType, IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureSaga(sagaType, configurator);
    }

    /// <summary>Configures saga.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public void ConfigureSaga<T>(IReceiveEndpointConfigurator configurator, Action<ISagaConfigurator<T>>? configure = null)
        where T : class
    {
        _registration.ConfigureSaga(configurator, configure);
    }

    /// <summary>Configures sagas.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public void ConfigureSagas(IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureSagas(configurator);
    }

    /// <summary>Configures execute activity.</summary>
    /// <param name="activityType">The runtime activity type used by the operation.</param>
    /// <param name="configurator">The configurator to update.</param>
    public void ConfigureExecuteActivity(Type activityType, IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureExecuteActivity(activityType, configurator);
    }

    /// <summary>Configures activity.</summary>
    /// <param name="activityType">The runtime activity type used by the operation.</param>
    /// <param name="executeEndpointConfigurator">The execute endpoint configurator.</param>
    /// <param name="compensateEndpointConfigurator">The compensate endpoint configurator.</param>
    public void ConfigureActivity(Type activityType, IReceiveEndpointConfigurator executeEndpointConfigurator,
        IReceiveEndpointConfigurator compensateEndpointConfigurator)
    {
        _registration.ConfigureActivity(activityType, executeEndpointConfigurator, compensateEndpointConfigurator);
    }

    /// <summary>Configures activity execute.</summary>
    /// <param name="activityType">The runtime activity type used by the operation.</param>
    /// <param name="executeEndpointConfigurator">The execute endpoint configurator.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    public void ConfigureActivityExecute(Type activityType, IReceiveEndpointConfigurator executeEndpointConfigurator, Uri compensateAddress)
    {
        _registration.ConfigureActivityExecute(activityType, executeEndpointConfigurator, compensateAddress);
    }

    /// <summary>Configures activity compensate.</summary>
    /// <param name="activityType">The runtime activity type used by the operation.</param>
    /// <param name="compensateEndpointConfigurator">The compensate endpoint configurator.</param>
    public void ConfigureActivityCompensate(Type activityType, IReceiveEndpointConfigurator compensateEndpointConfigurator)
    {
        _registration.ConfigureActivityCompensate(activityType, compensateEndpointConfigurator);
    }

    /// <summary>Configures future.</summary>
    /// <param name="futureType">The runtime future type used by the operation.</param>
    /// <param name="configurator">The configurator to update.</param>
    public void ConfigureFuture(Type futureType, IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureFuture(futureType, configurator);
    }

    /// <summary>Configures future.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void ConfigureFuture<T>(IReceiveEndpointConfigurator configurator)
        where T : class
    {
        _registration.ConfigureFuture<T>(configurator);
    }

    /// <summary>Pushes context.</summary>
    /// <param name="scope">The scope.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The disposable produced by the operation.</returns>
    public IDisposable PushContext(IServiceScope scope, ConsumeContext context)
    {
        return _registration.PushContext(scope, context);
    }
}
