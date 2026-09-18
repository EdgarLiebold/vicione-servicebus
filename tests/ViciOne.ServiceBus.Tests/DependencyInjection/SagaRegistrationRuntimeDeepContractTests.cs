using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class SagaRegistrationRuntimeDeepContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION-OWNER", "constructor-and-definition-context-required-boundaries")]
    public void RegistrationOwners_RejectMissingSelectorAndDefinitionContext(bool stateMachine)
    {
        ArgumentNullException selector = stateMachine
            ? Assert.Throws<ArgumentNullException>(() =>
                new SagaStateMachineRegistration<ContractStateMachine, ContractState>(null!))
            : Assert.Throws<ArgumentNullException>(() => new SagaRegistration<ContractSaga>(null!));
        Assert.Equal("selector", selector.ParamName);

        ISagaRegistration registration = CreateRegistration(stateMachine, new RecordingContainerSelector());

        ArgumentNullException context = Assert.Throws<ArgumentNullException>(() => registration.GetDefinition(null!));

        Assert.Equal("context", context.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION-OWNER", "type-state-machine-and-exclusion-metadata")]
    public void RegistrationOwners_ExposeExactTypeStateMachineAndExclusionMetadata()
    {
        var selector = new RecordingContainerSelector();
        ISagaRegistration saga = new SagaRegistration<ExcludedSaga>(selector);
        ISagaRegistration stateMachine =
            new SagaStateMachineRegistration<ExcludedStateMachine, ExcludedState>(selector);

        Assert.Equal(typeof(ExcludedSaga), saga.Type);
        Assert.Null(saga.StateMachineType);
        Assert.False(saga.IncludeInConfigureEndpoints);
        Assert.Equal(typeof(ExcludedState), stateMachine.Type);
        Assert.Equal(typeof(ExcludedStateMachine), stateMachine.StateMachineType);
        Assert.False(stateMachine.IncludeInConfigureEndpoints);

        saga.IncludeInConfigureEndpoints = true;
        stateMachine.IncludeInConfigureEndpoints = true;
        Assert.True(saga.IncludeInConfigureEndpoints);
        Assert.True(stateMachine.IncludeInConfigureEndpoints);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION-OWNER", "concrete-public-metadata-defaults-and-mutation")]
    public void RegistrationOwners_ExposeConcretePublicMetadataDefaultsAndMutation()
    {
        var selector = new RecordingContainerSelector();
        var saga = new SagaRegistration<ContractSaga>(selector);
        var stateMachine = new SagaStateMachineRegistration<ContractStateMachine, ContractState>(selector);
        var excludedSaga = new SagaRegistration<ExcludedSaga>(selector);
        var excludedStateMachine = new SagaStateMachineRegistration<ExcludedStateMachine, ExcludedState>(selector);

        Assert.Equal(typeof(ContractSaga), saga.Type);
        Assert.Null(saga.StateMachineType);
        Assert.True(saga.IncludeInConfigureEndpoints);
        Assert.Equal(typeof(ContractState), stateMachine.Type);
        Assert.Equal(typeof(ContractStateMachine), stateMachine.StateMachineType);
        Assert.True(stateMachine.IncludeInConfigureEndpoints);
        Assert.False(excludedSaga.IncludeInConfigureEndpoints);
        Assert.False(excludedStateMachine.IncludeInConfigureEndpoints);

        saga.IncludeInConfigureEndpoints = false;
        stateMachine.IncludeInConfigureEndpoints = false;
        excludedSaga.IncludeInConfigureEndpoints = true;
        excludedStateMachine.IncludeInConfigureEndpoints = true;

        Assert.False(saga.IncludeInConfigureEndpoints);
        Assert.False(stateMachine.IncludeInConfigureEndpoints);
        Assert.True(excludedSaga.IncludeInConfigureEndpoints);
        Assert.True(excludedStateMachine.IncludeInConfigureEndpoints);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION-OWNER", "saga-configure-order-exact-action-decoration-and-final-specification")]
    public void SagaRegistration_ConfigureAppliesDefinitionThenOnlyExactActionAndAddsFinalSpecification()
    {
        var events = new List<string>();
        var definition = new RecordingSagaDefinition<ContractSaga>(events);
        IEndpointDefinition<ContractSaga> endpointDefinition = CreateProxy<IEndpointDefinition<ContractSaga>>();
        var selector = new RecordingContainerSelector
        {
            SagaDefinition = definition,
            SagaEndpoint = endpointDefinition,
        };
        var decorator = new RecordingDecoratorRegistration<ContractSaga>(events);
        ITestRegistrationContext context = CreateRegistrationContext(
            new Dictionary<Type, object?>
            {
                [typeof(ISagaRepositoryDecoratorRegistration<ContractSaga>)] = decorator,
            });
        IReceiveEndpointConfigurator endpoint = CreateEndpointConfigurator(events, out RecordingEndpointProxy endpointProxy);
        var registration = new SagaRegistration<ContractSaga>(selector);
        ISagaRegistration owner = registration;
        var mismatchedCalls = 0;
        ISagaConfigurator<ContractSaga>? actionConfigurator = null;

        owner.AddConfigureAction<ContractSaga>(null);
        owner.AddConfigureAction<OtherSaga>((_, _) => mismatchedCalls++);
        owner.AddConfigureAction<ContractSaga>((actualContext, configurator) =>
        {
            events.Add("action");
            Assert.Same(context, actualContext);
            actionConfigurator = configurator;
        });

        owner.Configure(endpoint, context);

        Assert.Equal(
            ["decorate", "endpoint-definition", "definition", "action", "endpoint-specification"],
            events);
        Assert.Equal(0, mismatchedCalls);
        Assert.Equal(1, decorator.DecorateCount);
        Assert.IsType<DependencyInjectionSagaRepository<ContractSaga>>(decorator.InputRepository);
        Assert.Same(endpoint, definition.EndpointConfigurator);
        Assert.Same(context, definition.Context);
        Assert.Same(definition.SagaConfigurator, actionConfigurator);
        IReceiveEndpointSpecification finalSpecification =
            Assert.IsAssignableFrom<IReceiveEndpointSpecification>(actionConfigurator);
        Assert.Same(finalSpecification, endpointProxy.EndpointSpecification);
        Assert.Same(endpointDefinition, definition.EndpointDefinition);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION-OWNER", "state-machine-configure-order-action-decoration-observers-final-specification")]
    public void StateMachineRegistration_ConfigureResolvesOwnerConnectsObserversAndAddsFinalSpecification()
    {
        var events = new List<string>();
        var definition = new RecordingSagaDefinition<ContractState>(events);
        IEndpointDefinition<ContractState> endpointDefinition = CreateProxy<IEndpointDefinition<ContractState>>();
        var selector = new RecordingContainerSelector
        {
            StateDefinition = definition,
            StateEndpoint = endpointDefinition,
        };
        ISagaStateMachine<ContractState> stateMachine = CreateStateMachine(events, out RecordingStateMachineProxy stateMachineProxy);
        var decorator = new RecordingDecoratorRegistration<ContractState>(events);
        IEventObserver<ContractState> firstEventObserver = CreateProxy<IEventObserver<ContractState>>();
        IEventObserver<ContractState> secondEventObserver = CreateProxy<IEventObserver<ContractState>>();
        IStateObserver<ContractState> stateObserver = CreateProxy<IStateObserver<ContractState>>();
        ITestRegistrationContext context = CreateRegistrationContext(
            new Dictionary<Type, object?>
            {
                [typeof(ISagaStateMachine<ContractState>)] = stateMachine,
                [typeof(ISagaRepositoryDecoratorRegistration<ContractState>)] = decorator,
                [typeof(IEnumerable<IEventObserver<ContractState>>)] =
                    new[] { firstEventObserver, secondEventObserver },
                [typeof(IEnumerable<IStateObserver<ContractState>>)] = new[] { stateObserver },
            },
            serviceType =>
            {
                if (serviceType == typeof(ISagaStateMachine<ContractState>))
                    events.Add("resolve-state-machine");
            });
        IReceiveEndpointConfigurator endpoint = CreateEndpointConfigurator(events, out RecordingEndpointProxy endpointProxy);
        var registration =
            new SagaStateMachineRegistration<ISagaStateMachine<ContractState>, ContractState>(selector);
        var mismatchedCalls = 0;
        ISagaConfigurator<ContractState>? actionConfigurator = null;

        registration.AddConfigureAction<ContractState>(null);
        registration.AddConfigureAction<OtherSaga>((_, _) => mismatchedCalls++);
        registration.AddConfigureAction<ContractState>((actualContext, configurator) =>
        {
            events.Add("action");
            Assert.Same(context, actualContext);
            actionConfigurator = configurator;
        });

        registration.Configure(endpoint, context);

        Assert.Equal(
            [
                "resolve-state-machine",
                "decorate",
                "endpoint-definition",
                "definition",
                "action",
                "connect-event",
                "connect-event",
                "connect-state",
                "endpoint-specification",
            ],
            events);
        Assert.Equal(0, mismatchedCalls);
        Assert.Equal(1, decorator.DecorateCount);
        Assert.IsType<DependencyInjectionSagaRepository<ContractState>>(decorator.InputRepository);
        Assert.Same(endpoint, definition.EndpointConfigurator);
        Assert.Same(context, definition.Context);
        Assert.Same(definition.SagaConfigurator, actionConfigurator);
        IReceiveEndpointSpecification finalSpecification =
            Assert.IsAssignableFrom<IReceiveEndpointSpecification>(actionConfigurator);
        Assert.Same(finalSpecification, endpointProxy.EndpointSpecification);
        Assert.Equal([firstEventObserver, secondEventObserver], stateMachineProxy.EventObservers);
        Assert.Equal([stateObserver], stateMachineProxy.StateObservers);
        Assert.Same(endpointDefinition, definition.EndpointDefinition);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION-OWNER", "configure-required-owner-boundaries-before-collaborator-effects")]
    public void Configure_RejectsNullOwnersBeforeCollaboratorEffects(bool stateMachine)
    {
        var selector = new RecordingContainerSelector();
        ISagaRegistration registration = stateMachine
            ? new SagaStateMachineRegistration<ISagaStateMachine<ContractState>, ContractState>(selector)
            : new SagaRegistration<ContractSaga>(selector);
        var services = new Dictionary<Type, object?>();
        if (stateMachine)
        {
            services[typeof(ISagaStateMachine<ContractState>)] =
                CreateStateMachine([], out _);
            services[typeof(IEnumerable<IEventObserver<ContractState>>)] =
                Array.Empty<IEventObserver<ContractState>>();
            services[typeof(IEnumerable<IStateObserver<ContractState>>)] =
                Array.Empty<IStateObserver<ContractState>>();
        }

        var serviceRequestCount = 0;
        var endpointEvents = new List<string>();
        ITestRegistrationContext context = CreateRegistrationContext(services, _ => serviceRequestCount++);
        IReceiveEndpointConfigurator endpoint = CreateEndpointConfigurator(endpointEvents, out _);

        ArgumentNullException configurator =
            Assert.Throws<ArgumentNullException>(() => registration.Configure(null!, context));
        ArgumentNullException registrationContext =
            Assert.Throws<ArgumentNullException>(() => registration.Configure(endpoint, null!));

        Assert.Equal("configurator", configurator.ParamName);
        Assert.Equal("context", registrationContext.ParamName);
        Assert.Equal(0, serviceRequestCount);
        Assert.Equal(0, selector.DefinitionCount);
        Assert.Equal(0, selector.EndpointCount);
        Assert.Empty(endpointEvents);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION-OWNER", "default-definition-and-null-endpoint-selection")]
    public void GetDefinition_UsesDefaultDefinitionWhenSelectorReturnsNullAndAcceptsNoEndpoint(bool stateMachine)
    {
        var selector = new RecordingContainerSelector();
        ISagaRegistration registration = CreateRegistration(stateMachine, selector);
        IRegistrationContext context = CreateProxy<IRegistrationContext>();

        ISagaDefinition first = registration.GetDefinition(context);
        ISagaDefinition second = registration.GetDefinition(context);

        Type expectedType = stateMachine
            ? typeof(DefaultSagaDefinition<ContractState>)
            : typeof(DefaultSagaDefinition<ContractSaga>);
        Assert.Equal(expectedType, first.GetType());
        Assert.Same(first, second);
        Assert.Null(first.EndpointDefinition);
        Assert.Equal(1, selector.DefinitionCount);
        Assert.Equal(1, selector.EndpointCount);
        Assert.Same(context, selector.LastProvider);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION-OWNER", "concurrent-first-definition-single-identity-and-complete-endpoint")]
    public async Task DefinitionCache_ConcurrentFirstCallsPublishOneCompleteIdentityAsync(bool stateMachine)
    {
        var sagaDefinition = new RecordingSagaDefinition<ContractSaga>();
        var stateDefinition = new RecordingSagaDefinition<ContractState>();
        IEndpointDefinition<ContractSaga> sagaEndpoint = CreateProxy<IEndpointDefinition<ContractSaga>>();
        IEndpointDefinition<ContractState> stateEndpoint = CreateProxy<IEndpointDefinition<ContractState>>();
        var endpointSelectionStarted = NewSignal();
        using var releaseEndpointSelection = new ManualResetEventSlim(initialState: false);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var selector = new RecordingContainerSelector
        {
            SagaDefinition = sagaDefinition,
            StateDefinition = stateDefinition,
            SagaEndpoint = sagaEndpoint,
            StateEndpoint = stateEndpoint,
            BeforeEndpointSelection = () =>
            {
                endpointSelectionStarted.TrySetResult();
                releaseEndpointSelection.Wait(cancellationToken);
            },
        };
        ISagaRegistration registration = CreateRegistration(stateMachine, selector);
        IRegistrationContext context = CreateProxy<IRegistrationContext>();
        Task<DefinitionSnapshot> first = Task.Run(() => ObserveDefinition(registration, context), cancellationToken);

        await endpointSelectionStarted.Task.WaitAsync(cancellationToken);
        var contenderStarted = NewSignal();
        Task<DefinitionSnapshot> contender = Task.Factory.StartNew(
            () =>
            {
                contenderStarted.TrySetResult();
                return ObserveDefinition(registration, context);
            },
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        try
        {
            await contenderStarted.Task.WaitAsync(cancellationToken);
            Assert.False(contender.IsCompleted);
        }
        finally
        {
            releaseEndpointSelection.Set();
        }

        DefinitionSnapshot[] results = await Task.WhenAll(first, contender).WaitAsync(cancellationToken);

        ISagaDefinition expectedDefinition = stateMachine ? stateDefinition : sagaDefinition;
        IEndpointDefinition expectedEndpoint = stateMachine ? stateEndpoint : sagaEndpoint;
        Assert.All(results, result => Assert.Same(expectedDefinition, result.Definition));
        Assert.All(results, result => Assert.Same(expectedEndpoint, result.EndpointAtReturn));
        Assert.Same(results[0].Definition, results[1].Definition);
        Assert.Equal(1, selector.DefinitionCount);
        Assert.Equal(1, selector.EndpointCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-DI-REGISTRATION-OWNER", "definition-cache-failure-atomic-retry-and-identity")]
    public void DefinitionCache_PublishesOnlyAfterEndpointSelectionAndThenReturnsOneIdentity(bool stateMachine)
    {
        var sagaDefinition = new RecordingSagaDefinition<ContractSaga>();
        var stateDefinition = new RecordingSagaDefinition<ContractState>();
        IEndpointDefinition<ContractSaga> sagaEndpoint = CreateProxy<IEndpointDefinition<ContractSaga>>();
        IEndpointDefinition<ContractState> stateEndpoint = CreateProxy<IEndpointDefinition<ContractState>>();
        var selector = new RecordingContainerSelector
        {
            SagaDefinition = sagaDefinition,
            StateDefinition = stateDefinition,
            SagaEndpoint = sagaEndpoint,
            StateEndpoint = stateEndpoint,
            FailEndpointSelection = true,
        };
        ISagaRegistration registration = CreateRegistration(stateMachine, selector);
        IRegistrationContext context = CreateProxy<IRegistrationContext>();

        ExpectedEndpointSelectionFailure failure =
            Assert.Throws<ExpectedEndpointSelectionFailure>(() => registration.GetDefinition(context));
        Assert.Same(selector.EndpointFailure, failure);

        selector.FailEndpointSelection = false;
        ISagaDefinition first = registration.GetDefinition(context);
        ISagaDefinition second = registration.GetDefinition(context);

        ISagaDefinition expectedDefinition = stateMachine ? stateDefinition : sagaDefinition;
        IEndpointDefinition expectedEndpoint = stateMachine ? stateEndpoint : sagaEndpoint;
        Assert.Same(expectedDefinition, first);
        Assert.Same(first, second);
        Assert.Same(expectedEndpoint, first.EndpointDefinition);
        Assert.Equal(2, selector.DefinitionCount);
        Assert.Equal(2, selector.EndpointCount);
        Assert.Same(context, selector.LastProvider);
    }

    private static ISagaRegistration CreateRegistration(bool stateMachine, IContainerSelector selector) =>
        stateMachine
            ? new SagaStateMachineRegistration<ContractStateMachine, ContractState>(selector)
            : new SagaRegistration<ContractSaga>(selector);

    private static DefinitionSnapshot ObserveDefinition(
        ISagaRegistration registration,
        IRegistrationContext context)
    {
        ISagaDefinition definition = registration.GetDefinition(context);
        return new DefinitionSnapshot(definition, definition.EndpointDefinition);
    }

    private static ITestRegistrationContext CreateRegistrationContext(
        IReadOnlyDictionary<Type, object?> services,
        Action<Type>? serviceRequested = null)
    {
        ITestRegistrationContext context = DispatchProxy.Create<ITestRegistrationContext, RegistrationContextProxy>();
        var proxy = (RegistrationContextProxy)(object)context;
        proxy.Services = services;
        proxy.ServiceRequested = serviceRequested;
        return context;
    }

    private static IReceiveEndpointConfigurator CreateEndpointConfigurator(
        List<string> events,
        out RecordingEndpointProxy proxy)
    {
        IReceiveEndpointConfigurator configurator =
            DispatchProxy.Create<IReceiveEndpointConfigurator, RecordingEndpointProxy>();
        proxy = (RecordingEndpointProxy)(object)configurator;
        proxy.Events = events;
        return configurator;
    }

    private static ISagaStateMachine<ContractState> CreateStateMachine(
        List<string> events,
        out RecordingStateMachineProxy proxy)
    {
        ISagaStateMachine<ContractState> stateMachine =
            DispatchProxy.Create<ISagaStateMachine<ContractState>, RecordingStateMachineProxy>();
        proxy = (RecordingStateMachineProxy)(object)stateMachine;
        proxy.Events = events;
        return stateMachine;
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static T CreateProxy<T>()
        where T : class => DispatchProxy.Create<T, PassiveProxy>();

    private sealed class RecordingContainerSelector : IContainerSelector
    {
        private int _definitionCount;
        private int _endpointCount;

        public RecordingSagaDefinition<ContractSaga>? SagaDefinition { get; init; }

        public RecordingSagaDefinition<ContractState>? StateDefinition { get; init; }

        public IEndpointDefinition<ContractSaga>? SagaEndpoint { get; init; }

        public IEndpointDefinition<ContractState>? StateEndpoint { get; init; }

        public bool FailEndpointSelection { get; set; }

        public Action? BeforeEndpointSelection { get; init; }

        public ExpectedEndpointSelectionFailure EndpointFailure { get; } = new();

        public int DefinitionCount => Volatile.Read(ref _definitionCount);

        public int EndpointCount => Volatile.Read(ref _endpointCount);

        public IServiceProvider? LastProvider { get; private set; }

        public bool TryGetRegistration<T>(
            IServiceProvider provider,
            Type type,
            [NotNullWhen(true)] out T? value)
            where T : class, IRegistration
        {
            value = null;
            return false;
        }

        public IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
            where T : class, IRegistration => [];

        public T? GetDefinition<T>(IServiceProvider provider)
            where T : class, IDefinition
        {
            Interlocked.Increment(ref _definitionCount);
            LastProvider = provider;
            object? result = typeof(T) == typeof(ISagaDefinition<ContractSaga>)
                ? SagaDefinition
                : typeof(T) == typeof(ISagaDefinition<ContractState>)
                    ? StateDefinition
                    : null;
            return result as T;
        }

        public IEndpointDefinition<T>? GetEndpointDefinition<T>(IServiceProvider provider)
            where T : class
        {
            Interlocked.Increment(ref _endpointCount);
            LastProvider = provider;
            if (FailEndpointSelection)
                throw EndpointFailure;

            BeforeEndpointSelection?.Invoke();

            object? result = typeof(T) == typeof(ContractSaga)
                ? SagaEndpoint
                : typeof(T) == typeof(ContractState)
                    ? StateEndpoint
                    : null;
            return result as IEndpointDefinition<T>;
        }

        public IConfigureReceiveEndpoint GetConfigureReceiveEndpoints(IServiceProvider provider) =>
            throw new NotSupportedException();

        public IEndpointNameFormatter GetEndpointNameFormatter(IServiceProvider provider) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingSagaDefinition<TSaga> : ISagaDefinition<TSaga>
        where TSaga : class, ISaga
    {
        private readonly List<string>? _events;
        private IEndpointDefinition<TSaga>? _endpointDefinition;

        public RecordingSagaDefinition(List<string>? events = null)
        {
            _events = events;
        }

        public Type SagaType => typeof(TSaga);

        public int? ConcurrentMessageLimit => null;

        public IEndpointDefinition? EndpointDefinition => _endpointDefinition;

        IEndpointDefinition<TSaga> ISagaDefinition<TSaga>.EndpointDefinition
        {
            set
            {
                _endpointDefinition = value;
                _events?.Add("endpoint-definition");
            }
        }

        public IReceiveEndpointConfigurator? EndpointConfigurator { get; private set; }

        public ISagaConfigurator<TSaga>? SagaConfigurator { get; private set; }

        public IRegistrationContext? Context { get; private set; }

        public string GetEndpointName(IEndpointNameFormatter formatter) => typeof(TSaga).Name;

        public void Configure(
            IReceiveEndpointConfigurator endpointConfigurator,
            ISagaConfigurator<TSaga> sagaConfigurator,
            IRegistrationContext context)
        {
            _events?.Add("definition");
            EndpointConfigurator = endpointConfigurator;
            SagaConfigurator = sagaConfigurator;
            Context = context;
        }
    }

    private sealed class RecordingDecoratorRegistration<TSaga>(List<string> events) :
        ISagaRepositoryDecoratorRegistration<TSaga>
        where TSaga : class, ISaga
    {
        public int DecorateCount { get; private set; }

        public ISagaRepository<TSaga>? InputRepository { get; private set; }

        public ISagaRepository<TSaga> DecorateSagaRepository(ISagaRepository<TSaga> repository)
        {
            events.Add("decorate");
            DecorateCount++;
            InputRepository = repository;
            return repository;
        }
    }

    private interface ITestRegistrationContext :
        IRegistrationContext,
        ISetScopedConsumeContext;

    private sealed record DefinitionSnapshot(
        ISagaDefinition Definition,
        IEndpointDefinition? EndpointAtReturn);

    private class ContractSaga : ISaga, IInitiatedBy<RegistrationMessage>
    {
        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<RegistrationMessage> context) => Task.CompletedTask;
    }

    private sealed record RegistrationMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    private sealed class OtherSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    [ExcludeFromConfigureEndpoints]
    private sealed class ExcludedSaga : ContractSaga;

    private class ContractState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    [ExcludeFromConfigureEndpoints]
    private sealed class ExcludedState : ContractState;

    private sealed class ContractStateMachine : ViciOneServiceBusStateMachine<ContractState>;

    private sealed class ExcludedStateMachine : ViciOneServiceBusStateMachine<ExcludedState>;

    private sealed class ExpectedEndpointSelectionFailure : Exception;

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private class RegistrationContextProxy : DispatchProxy
    {
        public IReadOnlyDictionary<Type, object?> Services { get; set; } =
            new Dictionary<Type, object?>();

        public Action<Type>? ServiceRequested { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == nameof(IServiceProvider.GetService) && args is [Type serviceType])
            {
                ServiceRequested?.Invoke(serviceType);
                return Services.GetValueOrDefault(serviceType);
            }

            if (targetMethod.Name == nameof(ISetScopedConsumeContext.PushContext))
                return new NoopDisposable();

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class RecordingEndpointProxy : DispatchProxy
    {
        public List<string> Events { get; set; } = [];

        public IReceiveEndpointSpecification? EndpointSpecification { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == nameof(IReceiveEndpointConfigurator.AddEndpointSpecification)
                && args is [IReceiveEndpointSpecification specification])
            {
                Events.Add("endpoint-specification");
                EndpointSpecification = specification;
                return null;
            }

            if (targetMethod.Name == $"get_{nameof(IReceiveEndpointConfigurator.InputAddress)}")
                return new Uri("loopback://localhost/saga-registration");

            return targetMethod.ReturnType == typeof(void)
                ? null
                : targetMethod.ReturnType.IsValueType
                    ? Activator.CreateInstance(targetMethod.ReturnType)
                    : null;
        }
    }

    private class RecordingStateMachineProxy : DispatchProxy
    {
        public List<string> Events { get; set; } = [];

        public List<IEventObserver<ContractState>> EventObservers { get; } = [];

        public List<IStateObserver<ContractState>> StateObservers { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == $"get_{nameof(ISagaStateMachine<ContractState>.Correlations)}")
                return Array.Empty<IEventCorrelation>();

            if (targetMethod.Name == nameof(IStateMachine<ContractState>.ConnectEventObserver)
                && args is [IEventObserver<ContractState> eventObserver])
            {
                Events.Add("connect-event");
                EventObservers.Add(eventObserver);
                return new NoopDisposable();
            }

            if (targetMethod.Name == nameof(IStateMachine<ContractState>.ConnectStateObserver)
                && args is [IStateObserver<ContractState> stateObserver])
            {
                Events.Add("connect-state");
                StateObservers.Add(stateObserver);
                return new NoopDisposable();
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.ReturnType == typeof(void)
                ? null
                : targetMethod?.ReturnType.IsValueType == true
                    ? Activator.CreateInstance(targetMethod.ReturnType)
                    : null;
    }
}
