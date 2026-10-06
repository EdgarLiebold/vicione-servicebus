using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection.Registration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Resolves registered endpoint components and tracks which ones have been attached.</summary>
internal class RegistrationContext :
    IRegistrationContext,
    ISetScopedConsumeContext
{
    readonly HashSet<Type> _configuredTypes;
    readonly IServiceProvider _provider;
    readonly ISetScopedConsumeContext _setScopedConsumeContext;

    /// <summary>Creates a registration context over a service provider and registration selector.</summary>
    /// <param name="provider">The service provider that contains registered components.</param>
    /// <param name="selector">The component registration selector.</param>
    /// <param name="setScopedConsumeContext">The accessor that installs and restores ambient consume contexts.</param>
    public RegistrationContext(IServiceProvider provider, IContainerSelector selector, ISetScopedConsumeContext setScopedConsumeContext)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(setScopedConsumeContext);

        Selector = selector;
        _provider = provider;
        _setScopedConsumeContext = setScopedConsumeContext;

        _configuredTypes = new HashSet<Type>();
    }

    /// <summary>Gets the selector used to resolve component registrations.</summary>
    protected IContainerSelector Selector { get; }

    /// <summary>Attaches a runtime-selected registered consumer to a receive endpoint.</summary>
    /// <param name="consumerType">The registered consumer implementation.</param>
    /// <param name="configurator">The receive endpoint being configured.</param>
    public void ConfigureConsumer(Type consumerType, IReceiveEndpointConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(consumerType);
        ArgumentNullException.ThrowIfNull(configurator);

        if (!Selector.TryGetRegistration<IConsumerRegistration>(_provider, consumerType, out var consumer))
            throw new ArgumentException($"The consumer type was not found: {TypeCache.GetShortName(consumerType)}", nameof(consumerType));

        consumer.Configure(configurator, this);
        _configuredTypes.Add(consumerType);
    }

    /// <summary>Attaches a registered consumer to a receive endpoint.</summary>
    /// <typeparam name="T">The registered consumer implementation.</typeparam>
    /// <param name="configurator">The receive endpoint being configured.</param>
    /// <param name="configure">An optional callback applied to the consumer before attachment.</param>
    public void ConfigureConsumer<T>(IReceiveEndpointConfigurator configurator, Action<IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(configurator);

        if (!Selector.TryGetRegistration<IConsumerRegistration>(_provider, typeof(T), out var consumer))
            throw new ArgumentException($"The consumer type was not found: {TypeCache.GetShortName(typeof(T))}", nameof(T));

        if (configure == null)
            consumer.Configure(configurator, this);
        else
        {
            if (consumer is not ConsumerRegistration<T> registration)
                throw EndpointRegistrationConfiguration.Unsupported(consumer, this, "Consumer configuration");

            EndpointRegistrationConfiguration.RequireDefaultDispatch(consumer, typeof(IConsumerRegistration),
                typeof(ConsumerRegistration<T>), this, "Consumer configuration");
            registration.Configure(configurator, this, configure);
        }
        _configuredTypes.Add(typeof(T));
    }

    /// <summary>Attaches every registered consumer not already configured by this context.</summary>
    /// <param name="configurator">The receive endpoint being configured.</param>
    public void ConfigureConsumers(IReceiveEndpointConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        foreach (var consumer in Selector.GetRegistrations<IConsumerRegistration>(_provider).Where(x => !WasConfigured(x.Type)))
        {
            consumer.Configure(configurator, this);
            _configuredTypes.Add(consumer.Type);
        }
    }

    /// <summary>Attaches every registered consumer-kind capability in deterministic precedence order.</summary>
    /// <param name="configurator">The receive endpoint being configured.</param>
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

    /// <summary>Attaches a runtime-selected registered saga to a receive endpoint.</summary>
    /// <param name="sagaType">The registered saga implementation.</param>
    /// <param name="configurator">The receive endpoint being configured.</param>
    public void ConfigureSaga(Type sagaType, IReceiveEndpointConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(sagaType);
        ArgumentNullException.ThrowIfNull(configurator);

        IConsumerKindRuntimeConfigurator sagaKind = GetConsumerKindCapability<IConsumerKindRuntimeConfigurator>("Saga");
        if (!sagaKind.TryConfigure(sagaType, configurator, this))
            throw new ArgumentException($"The saga type was not found: {TypeCache.GetShortName(sagaType)}", nameof(sagaType));

        _configuredTypes.Add(sagaType);
    }

    /// <summary>Attaches a registered saga to a receive endpoint.</summary>
    /// <typeparam name="T">The registered saga implementation.</typeparam>
    /// <param name="configurator">The receive endpoint being configured.</param>
    /// <param name="configure">An optional callback applied to the saga before attachment.</param>
    public void ConfigureSaga<T>(IReceiveEndpointConfigurator configurator, Action<ISagaConfigurator<T>>? configure = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        IConsumerKindTypedConfigurator sagaKind = GetConsumerKindCapability<IConsumerKindTypedConfigurator>("Saga");
        if (!sagaKind.TryConfigure<T>(configurator, this, configure))
            throw new ArgumentException($"The saga type was not found: {TypeCache.GetShortName(typeof(T))}", nameof(T));

        _configuredTypes.Add(typeof(T));
    }

    /// <summary>Attaches every registered saga not already configured by this context.</summary>
    /// <param name="configurator">The receive endpoint being configured.</param>
    public void ConfigureSagas(IReceiveEndpointConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        IConsumerKindBulkConfigurator sagaKind = GetConsumerKindCapability<IConsumerKindBulkConfigurator>("Saga");
        foreach (Type configuredType in sagaKind.ConfigureAll(configurator, this, _configuredTypes))
            _configuredTypes.Add(configuredType);
    }

    /// <summary>Attaches a runtime-selected routing-slip execute activity to a receive endpoint.</summary>
    /// <param name="activityType">The registered execute-activity implementation.</param>
    /// <param name="configurator">The receive endpoint being configured.</param>
    public void ConfigureExecuteActivity(Type activityType, IReceiveEndpointConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(activityType);
        ArgumentNullException.ThrowIfNull(configurator);

        IConsumerKindRuntimeConfigurator activityKind = GetConsumerKindCapability<IConsumerKindRuntimeConfigurator>("ExecuteActivity");
        if (!activityKind.TryConfigure(activityType, configurator, this))
            throw new ArgumentException($"The activity type was not found: {TypeCache.GetShortName(activityType)}", nameof(activityType));

        _configuredTypes.Add(activityType);
    }

    /// <summary>Attaches both endpoints of a compensating routing-slip activity.</summary>
    /// <param name="activityType">The registered activity implementation.</param>
    /// <param name="executeEndpointConfigurator">The execute receive endpoint.</param>
    /// <param name="compensateEndpointConfigurator">The compensate receive endpoint.</param>
    public void ConfigureActivity(Type activityType, IReceiveEndpointConfigurator executeEndpointConfigurator,
        IReceiveEndpointConfigurator compensateEndpointConfigurator)
    {
        ArgumentNullException.ThrowIfNull(activityType);
        ArgumentNullException.ThrowIfNull(executeEndpointConfigurator);
        ArgumentNullException.ThrowIfNull(compensateEndpointConfigurator);

        IConsumerKindCompanionConfigurator activityKind = GetConsumerKindCapability<IConsumerKindCompanionConfigurator>("Activity");
        if (!activityKind.TryConfigurePair(activityType, executeEndpointConfigurator, compensateEndpointConfigurator, this))
            throw new ArgumentException($"The activity type was not found: {TypeCache.GetShortName(activityType)}", nameof(activityType));

        _configuredTypes.Add(activityType);
    }

    /// <summary>Attaches the execute endpoint of a compensating activity and links its compensate address.</summary>
    /// <param name="activityType">The registered activity implementation.</param>
    /// <param name="executeEndpointConfigurator">The execute receive endpoint.</param>
    /// <param name="compensateAddress">The destination of the companion compensate endpoint.</param>
    public void ConfigureActivityExecute(Type activityType, IReceiveEndpointConfigurator executeEndpointConfigurator, Uri compensateAddress)
    {
        ArgumentNullException.ThrowIfNull(activityType);
        ArgumentNullException.ThrowIfNull(executeEndpointConfigurator);
        ArgumentNullException.ThrowIfNull(compensateAddress);

        IConsumerKindCompanionConfigurator activityKind = GetConsumerKindCapability<IConsumerKindCompanionConfigurator>("Activity");
        if (!activityKind.TryConfigurePrimary(activityType, executeEndpointConfigurator, compensateAddress, this))
            throw new ArgumentException($"The activity type was not found: {TypeCache.GetShortName(activityType)}", nameof(activityType));

        _configuredTypes.Add(activityType);
    }

    /// <summary>Attaches the compensate endpoint of a registered routing-slip activity.</summary>
    /// <param name="activityType">The registered activity implementation.</param>
    /// <param name="compensateEndpointConfigurator">The compensate receive endpoint.</param>
    public void ConfigureActivityCompensate(Type activityType, IReceiveEndpointConfigurator compensateEndpointConfigurator)
    {
        ArgumentNullException.ThrowIfNull(activityType);
        ArgumentNullException.ThrowIfNull(compensateEndpointConfigurator);

        IConsumerKindCompanionConfigurator activityKind = GetConsumerKindCapability<IConsumerKindCompanionConfigurator>("Activity");
        if (!activityKind.TryConfigureCompanion(activityType, compensateEndpointConfigurator, this))
            throw new ArgumentException($"The activity type was not found: {TypeCache.GetShortName(activityType)}", nameof(activityType));

        _configuredTypes.Add(activityType);
    }

    /// <summary>Attaches a runtime-selected registered future to a receive endpoint.</summary>
    /// <param name="futureType">The registered future implementation.</param>
    /// <param name="configurator">The receive endpoint being configured.</param>
    public void ConfigureFuture(Type futureType, IReceiveEndpointConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(futureType);
        ArgumentNullException.ThrowIfNull(configurator);

        IConsumerKindRuntimeConfigurator futureKind = GetConsumerKindCapability<IConsumerKindRuntimeConfigurator>("Future");
        if (!futureKind.TryConfigure(futureType, configurator, this))
            throw new ArgumentException($"The future type was not found: {TypeCache.GetShortName(futureType)}", nameof(futureType));

        _configuredTypes.Add(futureType);
    }

    /// <summary>Attaches a registered future to a receive endpoint.</summary>
    /// <typeparam name="T">The registered future implementation.</typeparam>
    /// <param name="configurator">The receive endpoint being configured.</param>
    public void ConfigureFuture<T>(IReceiveEndpointConfigurator configurator)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        IConsumerKindTypedConfigurator futureKind = GetConsumerKindCapability<IConsumerKindTypedConfigurator>("Future");
        if (!futureKind.TryConfigure<T>(configurator, this))
            throw new ArgumentException($"The future type was not found: {TypeCache.GetShortName(typeof(T))}", nameof(T));

        _configuredTypes.Add(typeof(T));
    }

    /// <summary>Resolves a service while exposing this context's registration selector.</summary>
    /// <param name="serviceType">The requested service contract.</param>
    /// <returns>The resolved service, or <see langword="null" /> when the contract is not registered.</returns>
    public object? GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceType == typeof(IContainerSelector))
            return Selector;

        return _provider.GetService(serviceType);
    }

    /// <summary>Installs a consume context for the lifetime of a dependency-injection scope.</summary>
    /// <param name="scope">The active message scope.</param>
    /// <param name="context">The consume context exposed within that scope.</param>
    /// <returns>A handle that restores the previous ambient context when disposed.</returns>
    public IDisposable PushContext(IServiceScope scope, ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(context);

        return _setScopedConsumeContext.PushContext(scope, context);
    }

    /// <summary>Determines whether the specified type has already been configured.</summary>
    /// <param name="type">The component type.</param>
    /// <returns><see langword="true" /> when this context has already attached the component.</returns>
    protected bool WasConfigured(Type type)
    {
        return _configuredTypes.Contains(type);
    }

    /// <summary>Records a handler type as configured for this registration context.</summary>
    /// <param name="type">The component type.</param>
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
