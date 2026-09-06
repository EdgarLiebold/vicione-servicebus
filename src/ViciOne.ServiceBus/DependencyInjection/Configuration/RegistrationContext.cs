using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Carries state for registration operations.</summary>
public class RegistrationContext :
    IRegistrationContext,
    ISetScopedConsumeContext
{
    readonly HashSet<Type> _configuredTypes;
    readonly IServiceProvider _provider;
    readonly ISetScopedConsumeContext _setScopedConsumeContext;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <param name="selector">The selector.</param>
    /// <param name="setScopedConsumeContext">The set scoped consume context.</param>
    public RegistrationContext(IServiceProvider provider, IContainerSelector selector, ISetScopedConsumeContext setScopedConsumeContext)
    {
        Selector = selector;
        _provider = provider;
        _setScopedConsumeContext = setScopedConsumeContext;

        _configuredTypes = new HashSet<Type>();
    }

    /// <summary>Gets the selector.</summary>
    protected IContainerSelector Selector { get; }

    /// <summary>Configures consumer.</summary>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="configurator">The configurator to update.</param>
    public void ConfigureConsumer(Type consumerType, IReceiveEndpointConfigurator configurator)
    {
        if (!Selector.TryGetRegistration<IConsumerRegistration>(_provider, consumerType, out var consumer))
            throw new ArgumentException($"The consumer type was not found: {TypeCache.GetShortName(consumerType)}", nameof(consumerType));

        consumer.Configure(configurator, this);
        _configuredTypes.Add(consumerType);
    }

    /// <summary>Configures consumer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
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

    /// <summary>Configures consumers.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public void ConfigureConsumers(IReceiveEndpointConfigurator configurator)
    {
        foreach (var consumer in Selector.GetRegistrations<IConsumerRegistration>(_provider).Where(x => !WasConfigured(x.Type)))
        {
            consumer.Configure(configurator, this);
            _configuredTypes.Add(consumer.Type);
        }
    }

    /// <summary>Configures every registered handler category on the receive endpoint.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public void ConfigureConsumerKinds(IReceiveEndpointConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        IEnumerable<IConsumerKind> kinds =
            (IEnumerable<IConsumerKind>?)_provider.GetService(typeof(IEnumerable<IConsumerKind>))
            ?? Array.Empty<IConsumerKind>();

        foreach (IConsumerKind kind in kinds.OrderBy(candidate => candidate.IsFallback)
                     .ThenBy(candidate => candidate.Order)
                     .ThenBy(candidate => candidate.Name, StringComparer.Ordinal))
        {
            if (kind is not IConsumerKindBulkConfigurator bulkConfigurator)
                continue;

            foreach (Type configuredType in bulkConfigurator.ConfigureAll(configurator, this, _configuredTypes))
                _configuredTypes.Add(configuredType);
        }
    }

    /// <summary>Configures saga.</summary>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="configurator">The configurator to update.</param>
    public void ConfigureSaga(Type sagaType, IReceiveEndpointConfigurator configurator)
    {
        IConsumerKindRuntimeConfigurator sagaKind = GetConsumerKindCapability<IConsumerKindRuntimeConfigurator>("Saga");
        if (!sagaKind.TryConfigure(sagaType, configurator, this))
            throw new ArgumentException($"The saga type was not found: {TypeCache.GetShortName(sagaType)}", nameof(sagaType));

        _configuredTypes.Add(sagaType);
    }

    /// <summary>Configures saga.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public void ConfigureSaga<T>(IReceiveEndpointConfigurator configurator, Action<ISagaConfigurator<T>>? configure = null)
        where T : class
    {
        IConsumerKindTypedConfigurator sagaKind = GetConsumerKindCapability<IConsumerKindTypedConfigurator>("Saga");
        if (!sagaKind.TryConfigure<T>(configurator, this, configure))
            throw new ArgumentException($"The saga type was not found: {TypeCache.GetShortName(typeof(T))}", nameof(T));

        _configuredTypes.Add(typeof(T));
    }

    /// <summary>Configures sagas.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public void ConfigureSagas(IReceiveEndpointConfigurator configurator)
    {
        IConsumerKindBulkConfigurator sagaKind = GetConsumerKindCapability<IConsumerKindBulkConfigurator>("Saga");
        foreach (Type configuredType in sagaKind.ConfigureAll(configurator, this, _configuredTypes))
            _configuredTypes.Add(configuredType);
    }

    /// <summary>Configures execute activity.</summary>
    /// <param name="activityType">The runtime activity type used by the operation.</param>
    /// <param name="configurator">The configurator to update.</param>
    public void ConfigureExecuteActivity(Type activityType, IReceiveEndpointConfigurator configurator)
    {
        IConsumerKindRuntimeConfigurator activityKind = GetConsumerKindCapability<IConsumerKindRuntimeConfigurator>("ExecuteActivity");
        if (!activityKind.TryConfigure(activityType, configurator, this))
            throw new ArgumentException($"The activity type was not found: {TypeCache.GetShortName(activityType)}", nameof(activityType));

        _configuredTypes.Add(activityType);
    }

    /// <summary>Configures activity.</summary>
    /// <param name="activityType">The runtime activity type used by the operation.</param>
    /// <param name="executeEndpointConfigurator">The execute endpoint configurator.</param>
    /// <param name="compensateEndpointConfigurator">The compensate endpoint configurator.</param>
    public void ConfigureActivity(Type activityType, IReceiveEndpointConfigurator executeEndpointConfigurator,
        IReceiveEndpointConfigurator compensateEndpointConfigurator)
    {
        IConsumerKindCompanionConfigurator activityKind = GetConsumerKindCapability<IConsumerKindCompanionConfigurator>("Activity");
        if (!activityKind.TryConfigurePair(activityType, executeEndpointConfigurator, compensateEndpointConfigurator, this))
            throw new ArgumentException($"The activity type was not found: {TypeCache.GetShortName(activityType)}", nameof(activityType));

        _configuredTypes.Add(activityType);
    }

    /// <summary>Configures activity execute.</summary>
    /// <param name="activityType">The runtime activity type used by the operation.</param>
    /// <param name="executeEndpointConfigurator">The execute endpoint configurator.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    public void ConfigureActivityExecute(Type activityType, IReceiveEndpointConfigurator executeEndpointConfigurator, Uri compensateAddress)
    {
        IConsumerKindCompanionConfigurator activityKind = GetConsumerKindCapability<IConsumerKindCompanionConfigurator>("Activity");
        if (!activityKind.TryConfigurePrimary(activityType, executeEndpointConfigurator, compensateAddress, this))
            throw new ArgumentException($"The activity type was not found: {TypeCache.GetShortName(activityType)}", nameof(activityType));

        _configuredTypes.Add(activityType);
    }

    /// <summary>Configures activity compensate.</summary>
    /// <param name="activityType">The runtime activity type used by the operation.</param>
    /// <param name="compensateEndpointConfigurator">The compensate endpoint configurator.</param>
    public void ConfigureActivityCompensate(Type activityType, IReceiveEndpointConfigurator compensateEndpointConfigurator)
    {
        IConsumerKindCompanionConfigurator activityKind = GetConsumerKindCapability<IConsumerKindCompanionConfigurator>("Activity");
        if (!activityKind.TryConfigureCompanion(activityType, compensateEndpointConfigurator, this))
            throw new ArgumentException($"The activity type was not found: {TypeCache.GetShortName(activityType)}", nameof(activityType));

        _configuredTypes.Add(activityType);
    }

    /// <summary>Configures future.</summary>
    /// <param name="futureType">The runtime future type used by the operation.</param>
    /// <param name="configurator">The configurator to update.</param>
    public void ConfigureFuture(Type futureType, IReceiveEndpointConfigurator configurator)
    {
        IConsumerKindRuntimeConfigurator futureKind = GetConsumerKindCapability<IConsumerKindRuntimeConfigurator>("Future");
        if (!futureKind.TryConfigure(futureType, configurator, this))
            throw new ArgumentException($"The future type was not found: {TypeCache.GetShortName(futureType)}", nameof(futureType));

        _configuredTypes.Add(futureType);
    }

    /// <summary>Configures future.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void ConfigureFuture<T>(IReceiveEndpointConfigurator configurator)
        where T : class
    {
        IConsumerKindTypedConfigurator futureKind = GetConsumerKindCapability<IConsumerKindTypedConfigurator>("Future");
        if (!futureKind.TryConfigure<T>(configurator, this))
            throw new ArgumentException($"The future type was not found: {TypeCache.GetShortName(typeof(T))}", nameof(T));

        _configuredTypes.Add(typeof(T));
    }

    /// <summary>Gets service.</summary>
    /// <param name="serviceType">The runtime service type used by the operation.</param>
    /// <returns>The service.</returns>
    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IContainerSelector))
            return Selector;

        return _provider.GetService(serviceType);
    }

    /// <summary>Pushes context.</summary>
    /// <param name="scope">The scope.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The disposable produced by the operation.</returns>
    public IDisposable PushContext(IServiceScope scope, ConsumeContext context)
    {
        return _setScopedConsumeContext.PushContext(scope, context);
    }

    /// <summary>Determines whether the specified type has already been configured.</summary>
    /// <param name="type">The runtime type to inspect or use.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    protected bool WasConfigured(Type type)
    {
        return _configuredTypes.Contains(type);
    }

    /// <summary>Records a handler type as configured for this registration context.</summary>
    /// <param name="type">The runtime type to inspect or use.</param>
    protected void MarkConfigured(Type type)
    {
        _configuredTypes.Add(type);
    }

    IConsumerKind GetConsumerKind(string name)
    {
        IEnumerable<IConsumerKind> kinds =
            (IEnumerable<IConsumerKind>?)_provider.GetService(typeof(IEnumerable<IConsumerKind>))
            ?? Array.Empty<IConsumerKind>();

        return kinds.SingleOrDefault(kind => string.Equals(kind.Name, name, StringComparison.Ordinal))
            ?? throw new ConfigurationException(
                Providers.Configuration.ConfigurationMessages.Create("Consumer kind", name,
                    $"The {name} capability is not registered",
                    $"Reference and register the ViciOne.ServiceBus.{name}s package before configuring {name.ToLowerInvariant()} handlers"));
    }

    TCapability GetConsumerKindCapability<TCapability>(string name)
        where TCapability : class
    {
        IConsumerKind kind = GetConsumerKind(name);
        return kind as TCapability
            ?? throw new ConfigurationException(
                Providers.Configuration.ConfigurationMessages.Create("Consumer kind", name,
                    $"The {name} capability does not support {TypeCache.GetShortName(typeof(TCapability))}",
                    "Register a consumer kind that implements the required capability contract"));
    }
}
