using System.Reflection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class StateMachineInterfaceTypeFactoryDeepContractTests
{
    private const BindingFlags DeclaredPublicMembers =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private const BindingFlags DeclaredMembers =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly NullabilityInfoContext Nullability = new();

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONFIGURATION", "iteration-213-interface-builders-and-factory-public-shape")]
    public void PublicSurface_PreservesTheInterfaceBuilderAndConnectorFactoryShapes()
    {
        Type owner = typeof(StateMachineInterfaceType<MachineState, Message>);
        Assert.True(owner.IsPublic);
        Assert.False(owner.IsAbstract);
        Assert.False(owner.IsSealed);
        AssertDirectInterfaces(owner, typeof(IStateMachineInterfaceType));
        Type[] ownerArguments = typeof(StateMachineInterfaceType<,>).GetGenericArguments();
        AssertGenericParameter(ownerArguments[0], "TInstance", GenericParameterAttributes.ReferenceTypeConstraint,
            typeof(ISaga), typeof(ISagaStateMachineInstance));
        AssertGenericParameter(ownerArguments[1], "TData", GenericParameterAttributes.ReferenceTypeConstraint);
        ConstructorInfo ownerConstructor = Assert.Single(owner.GetConstructors());
        AssertParameters(
            ownerConstructor,
            ("machine", typeof(ISagaStateMachine<MachineState>)),
            ("correlation", typeof(IEventCorrelation<MachineState, Message>)));

        Type directBuilder = typeof(StateMachineInterfaceType<MachineState, Message>.MessageCorrelationIdEventCorrelationBuilder);
        Type faultBuilder = typeof(StateMachineInterfaceType<MachineState, Message>.MessageCorrelationIdFaultEventCorrelationBuilder);
        Type factory = typeof(StateMachineInterfaceType<MachineState, Message>.StateMachineEventConnectorFactory);
        AssertNestedPublicConcrete(directBuilder, typeof(IEventCorrelationBuilder));
        AssertNestedPublicConcrete(faultBuilder, typeof(IEventCorrelationBuilder));
        AssertNestedPublicConcrete(factory, typeof(ISagaConnectorFactory));
        ConstructorInfo directBuilderConstructor = Assert.Single(directBuilder.GetConstructors());
        ConstructorInfo faultBuilderConstructor = Assert.Single(faultBuilder.GetConstructors());
        ConstructorInfo factoryConstructor = Assert.Single(factory.GetConstructors());
        AssertParameters(
            directBuilderConstructor,
            ("machine", typeof(ISagaStateMachine<MachineState>)),
            ("event", typeof(IEvent<Message>)),
            ("messageCorrelationId", typeof(IMessageCorrelationId<Message>)));
        AssertParameters(
            faultBuilderConstructor,
            ("machine", typeof(ISagaStateMachine<MachineState>)),
            ("event", typeof(IEvent<Fault<Message>>)),
            ("messageCorrelationId", typeof(IMessageCorrelationId<Message>)));
        AssertParameters(
            factoryConstructor,
            ("stateMachine", typeof(ISagaStateMachine<MachineState>)),
            ("correlation", typeof(IEventCorrelation<MachineState, Message>)));
        MethodInfo directBuild = AssertBuildSurface(directBuilder);
        MethodInfo faultBuild = AssertBuildSurface(faultBuilder);
        AssertDeclaredPublicOrProtectedSurface(directBuilder, directBuilderConstructor, directBuild);
        AssertDeclaredPublicOrProtectedSurface(faultBuilder, faultBuilderConstructor, faultBuild);
        AssertDeclaredPublicOrProtectedSurface(factory, factoryConstructor);
        AssertExplicitConnectorMethod(owner, nameof(IStateMachineInterfaceType.GetConnector));
        AssertExplicitConnectorMethod(factory, nameof(ISagaConnectorFactory.CreateMessageConnector));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONFIGURATION", "iteration-213-constructor-null-precedence-at-each-owner")]
    public void Constructors_RejectEveryRequiredInputInSignatureOrderAtItsOwningBoundary()
    {
        ISagaStateMachine<MachineState> machine = StrictStub<ISagaStateMachine<MachineState>>();
        IEvent<Message> @event = StrictStub<IEvent<Message>>();
        IEvent<Fault<Message>> faultEvent = StrictStub<IEvent<Fault<Message>>>();
        var messageCorrelationId = new RecordingMessageCorrelationId<Message>(true, NewId.NextGuid());
        var correlation = CreateCorrelation(@event);

        AssertParameter("machine", () => new StateMachineInterfaceType<MachineState, Message>(null!, null!));
        AssertParameter("correlation", () => new StateMachineInterfaceType<MachineState, Message>(machine, null!));
        AssertParameter("machine", () =>
            new StateMachineInterfaceType<MachineState, Message>.MessageCorrelationIdEventCorrelationBuilder(null!, null!, null!));
        AssertParameter("event", () =>
            new StateMachineInterfaceType<MachineState, Message>.MessageCorrelationIdEventCorrelationBuilder(machine, null!, null!));
        AssertParameter("messageCorrelationId", () =>
            new StateMachineInterfaceType<MachineState, Message>.MessageCorrelationIdEventCorrelationBuilder(machine, @event, null!));
        AssertParameter("machine", () =>
            new StateMachineInterfaceType<MachineState, Message>.MessageCorrelationIdFaultEventCorrelationBuilder(null!, null!, null!));
        AssertParameter("event", () =>
            new StateMachineInterfaceType<MachineState, Message>.MessageCorrelationIdFaultEventCorrelationBuilder(machine, null!, null!));
        AssertParameter("messageCorrelationId", () =>
            new StateMachineInterfaceType<MachineState, Message>.MessageCorrelationIdFaultEventCorrelationBuilder(machine, faultEvent, null!));
        AssertParameter("stateMachine", () =>
            new StateMachineInterfaceType<MachineState, Message>.StateMachineEventConnectorFactory(null!, null!));
        AssertParameter("correlation", () =>
            new StateMachineInterfaceType<MachineState, Message>.StateMachineEventConnectorFactory(machine, null!));

        Assert.Equal(0, correlation.TotalPropertyCalls);
        _ = new StateMachineInterfaceType<MachineState, Message>.MessageCorrelationIdEventCorrelationBuilder(
            machine,
            @event,
            messageCorrelationId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "iteration-213-build-fresh-correlation-stable-owner-identity")]
    public void Build_ReturnsFreshTypedCorrelationsWithStableOwnersAndConfiguredIdentity()
    {
        ISagaStateMachine<MachineState> machine = StrictStub<ISagaStateMachine<MachineState>>();
        IEvent<Message> @event = StrictStub<IEvent<Message>>();
        IEvent<Fault<Message>> faultEvent = StrictStub<IEvent<Fault<Message>>>();
        var extractor = new RecordingMessageCorrelationId<Message>(true, NewId.NextGuid());
        var directBuilder = new StateMachineInterfaceType<MachineState, Message>.MessageCorrelationIdEventCorrelationBuilder(
            machine,
            @event,
            extractor);
        var faultBuilder = new StateMachineInterfaceType<MachineState, Message>.MessageCorrelationIdFaultEventCorrelationBuilder(
            machine,
            faultEvent,
            extractor);

        var directFirst = Assert.IsType<MessageEventCorrelation<MachineState, Message>>(directBuilder.Build());
        var directSecond = Assert.IsType<MessageEventCorrelation<MachineState, Message>>(directBuilder.Build());
        var faultFirst = Assert.IsType<MessageEventCorrelation<MachineState, Fault<Message>>>(faultBuilder.Build());
        var faultSecond = Assert.IsType<MessageEventCorrelation<MachineState, Fault<Message>>>(faultBuilder.Build());

        Assert.NotSame(directFirst, directSecond);
        Assert.NotSame(faultFirst, faultSecond);
        Assert.Same(machine, ReadField<object>(directFirst, "_machine"));
        Assert.Same(machine, ReadField<object>(faultFirst, "_machine"));
        Assert.Same(@event, directFirst.Event);
        Assert.Same(faultEvent, faultFirst.Event);
        Assert.Same(directFirst.MessageFilter, directSecond.MessageFilter);
        Assert.Same(faultFirst.MessageFilter, faultSecond.MessageFilter);
        Assert.NotNull(directFirst.FilterFactory);
        Assert.NotNull(faultFirst.FilterFactory);
        Assert.True(directFirst.ConfigureConsumeTopology);
        Assert.True(faultFirst.ConfigureConsumeTopology);
        Assert.Equal(0, extractor.Calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "iteration-213-message-correlation-success-missing-failure-identity")]
    public async Task MessageCorrelationIdBuilder_PreservesSuccessMissingAndFailureOutcomesAsync()
    {
        ISagaStateMachine<MachineState> machine = StrictStub<ISagaStateMachine<MachineState>>();
        IEvent<Message> @event = StrictStub<IEvent<Message>>();
        var message = new Message("direct");
        Guid envelopeId = NewId.NextGuid();
        Guid selectedId = NewId.NextGuid();
        ConsumeContext<Message> context = CreateContext(message, envelopeId);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var output = new RecordingPipe<ConsumeContext<Message>>(completion.Task);
        var success = new RecordingMessageCorrelationId<Message>(true, selectedId);
        CorrelationIdMessageFilter<Message> successFilter = BuildDirectFilter(machine, @event, success);

        Task result = successFilter.SendAsync(context, output);

        Assert.Same(completion.Task, result);
        Assert.Same(message, success.Message);
        Assert.Equal(1, success.Calls);
        Assert.NotNull(output.Context);
        Assert.Same(message, output.Context.Message);
        Assert.Equal(selectedId, output.Context.CorrelationId);
        Assert.NotEqual(envelopeId, output.Context.CorrelationId);

        var missing = new RecordingMessageCorrelationId<Message>(false, Guid.Empty);
        CorrelationIdMessageFilter<Message> missingFilter = BuildDirectFilter(machine, @event, missing);
        ArgumentException missingFailure = await Assert.ThrowsAsync<ArgumentException>(() =>
            missingFilter.SendAsync(context, new RecordingPipe<ConsumeContext<Message>>(Task.CompletedTask)));
        Assert.Equal($"The message {TypeCache<Message>.ShortName} did not have a correlationId", missingFailure.Message);
        Assert.Same(message, missing.Message);

        var expectedFailure = new SyntheticCorrelationException();
        var throwing = new RecordingMessageCorrelationId<Message>(expectedFailure);
        CorrelationIdMessageFilter<Message> throwingFilter = BuildDirectFilter(machine, @event, throwing);
        SyntheticCorrelationException actualFailure = await Assert.ThrowsAsync<SyntheticCorrelationException>(() =>
            throwingFilter.SendAsync(context, new RecordingPipe<ConsumeContext<Message>>(Task.CompletedTask)));
        Assert.Same(expectedFailure, actualFailure);
        Assert.Same(message, throwing.Message);

        completion.SetResult();
        await result;
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "iteration-213-fault-original-message-correlation-path")]
    public async Task FaultBuilder_ExtractsOnlyFromTheEnclosedOriginalMessageAsync()
    {
        ISagaStateMachine<MachineState> machine = StrictStub<ISagaStateMachine<MachineState>>();
        IEvent<Fault<Message>> @event = StrictStub<IEvent<Fault<Message>>>();
        var original = new Message("original");
        var fault = new TestFault<Message>(original);
        Guid envelopeId = NewId.NextGuid();
        Guid selectedId = NewId.NextGuid();
        ConsumeContext<Fault<Message>> context = CreateContext<Fault<Message>>(fault, envelopeId);
        var extractor = new RecordingMessageCorrelationId<Message>(true, selectedId);
        CorrelationIdMessageFilter<Fault<Message>> filter = BuildFaultFilter(machine, @event, extractor);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var output = new RecordingPipe<ConsumeContext<Fault<Message>>>(completion.Task);

        Task result = filter.SendAsync(context, output);

        Assert.Same(completion.Task, result);
        Assert.Same(original, extractor.Message);
        Assert.Equal(1, extractor.Calls);
        Assert.NotNull(output.Context);
        Assert.Same(fault, output.Context.Message);
        Assert.Same(original, output.Context.Message.Message);
        Assert.Equal(selectedId, output.Context.CorrelationId);
        Assert.NotEqual(envelopeId, output.Context.CorrelationId);

        var missing = new RecordingMessageCorrelationId<Message>(false, Guid.Empty);
        CorrelationIdMessageFilter<Fault<Message>> missingFilter = BuildFaultFilter(machine, @event, missing);
        ArgumentException failure = await Assert.ThrowsAsync<ArgumentException>(() =>
            missingFilter.SendAsync(context, new RecordingPipe<ConsumeContext<Fault<Message>>>(Task.CompletedTask)));
        Assert.Equal($"The message {TypeCache<Message>.ShortName} did not have a correlationId", failure.Message);
        Assert.Same(original, missing.Message);

        var expectedFailure = new SyntheticCorrelationException();
        var throwing = new RecordingMessageCorrelationId<Message>(expectedFailure);
        CorrelationIdMessageFilter<Fault<Message>> throwingFilter = BuildFaultFilter(machine, @event, throwing);
        SyntheticCorrelationException actualFailure = await Assert.ThrowsAsync<SyntheticCorrelationException>(() =>
            throwingFilter.SendAsync(context, new RecordingPipe<ConsumeContext<Fault<Message>>>(Task.CompletedTask)));
        Assert.Same(expectedFailure, actualFailure);
        Assert.Same(original, throwing.Message);
        Assert.Equal(1, throwing.Calls);

        completion.SetResult();
        await result;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONFIGURATION", "iteration-213-connector-exact-instance-type-cached-identity")]
    public void ConnectorFactories_RequireTheExactInstanceTypeAndPreserveTheirCachedConnectorIdentity(
        bool configureConsumeTopology)
    {
        ISagaStateMachine<MachineState> machine = StrictStub<ISagaStateMachine<MachineState>>();
        var correlation = CreateCorrelation(StrictStub<IEvent<Message>>());
        IFilter<ConsumeContext<Message>> messageFilter = StrictStub<IFilter<ConsumeContext<Message>>>();
        correlation.MessageFilterValue = messageFilter;
        correlation.ConfigureConsumeTopologyValue = configureConsumeTopology;
        var factory = new StateMachineInterfaceType<MachineState, Message>.StateMachineEventConnectorFactory(machine, correlation);
        ISagaConnectorFactory untypedFactory = factory;
        Assert.Equal(1, correlation.ConfigureConsumeTopologyCalls);

        ISagaMessageConnector<MachineState> first = untypedFactory.CreateMessageConnector<MachineState>();
        ISagaMessageConnector<MachineState> second = untypedFactory.CreateMessageConnector<MachineState>();
        Assert.Same(first, second);
        Assert.IsType<StateMachineInterfaceType<MachineState, Message>.StateMachineSagaMessageConnector>(first);
        Assert.Equal(typeof(Message), first.MessageType);
        Assert.Same(messageFilter, ReadField<object>(first, "_messageFilter"));
        Assert.Equal(configureConsumeTopology, ReadProperty<bool>(first, "ConfigureConsumeTopology"));
        ArgumentException factoryMismatch = Assert.Throws<ArgumentException>(() =>
            untypedFactory.CreateMessageConnector<OtherState>());
        Assert.Equal("T", factoryMismatch.ParamName);
        Assert.Same(first, untypedFactory.CreateMessageConnector<MachineState>());
        Assert.Equal(1, correlation.ConfigureConsumeTopologyCalls);

        var owner = new StateMachineInterfaceType<MachineState, Message>(machine, correlation);
        IStateMachineInterfaceType untypedOwner = owner;
        Assert.Equal(2, correlation.ConfigureConsumeTopologyCalls);
        ISagaMessageConnector<MachineState> ownerFirst = untypedOwner.GetConnector<MachineState>();
        ISagaMessageConnector<MachineState> ownerSecond = untypedOwner.GetConnector<MachineState>();
        Assert.Same(ownerFirst, ownerSecond);
        Assert.Same(messageFilter, ReadField<object>(ownerFirst, "_messageFilter"));
        Assert.Equal(configureConsumeTopology, ReadProperty<bool>(ownerFirst, "ConfigureConsumeTopology"));
        ArgumentException ownerMismatch = Assert.Throws<ArgumentException>(() => untypedOwner.GetConnector<OtherState>());
        Assert.Equal("T", ownerMismatch.ParamName);
        Assert.Same(ownerFirst, untypedOwner.GetConnector<MachineState>());
        Assert.Equal(2, correlation.ConfigureConsumeTopologyCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONFIGURATION", "iteration-213-connector-collaborator-null-results")]
    public void ConnectorFactory_PreservesFilterFactoryContractAndRejectsNullCollaboratorResultsAtTheExactOwningBoundary()
    {
        ISagaStateMachine<MachineState> machine = StrictStub<ISagaStateMachine<MachineState>>();
        var missingEvent = CreateCorrelation(null);
        InvalidOperationException eventFailure = Assert.Throws<InvalidOperationException>(() =>
            new StateMachineInterfaceType<MachineState, Message>.StateMachineEventConnectorFactory(machine, missingEvent));
        Assert.Equal("The event correlation returned a null event.", eventFailure.Message);
        Assert.Equal(1, missingEvent.EventCalls);
        Assert.Equal(0, missingEvent.PolicyCalls);

        var missingPolicy = CreateCorrelation(StrictStub<IEvent<Message>>());
        missingPolicy.PolicyValue = null;
        ConfigurationException policyFailure = Assert.Throws<ConfigurationException>(() =>
            new StateMachineInterfaceType<MachineState, Message>.StateMachineEventConnectorFactory(machine, missingPolicy));
        Assert.Contains("did not provide a repository policy", policyFailure.Message, StringComparison.Ordinal);

        ISagaRepository<MachineState> repository = StrictStub<ISagaRepository<MachineState>>();
        ISagaPolicy<MachineState, Message> sagaPolicy = StrictStub<ISagaPolicy<MachineState, Message>>();
        IPipe<SagaConsumeContext<MachineState, Message>> sagaPipe =
            StrictStub<IPipe<SagaConsumeContext<MachineState, Message>>>();
        IFilter<ConsumeContext<Message>> expectedFilter = StrictStub<IFilter<ConsumeContext<Message>>>();
        ISagaRepository<MachineState>? receivedRepository = null;
        ISagaPolicy<MachineState, Message>? receivedPolicy = null;
        IPipe<SagaConsumeContext<MachineState, Message>>? receivedPipe = null;
        var forwardingCalls = 0;
        SagaFilterFactory<MachineState, Message> forwardingFactory = (actualRepository, actualPolicy, actualPipe) =>
        {
            forwardingCalls++;
            receivedRepository = actualRepository;
            receivedPolicy = actualPolicy;
            receivedPipe = actualPipe;
            return expectedFilter;
        };
        var forwardingCorrelation = CreateCorrelation(StrictStub<IEvent<Message>>(), forwardingFactory);
        var forwardingConnectorFactory =
            new StateMachineInterfaceType<MachineState, Message>.StateMachineEventConnectorFactory(
                machine,
                forwardingCorrelation);
        ISagaMessageConnector<MachineState> forwardingConnector =
            ((ISagaConnectorFactory)forwardingConnectorFactory).CreateMessageConnector<MachineState>();
        SagaFilterFactory<MachineState, Message> guardedForwardingFactory =
            ReadField<SagaFilterFactory<MachineState, Message>>(forwardingConnector, "_sagaFilterFactory");

        IFilter<ConsumeContext<Message>> actualFilter = guardedForwardingFactory(repository, sagaPolicy, sagaPipe);

        Assert.Same(expectedFilter, actualFilter);
        Assert.Same(repository, receivedRepository);
        Assert.Same(sagaPolicy, receivedPolicy);
        Assert.Same(sagaPipe, receivedPipe);
        Assert.Equal(1, forwardingCalls);

        var expectedFactoryFailure = new SyntheticFilterFactoryException();
        SagaFilterFactory<MachineState, Message> throwingFactory = (_, _, _) => throw expectedFactoryFailure;
        var throwingCorrelation = CreateCorrelation(StrictStub<IEvent<Message>>(), throwingFactory);
        var throwingConnectorFactory =
            new StateMachineInterfaceType<MachineState, Message>.StateMachineEventConnectorFactory(
                machine,
                throwingCorrelation);
        ISagaMessageConnector<MachineState> throwingConnector =
            ((ISagaConnectorFactory)throwingConnectorFactory).CreateMessageConnector<MachineState>();
        SagaFilterFactory<MachineState, Message> guardedThrowingFactory =
            ReadField<SagaFilterFactory<MachineState, Message>>(throwingConnector, "_sagaFilterFactory");

        SyntheticFilterFactoryException actualFactoryFailure = Assert.Throws<SyntheticFilterFactoryException>(() =>
            guardedThrowingFactory(repository, sagaPolicy, sagaPipe));

        Assert.Same(expectedFactoryFailure, actualFactoryFailure);

        SagaFilterFactory<MachineState, Message> nullFactory = (_, _, _) => null!;
        var nullFilter = CreateCorrelation(StrictStub<IEvent<Message>>(), nullFactory);
        var factory = new StateMachineInterfaceType<MachineState, Message>.StateMachineEventConnectorFactory(machine, nullFilter);
        ISagaMessageConnector<MachineState> connector = ((ISagaConnectorFactory)factory).CreateMessageConnector<MachineState>();
        SagaFilterFactory<MachineState, Message> guardedFactory =
            ReadField<SagaFilterFactory<MachineState, Message>>(connector, "_sagaFilterFactory");

        InvalidOperationException filterFailure = Assert.Throws<InvalidOperationException>(() => guardedFactory(
            StrictStub<ISagaRepository<MachineState>>(),
            StrictStub<ISagaPolicy<MachineState, Message>>(),
            StrictStub<IPipe<SagaConsumeContext<MachineState, Message>>>()));

        Assert.Equal("The event correlation filter factory returned a null saga filter.", filterFailure.Message);
        Assert.Equal(1, nullFilter.EventCalls);
        Assert.Equal(1, nullFilter.PolicyCalls);
        Assert.Equal(1, nullFilter.FilterFactoryCalls);
        Assert.Equal(1, nullFilter.MessageFilterCalls);
        Assert.Equal(1, nullFilter.ConfigureConsumeTopologyCalls);

        var deferredMissingFilter = CreateCorrelation(StrictStub<IEvent<Message>>());
        deferredMissingFilter.FilterFactoryValue = null;
        var deferredFactory = new StateMachineInterfaceType<MachineState, Message>.StateMachineEventConnectorFactory(
            machine,
            deferredMissingFilter);
        Assert.NotNull(((ISagaConnectorFactory)deferredFactory).CreateMessageConnector<MachineState>());
    }

    private static RecordingEventCorrelation CreateCorrelation(
        IEvent<Message>? @event,
        SagaFilterFactory<MachineState, Message>? filterFactory = null)
    {
        filterFactory ??= (_, _, _) => StrictStub<IFilter<ConsumeContext<Message>>>();
        return new RecordingEventCorrelation
        {
            EventValue = @event,
            PolicyValue = StrictStub<ISagaPolicy<MachineState, Message>>(),
            FilterFactoryValue = filterFactory,
        };
    }

    private static CorrelationIdMessageFilter<Message> BuildDirectFilter(
        ISagaStateMachine<MachineState> machine,
        IEvent<Message> @event,
        IMessageCorrelationId<Message> extractor)
    {
        var builder = new StateMachineInterfaceType<MachineState, Message>.MessageCorrelationIdEventCorrelationBuilder(
            machine,
            @event,
            extractor);
        var correlation = Assert.IsType<MessageEventCorrelation<MachineState, Message>>(builder.Build());
        return Assert.IsType<CorrelationIdMessageFilter<Message>>(correlation.MessageFilter);
    }

    private static CorrelationIdMessageFilter<Fault<Message>> BuildFaultFilter(
        ISagaStateMachine<MachineState> machine,
        IEvent<Fault<Message>> @event,
        IMessageCorrelationId<Message> extractor)
    {
        var builder = new StateMachineInterfaceType<MachineState, Message>.MessageCorrelationIdFaultEventCorrelationBuilder(
            machine,
            @event,
            extractor);
        var correlation = Assert.IsType<MessageEventCorrelation<MachineState, Fault<Message>>>(builder.Build());
        return Assert.IsType<CorrelationIdMessageFilter<Fault<Message>>>(correlation.MessageFilter);
    }

    private static ConsumeContext<T> CreateContext<T>(T message, Guid correlationId)
        where T : class =>
        InMemoryOutboxTestContextFactory.Create(message, TestContext.Current.CancellationToken, correlationId: correlationId);

    private static void AssertNestedPublicConcrete(Type type, Type expectedInterface)
    {
        Assert.True(type.IsNestedPublic);
        Assert.False(type.IsAbstract);
        Assert.False(type.IsSealed);
        AssertDirectInterfaces(type, expectedInterface);
    }

    private static MethodInfo AssertBuildSurface(Type type)
    {
        MethodInfo build = Assert.Single(type.GetMethods(DeclaredPublicMembers));
        Assert.Equal(nameof(IEventCorrelationBuilder.Build), build.Name);
        Assert.Equal(typeof(IEventCorrelation), build.ReturnType);
        Assert.Empty(build.GetParameters());
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(build.ReturnParameter).ReadState);
        return build;
    }

    private static void AssertDeclaredPublicOrProtectedSurface(
        Type type,
        ConstructorInfo expectedConstructor,
        params MethodInfo[] expectedMethods)
    {
        Assert.Equal(
            new[] { expectedConstructor },
            type.GetConstructors(DeclaredMembers).Where(IsPublicOrProtected));
        Assert.Equal(
            expectedMethods.OrderBy(method => method.MetadataToken),
            type.GetMethods(DeclaredMembers).Where(IsPublicOrProtected).OrderBy(method => method.MetadataToken));
        Assert.DoesNotContain(type.GetFields(DeclaredMembers), IsPublicOrProtected);
        Assert.DoesNotContain(type.GetProperties(DeclaredMembers), property =>
            property.GetAccessors(true).Any(IsPublicOrProtected));
        Assert.DoesNotContain(type.GetEvents(DeclaredMembers), @event =>
            IsPublicOrProtected(@event.AddMethod)
            || IsPublicOrProtected(@event.RemoveMethod)
            || IsPublicOrProtected(@event.RaiseMethod));
        Assert.DoesNotContain(type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic), nested =>
            nested.IsNestedPublic
            || nested.IsNestedFamily
            || nested.IsNestedFamORAssem
            || nested.IsNestedFamANDAssem);
    }

    private static bool IsPublicOrProtected(MethodBase? member) =>
        member is not null
        && (member.IsPublic || member.IsFamily || member.IsFamilyOrAssembly || member.IsFamilyAndAssembly);

    private static bool IsPublicOrProtected(FieldInfo member) =>
        member.IsPublic || member.IsFamily || member.IsFamilyOrAssembly || member.IsFamilyAndAssembly;

    private static void AssertParameters(MethodBase member, params (string Name, Type Type)[] expected)
    {
        ParameterInfo[] parameters = member.GetParameters();
        Assert.Equal(expected.Length, parameters.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            Assert.Equal(expected[index].Name, parameters[index].Name);
            Assert.Equal(expected[index].Type, parameters[index].ParameterType);
            if (!parameters[index].ParameterType.IsValueType)
                Assert.Equal(NullabilityState.NotNull, Nullability.Create(parameters[index]).ReadState);
        }
    }

    private static void AssertDirectInterfaces(Type type, params Type[] expected)
    {
        Type[] all = type.GetInterfaces();
        Type[] direct = all.Where(candidate => !all.Any(
            other => other != candidate && other.GetInterfaces().Contains(candidate))).ToArray();
        Assert.Equal(
            expected.OrderBy(TypeIdentity, StringComparer.Ordinal),
            direct.OrderBy(TypeIdentity, StringComparer.Ordinal));
    }

    private static void AssertGenericParameter(Type parameter, string name, GenericParameterAttributes attributes,
        params Type[] constraints)
    {
        Assert.True(parameter.IsGenericParameter);
        Assert.Equal(name, parameter.Name);
        Assert.Equal(attributes, parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal(
            constraints.OrderBy(TypeIdentity, StringComparer.Ordinal),
            parameter.GetGenericParameterConstraints().OrderBy(TypeIdentity, StringComparer.Ordinal));
    }

    private static void AssertExplicitConnectorMethod(Type type, string methodName)
    {
        MethodInfo method = Assert.Single(type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly), candidate =>
            candidate.Name.EndsWith($".{methodName}", StringComparison.Ordinal));
        Assert.True(method.IsPrivate);
        Assert.True(method.IsFinal);
        Assert.True(method.IsVirtual);
        Assert.True(method.IsGenericMethodDefinition);
        Assert.Empty(method.GetParameters());

        Type argument = Assert.Single(method.GetGenericArguments());
        AssertGenericParameter(argument, "T", GenericParameterAttributes.ReferenceTypeConstraint, typeof(ISaga));
        Assert.Equal(typeof(ISagaMessageConnector<>).MakeGenericType(argument), method.ReturnType);
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(method.ReturnParameter).ReadState);
    }

    private static string TypeIdentity(Type type) => type.AssemblyQualifiedName ?? type.FullName ?? type.Name;

    private static void AssertParameter(string name, Action action) =>
        Assert.Equal(name, Assert.Throws<ArgumentNullException>(action).ParamName);

    private static T ReadField<T>(object owner, string name)
    {
        for (Type? type = owner.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo? field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                return Assert.IsAssignableFrom<T>(field.GetValue(owner));
        }

        throw new Xunit.Sdk.XunitException($"Field '{name}' was not found on {owner.GetType()}.");
    }

    private static T ReadProperty<T>(object owner, string name)
    {
        for (Type? type = owner.GetType(); type != null; type = type.BaseType)
        {
            PropertyInfo? property = type.GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (property != null)
                return Assert.IsAssignableFrom<T>(property.GetValue(owner));
        }

        throw new Xunit.Sdk.XunitException($"Property '{name}' was not found on {owner.GetType()}.");
    }

    private static T StrictStub<T>() where T : class => DispatchProxy.Create<T, StrictDispatchProxy>();

    private sealed class RecordingMessageCorrelationId<T> : IMessageCorrelationId<T> where T : class
    {
        readonly Exception? _failure;
        readonly Guid _correlationId;
        readonly bool _success;

        public RecordingMessageCorrelationId(bool success, Guid correlationId)
        {
            _success = success;
            _correlationId = correlationId;
        }

        public RecordingMessageCorrelationId(Exception failure)
        {
            _failure = failure;
        }

        public int Calls { get; private set; }
        public T? Message { get; private set; }

        public bool TryGetCorrelationId(T message, out Guid correlationId)
        {
            Calls++;
            Message = message;
            if (_failure != null)
                throw _failure;

            correlationId = _correlationId;
            return _success;
        }
    }

    private sealed class RecordingEventCorrelation : IEventCorrelation<MachineState, Message>
    {
        public IEvent<Message>? EventValue { get; set; }
        public ISagaPolicy<MachineState, Message>? PolicyValue { get; set; }
        public SagaFilterFactory<MachineState, Message>? FilterFactoryValue { get; set; }
        public IFilter<ConsumeContext<Message>>? MessageFilterValue { get; set; }
        public bool ConfigureConsumeTopologyValue { get; set; } = true;
        public int EventCalls { get; private set; }
        public int PolicyCalls { get; private set; }
        public int FilterFactoryCalls { get; private set; }
        public int MessageFilterCalls { get; private set; }
        public int ConfigureConsumeTopologyCalls { get; private set; }
        public int TotalPropertyCalls => EventCalls + PolicyCalls + FilterFactoryCalls + MessageFilterCalls + ConfigureConsumeTopologyCalls;
        public Type DataType => typeof(Message);
        public IEvent<Message> Event { get { EventCalls++; return EventValue!; } }
        public ISagaPolicy<MachineState, Message>? Policy { get { PolicyCalls++; return PolicyValue; } }
        public SagaFilterFactory<MachineState, Message>? FilterFactory { get { FilterFactoryCalls++; return FilterFactoryValue; } }
        public IFilter<ConsumeContext<Message>>? MessageFilter { get { MessageFilterCalls++; return MessageFilterValue; } }
        public bool ConfigureConsumeTopology { get { ConfigureConsumeTopologyCalls++; return ConfigureConsumeTopologyValue; } }
        public IEnumerable<ValidationResult> Validate() => [];
    }

    private sealed class RecordingPipe<TContext>(Task completion) : IPipe<TContext> where TContext : class, PipeContext
    {
        public TContext? Context { get; private set; }
        public Task SendAsync(TContext context) { Context = context; return completion; }
        public void Probe(ProbeContext context) { }
    }

    private sealed class TestFault<T>(T message) : Fault<T>
    {
        public T Message { get; } = message;
        public Guid FaultId { get; } = NewId.NextGuid();
        public Guid? FaultedMessageId => null;
        public DateTimeOffset Timestamp => DateTimeOffset.UnixEpoch;
        public ExceptionInfo[] Exceptions { get; } = [];
        public HostInfo Host => null!;
        public string[] FaultMessageTypes { get; } = [];
    }

    private class StrictDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected collaborator call: {targetMethod?.Name}.");
    }

    public sealed class MachineState : ISagaStateMachineInstance { public Guid CorrelationId { get; set; } }
    public sealed class OtherState : ISagaStateMachineInstance { public Guid CorrelationId { get; set; } }
    public sealed record Message(string Value);
    private sealed class SyntheticCorrelationException : Exception;
    private sealed class SyntheticFilterFactoryException : Exception;
}
