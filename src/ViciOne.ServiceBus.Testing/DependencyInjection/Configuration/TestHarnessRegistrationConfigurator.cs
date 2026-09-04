using System;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a test harness registration configurator implementation.
/// </summary>
public class TestHarnessRegistrationConfigurator :
    IBusRegistrationConfigurator,
    IAdvancedBusRegistrationConfigurator
{
    readonly IBusRegistrationConfigurator _configurator;
    readonly IAdvancedBusRegistrationConfigurator _advancedConfigurator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    public TestHarnessRegistrationConfigurator(IBusRegistrationConfigurator configurator)
    {
        _configurator = configurator;
        _advancedConfigurator = configurator.Advanced();
    }

    /// <summary>
    /// Gets or sets the use default bus factory value.
    /// </summary>
    public bool UseDefaultBusFactory { get; private set; } = true;

    /// <summary>
    /// Gets the services value.
    /// </summary>
    public IServiceCollection Services => _configurator.Services;

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
        IConsumerRegistrationConfigurator<T> registrationConfigurator = _configurator.AddConsumer(consumerDefinitionType, configure);

        _configurator.Services.AddConsumerContainerTestHarness<T>();

        return registrationConfigurator;
    }

    /// <summary>
    /// Adds saga to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public ISagaRegistrationConfigurator<T> AddSaga<T>(Action<IRegistrationContext, ISagaConfigurator<T>>? configure = null)
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
        ISagaRegistrationConfigurator<T> registrationConfigurator = _configurator.AddSaga(sagaDefinitionType, configure);

        _configurator.Services.AddSagaContainerTestHarness<T>();

        return registrationConfigurator;
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
        ISagaRegistrationConfigurator<T> registrationConfigurator = _configurator.AddSagaStateMachine<TStateMachine, T>(sagaDefinitionType, configure);

        _configurator.Services.AddSagaStateMachineContainerTestHarness<TStateMachine, T>();

        return registrationConfigurator;
    }

    /// <summary>
    /// Adds execute activity to the configuration.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IExecuteActivityRegistrationConfigurator<TActivity, TArguments> AddExecuteActivity<TActivity, TArguments>(
        Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        return _configurator.AddExecuteActivity(configure);
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
        return _configurator.AddExecuteActivity(executeActivityDefinitionType, configure);
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
        Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>? configureExecute = null,
        Action<IRegistrationContext, ICompensateActivityConfigurator<TActivity, TLog>>? configureCompensate = null)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
        return _configurator.AddActivity(configureExecute, configureCompensate);
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
        return _configurator.AddActivity(activityDefinitionType, configureExecute, configureCompensate);
    }

    /// <summary>
    /// Adds endpoint to the configuration.
    /// </summary>
    /// <param name="endpointDefinition">The endpoint definition value.</param>
    public void AddEndpoint(Type endpointDefinition)
    {
        _configurator.AddEndpoint(endpointDefinition);
    }

    /// <summary>
    /// Adds endpoint to the configuration.
    /// </summary>
    /// <typeparam name="TDefinition">The t definition type.</typeparam>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="registration">The registration value.</param>
    /// <param name="settings">The settings value.</param>
    public void AddEndpoint<TDefinition, T>(IRegistration registration, IEndpointSettings<IEndpointDefinition<T>>? settings = null)
        where TDefinition : class, IEndpointDefinition<T>
        where T : class
    {
        _advancedConfigurator.AddEndpoint<TDefinition, T>(registration, settings);
    }

    /// <summary>
    /// Adds request client to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="timeout">The timeout value.</param>
    public void AddRequestClient<T>(RequestTimeout timeout = default)
        where T : class
    {
        _configurator.AddRequestClient<T>(timeout);
    }

    /// <summary>
    /// Adds request client to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="timeout">The timeout value.</param>
    public void AddRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default)
        where T : class
    {
        _configurator.AddRequestClient<T>(destinationAddress, timeout);
    }

    /// <summary>
    /// Adds request client to the configuration.
    /// </summary>
    /// <param name="requestType">The request type value.</param>
    /// <param name="timeout">The timeout value.</param>
    public void AddRequestClient(Type requestType, RequestTimeout timeout = default)
    {
        _configurator.AddRequestClient(requestType, timeout);
    }

    /// <summary>
    /// Adds request client to the configuration.
    /// </summary>
    /// <param name="requestType">The request type value.</param>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="timeout">The timeout value.</param>
    public void AddRequestClient(Type requestType, Uri destinationAddress, RequestTimeout timeout = default)
    {
        _configurator.AddRequestClient(requestType, destinationAddress, timeout);
    }

    /// <summary>
    /// Sets default request timeout.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    public void SetDefaultRequestTimeout(RequestTimeout timeout)
    {
        _configurator.SetDefaultRequestTimeout(timeout);
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
        _advancedConfigurator.SetDefaultRequestTimeout(d, h, m, s, ms);
    }

    /// <summary>
    /// Sets endpoint name formatter.
    /// </summary>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    public void SetEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter)
    {
        _configurator.SetEndpointNameFormatter(endpointNameFormatter);
    }

    /// <summary>
    /// Adds saga repository to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public ISagaRegistrationConfigurator<T> AddSagaRepository<T>()
        where T : class, ISaga
    {
        return _configurator.AddSagaRepository<T>();
    }

    /// <summary>
    /// Sets saga repository provider.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    public void SetSagaRepositoryProvider(ISagaRepositoryRegistrationProvider provider)
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        _advancedConfigurator.SetSagaRepositoryProvider(provider);
    }

    /// <summary>
    /// Adds future to the configuration.
    /// </summary>
    /// <typeparam name="TFuture">The t future type.</typeparam>
    /// <param name="futureDefinitionType">The future definition type value.</param>
    /// <returns>The result of the operation.</returns>
    public IFutureRegistrationConfigurator<TFuture> AddFuture<TFuture>(Type? futureDefinitionType = null)
        where TFuture : class, SagaStateMachine<FutureState>
    {
        return _configurator.AddFuture<TFuture>(futureDefinitionType);
    }

    /// <summary>
    /// Adds configure endpoints callback to the configuration.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public void AddConfigureEndpointsCallback(ConfigureEndpointsCallback callback)
    {
        _advancedConfigurator.AddConfigureEndpointsCallback(callback);
    }

    /// <summary>
    /// Adds configure endpoints callback to the configuration.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public void AddConfigureEndpointsCallback(ConfigureEndpointsProviderCallback callback)
    {
        _advancedConfigurator.AddConfigureEndpointsCallback(callback);
    }

    /// <summary>
    /// Sets request client factory.
    /// </summary>
    /// <param name="clientFactory">The client factory value.</param>
    public void SetRequestClientFactory(Func<IBus, RequestTimeout, IClientFactory> clientFactory)
    {
        _advancedConfigurator.SetRequestClientFactory(clientFactory);
    }

    /// <summary>
    /// Gets the registrar value.
    /// </summary>
    public IContainerRegistrar Registrar => _advancedConfigurator.Registrar;

    /// <summary>
    /// Sets bus factory.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="busFactory">The bus factory value.</param>
    public void SetBusFactory<T>(T busFactory)
        where T : class, IRegistrationBusFactory
    {
        _advancedConfigurator.SetBusFactory(busFactory);

        UseDefaultBusFactory = false;
    }

    /// <summary>
    /// Adds rider to the configuration.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void AddRider(Action<IRiderRegistrationConfigurator> configure)
    {
        _advancedConfigurator.AddRider(configure);
    }
}
