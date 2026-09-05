using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection.Registration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Used for registration of consumers and sagas
/// </summary>
public abstract class RegistrationConfigurator :
    IRegistrationConfigurator,
    IAdvancedRegistrationConfigurator
{
    readonly IServiceCollection _collection;
    readonly Dictionary<Type, IRegistrationCompletionParticipant> _registrationCompletionParticipants = new();
    bool _configured;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="collection">The collection value.</param>
    /// <param name="registrar">The registrar value.</param>
    protected RegistrationConfigurator(IServiceCollection collection, IContainerRegistrar registrar)
    {
        _collection = collection ?? throw new ArgumentNullException(nameof(collection));

        Registrar = registrar ?? new DependencyInjectionContainerRegistrar(collection);
    }

    /// <summary>
    /// Gets the registrar value.
    /// </summary>
    public IContainerRegistrar Registrar { get; }

    /// <summary>
    /// Gets the services value.
    /// </summary>
    public IServiceCollection Services => _collection;

    /// <inheritdoc />
    public virtual Type BusType => typeof(IBus);

    /// <summary>
    /// Gets or sets the default request timeout value.
    /// </summary>
    protected RequestTimeout DefaultRequestTimeout { get; private set; } = RequestTimeout.Default;

    /// <summary>
    /// Adds consumer to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IConsumerRegistrationConfigurator<T> AddConsumer<T>(Action<IRegistrationContext, IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
    {
        return AddConsumer(null, configure);
    }

    /// <summary>
    /// Adds consumer to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="consumerDefinitionType">The consumer definition type value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IConsumerRegistrationConfigurator<T> AddConsumer<T>(Type? consumerDefinitionType,
        Action<IRegistrationContext, IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
    {
        var registration = _collection.RegisterConsumer<T>(Registrar, consumerDefinitionType);

        registration.AddConfigureAction(configure);

        return new ConsumerRegistrationConfigurator<T>(this, registration);
    }

    /// <summary>
    /// Adds endpoint to the configuration.
    /// </summary>
    /// <param name="definitionType">The definition type value.</param>
    public void AddEndpoint(Type definitionType)
    {
        _collection.RegisterEndpoint(Registrar, definitionType);
    }

    /// <summary>
    /// Adds endpoint to the configuration.
    /// </summary>
    /// <typeparam name="TDefinition">The t definition type.</typeparam>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="registration">The registration value.</param>
    /// <param name="settings">The settings value.</param>
    public void AddEndpoint<TDefinition, T>(IRegistration registration, IEndpointSettings<IEndpointDefinition<T>>? settings)
        where TDefinition : class, IEndpointDefinition<T>
        where T : class
    {
        _collection.RegisterEndpoint<TDefinition, T>(Registrar, registration, settings);
    }

    /// <summary>
    /// Adds request client to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="timeout">The timeout value.</param>
    public void AddRequestClient<T>(RequestTimeout timeout)
        where T : class
    {
        Registrar.RegisterRequestClient<T>(GetRequestTimeout(timeout));
    }

    /// <summary>
    /// Adds request client to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="timeout">The timeout value.</param>
    public void AddRequestClient<T>(Uri destinationAddress, RequestTimeout timeout)
        where T : class
    {
        Registrar.RegisterRequestClient<T>(destinationAddress, timeout);
    }

    /// <summary>
    /// Adds request client to the configuration.
    /// </summary>
    /// <param name="requestType">The request type value.</param>
    /// <param name="timeout">The timeout value.</param>
    public void AddRequestClient(Type requestType, RequestTimeout timeout = default)
    {
        RequestClientRegistrationCache.Register(requestType, GetRequestTimeout(timeout), Registrar);
    }

    /// <summary>
    /// Adds request client to the configuration.
    /// </summary>
    /// <param name="requestType">The request type value.</param>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="timeout">The timeout value.</param>
    public void AddRequestClient(Type requestType, Uri destinationAddress, RequestTimeout timeout = default)
    {
        RequestClientRegistrationCache.Register(requestType, destinationAddress, GetRequestTimeout(timeout), Registrar);
    }

    /// <summary>
    /// Sets default request timeout.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    public void SetDefaultRequestTimeout(RequestTimeout timeout)
    {
        DefaultRequestTimeout = timeout;
    }

    /// <summary>
    /// Sets default request timeout.
    /// </summary>
    /// <param name="d">The d value.</param>
    /// <param name="h">The h value.</param>
    /// <param name="m">The m value.</param>
    /// <param name="s">The s value.</param>
    /// <param name="ms">The ms value.</param>
    public void SetDefaultRequestTimeout(int? d = null, int? h = null, int? m = null, int? s = null, int? ms = null)
    {
        var timeout = new TimeSpan(d ?? 0, h ?? 0, m ?? 0, s ?? 0, ms ?? 0);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentException("The timeout must be > 0");

        DefaultRequestTimeout = timeout;
    }

    /// <summary>
    /// Sets endpoint name formatter.
    /// </summary>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    public void SetEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter)
    {
        Registrar.RegisterEndpointNameFormatter(endpointNameFormatter);
    }

    /// <inheritdoc />
    public TParticipant GetOrAddRegistrationCompletionParticipant<TParticipant>(Func<TParticipant> factory)
        where TParticipant : class, IRegistrationCompletionParticipant
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (_registrationCompletionParticipants.TryGetValue(typeof(TParticipant), out IRegistrationCompletionParticipant? participant))
            return (TParticipant)participant;

        TParticipant created = factory();
        _registrationCompletionParticipants.Add(typeof(TParticipant), created);
        return created;
    }

    RequestTimeout GetRequestTimeout(RequestTimeout timeout)
    {
        return timeout == RequestTimeout.Default ? DefaultRequestTimeout : timeout;
    }

    /// <summary>
    /// Performs the complete operation.
    /// </summary>
    public void Complete()
    {
        foreach (IRegistrationCompletionParticipant participant in _registrationCompletionParticipants.Values
                     .OrderBy(x => x.Order)
                     .ThenBy(x => x.GetType().FullName, StringComparer.Ordinal))
            participant.Complete(this);
    }

    /// <summary>
    /// Creates registration.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="setScopedConsumeContext">The set scoped consume context value.</param>
    /// <returns>The result of the operation.</returns>
    protected RegistrationContext CreateRegistration(IServiceProvider provider, ISetScopedConsumeContext setScopedConsumeContext)
    {
        return new RegistrationContext(provider, Registrar, setScopedConsumeContext);
    }

    /// <summary>
    /// Performs the throw if already configured operation.
    /// </summary>
    /// <param name="methodName">The method name value.</param>
    protected void ThrowIfAlreadyConfigured(string methodName)
    {
        if (_configured)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Registration", "unknown", $"'{methodName}' can be called only once.", "Correct the named configuration before starting the host"));

        _configured = true;
    }

    /// <summary>
    /// Configures log context.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    protected static void ConfigureLogContext(IServiceProvider provider)
    {
        LogContext.ConfigureCurrentLogContextIfNull(provider);
    }
}
