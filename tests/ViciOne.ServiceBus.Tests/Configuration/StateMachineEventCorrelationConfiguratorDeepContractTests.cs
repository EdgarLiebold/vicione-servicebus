using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class StateMachineEventCorrelationConfiguratorDeepContractTests
{
    private const BindingFlags DeclaredPublicMembers =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    private static readonly NullabilityInfoContext Nullability = new();

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "iteration-213-configurator-exact-surface-nullability-sync-names")]
    public void PublicSurface_HasExactInterfacesMembersNullabilityAndSynchronousNames()
    {
        Type configurator = typeof(StateMachineInterfaceType<CorrelationSaga, CorrelationMessage>
            .ViciOneServiceBusEventCorrelationConfigurator);

        Assert.True(configurator.IsNestedPublic);
        Assert.False(configurator.IsAbstract);
        Assert.False(configurator.IsSealed);
        AssertDirectInterfaces(configurator,
            typeof(IEventCorrelationConfigurator<CorrelationSaga, CorrelationMessage>),
            typeof(IEventCorrelationBuilder));

        ConstructorInfo constructor = Assert.Single(configurator.GetConstructors(DeclaredPublicMembers));
        ParameterInfo[] constructorParameters = constructor.GetParameters();
        Assert.Equal(
            [typeof(ISagaStateMachine<CorrelationSaga>), typeof(IEvent<CorrelationMessage>), typeof(IEventCorrelation)],
            constructorParameters.Select(parameter => parameter.ParameterType));
        Assert.Equal(["machine", "event", "existingCorrelation"], constructorParameters.Select(parameter => parameter.Name));
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(constructorParameters[0]).ReadState);
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(constructorParameters[1]).ReadState);
        Assert.Equal(NullabilityState.Nullable, Nullability.Create(constructorParameters[2]).ReadState);

        FieldInfo missingPipe = Assert.IsAssignableFrom<FieldInfo>(
            configurator.GetField("_missingPipe", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.Equal(typeof(IPipe<ConsumeContext<CorrelationMessage>>), missingPipe.FieldType);
        Assert.Equal(NullabilityState.Nullable, Nullability.Create(missingPipe).ReadState);

        Type correlation = typeof(MessageEventCorrelation<CorrelationSaga, CorrelationMessage>);
        ConstructorInfo correlationConstructor = Assert.Single(correlation.GetConstructors(DeclaredPublicMembers));
        ParameterInfo[] correlationParameters = correlationConstructor.GetParameters();
        Assert.Equal(
            [
                typeof(ISagaStateMachine<CorrelationSaga>), typeof(IEvent<CorrelationMessage>),
                typeof(SagaFilterFactory<CorrelationSaga, CorrelationMessage>), typeof(IFilter<ConsumeContext<CorrelationMessage>>),
                typeof(IPipe<ConsumeContext<CorrelationMessage>>), typeof(ISagaFactory<CorrelationSaga, CorrelationMessage>),
                typeof(bool), typeof(bool), typeof(bool)
            ],
            correlationParameters.Select(parameter => parameter.ParameterType));
        Assert.Equal(
            [
                "machine", "event", "sagaFilterFactory", "messageFilter", "missingPipe", "sagaFactory", "insertOnInitial", "readOnly",
                "configureConsumeTopology"
            ],
            correlationParameters.Select(parameter => parameter.Name));
        Assert.Equal(
            [
                NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.Nullable, NullabilityState.Nullable,
                NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.NotNull
            ],
            correlationParameters.Select(parameter => Nullability.Create(parameter).ReadState));

        AssertProperty(configurator, nameof(IEventCorrelationConfigurator<CorrelationSaga, CorrelationMessage>.InsertOnInitial));
        AssertProperty(configurator, nameof(IEventCorrelationConfigurator<CorrelationSaga, CorrelationMessage>.ReadOnly));
        AssertProperty(configurator, nameof(IEventCorrelationConfigurator<CorrelationSaga, CorrelationMessage>.ConfigureConsumeTopology));

        MethodInfo[] methods = configurator.GetMethods(DeclaredPublicMembers)
            .Where(method => !method.IsSpecialName)
            .ToArray();
        Assert.Equal(
            ["Build", "CorrelateBy", "CorrelateBy", "CorrelateBy", "CorrelateById", "CorrelateById", "OnMissingInstance", "SelectId",
                "SetSagaFactory"],
            methods.Select(method => method.Name).OrderBy(name => name, StringComparer.Ordinal));
        Assert.Single(methods, method => method.Name == nameof(IEventCorrelationBuilder.Build) &&
            method.ReturnType == typeof(IEventCorrelation) && method.GetParameters().Length == 0);
        Assert.Equal(2, methods.Count(method => method.Name == nameof(IEventCorrelationConfigurator<CorrelationSaga, CorrelationMessage>.CorrelateById)));
        Assert.Equal(3, methods.Count(method => method.Name == nameof(IEventCorrelationConfigurator<CorrelationSaga, CorrelationMessage>.CorrelateBy)));
        Assert.All(methods.SelectMany(method => method.GetParameters()), parameter =>
            Assert.Equal(NullabilityState.NotNull, Nullability.Create(parameter).ReadState));
        Assert.All(methods.Where(method => !method.ReturnType.IsValueType), method =>
            Assert.Equal(NullabilityState.NotNull, Nullability.Create(method.ReturnParameter).ReadState));
        Assert.DoesNotContain(methods, method =>
            typeof(Task).IsAssignableFrom(method.ReturnType) && !method.Name.EndsWith("Async", StringComparison.Ordinal));

        MethodInfo[] genericMethods = methods.Where(method => method.IsGenericMethodDefinition).ToArray();
        Assert.Equal(3, genericMethods.Length);
        Assert.Single(genericMethods, method => method.Name == nameof(IEventCorrelationConfigurator<CorrelationSaga, CorrelationMessage>.CorrelateById) &&
            HasGenericConstraint(method, GenericParameterAttributes.NotNullableValueTypeConstraint));
        Assert.Single(genericMethods, method => method.Name == nameof(IEventCorrelationConfigurator<CorrelationSaga, CorrelationMessage>.CorrelateBy) &&
            HasGenericConstraint(method, GenericParameterAttributes.NotNullableValueTypeConstraint));
        Assert.Single(genericMethods, method => method.Name == nameof(IEventCorrelationConfigurator<CorrelationSaga, CorrelationMessage>.CorrelateBy) &&
            HasGenericConstraint(method, GenericParameterAttributes.ReferenceTypeConstraint));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "iteration-213-constructor-guards-defaults-existing-inheritance")]
    public void Constructor_RejectsOwnersInOrderAndInheritsOnlyMatchingCorrelationFilters()
    {
        ISagaStateMachine<CorrelationSaga> machine = StrictStub<ISagaStateMachine<CorrelationSaga>>();
        IEvent<CorrelationMessage> @event = StrictStub<IEvent<CorrelationMessage>>();
        ISagaFactory<CorrelationSaga, CorrelationMessage> sagaFactory = StrictStub<ISagaFactory<CorrelationSaga, CorrelationMessage>>();

        AssertArgument("machine", () =>
            new StateMachineInterfaceType<CorrelationSaga, CorrelationMessage>.ViciOneServiceBusEventCorrelationConfigurator(
                null!, @event, null));
        AssertArgument("machine", () =>
            new StateMachineInterfaceType<CorrelationSaga, CorrelationMessage>.ViciOneServiceBusEventCorrelationConfigurator(
                null!, null!, null));
        AssertArgument("event", () =>
            new StateMachineInterfaceType<CorrelationSaga, CorrelationMessage>.ViciOneServiceBusEventCorrelationConfigurator(
                machine, null!, null));

        AssertArgument("machine", () => new MessageEventCorrelation<CorrelationSaga, CorrelationMessage>(
            null!, null!, null, null, null, null!, false, false, true));
        AssertArgument("event", () => new MessageEventCorrelation<CorrelationSaga, CorrelationMessage>(
            machine, null!, null, null, null, null!, false, false, true));
        AssertArgument("sagaFactory", () => new MessageEventCorrelation<CorrelationSaga, CorrelationMessage>(
            machine, @event, null, null, null, null!, false, false, true));

        var direct = new MessageEventCorrelation<CorrelationSaga, CorrelationMessage>(
            machine, @event, null, null, null, sagaFactory, false, false, true);
        Assert.Same(machine, ReadField<object>(direct, "_machine"));
        Assert.Same(@event, direct.Event);
        Assert.Same(sagaFactory, ReadField<object>(direct, "_sagaFactory"));

        var inheritedMessageFilter = new RecordingFilter<ConsumeContext<CorrelationMessage>>();
        SagaFilterFactory<CorrelationSaga, CorrelationMessage> inheritedFactory = (_, _, _) => inheritedMessageFilter;
        IEvent<CorrelationMessage> existingEvent = StrictStub<IEvent<CorrelationMessage>>();
        var existing = new ExistingCorrelation(existingEvent, inheritedFactory, inheritedMessageFilter);

        MessageEventCorrelation<CorrelationSaga, CorrelationMessage> inherited = Build(Create(machine, @event, existing));
        Assert.Same(machine, ReadField<ISagaStateMachine<CorrelationSaga>>(inherited, "_machine"));
        Assert.Same(@event, inherited.Event);
        Assert.NotSame(existingEvent, inherited.Event);
        Assert.Same(inheritedFactory, inherited.FilterFactory);
        Assert.Same(inheritedMessageFilter, inherited.MessageFilter);
        Assert.True(inherited.ConfigureConsumeTopology);

        var factoryOnlyExisting = new ExistingCorrelation(existingEvent, inheritedFactory, null);
        MessageEventCorrelation<CorrelationSaga, CorrelationMessage> factoryOnly = Build(Create(machine, @event, factoryOnlyExisting));
        Assert.Same(@event, factoryOnly.Event);
        Assert.Same(inheritedFactory, factoryOnly.FilterFactory);
        Assert.Null(factoryOnly.MessageFilter);

        MessageEventCorrelation<CorrelationSaga, CorrelationMessage> defaults = Build(Create(machine, @event));
        Assert.Null(defaults.FilterFactory);
        Assert.Null(defaults.MessageFilter);
        Assert.True(defaults.ConfigureConsumeTopology);
        Assert.False(ReadField<bool>(defaults, "_insertOnInitial"));
        Assert.False(ReadField<bool>(defaults, "_readOnly"));
        Assert.Null(ReadNullableField(defaults, "_missingPipe"));
        Assert.IsType<DefaultSagaFactory<CorrelationSaga, CorrelationMessage>>(ReadField<object>(defaults, "_sagaFactory"));

        IEventCorrelation unrelated = StrictStub<IEventCorrelation>();
        MessageEventCorrelation<CorrelationSaga, CorrelationMessage> ignored = Build(Create(machine, @event, unrelated));
        Assert.Null(ignored.FilterFactory);
        Assert.Null(ignored.MessageFilter);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "iteration-213-all-method-null-boundaries-atomicity")]
    public void ConfigurationMethods_RejectEveryNullInputBeforeMutation()
    {
        var configurator = Create();
        Expression<Func<CorrelationSaga, int>> valueProperty = instance => instance.BusinessCode;
        Expression<Func<CorrelationSaga, int?>> nullableProperty = instance => instance.OptionalCode;
        Expression<Func<CorrelationSaga, string>> referenceProperty = instance => instance.BusinessKey;

        AssertArgument("selector", () => configurator.CorrelateById((Func<ConsumeContext<CorrelationMessage>, Guid>)null!));
        AssertArgument("propertyExpression", () => configurator.CorrelateById<int>(null!, null!));
        AssertArgument("selector", () => configurator.CorrelateById(valueProperty, null!));
        AssertArgument("propertyExpression", () => configurator.CorrelateBy<int>(
            (Expression<Func<CorrelationSaga, int?>>)null!,
            (Func<ConsumeContext<CorrelationMessage>, int?>)null!));
        AssertArgument("selector", () => configurator.CorrelateBy(nullableProperty, null!));
        AssertArgument("propertyExpression", () => configurator.CorrelateBy<string>(
            (Expression<Func<CorrelationSaga, string>>)null!,
            (Func<ConsumeContext<CorrelationMessage>, string>)null!));
        AssertArgument("selector", () => configurator.CorrelateBy(referenceProperty, null!));
        AssertArgument("selector", () => configurator.SelectId(null!));
        AssertArgument("correlationExpression", () => configurator.CorrelateBy(
            (Expression<Func<CorrelationSaga, ConsumeContext<CorrelationMessage>, bool>>)null!));
        AssertArgument("factoryMethod", () => configurator.SetSagaFactory(null!));
        AssertArgument("getMissingPipe", () => configurator.OnMissingInstance(null!));

        MessageEventCorrelation<CorrelationSaga, CorrelationMessage> unchanged = Build(configurator);
        Assert.Null(unchanged.FilterFactory);
        Assert.Null(unchanged.MessageFilter);
        Assert.Null(ReadNullableField(unchanged, "_missingPipe"));
        Assert.IsType<DefaultSagaFactory<CorrelationSaga, CorrelationMessage>>(ReadField<object>(unchanged, "_sagaFactory"));

        var configured = Create();
        Func<ConsumeContext<CorrelationMessage>, Guid> priorSelector = _ => Guid.Parse("bb56f97d-f159-48cb-a473-c7c8d584193e");
        configured.SelectId(priorSelector);
        IFilter<ConsumeContext<CorrelationMessage>> priorFilter = Build(configured).MessageFilter!;

        AssertArgument("selector", () => configured.SelectId(null!));

        MessageEventCorrelation<CorrelationSaga, CorrelationMessage> afterFailure = Build(configured);
        Assert.Same(priorFilter, afterFailure.MessageFilter);
        Assert.Same(priorSelector, ReadField<object>(afterFailure.MessageFilter!, "_getCorrelationId"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "iteration-213-build-fresh-snapshots-and-mutation-isolation")]
    public void Build_ReturnsFreshSnapshotsUnaffectedByLaterConfiguratorMutation()
    {
        var configurator = Create();
        MessageEventCorrelation<CorrelationSaga, CorrelationMessage> initial = Build(configurator);

        Func<ConsumeContext<CorrelationMessage>, Guid> selector = _ => Guid.Parse("67b70226-d475-46eb-a092-d176ed1a00d1");
        SagaFactoryMethod<CorrelationSaga, CorrelationMessage> factory = _ => new CorrelationSaga();
        IPipe<ConsumeContext<CorrelationMessage>> missingPipe = StrictStub<IPipe<ConsumeContext<CorrelationMessage>>>();
        configurator.InsertOnInitial = true;
        configurator.ReadOnly = false;
        configurator.ConfigureConsumeTopology = false;
        configurator.CorrelateById(selector);
        configurator.SetSagaFactory(factory);
        configurator.OnMissingInstance(_ => missingPipe);

        MessageEventCorrelation<CorrelationSaga, CorrelationMessage> configured = Build(configurator);
        MessageEventCorrelation<CorrelationSaga, CorrelationMessage> repeated = Build(configurator);

        Assert.NotSame(initial, configured);
        Assert.NotSame(configured, repeated);
        Assert.Null(initial.FilterFactory);
        Assert.Null(initial.MessageFilter);
        Assert.True(initial.ConfigureConsumeTopology);
        Assert.False(ReadField<bool>(initial, "_insertOnInitial"));
        Assert.False(ReadField<bool>(initial, "_readOnly"));
        Assert.Null(ReadNullableField(initial, "_missingPipe"));

        Assert.Same(configured.FilterFactory, repeated.FilterFactory);
        Assert.Same(configured.MessageFilter, repeated.MessageFilter);
        Assert.False(configured.ConfigureConsumeTopology);
        Assert.True(ReadField<bool>(configured, "_insertOnInitial"));
        Assert.False(ReadField<bool>(configured, "_readOnly"));
        Assert.Same(missingPipe, ReadField<object>(configured, "_missingPipe"));
        object configuredSagaFactory = ReadField<object>(configured, "_sagaFactory");
        Assert.Same(configuredSagaFactory, ReadField<object>(repeated, "_sagaFactory"));

        configurator.InsertOnInitial = false;
        configurator.ReadOnly = true;
        configurator.ConfigureConsumeTopology = true;
        configurator.SelectId(_ => Guid.Empty);
        MessageEventCorrelation<CorrelationSaga, CorrelationMessage> remapped = Build(configurator);

        Assert.True(ReadField<bool>(configured, "_insertOnInitial"));
        Assert.False(ReadField<bool>(configured, "_readOnly"));
        Assert.False(configured.ConfigureConsumeTopology);
        Assert.Same(selector, ReadField<object>(configured.MessageFilter!, "_getCorrelationId"));
        Assert.False(ReadField<bool>(remapped, "_insertOnInitial"));
        Assert.True(ReadField<bool>(remapped, "_readOnly"));
        Assert.True(remapped.ConfigureConsumeTopology);
        Assert.Same(missingPipe, ReadField<object>(remapped, "_missingPipe"));
        Assert.Same(configuredSagaFactory, ReadField<object>(remapped, "_sagaFactory"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "iteration-213-id-select-filter-factory-composition")]
    public void CorrelateByIdAndSelectId_ComposeAndReplaceOnlyTheirOwnedFilters()
    {
        var configurator = Create();
        Func<ConsumeContext<CorrelationMessage>, Guid> correlationSelector = _ =>
            Guid.Parse("14468dbe-93c2-4e9e-b6cd-04f792a7efea");
        Func<ConsumeContext<CorrelationMessage>, Guid> selectedId = _ =>
            Guid.Parse("6ed83f36-e42e-4f13-bde4-90ee4c44a002");

        Assert.Same(configurator, configurator.CorrelateById(correlationSelector));
        MessageEventCorrelation<CorrelationSaga, CorrelationMessage> correlated = Build(configurator);
        var messageFilter = Assert.IsType<CorrelationIdMessageFilter<CorrelationMessage>>(correlated.MessageFilter);
        Assert.Same(correlationSelector, ReadField<object>(messageFilter, "_getCorrelationId"));
        SagaFilterFactory<CorrelationSaga, CorrelationMessage> correlatedFactory = Assert.IsType<SagaFilterFactory<CorrelationSaga, CorrelationMessage>>(
            correlated.FilterFactory);
        FilterComposition correlatedComposition = Invoke(correlatedFactory);
        var correlatedFilter = Assert.IsType<CorrelatedSagaFilter<CorrelationSaga, CorrelationMessage>>(correlatedComposition.Filter);
        AssertFilterCollaborators(correlatedFilter, correlatedComposition);

        Assert.Same(configurator, configurator.SelectId(selectedId));
        MessageEventCorrelation<CorrelationSaga, CorrelationMessage> selected = Build(configurator);
        Assert.Same(correlatedFactory, selected.FilterFactory);
        var selectedFilter = Assert.IsType<CorrelationIdMessageFilter<CorrelationMessage>>(selected.MessageFilter);
        Assert.Same(selectedId, ReadField<object>(selectedFilter, "_getCorrelationId"));

        var selectionOnly = Create();
        selectionOnly.SelectId(selectedId);
        MessageEventCorrelation<CorrelationSaga, CorrelationMessage> selectedWithoutDispatch = Build(selectionOnly);
        Assert.Null(selectedWithoutDispatch.FilterFactory);
        Assert.Same(selectedId, ReadField<object>(selectedWithoutDispatch.MessageFilter!, "_getCorrelationId"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "iteration-213-property-overloads-query-factory-composition")]
    public void PropertyCorrelationOverloads_ComposeExactQueryFactoriesExpressionsSelectorsAndCollaborators()
    {
        Expression<Func<CorrelationSaga, int>> valueProperty = instance => instance.BusinessCode;
        Func<ConsumeContext<CorrelationMessage>, int> valueSelector = _ => 27;
        var valueConfigurator = Create();
        Assert.Same(valueConfigurator, valueConfigurator.CorrelateById(valueProperty, valueSelector));
        AssertPropertyQuery<int, NotDefaultValueTypeSagaQueryPropertySelector<CorrelationMessage, int>>(
            Build(valueConfigurator), valueProperty, valueSelector);

        Expression<Func<CorrelationSaga, int?>> nullableProperty = instance => instance.OptionalCode;
        Func<ConsumeContext<CorrelationMessage>, int?> nullableSelector = _ => 31;
        var nullableConfigurator = Create();
        Assert.Same(nullableConfigurator, nullableConfigurator.CorrelateBy(nullableProperty, nullableSelector));
        AssertPropertyQuery<int?, HasValueTypeSagaQueryPropertySelector<CorrelationMessage, int>>(
            Build(nullableConfigurator), nullableProperty, nullableSelector);

        Expression<Func<CorrelationSaga, string>> referenceProperty = instance => instance.BusinessKey;
        Func<ConsumeContext<CorrelationMessage>, string> referenceSelector = _ => "business-key";
        var inheritedMessageFilter = new RecordingFilter<ConsumeContext<CorrelationMessage>>();
        var existing = new ExistingCorrelation(
            StrictStub<IEvent<CorrelationMessage>>(),
            null,
            inheritedMessageFilter);
        var referenceConfigurator = Create(existingCorrelation: existing);
        Assert.Same(referenceConfigurator, referenceConfigurator.CorrelateBy(referenceProperty, referenceSelector));
        MessageEventCorrelation<CorrelationSaga, CorrelationMessage> referenceCorrelation = Build(referenceConfigurator);
        Assert.Same(inheritedMessageFilter, referenceCorrelation.MessageFilter);
        AssertPropertyQuery<string, SagaQueryPropertySelector<CorrelationMessage, string>>(
            referenceCorrelation, referenceProperty, referenceSelector);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "iteration-213-expression-query-factory-composition")]
    public void ExpressionCorrelation_ComposesExactQueryFactoryAndFilterCollaborators()
    {
        var configurator = Create();
        Expression<Func<CorrelationSaga, ConsumeContext<CorrelationMessage>, bool>> expression =
            (instance, context) => instance.BusinessKey == context.Message.BusinessKey;

        Assert.Same(configurator, configurator.CorrelateBy(expression));
        MessageEventCorrelation<CorrelationSaga, CorrelationMessage> correlation = Build(configurator);
        SagaFilterFactory<CorrelationSaga, CorrelationMessage> factory = Assert.IsType<SagaFilterFactory<CorrelationSaga, CorrelationMessage>>(
            correlation.FilterFactory);
        FilterComposition composition = Invoke(factory);
        var filter = Assert.IsType<QuerySagaFilter<CorrelationSaga, CorrelationMessage>>(composition.Filter);
        AssertFilterCollaborators(filter, composition);
        var queryFactory = Assert.IsType<ExpressionCorrelationSagaQueryFactory<CorrelationSaga, CorrelationMessage>>(
            ReadField<object>(filter, "_queryFactory"));
        Assert.Same(expression, ReadField<object>(queryFactory, "_correlationExpression"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "iteration-213-saga-factory-missing-pipe-outcomes-and-atomicity")]
    public void SagaFactoryAndMissingInstance_PreserveIdentityAndRejectNullResultsWithoutLosingPriorState()
    {
        var configurator = Create();
        SagaFactoryMethod<CorrelationSaga, CorrelationMessage> firstFactory = _ => new CorrelationSaga();
        SagaFactoryMethod<CorrelationSaga, CorrelationMessage> secondFactory = _ => new CorrelationSaga();
        IPipe<ConsumeContext<CorrelationMessage>> firstMissingPipe = StrictStub<IPipe<ConsumeContext<CorrelationMessage>>>();
        IPipe<ConsumeContext<CorrelationMessage>> secondMissingPipe = StrictStub<IPipe<ConsumeContext<CorrelationMessage>>>();
        IMissingInstanceConfigurator<CorrelationSaga, CorrelationMessage>? callbackArgument = null;
        var callbackCalls = 0;

        Assert.Same(configurator, configurator.SetSagaFactory(firstFactory));
        Assert.Same(configurator, configurator.OnMissingInstance(missing =>
        {
            callbackCalls++;
            callbackArgument = missing;
            return firstMissingPipe;
        }));
        MessageEventCorrelation<CorrelationSaga, CorrelationMessage> first = Build(configurator);

        Assert.Equal(1, callbackCalls);
        Assert.IsType<EventMissingInstanceConfigurator<CorrelationSaga, CorrelationMessage>>(callbackArgument);
        Assert.Same(firstMissingPipe, ReadField<object>(first, "_missingPipe"));
        var firstFactoryAdapter = Assert.IsType<FactoryMethodSagaFactory<CorrelationSaga, CorrelationMessage>>(
            ReadField<object>(first, "_sagaFactory"));
        Assert.Same(firstFactory, ReadField<object>(firstFactoryAdapter, "_factoryMethod"));

        InvalidOperationException nullResult = Assert.Throws<InvalidOperationException>(() =>
            configurator.OnMissingInstance(_ => null!));
        Assert.Equal("The missing-instance configuration callback returned no pipe.", nullResult.Message);
        var callbackFailure = new InvalidOperationException("missing callback failed");
        Assert.Same(callbackFailure, Assert.Throws<InvalidOperationException>(() =>
            configurator.OnMissingInstance(_ => throw callbackFailure)));
        MessageEventCorrelation<CorrelationSaga, CorrelationMessage> afterFailures = Build(configurator);
        Assert.Same(firstMissingPipe, ReadField<object>(afterFailures, "_missingPipe"));

        configurator.SetSagaFactory(secondFactory);
        configurator.OnMissingInstance(_ => secondMissingPipe);
        MessageEventCorrelation<CorrelationSaga, CorrelationMessage> second = Build(configurator);
        Assert.Same(firstMissingPipe, ReadField<object>(first, "_missingPipe"));
        Assert.Same(firstFactory, ReadField<object>(firstFactoryAdapter, "_factoryMethod"));
        Assert.Same(secondMissingPipe, ReadField<object>(second, "_missingPipe"));
        var secondFactoryAdapter = Assert.IsType<FactoryMethodSagaFactory<CorrelationSaga, CorrelationMessage>>(
            ReadField<object>(second, "_sagaFactory"));
        Assert.Same(secondFactory, ReadField<object>(secondFactoryAdapter, "_factoryMethod"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "iteration-213-lazy-policy-exact-composition-stable-identity")]
    public void CorrelationPolicy_LazilySelectsExactStablePolicyWithEveryConfiguredCollaborator()
    {
        var machine = new ValidationMachine();
        var sagaFactory = new DefaultSagaFactory<CorrelationSaga, CorrelationMessage>();
        IPipe<ConsumeContext<CorrelationMessage>> missingPipe = StrictStub<IPipe<ConsumeContext<CorrelationMessage>>>();

        var initial = new MessageEventCorrelation<CorrelationSaga, CorrelationMessage>(
            machine, machine.InitialEvent, null, null, missingPipe, sagaFactory, true, false, true);
        ISagaPolicy<CorrelationSaga, CorrelationMessage> initialPolicy = initial.Policy;
        Assert.Same(initialPolicy, initial.Policy);
        var newOrExisting = Assert.IsType<NewOrExistingSagaPolicy<CorrelationSaga, CorrelationMessage>>(initialPolicy);
        Assert.Same(sagaFactory, ReadField<object>(newOrExisting, "_sagaFactory"));
        Assert.True(ReadField<bool>(newOrExisting, "_insertOnInitial"));
        Assert.False(newOrExisting.IsReadOnly);

        var existing = new MessageEventCorrelation<CorrelationSaga, CorrelationMessage>(
            machine, machine.OtherEvent, null, null, missingPipe, sagaFactory, false, true, true);
        ISagaPolicy<CorrelationSaga, CorrelationMessage> existingPolicy = existing.Policy;
        Assert.Same(existingPolicy, existing.Policy);
        var anyExisting = Assert.IsType<AnyExistingSagaPolicy<CorrelationSaga, CorrelationMessage>>(existingPolicy);
        Assert.Same(missingPipe, ReadField<object>(anyExisting, "_missingPipe"));
        Assert.True(anyExisting.IsReadOnly);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "iteration-213-correlation-readonly-validation-matrix")]
    public void CorrelationValidation_RejectsOnlyTheTwoReadOnlyConflicts()
    {
        var machine = new ValidationMachine();
        var sagaFactory = new DefaultSagaFactory<CorrelationSaga, CorrelationMessage>();

        var insertedReadOnly = new MessageEventCorrelation<CorrelationSaga, CorrelationMessage>(
            machine, machine.OtherEvent, null, null, null, sagaFactory, true, true, true);
        ValidationResult insertionFailure = Assert.Single(insertedReadOnly.Validate());
        Assert.Equal(ValidationResultDisposition.Failure, insertionFailure.Disposition);
        Assert.Equal("ReadOnly", insertionFailure.Key);
        Assert.Equal("ReadOnly cannot be set when InsertOnInitial is true", insertionFailure.Message);

        var initialReadOnly = new MessageEventCorrelation<CorrelationSaga, CorrelationMessage>(
            machine, machine.InitialEvent, null, null, null, sagaFactory, false, true, true);
        ValidationResult initialFailure = Assert.Single(initialReadOnly.Validate());
        Assert.Equal(ValidationResultDisposition.Failure, initialFailure.Disposition);
        Assert.Equal("ReadOnly", initialFailure.Key);
        Assert.Equal("ReadOnly cannot be used for events in the initial state", initialFailure.Message);

        var combined = new MessageEventCorrelation<CorrelationSaga, CorrelationMessage>(
            machine, machine.InitialEvent, null, null, null, sagaFactory, true, true, true);
        ValidationResult[] combinedFailures = combined.Validate().ToArray();
        Assert.Equal(2, combinedFailures.Length);
        Assert.All(combinedFailures, failure =>
        {
            Assert.Equal(ValidationResultDisposition.Failure, failure.Disposition);
            Assert.Equal("ReadOnly", failure.Key);
        });
        Assert.Equal("ReadOnly cannot be set when InsertOnInitial is true", combinedFailures[0].Message);
        Assert.Equal("ReadOnly cannot be used for events in the initial state", combinedFailures[1].Message);

        var nonInitialReadOnly = new MessageEventCorrelation<CorrelationSaga, CorrelationMessage>(
            machine, machine.OtherEvent, null, null, null, sagaFactory, false, true, true);
        var initialWritable = new MessageEventCorrelation<CorrelationSaga, CorrelationMessage>(
            machine, machine.InitialEvent, null, null, null, sagaFactory, true, false, true);
        Assert.Empty(nonInitialReadOnly.Validate());
        Assert.Empty(initialWritable.Validate());

        var strictlyWritable = new MessageEventCorrelation<CorrelationSaga, CorrelationMessage>(
            StrictStub<ISagaStateMachine<CorrelationSaga>>(), StrictStub<IEvent<CorrelationMessage>>(), null, null, null,
            sagaFactory, false, false, true);
        Assert.Empty(strictlyWritable.Validate());
    }

    private static StateMachineInterfaceType<CorrelationSaga, CorrelationMessage>.ViciOneServiceBusEventCorrelationConfigurator Create(
        ISagaStateMachine<CorrelationSaga>? machine = null,
        IEvent<CorrelationMessage>? @event = null,
        IEventCorrelation? existingCorrelation = null) =>
        new(machine ?? StrictStub<ISagaStateMachine<CorrelationSaga>>(),
            @event ?? StrictStub<IEvent<CorrelationMessage>>(),
            existingCorrelation);

    private static MessageEventCorrelation<CorrelationSaga, CorrelationMessage> Build(
        StateMachineInterfaceType<CorrelationSaga, CorrelationMessage>.ViciOneServiceBusEventCorrelationConfigurator configurator) =>
        Assert.IsType<MessageEventCorrelation<CorrelationSaga, CorrelationMessage>>(configurator.Build());

    private static FilterComposition Invoke(SagaFilterFactory<CorrelationSaga, CorrelationMessage> factory)
    {
        ISagaRepository<CorrelationSaga> repository = StrictStub<ISagaRepository<CorrelationSaga>>();
        ISagaPolicy<CorrelationSaga, CorrelationMessage> policy = StrictStub<ISagaPolicy<CorrelationSaga, CorrelationMessage>>();
        IPipe<SagaConsumeContext<CorrelationSaga, CorrelationMessage>> sagaPipe =
            StrictStub<IPipe<SagaConsumeContext<CorrelationSaga, CorrelationMessage>>>();

        return new FilterComposition(factory(repository, policy, sagaPipe), repository, policy, sagaPipe);
    }

    private static void AssertPropertyQuery<TProperty, TSelector>(
        MessageEventCorrelation<CorrelationSaga, CorrelationMessage> correlation,
        Expression<Func<CorrelationSaga, TProperty>> propertyExpression,
        Delegate selector)
        where TSelector : class
    {
        SagaFilterFactory<CorrelationSaga, CorrelationMessage> factory = Assert.IsType<SagaFilterFactory<CorrelationSaga, CorrelationMessage>>(
            correlation.FilterFactory);
        FilterComposition composition = Invoke(factory);
        var filter = Assert.IsType<QuerySagaFilter<CorrelationSaga, CorrelationMessage>>(composition.Filter);
        AssertFilterCollaborators(filter, composition);
        var queryFactory = Assert.IsType<PropertyExpressionSagaQueryFactory<CorrelationSaga, CorrelationMessage, TProperty>>(
            ReadField<object>(filter, "_queryFactory"));
        Assert.Same(propertyExpression, ReadField<object>(queryFactory, "_propertyExpression"));
        var propertySelector = Assert.IsType<TSelector>(ReadField<object>(queryFactory, "_selector"));
        Assert.Same(selector, ReadField<object>(propertySelector, "_selector"));
    }

    private static void AssertFilterCollaborators(object filter, FilterComposition composition)
    {
        Assert.Same(composition.Repository, ReadField<object>(filter, "_sagaRepository"));
        Assert.Same(composition.Policy, ReadField<object>(filter, "_policy"));
        Assert.Same(composition.SagaPipe, ReadField<object>(filter, "_messagePipe"));
    }

    private static void AssertProperty(Type type, string name)
    {
        PropertyInfo property = Assert.Single(type.GetProperties(DeclaredPublicMembers), candidate => candidate.Name == name);
        Assert.Equal(typeof(bool), property.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.NotNull(property.SetMethod);
        Assert.True(property.GetMethod.IsPublic);
        Assert.True(property.SetMethod.IsPublic);
    }

    private static bool HasGenericConstraint(MethodInfo method, GenericParameterAttributes constraint) =>
        (Assert.Single(method.GetGenericArguments()).GenericParameterAttributes & constraint) == constraint;

    private static void AssertDirectInterfaces(Type type, params Type[] expected)
    {
        Type[] all = type.GetInterfaces();
        Type[] direct = all.Where(candidate => !all.Any(
            other => other != candidate && other.GetInterfaces().Contains(candidate))).ToArray();
        Assert.Equal(
            expected.OrderBy(TypeIdentity, StringComparer.Ordinal),
            direct.OrderBy(TypeIdentity, StringComparer.Ordinal));
    }

    private static string TypeIdentity(Type type) => type.AssemblyQualifiedName ?? type.FullName ?? type.Name;

    private static void AssertArgument(string parameterName, Action action) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);

    private static object? ReadNullableField(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{fieldName}' was not found on {target.GetType()}.");
        return field.GetValue(target);
    }

    private static T ReadField<T>(object target, string fieldName)
    {
        object? value = ReadNullableField(target, fieldName);
        return Assert.IsAssignableFrom<T>(value);
    }

    private static T StrictStub<T>()
        where T : class => DispatchProxy.Create<T, StrictDispatchProxy>();

    private sealed record FilterComposition(
        IFilter<ConsumeContext<CorrelationMessage>> Filter,
        ISagaRepository<CorrelationSaga> Repository,
        ISagaPolicy<CorrelationSaga, CorrelationMessage> Policy,
        IPipe<SagaConsumeContext<CorrelationSaga, CorrelationMessage>> SagaPipe);

    private sealed class ExistingCorrelation(
        IEvent<CorrelationMessage> @event,
        SagaFilterFactory<CorrelationSaga, CorrelationMessage>? filterFactory,
        IFilter<ConsumeContext<CorrelationMessage>>? messageFilter) : IEventCorrelation<CorrelationSaga, CorrelationMessage>
    {
        public Type DataType => typeof(CorrelationMessage);

        public bool ConfigureConsumeTopology => false;

        public IEvent<CorrelationMessage> Event => @event;

        public ISagaPolicy<CorrelationSaga, CorrelationMessage>? Policy => null;

        public SagaFilterFactory<CorrelationSaga, CorrelationMessage>? FilterFactory => filterFactory;

        public IFilter<ConsumeContext<CorrelationMessage>>? MessageFilter => messageFilter;

        public IEnumerable<ValidationResult> Validate() => [];
    }

    private sealed class RecordingFilter<TContext> : IFilter<TContext>
        where TContext : class, PipeContext
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(TContext context, IPipe<TContext> next) => next.SendAsync(context);
    }

    private class StrictDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected collaborator call: {targetMethod?.Name}.");
    }

    private sealed class CorrelationSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public int BusinessCode { get; set; }

        public int? OptionalCode { get; set; }

        public string BusinessKey { get; set; } = string.Empty;
    }

    private sealed class ValidationMachine : ViciOneServiceBusStateMachine<CorrelationSaga>
    {
        public ValidationMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => InitialEvent);
            Event(() => OtherEvent);
            Initially(When(InitialEvent).TransitionTo(Running));
        }

        public IState Running { get; } = null!;

        public IEvent<CorrelationMessage> InitialEvent { get; } = null!;

        public IEvent<CorrelationMessage> OtherEvent { get; } = null!;
    }

    private sealed record CorrelationMessage(string BusinessKey = "message-key");
}
