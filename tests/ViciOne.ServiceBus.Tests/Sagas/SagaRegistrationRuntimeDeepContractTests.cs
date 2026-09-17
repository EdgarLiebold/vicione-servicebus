using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaRegistrationRuntimeDeepContractTests
{
    private static readonly Func<Type, bool> IsSagaOrDefinition = CreateMetadataClassifier(nameof(IsSagaOrDefinition));
    private static readonly Func<Type, bool> IsSagaStateMachineOrDefinition = CreateMetadataClassifier(nameof(IsSagaStateMachineOrDefinition));
    private static readonly Func<Type, bool> IsConsumerKindOwned = CreateMetadataClassifier(nameof(IsConsumerKindOwned));

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-RUNTIME", "required-null-boundaries-before-collaborator-effects")]
    public void RequiredBoundaries_RejectNullInputsBeforeAnyCollaboratorEffect()
    {
        IConsumerKind kind = CreateConsumerKind();
        var runtime = Assert.IsAssignableFrom<IConsumerKindRuntimeConfigurator>(kind);
        var bulk = Assert.IsAssignableFrom<IConsumerKindBulkConfigurator>(kind);
        var dispatcherProvider = Assert.IsAssignableFrom<IConsumerKindDispatcherProvider>(kind);
        var registration = Registration<FirstSaga>();
        var selector = new RecordingContainerSelector(registration);
        IRegistrationContext registrationContext = CreateRegistrationContext(selector);
        IReceiveEndpointConfigurator endpoint = CreateProxy<IReceiveEndpointConfigurator>();
        var factory = new RecordingDispatcherFactory();
        var formatter = new KebabCaseEndpointNameFormatter(false);

        AssertArgument("context", () => kind.GetRegistrations(null!));
        AssertArgument("registrationType", () => runtime.TryConfigure(null!, endpoint, registrationContext));
        AssertArgument("endpointConfigurator", () => runtime.TryConfigure(typeof(FirstSaga), null!, registrationContext));
        AssertArgument("registrationContext", () => runtime.TryConfigure(typeof(FirstSaga), endpoint, null!));

        var typed = Assert.IsAssignableFrom<IConsumerKindTypedConfigurator>(kind);
        AssertArgument("endpointConfigurator", () => typed.TryConfigure<FirstSaga>(null!, registrationContext));
        AssertArgument("registrationContext", () => typed.TryConfigure<FirstSaga>(endpoint, null!));

        AssertArgument("endpointConfigurator", () =>
            bulk.ConfigureAll(null!, registrationContext, new HashSet<Type>()));
        AssertArgument("registrationContext", () =>
            bulk.ConfigureAll(endpoint, null!, new HashSet<Type>()));
        AssertArgument("excludedRegistrationTypes", () =>
            bulk.ConfigureAll(endpoint, registrationContext, null!));

        AssertArgument("registrationType", () =>
            dispatcherProvider.TryCreateDispatcher(null!, factory, formatter, out _));
        AssertArgument("factory", () =>
            dispatcherProvider.TryCreateDispatcher(typeof(FirstSaga), null!, formatter, out _));
        AssertArgument("formatter", () =>
            dispatcherProvider.TryCreateDispatcher(typeof(FirstSaga), factory, null!, out _));
        AssertArgument("context", () => kind.ConfigureTestHarness(null!));

        var planning = new RecordingConsumerKindContext(
            registrationContext,
            formatter,
            [registration]);
        IConsumerKindRegistration planned = Assert.Single(kind.GetRegistrations(planning));
        AssertArgument("context", () => planned.Configure(null!));

        AssertArgument("type", () => IsSagaOrDefinition(null!));
        AssertArgument("type", () => IsSagaStateMachineOrDefinition(null!));
        AssertArgument("type", () => IsConsumerKindOwned(null!));

        Assert.Equal(0, selector.TryGetCount);
        Assert.Equal(0, registration.ConfigureCount);
        Assert.Equal(0, factory.CreateCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-RUNTIME", "planning-order-definition-identity-and-nested-forwarding")]
    public void Planning_PreservesRegistrationOrderDefinitionIdentityAndNestedForwarding()
    {
        IConsumerKind kind = CreateConsumerKind();
        IEndpointDefinition secondEndpointDefinition = CreateProxy<IEndpointDefinition>();
        IEndpointDefinition firstEndpointDefinition = CreateProxy<IEndpointDefinition>();
        var secondDefinition = new RecordingSagaDefinition(typeof(SecondSaga), secondEndpointDefinition);
        var firstDefinition = new RecordingSagaDefinition(typeof(FirstSaga), firstEndpointDefinition);
        var second = new RecordingSagaRegistration(typeof(SecondSaga), secondDefinition);
        var first = new RecordingSagaRegistration(typeof(FirstSaga), firstDefinition);
        IRegistrationContext registrationContext = CreateRegistrationContext(new RecordingContainerSelector());
        var formatter = new KebabCaseEndpointNameFormatter(false);
        var planning = new RecordingConsumerKindContext(
            registrationContext,
            formatter,
            [second, first]);

        IConsumerKindRegistration[] planned = kind.GetRegistrations(planning).ToArray();

        Assert.Equal("Saga", kind.Name);
        Assert.Equal(10, kind.Order);
        Assert.False(kind.IsFallback);
        Assert.Equal([typeof(SecondSaga), typeof(FirstSaga)], planned.Select(x => x.RegistrationType));
        Assert.Same(secondDefinition, planned[0].Definition);
        Assert.Same(firstDefinition, planned[1].Definition);
        Assert.Equal("second", planned[0].EndpointName);
        Assert.Equal("first", planned[1].EndpointName);
        Assert.Same(secondEndpointDefinition, planned[0].EndpointDefinition);
        Assert.Same(firstEndpointDefinition, planned[1].EndpointDefinition);
        Assert.False(planned[0].RequiresServiceInstance);
        Assert.False(planned[1].RequiresServiceInstance);
        Assert.Empty(planned[0].CompanionEndpointNames);
        Assert.Empty(planned[1].CompanionEndpointNames);
        Assert.Same(planned[0].CompanionEndpointNames, planned[1].CompanionEndpointNames);
        Assert.Equal(1, planning.EnumerationCount);
        Assert.Same(registrationContext, second.DefinitionContext);
        Assert.Same(registrationContext, first.DefinitionContext);
        Assert.Same(formatter, secondDefinition.Formatter);
        Assert.Same(formatter, firstDefinition.Formatter);

        IReceiveEndpointConfigurator endpoint = CreateProxy<IReceiveEndpointConfigurator>();
        var endpointContext = new RecordingEndpointContext(registrationContext, endpoint);
        planned[1].Configure(endpointContext);

        Assert.Equal(0, second.ConfigureCount);
        Assert.Equal(1, first.ConfigureCount);
        Assert.Same(endpoint, first.EndpointConfigurator);
        Assert.Same(registrationContext, first.ConfigurationContext);
        Assert.Equal(["Configure:FirstSaga"], first.Events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-RUNTIME", "runtime-and-bulk-filtering-order-and-input-identity")]
    public void RuntimeAndBulkConfiguration_FilterInOrderAndForwardExactInputs()
    {
        IConsumerKind kind = CreateConsumerKind();
        var runtime = Assert.IsAssignableFrom<IConsumerKindRuntimeConfigurator>(kind);
        var bulk = Assert.IsAssignableFrom<IConsumerKindBulkConfigurator>(kind);
        IReceiveEndpointConfigurator endpoint = CreateProxy<IReceiveEndpointConfigurator>();

        var runtimeRegistration = Registration<FirstSaga>();
        var runtimeSelector = new RecordingContainerSelector(runtimeRegistration);
        IRegistrationContext runtimeContext = CreateRegistrationContext(runtimeSelector);

        Assert.False(runtime.TryConfigure(typeof(UnrelatedType), endpoint, runtimeContext));
        Assert.True(runtime.TryConfigure(typeof(FirstSaga), endpoint, runtimeContext));
        Assert.Equal([typeof(UnrelatedType), typeof(FirstSaga)], runtimeSelector.RequestedTypes);
        Assert.Equal(1, runtimeRegistration.ConfigureCount);
        Assert.Same(endpoint, runtimeRegistration.EndpointConfigurator);
        Assert.Same(runtimeContext, runtimeRegistration.ConfigurationContext);

        var configurationOrder = new List<Type>();
        var second = Registration<SecondSaga>(configurationOrder);
        var excluded = Registration<ThirdSaga>(configurationOrder);
        var first = Registration<FirstSaga>(configurationOrder);
        var bulkSelector = new RecordingContainerSelector(second, excluded, first);
        IRegistrationContext bulkContext = CreateRegistrationContext(bulkSelector);

        IReadOnlyCollection<Type> configured = bulk.ConfigureAll(
            endpoint,
            bulkContext,
            new HashSet<Type> { typeof(ThirdSaga) });

        Assert.Equal([typeof(SecondSaga), typeof(FirstSaga)], configured);
        Assert.Equal([typeof(SecondSaga), typeof(FirstSaga)], configurationOrder);
        Assert.Equal(1, second.ConfigureCount);
        Assert.Equal(0, excluded.ConfigureCount);
        Assert.Equal(1, first.ConfigureCount);
        Assert.Same(endpoint, second.EndpointConfigurator);
        Assert.Same(bulkContext, second.ConfigurationContext);
        Assert.Same(endpoint, first.EndpointConfigurator);
        Assert.Same(bulkContext, first.ConfigurationContext);
        Assert.Same(bulkContext, bulkSelector.LastProvider);
        Assert.Equal(1, bulkSelector.GetRegistrationsCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-RUNTIME", "typed-delegate-validation-and-exact-configurator-forwarding")]
    public void TypedConfiguration_ValidatesDelegateBeforeLookupAndForwardsExactConfigurator()
    {
        IConsumerKind kind = CreateConsumerKind();
        var typed = Assert.IsAssignableFrom<IConsumerKindTypedConfigurator>(kind);
        IReceiveEndpointConfigurator endpoint = CreateProxy<IReceiveEndpointConfigurator>();
        var emptySelector = new RecordingContainerSelector();
        IRegistrationContext emptyContext = CreateRegistrationContext(emptySelector);

        var wrongCallback = new Action(() => { });
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            typed.TryConfigure<FirstSaga>(endpoint, emptyContext, wrongCallback));

        Assert.Equal("configure", exception.ParamName);
        Assert.Contains(
            TypeCache<ISagaConfigurator<FirstSaga>>.ShortName,
            exception.Message,
            StringComparison.Ordinal);
        Assert.Equal(0, emptySelector.TryGetCount);

        var events = new List<string>();
        var registration = Registration<FirstSaga>(events: events);
        var selector = new RecordingContainerSelector(registration);
        IRegistrationContext registrationContext = CreateRegistrationContext(selector);
        ISagaConfigurator<FirstSaga>? callbackConfigurator = null;
        Action<ISagaConfigurator<FirstSaga>> callback = configurator =>
        {
            events.Add("Callback");
            callbackConfigurator = configurator;
        };

        Assert.True(typed.TryConfigure<FirstSaga>(endpoint, registrationContext, callback));

        Assert.Equal(["AddConfigureAction:FirstSaga", "Configure:FirstSaga"], events);
        Assert.Equal(1, registration.AddConfigureActionCount);
        Assert.Equal(1, registration.ConfigureCount);
        Assert.Equal(1, selector.TryGetCount);
        Assert.Equal(typeof(FirstSaga), Assert.Single(selector.RequestedTypes));
        Assert.Same(endpoint, registration.EndpointConfigurator);
        Assert.Same(registrationContext, registration.ConfigurationContext);

        Action<IRegistrationContext, ISagaConfigurator<FirstSaga>> forwarded =
            Assert.IsType<Action<IRegistrationContext, ISagaConfigurator<FirstSaga>>>(registration.ConfigureAction);
        ISagaConfigurator<FirstSaga> sagaConfigurator = CreateProxy<ISagaConfigurator<FirstSaga>>();
        forwarded(registrationContext, sagaConfigurator);

        Assert.Same(sagaConfigurator, callbackConfigurator);
        Assert.Equal(
            ["AddConfigureAction:FirstSaga", "Configure:FirstSaga", "Callback"],
            events);

        Assert.False(typed.TryConfigure<SecondSaga>(endpoint, registrationContext));
        Assert.Equal(1, registration.AddConfigureActionCount);
        Assert.Equal(1, registration.ConfigureCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-RUNTIME", "dispatcher-acceptance-and-harness-observation-shape")]
    public void DispatcherAndHarness_AcceptOnlySagasAndObserveExactRegistrationShape()
    {
        IConsumerKind kind = CreateConsumerKind();
        var dispatcherProvider = Assert.IsAssignableFrom<IConsumerKindDispatcherProvider>(kind);
        var factory = new RecordingDispatcherFactory();
        var formatter = new KebabCaseEndpointNameFormatter(false);

        Assert.False(dispatcherProvider.TryCreateDispatcher(
            typeof(UnrelatedType),
            factory,
            formatter,
            out IReceiveEndpointDispatcher? rejected));
        Assert.Null(rejected);
        Assert.Equal(0, factory.CreateCount);

        Assert.True(dispatcherProvider.TryCreateDispatcher(
            typeof(FirstSaga),
            factory,
            formatter,
            out IReceiveEndpointDispatcher? accepted));
        Assert.Same(factory.Dispatcher, accepted);
        Assert.Equal(1, factory.CreateCount);
        Assert.Equal("first", factory.QueueName);
        Assert.NotNull(factory.Configure);

        var regular = Registration<FirstSaga>();
        var stateMachine = new RecordingSagaRegistration(
            typeof(StateMachineState),
            new RecordingSagaDefinition(typeof(StateMachineState), null),
            typeof(RegularStateMachine));
        var harness = new RecordingTestHarnessContext(regular, stateMachine);

        kind.ConfigureTestHarness(harness);

        Assert.Collection(
            harness.Observations,
            observation =>
            {
                Assert.Equal(typeof(FirstSaga), observation.RegistrationType);
                Assert.Empty(observation.SupportingTypes);
            },
            observation =>
            {
                Assert.Equal(typeof(StateMachineState), observation.RegistrationType);
                Assert.Equal([typeof(RegularStateMachine)], observation.SupportingTypes);
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-METADATA", "classification-and-capability-exclusion-matrix")]
    public void MetadataClassification_SeparatesSagaStateMachineDefinitionAndCapabilityOwnership()
    {
        ClassificationCase[] cases =
        [
            new(typeof(FirstSaga), true, false, true),
            new(typeof(InitiatedSaga), true, false, true),
            new(typeof(OrchestratingSaga), true, false, true),
            new(typeof(InitiatedOrOrchestratingSaga), true, false, true),
            new(typeof(ObservingSaga), true, false, true),
            new(typeof(RegularSagaDefinition), true, true, false),
            new(typeof(RegularStateMachine), false, true, false),
            new(typeof(CapabilityOwnedStateMachine), false, false, true),
            new(typeof(CapabilityOwnedDefinition), true, true, true),
            new(typeof(UnrelatedType), false, false, false),
        ];

        foreach (ClassificationCase item in cases)
        {
            Assert.Equal(item.IsSagaOrDefinition, IsSagaOrDefinition(item.Type));
            Assert.Equal(
                item.IsSagaStateMachineOrDefinition,
                IsSagaStateMachineOrDefinition(item.Type));
            Assert.Equal(item.IsConsumerKindOwned, IsConsumerKindOwned(item.Type));
        }
    }

    private static IConsumerKind CreateConsumerKind()
    {
        Type type = typeof(ISagaStateMachine<>).Assembly.GetType(
            "ViciOne.ServiceBus.Sagas.SagaConsumerKind",
            throwOnError: true)!;
        return Assert.IsAssignableFrom<IConsumerKind>(Activator.CreateInstance(type, nonPublic: true));
    }

    private static Func<Type, bool> CreateMetadataClassifier(string methodName)
    {
        Type type = typeof(ISagaStateMachine<>).Assembly.GetType(
            "ViciOne.ServiceBus.Sagas.SagaRegistrationMetadata",
            throwOnError: true)!;
        MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new MissingMethodException(type.FullName, methodName);
        return method.CreateDelegate<Func<Type, bool>>();
    }

    private static void AssertArgument(string parameterName, Action action) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);

    private static RecordingSagaRegistration Registration<TSaga>(
        List<Type>? configurationOrder = null,
        List<string>? events = null)
        where TSaga : class, ISaga =>
        new(
            typeof(TSaga),
            new RecordingSagaDefinition(typeof(TSaga), null),
            configurationOrder: configurationOrder,
            events: events);

    private static IRegistrationContext CreateRegistrationContext(IContainerSelector selector)
    {
        IRegistrationContext context = DispatchProxy.Create<IRegistrationContext, RegistrationContextProxy>();
        ((RegistrationContextProxy)(object)context).Selector = selector;
        return context;
    }

    private static T CreateProxy<T>()
        where T : class => DispatchProxy.Create<T, PassiveProxy>();

    private sealed class RecordingConsumerKindContext(
        IRegistrationContext registrationContext,
        IEndpointNameFormatter endpointNameFormatter,
        IReadOnlyCollection<ISagaRegistration> registrations) :
        IConsumerKindContext
    {
        public IRegistrationContext RegistrationContext { get; } = registrationContext;

        public IEndpointNameFormatter EndpointNameFormatter { get; } = endpointNameFormatter;

        public int EnumerationCount { get; private set; }

        public IEnumerable<TRegistration> GetRegistrations<TRegistration>()
            where TRegistration : class, IRegistration
        {
            EnumerationCount++;
            return registrations.OfType<TRegistration>();
        }
    }

    private sealed class RecordingEndpointContext(
        IRegistrationContext registrationContext,
        IReceiveEndpointConfigurator endpointConfigurator) :
        IConsumerKindEndpointContext
    {
        public IRegistrationContext RegistrationContext { get; } = registrationContext;

        public IReceiveEndpointConfigurator EndpointConfigurator { get; } = endpointConfigurator;

        public void ConfigureCompanionEndpoint(
            string endpointName,
            IEndpointDefinition? endpointDefinition,
            Action<IReceiveEndpointConfigurator> configure) =>
            throw new InvalidOperationException("Saga registrations do not own companion endpoints.");
    }

    private sealed class RecordingTestHarnessContext(params ISagaRegistration[] registrations) :
        IConsumerKindTestHarnessContext
    {
        public string KindName => "Saga";

        public List<Observation> Observations { get; } = [];

        public IEnumerable<TRegistration> GetRegistrations<TRegistration>()
            where TRegistration : class, IRegistration => registrations.OfType<TRegistration>();

        public void Observe(Type registrationType, params Type[] supportingTypes) =>
            Observations.Add(new Observation(registrationType, supportingTypes));
    }

    private sealed class RecordingContainerSelector(params ISagaRegistration[] registrations) :
        IContainerSelector
    {
        public int TryGetCount { get; private set; }

        public int GetRegistrationsCount { get; private set; }

        public List<Type> RequestedTypes { get; } = [];

        public IServiceProvider? LastProvider { get; private set; }

        public bool TryGetRegistration<T>(
            IServiceProvider provider,
            Type type,
            [NotNullWhen(true)] out T? value)
            where T : class, IRegistration
        {
            TryGetCount++;
            LastProvider = provider;
            RequestedTypes.Add(type);
            value = registrations.FirstOrDefault(registration => registration.Type == type) as T;
            return value is not null;
        }

        public IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
            where T : class, IRegistration
        {
            GetRegistrationsCount++;
            LastProvider = provider;
            return registrations.OfType<T>();
        }

        public T? GetDefinition<T>(IServiceProvider provider)
            where T : class, IDefinition => null;

        public IEndpointDefinition<T>? GetEndpointDefinition<T>(IServiceProvider provider)
            where T : class => null;

        public IConfigureReceiveEndpoint GetConfigureReceiveEndpoints(IServiceProvider provider) =>
            throw new NotSupportedException();

        public IEndpointNameFormatter GetEndpointNameFormatter(IServiceProvider provider) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingSagaRegistration :
        ISagaRegistration
    {
        private readonly List<Type>? _configurationOrder;
        private readonly List<string> _events;
        private readonly ISagaDefinition _definition;

        public RecordingSagaRegistration(
            Type type,
            ISagaDefinition definition,
            Type? stateMachineType = null,
            List<Type>? configurationOrder = null,
            List<string>? events = null)
        {
            Type = type;
            _definition = definition;
            StateMachineType = stateMachineType;
            _configurationOrder = configurationOrder;
            _events = events ?? [];
        }

        public Type Type { get; }

        public bool IncludeInConfigureEndpoints { get; set; } = true;

        public Type? StateMachineType { get; }

        public int AddConfigureActionCount { get; private set; }

        public int ConfigureCount { get; private set; }

        public Delegate? ConfigureAction { get; private set; }

        public IReceiveEndpointConfigurator? EndpointConfigurator { get; private set; }

        public IRegistrationContext? ConfigurationContext { get; private set; }

        public IRegistrationContext? DefinitionContext { get; private set; }

        public IReadOnlyList<string> Events => _events;

        public void AddConfigureAction<T>(Action<IRegistrationContext, ISagaConfigurator<T>>? configure)
            where T : class
        {
            AddConfigureActionCount++;
            ConfigureAction = configure;
            _events.Add($"AddConfigureAction:{typeof(T).Name}");
        }

        public void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context)
        {
            ConfigureCount++;
            EndpointConfigurator = configurator;
            ConfigurationContext = context;
            _configurationOrder?.Add(Type);
            _events.Add($"Configure:{Type.Name}");
        }

        public ISagaDefinition GetDefinition(IRegistrationContext context)
        {
            DefinitionContext = context;
            return _definition;
        }
    }

    private sealed class RecordingSagaDefinition(Type sagaType, IEndpointDefinition? endpointDefinition) :
        ISagaDefinition
    {
        public Type SagaType { get; } = sagaType;

        public IEndpointDefinition? EndpointDefinition { get; } = endpointDefinition;

        public int? ConcurrentMessageLimit => null;

        public IEndpointNameFormatter? Formatter { get; private set; }

        public string GetEndpointName(IEndpointNameFormatter formatter)
        {
            Formatter = formatter;
            string name = SagaType.Name.EndsWith("Saga", StringComparison.Ordinal)
                ? SagaType.Name[..^"Saga".Length]
                : SagaType.Name;
            return formatter.SanitizeName(name);
        }
    }

    private sealed class RecordingDispatcherFactory :
        IReceiveEndpointDispatcherFactory
    {
        public IReceiveEndpointDispatcher Dispatcher { get; } = CreateProxy<IReceiveEndpointDispatcher>();

        public int CreateCount { get; private set; }

        public string? QueueName { get; private set; }

        public Action<IReceiveEndpointConfigurator, IRegistrationContext>? Configure { get; private set; }

        public IReceiveEndpointDispatcher CreateReceiver(string queueName)
        {
            CreateCount++;
            QueueName = queueName;
            return Dispatcher;
        }

        public IReceiveEndpointDispatcher CreateReceiver(
            string queueName,
            Action<IReceiveEndpointConfigurator, IRegistrationContext> configure)
        {
            CreateCount++;
            QueueName = queueName;
            Configure = configure;
            return Dispatcher;
        }

        public IReceiveEndpointDispatcher CreateRegistrationReceiver(
            Type registrationType,
            string fallbackQueueName,
            IEndpointNameFormatter formatter) => throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private class RegistrationContextProxy :
        DispatchProxy
    {
        public IContainerSelector Selector { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == nameof(IServiceProvider.GetService)
                && args is [Type serviceType]
                && serviceType == typeof(IContainerSelector))
                return Selector;

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class PassiveProxy :
        DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private sealed class FirstSaga :
        ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class SecondSaga :
        ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class ThirdSaga :
        ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class InitiatedSaga :
        ISaga,
        IInitiatedBy<CorrelatedMessage>
    {
        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<CorrelatedMessage> context) => Task.CompletedTask;
    }

    private sealed class OrchestratingSaga :
        ISaga,
        IOrchestrates<CorrelatedMessage>
    {
        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<CorrelatedMessage> context) => Task.CompletedTask;
    }

    private sealed class InitiatedOrOrchestratingSaga :
        ISaga,
        IInitiatedByOrOrchestrates<CorrelatedMessage>
    {
        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<CorrelatedMessage> context) => Task.CompletedTask;
    }

    private sealed class ObservingSaga :
        ISaga,
        IObserves<ObservedMessage, FirstSaga>
    {
        public Guid CorrelationId { get; set; }

        public Expression<Func<FirstSaga, ObservedMessage, bool>> CorrelationExpression =>
            (saga, _) => saga.CorrelationId == CorrelationId;

        public Task ConsumeAsync(ConsumeContext<ObservedMessage> context) => Task.CompletedTask;
    }

    private sealed class RegularSagaDefinition :
        SagaDefinition<FirstSaga>;

    private sealed class StateMachineState :
        ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class RegularStateMachine :
        ViciOneServiceBusStateMachine<StateMachineState>;

    private sealed class CapabilityOwnedState :
        ISagaStateMachineInstance,
        IConsumerKindOwnedState
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class CapabilityOwnedStateMachine :
        ViciOneServiceBusStateMachine<CapabilityOwnedState>;

    [ConsumerRegistrationExclusion]
    private interface ICapabilityOwnedRegistration;

    private sealed class CapabilityOwnedDefinition :
        SagaDefinition<FirstSaga>,
        ICapabilityOwnedRegistration;

    private sealed class UnrelatedType;

    private sealed record CorrelatedMessage(Guid CorrelationId) :
        ICorrelatedBy<Guid>;

    private sealed record ObservedMessage;

    private sealed record Observation(Type RegistrationType, IReadOnlyList<Type> SupportingTypes);

    private sealed record ClassificationCase(
        Type Type,
        bool IsSagaOrDefinition,
        bool IsSagaStateMachineOrDefinition,
        bool IsConsumerKindOwned);
}
