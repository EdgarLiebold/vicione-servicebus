using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;

namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Decorates bus registration so consumer and saga registrations also receive test harnesses.</summary>
internal sealed class TestHarnessRegistrationConfigurator :
    IBusRegistrationConfigurator,
    IAdvancedBusRegistrationConfigurator
{
    readonly IBusRegistrationConfigurator _configurator;
    readonly IAdvancedBusRegistrationConfigurator _advancedConfigurator;

    /// <summary>Creates a test-harness-aware decorator over a bus registration configurator.</summary>
    /// <param name="configurator">The bus registration configurator to decorate.</param>
    public TestHarnessRegistrationConfigurator(IBusRegistrationConfigurator configurator)
    {
        _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
        _advancedConfigurator = configurator.Advanced();
    }

    /// <summary>Gets whether the harness must configure its default in-memory bus factory.</summary>
    public bool UseDefaultBusFactory { get; private set; } = true;

    /// <inheritdoc />
    public IServiceCollection Services => _configurator.Services;

    /// <inheritdoc />
    public Type BusType => _configurator.BusType;

    /// <inheritdoc />
    public IConsumerRegistrationConfigurator<T> AddConsumer<T>(Action<IRegistrationContext, IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
    {
        return AddConsumer(null, configure);
    }

    /// <inheritdoc />
    public IConsumerRegistrationConfigurator<T> AddConsumer<T>(Type? consumerDefinitionType,
        Action<IRegistrationContext, IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
    {
        IConsumerRegistrationConfigurator<T> registrationConfigurator = _configurator.AddConsumer(consumerDefinitionType, configure);

        _configurator.Services.AddConsumerContainerTestHarness<T>();

        return registrationConfigurator;
    }

    /// <inheritdoc />
    public void AddEndpoint(Type endpointDefinition)
    {
        ArgumentNullException.ThrowIfNull(endpointDefinition);
        _configurator.AddEndpoint(endpointDefinition);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void AddRequestClient<T>(RequestTimeout timeout = default)
        where T : class
    {
        _configurator.AddRequestClient<T>(timeout);
    }

    /// <inheritdoc />
    public void AddRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default)
        where T : class
    {
        _configurator.AddRequestClient<T>(destinationAddress, timeout);
    }

    /// <inheritdoc />
    public void AddRequestClient(Type requestType, RequestTimeout timeout = default)
    {
        _configurator.AddRequestClient(requestType, timeout);
    }

    /// <inheritdoc />
    public void AddRequestClient(Type requestType, Uri destinationAddress, RequestTimeout timeout = default)
    {
        _configurator.AddRequestClient(requestType, destinationAddress, timeout);
    }

    /// <inheritdoc />
    public void SetDefaultRequestTimeout(RequestTimeout timeout)
    {
        _configurator.SetDefaultRequestTimeout(timeout);
    }

    /// <inheritdoc />
    public void SetEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter)
    {
        _configurator.SetEndpointNameFormatter(endpointNameFormatter);
    }

    /// <inheritdoc />
    public void AddConfigureEndpointsCallback(ConfigureEndpointsCallback callback)
    {
        _advancedConfigurator.AddConfigureEndpointsCallback(callback);
    }

    /// <inheritdoc />
    public void AddConfigureEndpointsCallback(ConfigureEndpointsProviderCallback callback)
    {
        _advancedConfigurator.AddConfigureEndpointsCallback(callback);
    }

    /// <inheritdoc />
    public void SetRequestClientFactory(Func<IBus, RequestTimeout, IClientFactory> clientFactory)
    {
        _advancedConfigurator.SetRequestClientFactory(clientFactory);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void SetBusFactory<T>(T busFactory)
        where T : class, IRegistrationBusFactory
    {
        _advancedConfigurator.SetBusFactory(busFactory);

        UseDefaultBusFactory = false;
    }

    /// <inheritdoc />
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
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
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
