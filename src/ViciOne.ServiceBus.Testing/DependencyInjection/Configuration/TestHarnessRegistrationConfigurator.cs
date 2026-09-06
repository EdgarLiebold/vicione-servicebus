using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures test harness registration.</summary>
public class TestHarnessRegistrationConfigurator :
    IBusRegistrationConfigurator,
    IAdvancedBusRegistrationConfigurator
{
    readonly IBusRegistrationConfigurator _configurator;
    readonly IAdvancedBusRegistrationConfigurator _advancedConfigurator;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public TestHarnessRegistrationConfigurator(IBusRegistrationConfigurator configurator)
    {
        _configurator = configurator;
        _advancedConfigurator = configurator.Advanced();
    }

    /// <summary>Gets or sets the use default bus factory.</summary>
    public bool UseDefaultBusFactory { get; private set; } = true;

    /// <summary>Gets the services.</summary>
    public IServiceCollection Services => _configurator.Services;

    /// <inheritdoc />
    public Type BusType => _configurator.BusType;

    /// <summary>Adds consumer to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The consumer registration configurator produced by the operation.</returns>
    public IConsumerRegistrationConfigurator<T> AddConsumer<T>(Action<IRegistrationContext, IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
    {
        return AddConsumer(null, configure);
    }

    /// <summary>Adds consumer to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumerDefinitionType">The runtime consumer definition type used by the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The consumer registration configurator produced by the operation.</returns>
    public IConsumerRegistrationConfigurator<T> AddConsumer<T>(Type? consumerDefinitionType,
        Action<IRegistrationContext, IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
    {
        IConsumerRegistrationConfigurator<T> registrationConfigurator = _configurator.AddConsumer(consumerDefinitionType, configure);

        _configurator.Services.AddConsumerContainerTestHarness<T>();

        return registrationConfigurator;
    }

    /// <summary>Adds saga to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    public ISagaRegistrationConfigurator<T> AddSaga<T>(Action<IRegistrationContext, ISagaConfigurator<T>>? configure = null)
        where T : class, ISaga
    {
        return AddSaga(null, configure);
    }

    /// <summary>Adds saga to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="sagaDefinitionType">The runtime saga definition type used by the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    public ISagaRegistrationConfigurator<T> AddSaga<T>(Type? sagaDefinitionType, Action<IRegistrationContext, ISagaConfigurator<T>>? configure = null)
        where T : class, ISaga
    {
        ISagaRegistrationConfigurator<T> registrationConfigurator = _configurator.AddSaga(sagaDefinitionType, configure);

        _configurator.Services.AddSagaContainerTestHarness<T>();

        return registrationConfigurator;
    }

    /// <summary>Adds saga state machine to the configuration.</summary>
    /// <typeparam name="TStateMachine">The state machine type.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    public ISagaRegistrationConfigurator<T> AddSagaStateMachine<TStateMachine, T>(Action<IRegistrationContext, ISagaConfigurator<T>>? configure = null)
        where TStateMachine : class, SagaStateMachine<T>
        where T : class, SagaStateMachineInstance
    {
        return AddSagaStateMachine<TStateMachine, T>(null, configure);
    }

    /// <summary>Adds saga state machine to the configuration.</summary>
    /// <typeparam name="TStateMachine">The state machine type.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="sagaDefinitionType">The runtime saga definition type used by the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    public ISagaRegistrationConfigurator<T> AddSagaStateMachine<TStateMachine, T>(Type? sagaDefinitionType,
        Action<IRegistrationContext, ISagaConfigurator<T>>? configure = null)
        where TStateMachine : class, SagaStateMachine<T>
        where T : class, SagaStateMachineInstance
    {
        ISagaRegistrationConfigurator<T> registrationConfigurator = _configurator.AddSagaStateMachine<TStateMachine, T>(sagaDefinitionType, configure);

        _configurator.Services.AddSagaStateMachineContainerTestHarness<TStateMachine, T>();

        return registrationConfigurator;
    }

    /// <summary>Adds execute activity to the configuration.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The execute activity registration configurator produced by the operation.</returns>
    public IExecuteActivityRegistrationConfigurator<TActivity, TArguments> AddExecuteActivity<TActivity, TArguments>(
        Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        return _configurator.AddExecuteActivity(configure);
    }

    /// <summary>Adds execute activity to the configuration.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="executeActivityDefinitionType">The runtime execute activity definition type used by the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The execute activity registration configurator produced by the operation.</returns>
    public IExecuteActivityRegistrationConfigurator<TActivity, TArguments> AddExecuteActivity<TActivity, TArguments>(Type? executeActivityDefinitionType,
        Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        return _configurator.AddExecuteActivity(executeActivityDefinitionType, configure);
    }

    /// <summary>Adds activity to the configuration.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configureExecute">The configure execute.</param>
    /// <param name="configureCompensate">The configure compensate.</param>
    /// <returns>The activity registration configurator produced by the operation.</returns>
    public IActivityRegistrationConfigurator<TActivity, TArguments, TLog> AddActivity<TActivity, TArguments, TLog>(
        Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>? configureExecute = null,
        Action<IRegistrationContext, ICompensateActivityConfigurator<TActivity, TLog>>? configureCompensate = null)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
        return _configurator.AddActivity(configureExecute, configureCompensate);
    }

    /// <summary>Adds activity to the configuration.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="activityDefinitionType">The runtime activity definition type used by the operation.</param>
    /// <param name="configureExecute">The configure execute.</param>
    /// <param name="configureCompensate">The configure compensate.</param>
    /// <returns>The activity registration configurator produced by the operation.</returns>
    public IActivityRegistrationConfigurator<TActivity, TArguments, TLog> AddActivity<TActivity, TArguments, TLog>(Type? activityDefinitionType,
        Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>? configureExecute = null,
        Action<IRegistrationContext, ICompensateActivityConfigurator<TActivity, TLog>>? configureCompensate = null)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
        return _configurator.AddActivity(activityDefinitionType, configureExecute, configureCompensate);
    }

    /// <summary>Adds endpoint to the configuration.</summary>
    /// <param name="endpointDefinition">The endpoint definition.</param>
    public void AddEndpoint(Type endpointDefinition)
    {
        _configurator.AddEndpoint(endpointDefinition);
    }

    /// <summary>Adds endpoint to the configuration.</summary>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="registration">The registration.</param>
    /// <param name="settings">The settings that control the operation.</param>
    public void AddEndpoint<TDefinition, T>(IRegistration registration, IEndpointSettings<IEndpointDefinition<T>>? settings = null)
        where TDefinition : class, IEndpointDefinition<T>
        where T : class
    {
        _advancedConfigurator.AddEndpoint<TDefinition, T>(registration, settings);
    }

    /// <inheritdoc />
    public TParticipant GetOrAddRegistrationCompletionParticipant<TParticipant>(Func<TParticipant> factory)
        where TParticipant : class, IRegistrationCompletionParticipant
    {
        return _advancedConfigurator.GetOrAddRegistrationCompletionParticipant(factory);
    }

    /// <summary>Adds request client to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    public void AddRequestClient<T>(RequestTimeout timeout = default)
        where T : class
    {
        _configurator.AddRequestClient<T>(timeout);
    }

    /// <summary>Adds request client to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    public void AddRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default)
        where T : class
    {
        _configurator.AddRequestClient<T>(destinationAddress, timeout);
    }

    /// <summary>Adds request client to the configuration.</summary>
    /// <param name="requestType">The runtime request type used by the operation.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    public void AddRequestClient(Type requestType, RequestTimeout timeout = default)
    {
        _configurator.AddRequestClient(requestType, timeout);
    }

    /// <summary>Adds request client to the configuration.</summary>
    /// <param name="requestType">The runtime request type used by the operation.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    public void AddRequestClient(Type requestType, Uri destinationAddress, RequestTimeout timeout = default)
    {
        _configurator.AddRequestClient(requestType, destinationAddress, timeout);
    }

    /// <summary>Sets default request timeout.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    public void SetDefaultRequestTimeout(RequestTimeout timeout)
    {
        _configurator.SetDefaultRequestTimeout(timeout);
    }

    /// <summary>Sets default request timeout.</summary>
    /// <param name="d">The <c>d</c> value.</param>
    /// <param name="h">The <c>h</c> value.</param>
    /// <param name="m">The <c>m</c> value.</param>
    /// <param name="s">The <c>s</c> value.</param>
    /// <param name="ms">The ms.</param>
    public void SetDefaultRequestTimeout(int? d = null, int? h = null, int? m = null, int? s = null, int? ms = null)
    {
        _advancedConfigurator.SetDefaultRequestTimeout(d, h, m, s, ms);
    }

    /// <summary>Sets endpoint name formatter.</summary>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    public void SetEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter)
    {
        _configurator.SetEndpointNameFormatter(endpointNameFormatter);
    }

    /// <summary>Adds saga repository to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    public ISagaRegistrationConfigurator<T> AddSagaRepository<T>()
        where T : class, ISaga
    {
        return _configurator.AddSagaRepository<T>();
    }

    /// <summary>Sets saga repository provider.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public void SetSagaRepositoryProvider(ISagaRepositoryRegistrationProvider provider)
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        _configurator.SetSagaRepositoryProvider(provider);
    }

    /// <summary>Adds future to the configuration.</summary>
    /// <typeparam name="TFuture">The future type.</typeparam>
    /// <param name="futureDefinitionType">The runtime future definition type used by the operation.</param>
    /// <returns>The future registration configurator produced by the operation.</returns>
    public IFutureRegistrationConfigurator<TFuture> AddFuture<TFuture>(Type? futureDefinitionType = null)
        where TFuture : class, SagaStateMachine<FutureState>
    {
        return _configurator.AddFuture<TFuture>(futureDefinitionType);
    }

    /// <summary>Adds configure endpoints callback to the configuration.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    public void AddConfigureEndpointsCallback(ConfigureEndpointsCallback callback)
    {
        _advancedConfigurator.AddConfigureEndpointsCallback(callback);
    }

    /// <summary>Adds configure endpoints callback to the configuration.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    public void AddConfigureEndpointsCallback(ConfigureEndpointsProviderCallback callback)
    {
        _advancedConfigurator.AddConfigureEndpointsCallback(callback);
    }

    /// <summary>Sets request client factory.</summary>
    /// <param name="clientFactory">The client factory.</param>
    public void SetRequestClientFactory(Func<IBus, RequestTimeout, IClientFactory> clientFactory)
    {
        _advancedConfigurator.SetRequestClientFactory(clientFactory);
    }

    /// <summary>Gets the registrar.</summary>
    public IContainerRegistrar Registrar => _advancedConfigurator.Registrar;

    internal void ConfigureConsumerKindTestHarnesses()
    {
        IEnumerable<IConsumerKind> consumerKinds = Services
            .Where(static descriptor => descriptor.ServiceType == typeof(IConsumerKind))
            .Select(CreateConsumerKind)
            .DistinctBy(static kind => kind.GetType())
            .ToArray();

        foreach (IConsumerKind consumerKind in consumerKinds)
        {
            var context = new ConsumerKindTestHarnessContext(consumerKind.Name, Services, Registrar);
            consumerKind.ConfigureTestHarness(context);
        }
    }

    static IConsumerKind CreateConsumerKind(ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance is IConsumerKind instance)
            return instance;

        if (descriptor.ImplementationType is { } implementationType
            && Activator.CreateInstance(implementationType, nonPublic: true) is IConsumerKind created)
            return created;

        throw new ConfigurationException(
            Providers.Configuration.ConfigurationMessages.Create("Test harness consumer kind", "default",
                $"Consumer kind registration '{descriptor}' cannot be inspected before the service provider is built",
                "Register the consumer kind as an implementation type or singleton instance"));
    }

    /// <summary>Sets bus factory.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="busFactory">The bus factory.</param>
    public void SetBusFactory<T>(T busFactory)
        where T : class, IRegistrationBusFactory
    {
        _advancedConfigurator.SetBusFactory(busFactory);

        UseDefaultBusFactory = false;
    }

    /// <summary>Adds rider to the configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void AddRider(Action<IRiderRegistrationConfigurator> configure)
    {
        _advancedConfigurator.AddRider(configure);
    }


    sealed class ConsumerKindTestHarnessContext :
        IConsumerKindTestHarnessContext
    {
        static readonly MethodInfo AddConsumerMethod = GetRegistrationMethod(nameof(AddConsumer));
        static readonly MethodInfo AddSagaMethod = GetRegistrationMethod(nameof(AddSaga));
        static readonly MethodInfo AddSagaStateMachineMethod = GetRegistrationMethod(nameof(AddSagaStateMachine));
        readonly IContainerRegistrar _registrar;
        readonly IServiceCollection _services;

        public ConsumerKindTestHarnessContext(string kindName, IServiceCollection services, IContainerRegistrar registrar)
        {
            KindName = kindName;
            _services = services;
            _registrar = registrar;
        }

        public string KindName { get; }

        public IEnumerable<TRegistration> GetRegistrations<TRegistration>()
            where TRegistration : class, IRegistration =>
            _registrar.GetRegistrations<TRegistration>().ToArray();

        public void Observe(Type registrationType, params Type[] supportingTypes)
        {
            ArgumentNullException.ThrowIfNull(registrationType);
            ArgumentNullException.ThrowIfNull(supportingTypes);

            switch (KindName)
            {
                case "Consumer":
                case "Job":
                    RequireSupportingTypes(registrationType, supportingTypes, 0);
                    Invoke(AddConsumerMethod, registrationType);
                    break;
                case "Saga" when supportingTypes.Length == 0:
                    Invoke(AddSagaMethod, registrationType);
                    break;
                case "Saga":
                case "Future":
                    RequireSupportingTypes(registrationType, supportingTypes, 1);
                    Invoke(AddSagaStateMachineMethod, supportingTypes[0], registrationType);
                    break;
                case "Activity":
                case "ExecuteActivity":
                    RequireSupportingTypes(registrationType, supportingTypes, 0);
                    break;
                default:
                    ObserveCustomKind(registrationType, supportingTypes);
                    break;
            }
        }

        void ObserveCustomKind(Type registrationType, Type[] supportingTypes)
        {
            if (supportingTypes.Length == 1
                && typeof(SagaStateMachineInstance).IsAssignableFrom(registrationType))
            {
                Invoke(AddSagaStateMachineMethod, supportingTypes[0], registrationType);
                return;
            }

            RequireSupportingTypes(registrationType, supportingTypes, 0);

            if (typeof(IConsumer).IsAssignableFrom(registrationType))
            {
                Invoke(AddConsumerMethod, registrationType);
                return;
            }

            if (typeof(ISaga).IsAssignableFrom(registrationType))
            {
                Invoke(AddSagaMethod, registrationType);
                return;
            }

            if (ImplementsOpenGeneric(registrationType, typeof(IActivity<,>))
                || ImplementsOpenGeneric(registrationType, typeof(IExecuteActivity<>)))
                return;

            throw new ConfigurationException(
                Providers.Configuration.ConfigurationMessages.Create("Test harness consumer kind", "default",
                    $"Registration '{registrationType}' for consumer kind '{KindName}' has no supported harness observation shape",
                    "Observe a consumer, saga, state machine, or activity registration"));
        }

        static bool ImplementsOpenGeneric(Type type, Type openGeneric) =>
            type.GetInterfaces().Any(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == openGeneric);

        static MethodInfo GetRegistrationMethod(string name) =>
            typeof(ConsumerKindTestHarnessContext).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Test-harness registration method '{name}' was not found.");

        static void RequireSupportingTypes(Type registrationType, Type[] supportingTypes, int expectedCount)
        {
            if (supportingTypes.Length != expectedCount)
            {
                throw new ConfigurationException(
                    Providers.Configuration.ConfigurationMessages.Create("Test harness consumer kind", "default",
                        $"Registration '{registrationType}' for the active consumer kind supplied {supportingTypes.Length} supporting types; expected {expectedCount}",
                        "Correct the consumer-kind test-harness contribution"));
            }
        }

        void Invoke(MethodInfo method, params Type[] genericTypes)
        {
            try
            {
                method.MakeGenericMethod(genericTypes).Invoke(null, [_services]);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                throw exception.InnerException;
            }
        }

        static void AddConsumer<T>(IServiceCollection services)
            where T : class, IConsumer =>
            services.AddConsumerContainerTestHarness<T>();

        static void AddSaga<T>(IServiceCollection services)
            where T : class, ISaga =>
            services.AddSagaContainerTestHarness<T>();

        static void AddSagaStateMachine<TStateMachine, TInstance>(IServiceCollection services)
            where TStateMachine : class, SagaStateMachine<TInstance>
            where TInstance : class, SagaStateMachineInstance =>
            services.AddSagaStateMachineContainerTestHarness<TStateMachine, TInstance>();
    }
}
