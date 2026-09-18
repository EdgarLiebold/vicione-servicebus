using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class StateMachineSagaMessageConnectorDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "iteration-213-state-machine-connector-exact-public-protected-surface")]
    public void PublicSurface_PreservesTheExactConstructorAndProtectedOverrides()
    {
        Type type = typeof(StateMachineInterfaceType<MachineSaga, MachineMessage>.StateMachineSagaMessageConnector);
        Type baseType = typeof(SagaConnector<MachineSaga, MachineMessage>.SagaMessageConnector);

        Assert.True(type.IsNestedPublic);
        Assert.False(type.IsAbstract);
        Assert.False(type.IsSealed);
        Assert.Same(baseType, type.BaseType);
        ConstructorInfo constructor = Assert.Single(type.GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
        Assert.True(constructor.IsPublic);
        Assert.Equal(
            [
                typeof(IFilter<SagaConsumeContext<MachineSaga, MachineMessage>>),
                typeof(ISagaPolicy<MachineSaga, MachineMessage>),
                typeof(SagaFilterFactory<MachineSaga, MachineMessage>),
                typeof(IFilter<ConsumeContext<MachineMessage>>),
                typeof(bool),
            ],
            constructor.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal(
            ["consumeFilter", "policy", "sagaFilterFactory", "messageFilter", "configureConsumeTopology"],
            constructor.GetParameters().Select(parameter => parameter.Name));
        var nullability = new NullabilityInfoContext();
        Assert.Equal(
            [
                NullabilityState.NotNull,
                NullabilityState.Nullable,
                NullabilityState.Nullable,
                NullabilityState.Nullable,
                NullabilityState.NotNull,
            ],
            constructor.GetParameters().Select(parameter => nullability.Create(parameter).ReadState));

        PropertyInfo topology = type.GetProperty(
                "ConfigureConsumeTopology",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            ?? throw new InvalidOperationException("The protected topology property was not found.");
        Assert.Equal(typeof(bool), topology.PropertyType);
        Assert.True(topology.GetMethod!.IsFamily);
        Assert.True(topology.GetMethod.IsVirtual);
        Assert.False(topology.GetMethod.IsFinal);
        Assert.Same(baseType, topology.GetMethod.GetBaseDefinition().DeclaringType);
        Assert.False(topology.CanWrite);

        MethodInfo configure = type.GetMethod(
                "ConfigureMessagePipe",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            ?? throw new InvalidOperationException("The protected message-pipe method was not found.");
        Assert.True(configure.IsFamily);
        Assert.True(configure.IsVirtual);
        Assert.False(configure.IsFinal);
        Assert.Same(baseType, configure.GetBaseDefinition().DeclaringType);
        Assert.Equal(typeof(void), configure.ReturnType);
        Assert.Equal(
            [
                typeof(IPipeConfigurator<ConsumeContext<MachineMessage>>),
                typeof(ISagaRepository<MachineSaga>),
                typeof(IPipe<SagaConsumeContext<MachineSaga, MachineMessage>>),
            ],
            configure.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal(
            ["configurator", "repository", "sagaPipe"],
            configure.GetParameters().Select(parameter => parameter.Name));
        Assert.All(
            configure.GetParameters(),
            parameter => Assert.Equal(NullabilityState.NotNull, nullability.Create(parameter).ReadState));

        const BindingFlags declaredMembers = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
            | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        Assert.Equal(
            ["ConfigureConsumeTopology"],
            type.GetProperties(declaredMembers)
                .Where(property => IsPublicOrProtected(property.GetMethod) || IsPublicOrProtected(property.SetMethod))
                .Select(property => property.Name));
        Assert.Equal(
            ["ConfigureMessagePipe"],
            type.GetMethods(declaredMembers)
                .Where(method => !method.IsSpecialName && IsPublicOrProtected(method))
                .Select(method => method.Name));
        Assert.DoesNotContain(type.GetFields(declaredMembers), field =>
            field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly || field.IsFamilyAndAssembly);
        Assert.DoesNotContain(type.GetEvents(declaredMembers), @event =>
            IsPublicOrProtected(@event.AddMethod) || IsPublicOrProtected(@event.RemoveMethod));
        Assert.DoesNotContain(type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic), nested =>
            nested.IsNestedPublic || nested.IsNestedFamily || nested.IsNestedFamORAssem || nested.IsNestedFamANDAssem);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "iteration-213-state-machine-constructor-precedence-optionality-identity")]
    public void Constructor_EnforcesConsumeFilterThenPolicyWhilePreservingOptionalFiltersAndTopology()
    {
        var consumeFilter = new RecordingFilter<SagaConsumeContext<MachineSaga, MachineMessage>>();
        ISagaPolicy<MachineSaga, MachineMessage> policy = StrictStub<ISagaPolicy<MachineSaga, MachineMessage>>();
        var messageFilter = new RecordingFilter<ConsumeContext<MachineMessage>>();
        var factoryCalls = 0;
        SagaFilterFactory<MachineSaga, MachineMessage> factory = (_, _, _) =>
        {
            factoryCalls++;
            return new RecordingFilter<ConsumeContext<MachineMessage>>();
        };

        ArgumentNullException consumeException = Assert.Throws<ArgumentNullException>(() =>
            new ExposedConnector(null!, null, factory, messageFilter, configureConsumeTopology: true));
        ConfigurationException policyException = Assert.Throws<ConfigurationException>(() =>
            new ExposedConnector(consumeFilter, null, factory, messageFilter, configureConsumeTopology: true));
        var withOptionalFilters = new ExposedConnector(
            consumeFilter,
            policy,
            factory,
            messageFilter,
            configureConsumeTopology: true);
        var withoutOptionalFilters = new ExposedConnector(
            consumeFilter,
            policy,
            sagaFilterFactory: null,
            messageFilter: null,
            configureConsumeTopology: false);

        Assert.Equal("consumeFilter", consumeException.ParamName);
        Assert.Contains("repository policy", policyException.Message, StringComparison.Ordinal);
        Assert.Null(policyException.InnerException);
        Assert.True(withOptionalFilters.TopologyEnabled);
        Assert.False(withoutOptionalFilters.TopologyEnabled);
        Assert.Same(policy, ReadField<object>(withOptionalFilters, "_policy"));
        Assert.Same(factory, ReadField<object>(withOptionalFilters, "_sagaFilterFactory"));
        Assert.Same(messageFilter, ReadField<object>(withOptionalFilters, "_messageFilter"));
        Assert.Equal(0, factoryCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "iteration-213-state-machine-configure-argument-precedence-zero-effects")]
    public void ConfigureMessagePipe_RejectsRequiredArgumentsInSignatureOrderBeforeAnyEffect()
    {
        var factoryCalls = 0;
        SagaFilterFactory<MachineSaga, MachineMessage> factory = (_, _, _) =>
        {
            factoryCalls++;
            return new RecordingFilter<ConsumeContext<MachineMessage>>();
        };
        var connector = CreateConnector(factory, new RecordingFilter<ConsumeContext<MachineMessage>>());
        var configurator = new RecordingPipeConfigurator<ConsumeContext<MachineMessage>>();
        ISagaRepository<MachineSaga> repository = StrictStub<ISagaRepository<MachineSaga>>();
        IPipe<SagaConsumeContext<MachineSaga, MachineMessage>> sagaPipe =
            new RecordingPipe<SagaConsumeContext<MachineSaga, MachineMessage>>();

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            connector.Configure(null!, null!, null!)).ParamName);
        Assert.Equal("repository", Assert.Throws<ArgumentNullException>(() =>
            connector.Configure(configurator, null!, null!)).ParamName);
        Assert.Equal("sagaPipe", Assert.Throws<ArgumentNullException>(() =>
            connector.Configure(configurator, repository, null!)).ParamName);

        Assert.Equal(0, factoryCalls);
        Assert.Equal(0, configurator.AddCalls);

        var missingFactoryConnector = CreateConnector(
            sagaFilterFactory: null,
            new RecordingFilter<ConsumeContext<MachineMessage>>());
        var missingFactoryConfigurator = new RecordingPipeConfigurator<ConsumeContext<MachineMessage>>();

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            missingFactoryConnector.Configure(null!, null!, null!)).ParamName);
        Assert.Equal("repository", Assert.Throws<ArgumentNullException>(() =>
            missingFactoryConnector.Configure(missingFactoryConfigurator, null!, null!)).ParamName);
        Assert.Equal("sagaPipe", Assert.Throws<ArgumentNullException>(() =>
            missingFactoryConnector.Configure(missingFactoryConfigurator, repository, null!)).ParamName);
        Assert.Equal(0, missingFactoryConfigurator.AddCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "iteration-213-state-machine-missing-factory-diagnostic-before-effect")]
    public void ConfigureMessagePipe_WhenFactoryIsMissing_ReportsCorrelationBeforeAddingTheOptionalFilter()
    {
        var messageFilter = new RecordingFilter<ConsumeContext<MachineMessage>>();
        var connector = CreateConnector(sagaFilterFactory: null, messageFilter);
        var configurator = new RecordingPipeConfigurator<ConsumeContext<MachineMessage>>();

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => connector.Configure(
            configurator,
            StrictStub<ISagaRepository<MachineSaga>>(),
            new RecordingPipe<SagaConsumeContext<MachineSaga, MachineMessage>>()));

        Assert.Contains("not properly correlated", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(MachineSaga), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(MachineMessage), exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, configurator.AddCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "iteration-213-state-machine-factory-null-result-and-failure-identity")]
    public void ConfigureMessagePipe_ValidatesFactoryResultAndPropagatesFactoryFailureWithoutBuilderEffects()
    {
        ISagaRepository<MachineSaga> repository = StrictStub<ISagaRepository<MachineSaga>>();
        ISagaPolicy<MachineSaga, MachineMessage> policy = StrictStub<ISagaPolicy<MachineSaga, MachineMessage>>();
        IPipe<SagaConsumeContext<MachineSaga, MachineMessage>> sagaPipe =
            new RecordingPipe<SagaConsumeContext<MachineSaga, MachineMessage>>();
        ISagaRepository<MachineSaga>? receivedRepository = null;
        ISagaPolicy<MachineSaga, MachineMessage>? receivedPolicy = null;
        IPipe<SagaConsumeContext<MachineSaga, MachineMessage>>? receivedPipe = null;
        var nullConfigurator = new RecordingPipeConfigurator<ConsumeContext<MachineMessage>>();
        var nullConnector = new ExposedConnector(
            new RecordingFilter<SagaConsumeContext<MachineSaga, MachineMessage>>(),
            policy,
            (selectedRepository, selectedPolicy, selectedPipe) =>
            {
                receivedRepository = selectedRepository;
                receivedPolicy = selectedPolicy;
                receivedPipe = selectedPipe;
                return null!;
            },
            new RecordingFilter<ConsumeContext<MachineMessage>>(),
            configureConsumeTopology: true);

        InvalidOperationException nullResult = Assert.Throws<InvalidOperationException>(() =>
            nullConnector.Configure(nullConfigurator, repository, sagaPipe));

        Assert.Contains("returned a null filter", nullResult.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(MachineSaga), nullResult.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(MachineMessage), nullResult.Message, StringComparison.Ordinal);
        Assert.Same(repository, receivedRepository);
        Assert.Same(policy, receivedPolicy);
        Assert.Same(sagaPipe, receivedPipe);
        Assert.Equal(0, nullConfigurator.AddCalls);

        var failure = new InvalidOperationException("factory failed");
        var failureConfigurator = new RecordingPipeConfigurator<ConsumeContext<MachineMessage>>();
        var failingConnector = CreateConnector((_, _, _) => throw failure,
            new RecordingFilter<ConsumeContext<MachineMessage>>());

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
            failingConnector.Configure(failureConfigurator, repository, sagaPipe)));
        Assert.Equal(0, failureConfigurator.AddCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "iteration-213-state-machine-filter-order-identity-cardinality")]
    public void ConfigureMessagePipe_AddsOptionalMessageThenRequiredSagaFilterExactlyOnce()
    {
        var messageFilter = new RecordingFilter<ConsumeContext<MachineMessage>>();
        var sagaFilter = new RecordingFilter<ConsumeContext<MachineMessage>>();
        ISagaRepository<MachineSaga> repository = StrictStub<ISagaRepository<MachineSaga>>();
        ISagaPolicy<MachineSaga, MachineMessage> policy = StrictStub<ISagaPolicy<MachineSaga, MachineMessage>>();
        IPipe<SagaConsumeContext<MachineSaga, MachineMessage>> sagaPipe =
            new RecordingPipe<SagaConsumeContext<MachineSaga, MachineMessage>>();
        var factoryCalls = 0;
        var connector = new ExposedConnector(
            new RecordingFilter<SagaConsumeContext<MachineSaga, MachineMessage>>(),
            policy,
            (selectedRepository, selectedPolicy, selectedPipe) =>
            {
                factoryCalls++;
                Assert.Same(repository, selectedRepository);
                Assert.Same(policy, selectedPolicy);
                Assert.Same(sagaPipe, selectedPipe);
                return sagaFilter;
            },
            messageFilter,
            configureConsumeTopology: true);
        var configurator = new RecordingPipeConfigurator<ConsumeContext<MachineMessage>>();

        connector.Configure(configurator, repository, sagaPipe);

        Assert.Equal(1, factoryCalls);
        Assert.Collection(
            configurator.Specifications,
            specification => Assert.Same(messageFilter, ReadFilter(specification)),
            specification => Assert.Same(sagaFilter, ReadFilter(specification)));

        var requiredOnlyConfigurator = new RecordingPipeConfigurator<ConsumeContext<MachineMessage>>();
        var requiredOnlyConnector = new ExposedConnector(
            new RecordingFilter<SagaConsumeContext<MachineSaga, MachineMessage>>(),
            policy,
            (_, _, _) => sagaFilter,
            messageFilter: null,
            configureConsumeTopology: true);

        requiredOnlyConnector.Configure(requiredOnlyConfigurator, repository, sagaPipe);

        Assert.Collection(
            requiredOnlyConfigurator.Specifications,
            specification => Assert.Same(sagaFilter, ReadFilter(specification)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "iteration-213-state-machine-builder-failure-identity-cardinality")]
    public void ConfigureMessagePipe_PropagatesBuilderFailuresAtTheExactFilterAttempt()
    {
        var messageFilter = new RecordingFilter<ConsumeContext<MachineMessage>>();
        var sagaFilter = new RecordingFilter<ConsumeContext<MachineMessage>>();
        var connector = CreateConnector((_, _, _) => sagaFilter, messageFilter);
        ISagaRepository<MachineSaga> repository = StrictStub<ISagaRepository<MachineSaga>>();
        IPipe<SagaConsumeContext<MachineSaga, MachineMessage>> sagaPipe =
            new RecordingPipe<SagaConsumeContext<MachineSaga, MachineMessage>>();
        var firstFailure = new InvalidOperationException("first add failed");
        var firstConfigurator = new RecordingPipeConfigurator<ConsumeContext<MachineMessage>>(1, firstFailure);

        Assert.Same(firstFailure, Assert.Throws<InvalidOperationException>(() =>
            connector.Configure(firstConfigurator, repository, sagaPipe)));
        Assert.Equal(1, firstConfigurator.AddCalls);
        Assert.Same(messageFilter, ReadFilter(Assert.Single(firstConfigurator.Attempts)));

        var secondFailure = new InvalidOperationException("second add failed");
        var secondConfigurator = new RecordingPipeConfigurator<ConsumeContext<MachineMessage>>(2, secondFailure);

        Assert.Same(secondFailure, Assert.Throws<InvalidOperationException>(() =>
            connector.Configure(secondConfigurator, repository, sagaPipe)));
        Assert.Equal(2, secondConfigurator.AddCalls);
        Assert.Collection(
            secondConfigurator.Attempts,
            specification => Assert.Same(messageFilter, ReadFilter(specification)),
            specification => Assert.Same(sagaFilter, ReadFilter(specification)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "iteration-213-state-machine-inherited-topology-selection")]
    public void ConnectSaga_ComposesExactFiltersAndSelectsTheConfiguredTopologyOverload(bool configureConsumeTopology)
    {
        var consumeFilter = new RecordingFilter<SagaConsumeContext<MachineSaga, MachineMessage>>();
        var messageFilter = new RecordingFilter<ConsumeContext<MachineMessage>>();
        var sagaFilter = new RecordingFilter<ConsumeContext<MachineMessage>>();
        ISagaPolicy<MachineSaga, MachineMessage> policy = StrictStub<ISagaPolicy<MachineSaga, MachineMessage>>();
        ISagaRepository<MachineSaga> repository = StrictStub<ISagaRepository<MachineSaga>>();
        IPipe<SagaConsumeContext<MachineSaga, MachineMessage>>? receivedSagaPipe = null;
        var factoryCalls = 0;
        var connector = new ExposedConnector(
            consumeFilter,
            policy,
            (selectedRepository, selectedPolicy, selectedPipe) =>
            {
                factoryCalls++;
                Assert.Same(repository, selectedRepository);
                Assert.Same(policy, selectedPolicy);
                receivedSagaPipe = selectedPipe;
                return sagaFilter;
            },
            messageFilter,
            configureConsumeTopology);
        var messageSpecification = new SagaConnector<MachineSaga, MachineMessage>.SagaMessageSpecification();
        var sagaSpecification = new SagaSpecification<MachineSaga>([messageSpecification]);
        var consumePipe = new RecordingConsumePipeConnector();

        ConnectHandle result = connector.ConnectSaga(consumePipe, repository, sagaSpecification);

        Assert.Same(consumePipe.Handle, result);
        Assert.Equal(1, factoryCalls);
        Assert.NotNull(receivedSagaPipe);
        Assert.Equal([consumeFilter], ReadPipeFilters(receivedSagaPipe!));
        Assert.NotNull(consumePipe.ConnectedPipe);
        Assert.Equal([messageFilter, sagaFilter], ReadPipeFilters(consumePipe.ConnectedPipe!));
        Assert.Equal(configureConsumeTopology ? 1 : 0, consumePipe.DefaultCalls);
        Assert.Equal(configureConsumeTopology ? 0 : 1, consumePipe.OptionsCalls);
        Assert.Equal(
            configureConsumeTopology ? null : ConnectPipeOptions.All & ~ConnectPipeOptions.ConfigureConsumeTopology,
            consumePipe.Options);
        Assert.Equal(0, consumePipe.Handle.DisposeCalls);
    }

    private static ExposedConnector CreateConnector(
        SagaFilterFactory<MachineSaga, MachineMessage>? sagaFilterFactory,
        IFilter<ConsumeContext<MachineMessage>>? messageFilter) =>
        new(
            new RecordingFilter<SagaConsumeContext<MachineSaga, MachineMessage>>(),
            StrictStub<ISagaPolicy<MachineSaga, MachineMessage>>(),
            sagaFilterFactory,
            messageFilter,
            configureConsumeTopology: true);

    private static object ReadFilter(IPipeSpecification<ConsumeContext<MachineMessage>> specification)
    {
        var filterSpecification = Assert.IsType<FilterPipeSpecification<ConsumeContext<MachineMessage>>>(specification);
        return ReadField<object>(filterSpecification, "_filter");
    }

    private static object[] ReadPipeFilters<TContext>(IPipe<TContext> pipe)
        where TContext : class, PipeContext
    {
        var filters = new List<object>();
        object current = pipe;

        while (current.GetType().Name == "FilterPipe")
        {
            filters.Add(ReadField<object>(current, "_filter"));
            current = ReadField<object>(current, "_next");
        }

        if (current.GetType().Name == "LastPipe")
            filters.Add(ReadField<object>(current, "_filter"));

        return filters.ToArray();
    }

    private static T ReadField<T>(object target, string fieldName)
    {
        for (Type? type = target.GetType(); type is not null; type = type.BaseType)
        {
            FieldInfo? field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field is not null)
                return Assert.IsAssignableFrom<T>(field.GetValue(target));
        }

        throw new InvalidOperationException($"The field '{fieldName}' was not found on {target.GetType()}.");
    }

    private static bool IsPublicOrProtected(MethodBase? method) =>
        method is not null
        && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly || method.IsFamilyAndAssembly);

    private static T StrictStub<T>()
        where T : class => DispatchProxy.Create<T, StrictDispatchProxy>();

    private class StrictDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected collaborator call: {targetMethod?.Name}.");
    }

    private sealed class ExposedConnector :
        StateMachineInterfaceType<MachineSaga, MachineMessage>.StateMachineSagaMessageConnector
    {
        public ExposedConnector(
            IFilter<SagaConsumeContext<MachineSaga, MachineMessage>> consumeFilter,
            ISagaPolicy<MachineSaga, MachineMessage>? policy,
            SagaFilterFactory<MachineSaga, MachineMessage>? sagaFilterFactory,
            IFilter<ConsumeContext<MachineMessage>>? messageFilter,
            bool configureConsumeTopology)
            : base(consumeFilter, policy, sagaFilterFactory, messageFilter, configureConsumeTopology)
        {
        }

        public bool TopologyEnabled => ConfigureConsumeTopology;

        public void Configure(
            IPipeConfigurator<ConsumeContext<MachineMessage>> configurator,
            ISagaRepository<MachineSaga> repository,
            IPipe<SagaConsumeContext<MachineSaga, MachineMessage>> sagaPipe) =>
            ConfigureMessagePipe(configurator, repository, sagaPipe);
    }

    private sealed class RecordingPipeConfigurator<TContext>(int failAtCall = 0, Exception? failure = null) :
        IPipeConfigurator<TContext>
        where TContext : class, PipeContext
    {
        public List<IPipeSpecification<TContext>> Attempts { get; } = [];

        public List<IPipeSpecification<TContext>> Specifications { get; } = [];

        public int AddCalls { get; private set; }

        public void AddPipeSpecification(IPipeSpecification<TContext> specification)
        {
            AddCalls++;
            Attempts.Add(specification);

            if (AddCalls == failAtCall)
                throw failure ?? new InvalidOperationException("Synthetic add failure.");

            Specifications.Add(specification);
        }
    }

    private sealed class RecordingConsumePipeConnector : IConsumePipeConnector
    {
        public RecordingConnectHandle Handle { get; } = new();

        public int DefaultCalls { get; private set; }

        public int OptionsCalls { get; private set; }

        public IPipe<ConsumeContext<MachineMessage>>? ConnectedPipe { get; private set; }

        public ConnectPipeOptions? Options { get; private set; }

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
            where T : class
        {
            DefaultCalls++;
            ConnectedPipe = Assert.IsAssignableFrom<IPipe<ConsumeContext<MachineMessage>>>(pipe);
            return Handle;
        }

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
            where T : class
        {
            OptionsCalls++;
            ConnectedPipe = Assert.IsAssignableFrom<IPipe<ConsumeContext<MachineMessage>>>(pipe);
            Options = options;
            return Handle;
        }
    }

    private sealed class RecordingConnectHandle : ConnectHandle
    {
        public int DisposeCalls { get; private set; }

        public void Disconnect() => DisposeCalls++;

        public void Dispose() => DisposeCalls++;
    }

    private sealed class RecordingFilter<TContext> : IFilter<TContext>
        where TContext : class, PipeContext
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(TContext context, IPipe<TContext> next) => next.SendAsync(context);
    }

    private sealed class RecordingPipe<TContext> : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(TContext context) => Task.CompletedTask;
    }

    public sealed class MachineSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed record MachineMessage;
}
