using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Used for registration of consumers and sagas
/// </summary>
public abstract class RegistrationConfigurator :
    IRegistrationConfigurator,
    IAdvancedRegistrationConfigurator
{
    readonly IServiceCollection _collection;
    bool _configured;
    ISagaRepositoryRegistrationProvider _sagaRepositoryRegistrationProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="collection">The collection value.</param>
    /// <param name="registrar">The registrar value.</param>
    protected RegistrationConfigurator(IServiceCollection collection, IContainerRegistrar registrar)
    {
        _collection = collection ?? throw new ArgumentNullException(nameof(collection));

        Registrar = registrar ?? new DependencyInjectionContainerRegistrar(collection);

        _sagaRepositoryRegistrationProvider = new SagaRepositoryRegistrationProvider();
    }

    /// <summary>
    /// Gets the registrar value.
    /// </summary>
    public IContainerRegistrar Registrar { get; }

    /// <summary>
    /// Gets the services value.
    /// </summary>
    public IServiceCollection Services => _collection;

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
    /// Adds saga to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public ISagaRegistrationConfigurator<T> AddSaga<T>(Action<IRegistrationContext, ISagaConfigurator<T>>? configure)
        where T : class, ISaga
    {
        return AddSaga(null, configure);
    }

    /// <summary>
    /// Adds saga to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="sagaDefinitionType">The saga definition type value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public ISagaRegistrationConfigurator<T> AddSaga<T>(Type? sagaDefinitionType, Action<IRegistrationContext, ISagaConfigurator<T>>? configure = null)
        where T : class, ISaga
    {
        if (typeof(T).ImplementsInterface<SagaStateMachineInstance>())
            throw new ArgumentException($"State machine sagas must be registered using AddSagaStateMachine: {TypeCache<T>.ShortName}");

        var registration = _collection.RegisterSaga<T>(Registrar, sagaDefinitionType);

        registration.AddConfigureAction(configure);

        return new SagaRegistrationConfigurator<T>(this, registration);
    }

    /// <summary>
    /// Adds saga state machine to the configuration.
    /// </summary>
    /// <typeparam name="TStateMachine">The t state machine type.</typeparam>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public ISagaRegistrationConfigurator<T> AddSagaStateMachine<TStateMachine, T>(Action<IRegistrationContext, ISagaConfigurator<T>>? configure = null)
        where TStateMachine : class, SagaStateMachine<T>
        where T : class, SagaStateMachineInstance
    {
        return AddSagaStateMachine<TStateMachine, T>(null, configure);
    }

    /// <summary>
    /// Adds saga state machine to the configuration.
    /// </summary>
    /// <typeparam name="TStateMachine">The t state machine type.</typeparam>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="sagaDefinitionType">The saga definition type value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public ISagaRegistrationConfigurator<T> AddSagaStateMachine<TStateMachine, T>(Type? sagaDefinitionType,
        Action<IRegistrationContext, ISagaConfigurator<T>>? configure = null)
        where TStateMachine : class, SagaStateMachine<T>
        where T : class, SagaStateMachineInstance
    {
        var registration = _collection.RegisterSagaStateMachine<TStateMachine, T>(Registrar, sagaDefinitionType);

        registration.AddConfigureAction(configure);

        return new SagaRegistrationConfigurator<T>(this, registration);
    }

    /// <summary>
    /// Adds execute activity to the configuration.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IExecuteActivityRegistrationConfigurator<TActivity, TArguments> AddExecuteActivity<TActivity, TArguments>(
        Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>? configure)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        return AddExecuteActivity(null, configure);
    }

    /// <summary>
    /// Adds execute activity to the configuration.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="executeActivityDefinitionType">The execute activity definition type value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IExecuteActivityRegistrationConfigurator<TActivity, TArguments> AddExecuteActivity<TActivity, TArguments>(Type? executeActivityDefinitionType,
        Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        var registration = _collection.RegisterExecuteActivity<TActivity, TArguments>(Registrar, executeActivityDefinitionType);

        registration.AddConfigureAction(configure);

        return new ExecuteActivityRegistrationConfigurator<TActivity, TArguments>(this, registration);
    }

    /// <summary>
    /// Adds activity to the configuration.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="configureExecute">The configure execute value.</param>
    /// <param name="configureCompensate">The configure compensate value.</param>
    /// <returns>The result of the operation.</returns>
    public IActivityRegistrationConfigurator<TActivity, TArguments, TLog> AddActivity<TActivity, TArguments, TLog>(
        Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>? configureExecute,
        Action<IRegistrationContext, ICompensateActivityConfigurator<TActivity, TLog>>? configureCompensate)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
        return AddActivity(null, configureExecute, configureCompensate);
    }

    /// <summary>
    /// Adds activity to the configuration.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="activityDefinitionType">The activity definition type value.</param>
    /// <param name="configureExecute">The configure execute value.</param>
    /// <param name="configureCompensate">The configure compensate value.</param>
    /// <returns>The result of the operation.</returns>
    public IActivityRegistrationConfigurator<TActivity, TArguments, TLog> AddActivity<TActivity, TArguments, TLog>(Type? activityDefinitionType,
        Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>? configureExecute = null,
        Action<IRegistrationContext, ICompensateActivityConfigurator<TActivity, TLog>>? configureCompensate = null)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
        var registration = _collection.RegisterActivity<TActivity, TArguments, TLog>(Registrar, activityDefinitionType);

        registration.AddConfigureAction(configureExecute);
        registration.AddConfigureAction(configureCompensate);

        return new ActivityRegistrationConfigurator<TActivity, TArguments, TLog>(this, registration);
    }

    /// <summary>
    /// Adds future to the configuration.
    /// </summary>
    /// <typeparam name="TFuture">The t future type.</typeparam>
    /// <param name="futureDefinitionType">The future definition type value.</param>
    /// <returns>The result of the operation.</returns>
    public IFutureRegistrationConfigurator<TFuture> AddFuture<TFuture>(Type? futureDefinitionType)
        where TFuture : class, SagaStateMachine<FutureState>
    {
        var registration = _collection.RegisterFuture<TFuture>(Registrar, futureDefinitionType);

        return new FutureRegistrationConfigurator<TFuture>(this, registration);
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

    /// <summary>
    /// Adds saga repository to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public ISagaRegistrationConfigurator<T> AddSagaRepository<T>()
        where T : class, ISaga
    {
        return new SagaRegistrationConfigurator<T>(this);
    }

    /// <summary>
    /// Sets saga repository provider.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    public void SetSagaRepositoryProvider(ISagaRepositoryRegistrationProvider provider)
    {
        _sagaRepositoryRegistrationProvider = provider ?? throw new ArgumentNullException(nameof(provider));
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
        if (_sagaRepositoryRegistrationProvider != null)
        {
            List<ISagaRegistration> registrations = Registrar.GetRegistrations<ISagaRegistration>().ToList();

            foreach (var registration in registrations)
            {
                if (_collection.Any(x => x.ServiceType == typeof(ISagaRepositoryContextFactory<>).MakeGenericType(registration.Type)))
                    continue;

                var register = (IConfigureSagaRepository)(Activator.CreateInstance(typeof(ConfigureSagaRepository<>).MakeGenericType(registration.Type)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

                register.Configure(this, _sagaRepositoryRegistrationProvider, registration);
            }

            if (Registrar.GetRegistrations<IFutureRegistration>().Any()
                && _collection.All(x => x.ServiceType != typeof(ISagaRepositoryContextFactory<FutureState>)))
                new ConfigureSagaRepository<FutureState>().Configure(this, _sagaRepositoryRegistrationProvider, null);
        }
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
            throw new ConfigurationException($"'{methodName}' can be called only once.");

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


    interface IConfigureSagaRepository
    {
        void Configure(IRegistrationConfigurator configurator, ISagaRepositoryRegistrationProvider provider, ISagaRegistration registration);
    }


    class ConfigureSagaRepository<TSaga> :
        IConfigureSagaRepository
        where TSaga : class, ISaga
    {
        public void Configure(IRegistrationConfigurator configurator, ISagaRepositoryRegistrationProvider provider, ISagaRegistration? registration)
        {
            var registrationConfigurator = new SagaRegistrationConfigurator<TSaga>(configurator, registration);

            provider.Configure(registrationConfigurator);
        }
    }
}
