using System;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adapts container registrations and scoped consume context ownership for mediator configuration.</summary>
internal sealed class MediatorRegistrationContext :
    IMediatorRegistrationContext,
    ISetScopedConsumeContext
{
    readonly RegistrationContext _registration;

    /// <summary>Initializes the context over a resolved container registration.</summary>
    /// <param name="registration">The registration context that resolves and configures handlers.</param>
    public MediatorRegistrationContext(RegistrationContext registration)
    {
        _registration = registration ?? throw new ArgumentNullException(nameof(registration));
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType)
    {
        return _registration.GetService(serviceType);
    }

    /// <inheritdoc />
    public void ConfigureConsumer(Type consumerType, IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureConsumer(consumerType, configurator);
    }

    /// <inheritdoc />
    public void ConfigureConsumer<T>(IReceiveEndpointConfigurator configurator, Action<IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
    {
        _registration.ConfigureConsumer(configurator, configure);
    }

    /// <inheritdoc />
    public void ConfigureConsumers(IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureConsumers(configurator);
    }

    /// <inheritdoc />
    public void ConfigureConsumerKinds(IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureConsumerKinds(configurator);
    }

    /// <inheritdoc />
    public void ConfigureSaga(Type sagaType, IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureSaga(sagaType, configurator);
    }

    /// <inheritdoc />
    public void ConfigureSaga<T>(IReceiveEndpointConfigurator configurator, Action<ISagaConfigurator<T>>? configure = null)
        where T : class
    {
        _registration.ConfigureSaga(configurator, configure);
    }

    /// <inheritdoc />
    public void ConfigureSagas(IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureSagas(configurator);
    }

    /// <inheritdoc />
    public void ConfigureExecuteActivity(Type activityType, IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureExecuteActivity(activityType, configurator);
    }

    /// <inheritdoc />
    public void ConfigureActivity(Type activityType, IReceiveEndpointConfigurator executeEndpointConfigurator,
        IReceiveEndpointConfigurator compensateEndpointConfigurator)
    {
        _registration.ConfigureActivity(activityType, executeEndpointConfigurator, compensateEndpointConfigurator);
    }

    /// <inheritdoc />
    public void ConfigureActivityExecute(Type activityType, IReceiveEndpointConfigurator executeEndpointConfigurator, Uri compensateAddress)
    {
        _registration.ConfigureActivityExecute(activityType, executeEndpointConfigurator, compensateAddress);
    }

    /// <inheritdoc />
    public void ConfigureActivityCompensate(Type activityType, IReceiveEndpointConfigurator compensateEndpointConfigurator)
    {
        _registration.ConfigureActivityCompensate(activityType, compensateEndpointConfigurator);
    }

    /// <inheritdoc />
    public void ConfigureFuture(Type futureType, IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureFuture(futureType, configurator);
    }

    /// <inheritdoc />
    public void ConfigureFuture<T>(IReceiveEndpointConfigurator configurator)
        where T : class
    {
        _registration.ConfigureFuture<T>(configurator);
    }

    /// <inheritdoc />
    public IDisposable PushContext(IServiceScope scope, ConsumeContext context)
    {
        return _registration.PushContext(scope, context);
    }
}
