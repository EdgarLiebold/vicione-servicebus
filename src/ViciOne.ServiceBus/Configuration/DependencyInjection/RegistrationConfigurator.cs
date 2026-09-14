using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection.Registration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Coordinates component registrations, request-client defaults, and capability completion.</summary>
public abstract class RegistrationConfigurator :
    IRegistrationConfigurator,
    IAdvancedRegistrationConfigurator
{
    readonly IServiceCollection _collection;
    readonly Dictionary<Type, IRegistrationCompletionParticipant> _registrationCompletionParticipants = new();
    bool _completionStarted;
    bool _completed;
    bool _configured;

    /// <summary>Creates a configurator over a service collection and its container registrar.</summary>
    /// <param name="collection">The service collection that owns the registrations.</param>
    /// <param name="registrar">The container-specific registration strategy.</param>
    protected RegistrationConfigurator(IServiceCollection collection, IContainerRegistrar registrar)
    {
        _collection = collection ?? throw new ArgumentNullException(nameof(collection));

        Registrar = registrar ?? throw new ArgumentNullException(nameof(registrar));
    }

    /// <summary>Gets the container-specific registration strategy.</summary>
    public IContainerRegistrar Registrar { get; }

    /// <summary>Gets the service collection being configured.</summary>
    public IServiceCollection Services => _collection;

    /// <inheritdoc />
    public virtual Type BusType => typeof(IBus);

    /// <summary>Gets the timeout inherited by request clients that omit an explicit value.</summary>
    protected RequestTimeout DefaultRequestTimeout { get; private set; } = RequestTimeout.Default;

    /// <summary>Registers a consumer and an optional endpoint-time configuration callback.</summary>
    /// <typeparam name="T">The consumer implementation.</typeparam>
    /// <param name="configure">The callback that receives the active registration context and consumer configurator.</param>
    /// <returns>A fluent configurator for the consumer registration.</returns>
    public IConsumerRegistrationConfigurator<T> AddConsumer<T>(Action<IRegistrationContext, IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
    {
        return AddConsumer(null, configure);
    }

    /// <summary>Registers a consumer with an optional runtime definition type and endpoint-time callback.</summary>
    /// <typeparam name="T">The consumer implementation.</typeparam>
    /// <param name="consumerDefinitionType">The consumer definition implementation, or <see langword="null" /> to use convention defaults.</param>
    /// <param name="configure">The callback that receives the active registration context and consumer configurator.</param>
    /// <returns>A fluent configurator for the consumer registration.</returns>
    public IConsumerRegistrationConfigurator<T> AddConsumer<T>(Type? consumerDefinitionType,
        Action<IRegistrationContext, IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
    {
        var registration = _collection.RegisterConsumer<T>(Registrar, consumerDefinitionType);

        registration.AddConfigureAction(configure);

        return new ConsumerRegistrationConfigurator<T>(this, registration);
    }

    /// <summary>Registers an endpoint definition selected at runtime.</summary>
    /// <param name="endpointDefinitionType">The concrete endpoint definition type.</param>
    public void AddEndpoint(Type endpointDefinitionType)
    {
        ArgumentNullException.ThrowIfNull(endpointDefinitionType);

        _collection.RegisterEndpoint(Registrar, endpointDefinitionType);
    }

    /// <summary>Registers a typed endpoint definition for an existing component registration.</summary>
    /// <typeparam name="TDefinition">The concrete endpoint definition.</typeparam>
    /// <typeparam name="T">The component associated with the endpoint.</typeparam>
    /// <param name="registration">The component registration that owns the endpoint.</param>
    /// <param name="settings">Optional settings passed explicitly to the definition constructor.</param>
    public void AddEndpoint<TDefinition, T>(IRegistration registration, IEndpointSettings<IEndpointDefinition<T>>? settings = null)
        where TDefinition : class, IEndpointDefinition<T>
        where T : class
    {
        _collection.RegisterEndpoint<TDefinition, T>(Registrar, registration, settings);
    }

    /// <summary>Registers a request client whose destination is resolved from message topology.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="timeout">An explicit timeout, or an unspecified value to inherit the configured default.</param>
    public void AddRequestClient<T>(RequestTimeout timeout = default)
        where T : class
    {
        Registrar.RegisterRequestClient<T>(GetRequestTimeout(timeout));
    }

    /// <summary>Registers a request client bound to an explicit destination.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="destinationAddress">The address to which requests are sent.</param>
    /// <param name="timeout">An explicit timeout, or an unspecified value to inherit the configured default.</param>
    public void AddRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default)
        where T : class
    {
        Registrar.RegisterRequestClient<T>(destinationAddress, GetRequestTimeout(timeout));
    }

    /// <summary>Registers a runtime-selected request client whose destination is resolved from message topology.</summary>
    /// <param name="requestType">The request message contract.</param>
    /// <param name="timeout">An explicit timeout, or an unspecified value to inherit the configured default.</param>
    public void AddRequestClient(Type requestType, RequestTimeout timeout = default)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        RequestClientRegistrationCache.Register(requestType, GetRequestTimeout(timeout), Registrar);
    }

    /// <summary>Registers a runtime-selected request client bound to an explicit destination.</summary>
    /// <param name="requestType">The request message contract.</param>
    /// <param name="destinationAddress">The address to which requests are sent.</param>
    /// <param name="timeout">An explicit timeout, or an unspecified value to inherit the configured default.</param>
    public void AddRequestClient(Type requestType, Uri destinationAddress, RequestTimeout timeout = default)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(destinationAddress);

        RequestClientRegistrationCache.Register(requestType, destinationAddress, GetRequestTimeout(timeout), Registrar);
    }

    /// <summary>Sets the timeout used by request clients that do not specify one.</summary>
    /// <param name="timeout">The explicit default timeout.</param>
    /// <exception cref="ArgumentException"><paramref name="timeout" /> is unspecified.</exception>
    public void SetDefaultRequestTimeout(RequestTimeout timeout)
    {
        if (!timeout.HasValue)
            throw new ArgumentException("The default request timeout must be explicit.", nameof(timeout));

        DefaultRequestTimeout = timeout;
    }

    /// <summary>Sets the endpoint naming convention for this registration owner.</summary>
    /// <param name="endpointNameFormatter">The naming convention to register.</param>
    public void SetEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter)
    {
        ArgumentNullException.ThrowIfNull(endpointNameFormatter);

        Registrar.RegisterEndpointNameFormatter(endpointNameFormatter);
    }

    /// <inheritdoc />
    public TParticipant GetOrAddRegistrationCompletionParticipant<TParticipant>(Func<TParticipant> factory)
        where TParticipant : class, IRegistrationCompletionParticipant
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (_completed)
            throw new InvalidOperationException("Registration is complete; no further completion participants can be added.");
        if (_completionStarted)
            throw new InvalidOperationException("Registration completion has started; no further participants can be added.");

        if (_registrationCompletionParticipants.TryGetValue(typeof(TParticipant), out IRegistrationCompletionParticipant? participant))
            return (TParticipant)participant;

        TParticipant created = factory()
            ?? throw new InvalidOperationException("The registration completion participant factory returned null.");
        _registrationCompletionParticipants.Add(typeof(TParticipant), created);
        return created;
    }

    RequestTimeout GetRequestTimeout(RequestTimeout timeout)
    {
        return timeout.Or(DefaultRequestTimeout);
    }

    /// <summary>Completes every capability participant exactly once in deterministic order.</summary>
    public void Complete()
    {
        if (_completed)
            return;
        if (_completionStarted)
            throw new InvalidOperationException("Registration completion is already in progress or previously failed.");

        _completionStarted = true;
        foreach (IRegistrationCompletionParticipant participant in _registrationCompletionParticipants.Values
                     .OrderBy(x => x.Order)
                     .ThenBy(x => x.GetType().FullName, StringComparer.Ordinal))
            participant.Complete(this);

        _completed = true;
    }

    /// <summary>Creates a registration context that can establish ambient consume scopes.</summary>
    /// <param name="provider">The service provider that contains registered components.</param>
    /// <param name="setScopedConsumeContext">The accessor that installs and restores scoped consume contexts.</param>
    /// <returns>A registration context over the current container owner.</returns>
    private protected RegistrationContext CreateRegistration(IServiceProvider provider, ISetScopedConsumeContext setScopedConsumeContext)
    {
        return new RegistrationContext(provider, Registrar, setScopedConsumeContext);
    }

    /// <summary>Marks a single-use configuration operation and rejects subsequent attempts.</summary>
    /// <param name="methodName">The operation name included in a duplicate-configuration error.</param>
    protected void ThrowIfAlreadyConfigured(string methodName)
    {
        if (_configured)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Registration", "unknown", $"'{methodName}' can be called only once.", "Correct the named configuration before starting the host"));

        _configured = true;
    }

    /// <summary>Initializes the ambient logging context from dependency injection when necessary.</summary>
    /// <param name="provider">The service provider that contains logging services.</param>
    protected static void ConfigureLogContext(IServiceProvider provider)
    {
        LogContext.ConfigureCurrentLogContextIfNull(provider);
    }
}
