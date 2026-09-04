using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a registration context implementation.
/// </summary>
public class RegistrationContext :
    IRegistrationContext,
    ISetScopedConsumeContext
{
    readonly HashSet<Type> _configuredTypes;
    readonly IServiceProvider _provider;
    readonly ISetScopedConsumeContext _setScopedConsumeContext;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="selector">The selector value.</param>
    /// <param name="setScopedConsumeContext">The set scoped consume context value.</param>
    public RegistrationContext(IServiceProvider provider, IContainerSelector selector, ISetScopedConsumeContext setScopedConsumeContext)
    {
        Selector = selector;
        _provider = provider;
        _setScopedConsumeContext = setScopedConsumeContext;

        _configuredTypes = new HashSet<Type>();
    }

    /// <summary>
    /// Gets the selector value.
    /// </summary>
    protected IContainerSelector Selector { get; }

    /// <summary>
    /// Configures consumer.
    /// </summary>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="configurator">The configurator value.</param>
    public void ConfigureConsumer(Type consumerType, IReceiveEndpointConfigurator configurator)
    {
        if (!Selector.TryGetRegistration<IConsumerRegistration>(_provider, consumerType, out var consumer))
            throw new ArgumentException($"The consumer type was not found: {TypeCache.GetShortName(consumerType)}", nameof(consumerType));

        consumer.Configure(configurator, this);
        _configuredTypes.Add(consumerType);
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
        if (!Selector.TryGetRegistration<IConsumerRegistration>(_provider, typeof(T), out var consumer))
            throw new ArgumentException($"The consumer type was not found: {TypeCache.GetShortName(typeof(T))}", nameof(T));

        if (configure != null)
            consumer.AddConfigureAction<T>((_, cfg) => configure.Invoke(cfg));

        consumer.Configure(configurator, this);
        _configuredTypes.Add(typeof(T));
    }

    /// <summary>
    /// Configures consumers.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    public void ConfigureConsumers(IReceiveEndpointConfigurator configurator)
    {
        foreach (var consumer in Selector.GetRegistrations<IConsumerRegistration>(_provider).Where(x => !WasConfigured(x.Type)))
        {
            consumer.Configure(configurator, this);
            _configuredTypes.Add(consumer.Type);
        }
    }

    /// <summary>
    /// Configures saga.
    /// </summary>
    /// <param name="sagaType">The saga type value.</param>
    /// <param name="configurator">The configurator value.</param>
    public void ConfigureSaga(Type sagaType, IReceiveEndpointConfigurator configurator)
    {
        if (!Selector.TryGetRegistration<ISagaRegistration>(_provider, sagaType, out var saga))
            throw new ArgumentException($"The saga type was not found: {TypeCache.GetShortName(sagaType)}", nameof(sagaType));

        saga.Configure(configurator, this);
        _configuredTypes.Add(sagaType);
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
        if (!Selector.TryGetRegistration<ISagaRegistration>(_provider, typeof(T), out var saga))
            throw new ArgumentException($"The saga type was not found: {TypeCache.GetShortName(typeof(T))}", nameof(T));

        if (configure != null)
            saga.AddConfigureAction<T>((_, cfg) => configure.Invoke(cfg));

        saga.Configure(configurator, this);
        _configuredTypes.Add(typeof(T));
    }

    /// <summary>
    /// Configures sagas.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    public void ConfigureSagas(IReceiveEndpointConfigurator configurator)
    {
        foreach (var saga in Selector.GetRegistrations<ISagaRegistration>(_provider).Where(x => !WasConfigured(x.Type)))
        {
            saga.Configure(configurator, this);
            _configuredTypes.Add(saga.Type);
        }
    }

    /// <summary>
    /// Configures execute activity.
    /// </summary>
    /// <param name="activityType">The activity type value.</param>
    /// <param name="configurator">The configurator value.</param>
    public void ConfigureExecuteActivity(Type activityType, IReceiveEndpointConfigurator configurator)
    {
        if (!Selector.TryGetRegistration<IExecuteActivityRegistration>(_provider, activityType, out var activity))
            throw new ArgumentException($"The activity type was not found: {TypeCache.GetShortName(activityType)}", nameof(activityType));

        activity.Configure(configurator, this);
        _configuredTypes.Add(activityType);
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
        if (!Selector.TryGetRegistration<IActivityRegistration>(_provider, activityType, out var activity))
            throw new ArgumentException($"The activity type was not found: {TypeCache.GetShortName(activityType)}", nameof(activityType));

        activity.Configure(executeEndpointConfigurator, compensateEndpointConfigurator, this);
        _configuredTypes.Add(activityType);
    }

    /// <summary>
    /// Configures activity execute.
    /// </summary>
    /// <param name="activityType">The activity type value.</param>
    /// <param name="executeEndpointConfigurator">The execute endpoint configurator value.</param>
    /// <param name="compensateAddress">The compensate address value.</param>
    public void ConfigureActivityExecute(Type activityType, IReceiveEndpointConfigurator executeEndpointConfigurator, Uri compensateAddress)
    {
        if (!Selector.TryGetRegistration<IActivityRegistration>(_provider, activityType, out var activity))
            throw new ArgumentException($"The activity type was not found: {TypeCache.GetShortName(activityType)}", nameof(activityType));

        activity.ConfigureExecute(executeEndpointConfigurator, this, compensateAddress);
        _configuredTypes.Add(activityType);
    }

    /// <summary>
    /// Configures activity compensate.
    /// </summary>
    /// <param name="activityType">The activity type value.</param>
    /// <param name="compensateEndpointConfigurator">The compensate endpoint configurator value.</param>
    public void ConfigureActivityCompensate(Type activityType, IReceiveEndpointConfigurator compensateEndpointConfigurator)
    {
        if (!Selector.TryGetRegistration<IActivityRegistration>(_provider, activityType, out var activity))
            throw new ArgumentException($"The activity type was not found: {TypeCache.GetShortName(activityType)}", nameof(activityType));

        activity.ConfigureCompensate(compensateEndpointConfigurator, this);
        _configuredTypes.Add(activityType);
    }

    /// <summary>
    /// Configures future.
    /// </summary>
    /// <param name="futureType">The future type value.</param>
    /// <param name="configurator">The configurator value.</param>
    public void ConfigureFuture(Type futureType, IReceiveEndpointConfigurator configurator)
    {
        if (!Selector.TryGetRegistration<IFutureRegistration>(_provider, futureType, out var future))
            throw new ArgumentException($"The future type was not found: {TypeCache.GetShortName(futureType)}", nameof(futureType));

        future.Configure(configurator, this);
        _configuredTypes.Add(futureType);
    }

    /// <summary>
    /// Configures future.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void ConfigureFuture<T>(IReceiveEndpointConfigurator configurator)
        where T : class, ISaga
    {
        if (!Selector.TryGetRegistration<IFutureRegistration>(_provider, typeof(T), out var future))
            throw new ArgumentException($"The future type was not found: {TypeCache.GetShortName(typeof(T))}", nameof(T));

        future.Configure(configurator, this);
        _configuredTypes.Add(typeof(T));
    }

    /// <summary>
    /// Gets service.
    /// </summary>
    /// <param name="serviceType">The service type value.</param>
    /// <returns>The result of the operation.</returns>
    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IContainerSelector))
            return Selector;

        return _provider.GetService(serviceType);
    }

    /// <summary>
    /// Performs the push context operation.
    /// </summary>
    /// <param name="scope">The scope value.</param>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public IDisposable PushContext(IServiceScope scope, ConsumeContext context)
    {
        return _setScopedConsumeContext.PushContext(scope, context);
    }

    /// <summary>
    /// Performs the was configured operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    protected bool WasConfigured(Type type)
    {
        return _configuredTypes.Contains(type);
    }
}
