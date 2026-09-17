using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineRuntimeSurfacePublicContractTests
{
    private const BindingFlags DeclaredPublicInstance = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "public-event-correlation-contract-shape")]
    public void EventCorrelationContracts_HaveExactInheritanceConstraintsAndMembers()
    {
        Type correlationType = typeof(IEventCorrelation);
        AssertInterface(correlationType, typeof(ISpecification));
        AssertDeclaredSurface(correlationType, propertyCount: 2, methodCount: 0);
        AssertReadOnlyProperty(correlationType, nameof(IEventCorrelation.DataType), typeof(Type));
        AssertReadOnlyProperty(correlationType, nameof(IEventCorrelation.ConfigureConsumeTopology), typeof(bool));

        Type typedCorrelationType = typeof(IEventCorrelation<,>);
        Type[] arguments = typedCorrelationType.GetGenericArguments();
        Type instance = arguments[0];
        Type data = arguments[1];
        AssertInterface(typedCorrelationType, typeof(IEventCorrelation));
        AssertGenericParameter(
            instance,
            "TInstance",
            GenericParameterAttributes.ReferenceTypeConstraint,
            typeof(ISagaStateMachineInstance));
        AssertGenericParameter(data, "TData", GenericParameterAttributes.ReferenceTypeConstraint);
        AssertDeclaredSurface(typedCorrelationType, propertyCount: 4, methodCount: 0);
        AssertReadOnlyProperty(typedCorrelationType, nameof(IEventCorrelation<ISagaStateMachineInstance, object>.Event),
            typeof(IEvent<>).MakeGenericType(data));
        AssertReadOnlyProperty(typedCorrelationType, nameof(IEventCorrelation<ISagaStateMachineInstance, object>.Policy),
            typeof(ISagaPolicy<,>).MakeGenericType(instance, data));
        AssertReadOnlyProperty(typedCorrelationType, nameof(IEventCorrelation<ISagaStateMachineInstance, object>.FilterFactory),
            typeof(SagaFilterFactory<,>).MakeGenericType(instance, data));
        AssertReadOnlyProperty(typedCorrelationType, nameof(IEventCorrelation<ISagaStateMachineInstance, object>.MessageFilter),
            typeof(IFilter<>).MakeGenericType(typeof(ConsumeContext<>).MakeGenericType(data)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "public-event-observer-contract-shape")]
    public void EventObserverContract_HasExactCallbacksReturnsAndGenericConstraints()
    {
        Type observerType = typeof(IEventObserver<>);
        Type saga = observerType.GetGenericArguments()[0];
        Type behaviorContext = typeof(IBehaviorContext<>).MakeGenericType(saga);
        AssertInterface(observerType);
        AssertSagaParameter(saga);
        AssertDeclaredSurface(observerType, propertyCount: 0, methodCount: 6);

        AssertMethod(observerType, nameof(IEventObserver<ISagaStateMachineInstance>.PreExecuteAsync), typeof(Task), 0, behaviorContext);
        AssertTypedObserverMethod(observerType, nameof(IEventObserver<ISagaStateMachineInstance>.PreExecuteAsync), saga, hasException: false);
        AssertMethod(observerType, nameof(IEventObserver<ISagaStateMachineInstance>.PostExecuteAsync), typeof(Task), 0, behaviorContext);
        AssertTypedObserverMethod(observerType, nameof(IEventObserver<ISagaStateMachineInstance>.PostExecuteAsync), saga, hasException: false);
        AssertMethod(
            observerType,
            nameof(IEventObserver<ISagaStateMachineInstance>.ExecuteFaultAsync),
            typeof(Task),
            0,
            behaviorContext,
            typeof(Exception));
        AssertTypedObserverMethod(observerType, nameof(IEventObserver<ISagaStateMachineInstance>.ExecuteFaultAsync), saga, hasException: true);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "public-state-accessor-contract-shape")]
    public void StateAccessorContract_HasExactInheritanceMembersAndCancellationDefaults()
    {
        Type accessorType = typeof(IStateAccessor<>);
        Type saga = accessorType.GetGenericArguments()[0];
        Type behaviorContext = typeof(IBehaviorContext<>).MakeGenericType(saga);
        AssertInterface(accessorType, typeof(IProbeSite));
        AssertSagaParameter(saga);
        AssertDeclaredSurface(accessorType, propertyCount: 0, methodCount: 3);

        MethodInfo get = AssertMethod(
            accessorType,
            nameof(IStateAccessor<ISagaStateMachineInstance>.GetAsync),
            typeof(Task<>).MakeGenericType(typeof(IState<>).MakeGenericType(saga)),
            0,
            behaviorContext,
            typeof(CancellationToken));
        AssertCancellationTokenDefault(get);

        MethodInfo set = AssertMethod(
            accessorType,
            nameof(IStateAccessor<ISagaStateMachineInstance>.SetAsync),
            typeof(Task),
            0,
            behaviorContext,
            typeof(IState<>).MakeGenericType(saga),
            typeof(CancellationToken));
        AssertCancellationTokenDefault(set);

        MethodInfo expression = AssertMethod(
            accessorType,
            nameof(IStateAccessor<ISagaStateMachineInstance>.GetStateExpression),
            ExpressionOf(typeof(Func<,>).MakeGenericType(saga, typeof(bool))),
            0,
            typeof(IState[]));
        AssertParamArray(expression, 0, typeof(IState));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "public-state-machine-modifier-contract-shape")]
    public void StateMachineModifierContract_HasEveryExactOverloadCallbackAndReturnType()
    {
        Type modifierType = typeof(IStateMachineModifier<>);
        Type saga = modifierType.GetGenericArguments()[0];
        Type typedState = typeof(IState<>).MakeGenericType(saga);
        Type untypedBinder = typeof(IEventActivityBinder<>).MakeGenericType(saga);
        Type untypedCallback = typeof(Func<,>).MakeGenericType(untypedBinder, untypedBinder);
        Type typedBinder = typeof(IEventActivityBinder<,>).MakeGenericType(saga, typeof(IState));
        Type typedCallback = typeof(Func<,>).MakeGenericType(typedBinder, typedBinder);

        AssertInterface(modifierType);
        AssertSagaParameter(saga);
        AssertDeclaredSurface(modifierType, propertyCount: 2, methodCount: 31);
        AssertReadOnlyProperty(modifierType, nameof(IStateMachineModifier<ISagaStateMachineInstance>.Initial), typeof(IState));
        AssertReadOnlyProperty(modifierType, nameof(IStateMachineModifier<ISagaStateMachineInstance>.Final), typeof(IState));

        AssertMethod(modifierType, "InstanceState", modifierType, 0,
            ExpressionOf(typeof(Func<,>).MakeGenericType(saga, typeof(IState))));
        AssertMethod(modifierType, "InstanceState", modifierType, 0,
            ExpressionOf(typeof(Func<,>).MakeGenericType(saga, typeof(string))));
        MethodInfo indexedInstanceState = AssertMethod(modifierType, "InstanceState", modifierType, 0,
            ExpressionOf(typeof(Func<,>).MakeGenericType(saga, typeof(int))), typeof(IState[]));
        AssertParamArray(indexedInstanceState, 1, typeof(IState));
        AssertMethod(modifierType, nameof(IStateMachineModifier<ISagaStateMachineInstance>.Name), modifierType, 0, typeof(string));

        MethodInfo untypedEvent = AssertMethod(modifierType, "Event", modifierType, 0, typeof(string), typeof(IEvent).MakeByRefType());
        AssertOutParameter(untypedEvent, 1, typeof(IEvent));

        MethodInfo typedEvent = AssertGenericMethod(modifierType, "Event", genericArity: 1, parameterCount: 2);
        Type eventData = typedEvent.GetGenericArguments()[0];
        AssertGenericParameter(eventData, "T", GenericParameterAttributes.ReferenceTypeConstraint);
        AssertSignature(typedEvent, modifierType, typeof(string), typeof(IEvent<>).MakeGenericType(eventData).MakeByRefType());
        AssertOutParameter(typedEvent, 1, typeof(IEvent<>).MakeGenericType(eventData));

        MethodInfo configuredEvent = AssertGenericMethod(modifierType, "Event", genericArity: 1, parameterCount: 3);
        Type configuredData = configuredEvent.GetGenericArguments()[0];
        AssertGenericParameter(configuredData, "T", GenericParameterAttributes.ReferenceTypeConstraint);
        AssertSignature(
            configuredEvent,
            modifierType,
            typeof(string),
            typeof(Action<>).MakeGenericType(typeof(IEventCorrelationConfigurator<,>).MakeGenericType(saga, configuredData)),
            typeof(IEvent<>).MakeGenericType(configuredData).MakeByRefType());
        AssertOutParameter(configuredEvent, 2, typeof(IEvent<>).MakeGenericType(configuredData));

        MethodInfo propertyEvent = AssertGenericMethod(modifierType, "Event", genericArity: 2, parameterCount: 2);
        Type[] propertyEventArguments = propertyEvent.GetGenericArguments();
        Type eventProperty = propertyEventArguments[0];
        Type propertyData = propertyEventArguments[1];
        AssertGenericParameter(eventProperty, "TProperty", GenericParameterAttributes.ReferenceTypeConstraint);
        AssertGenericParameter(propertyData, "T", GenericParameterAttributes.ReferenceTypeConstraint);
        AssertSignature(
            propertyEvent,
            modifierType,
            ExpressionOf(typeof(Func<>).MakeGenericType(eventProperty)),
            ExpressionOf(typeof(Func<,>).MakeGenericType(eventProperty, typeof(IEvent<>).MakeGenericType(propertyData))));

        Type compositeExpression = ExpressionOf(typeof(Func<,>).MakeGenericType(saga, typeof(CompositeEventStatus)));
        MethodInfo composite = AssertMethod(modifierType, "CompositeEvent", modifierType, 0,
            typeof(string), typeof(IEvent).MakeByRefType(), compositeExpression, typeof(IEvent[]));
        AssertOutParameter(composite, 1, typeof(IEvent));
        AssertParamArray(composite, 3, typeof(IEvent));
        MethodInfo compositeWithOptions = AssertMethod(modifierType, "CompositeEvent", modifierType, 0,
            typeof(string), typeof(IEvent).MakeByRefType(), compositeExpression, typeof(CompositeEventOptions), typeof(IEvent[]));
        AssertOutParameter(compositeWithOptions, 1, typeof(IEvent));
        AssertParamArray(compositeWithOptions, 4, typeof(IEvent));

        Type integerExpression = ExpressionOf(typeof(Func<,>).MakeGenericType(saga, typeof(int)));
        MethodInfo integerComposite = AssertMethod(modifierType, "CompositeEvent", modifierType, 0,
            typeof(string), typeof(IEvent).MakeByRefType(), integerExpression, typeof(IEvent[]));
        AssertOutParameter(integerComposite, 1, typeof(IEvent));
        AssertParamArray(integerComposite, 3, typeof(IEvent));
        MethodInfo integerCompositeWithOptions = AssertMethod(modifierType, "CompositeEvent", modifierType, 0,
            typeof(string), typeof(IEvent).MakeByRefType(), integerExpression, typeof(CompositeEventOptions), typeof(IEvent[]));
        AssertOutParameter(integerCompositeWithOptions, 1, typeof(IEvent));
        AssertParamArray(integerCompositeWithOptions, 4, typeof(IEvent));

        MethodInfo typedStateMethod = AssertMethod(modifierType, "State", modifierType, 0,
            typeof(string), typedState.MakeByRefType());
        AssertOutParameter(typedStateMethod, 1, typedState);
        MethodInfo untypedStateMethod = AssertMethod(modifierType, "State", modifierType, 0,
            typeof(string), typeof(IState).MakeByRefType());
        AssertOutParameter(untypedStateMethod, 1, typeof(IState));

        MethodInfo propertyState = AssertGenericMethod(modifierType, "State", genericArity: 1, parameterCount: 2);
        Type stateProperty = propertyState.GetGenericArguments()[0];
        AssertGenericParameter(stateProperty, "TProperty", GenericParameterAttributes.ReferenceTypeConstraint);
        AssertSignature(
            propertyState,
            modifierType,
            ExpressionOf(typeof(Func<>).MakeGenericType(stateProperty)),
            ExpressionOf(typeof(Func<,>).MakeGenericType(stateProperty, typeof(IState))));

        MethodInfo subState = AssertMethod(modifierType, "SubState", modifierType, 0,
            typeof(string), typeof(IState), typedState.MakeByRefType());
        AssertOutParameter(subState, 2, typedState);

        MethodInfo propertySubState = AssertGenericMethod(modifierType, "SubState", genericArity: 1, parameterCount: 3);
        Type subStateProperty = propertySubState.GetGenericArguments()[0];
        AssertGenericParameter(subStateProperty, "TProperty", GenericParameterAttributes.ReferenceTypeConstraint);
        AssertSignature(
            propertySubState,
            modifierType,
            ExpressionOf(typeof(Func<>).MakeGenericType(subStateProperty)),
            ExpressionOf(typeof(Func<,>).MakeGenericType(subStateProperty, typeof(IState))),
            typeof(IState));

        MethodInfo during = AssertMethod(modifierType, nameof(IStateMachineModifier<ISagaStateMachineInstance>.During),
            typeof(IStateMachineEventActivitiesBuilder<>).MakeGenericType(saga), 0, typeof(IState[]));
        AssertParamArray(during, 0, typeof(IState));
        AssertMethod(modifierType, nameof(IStateMachineModifier<ISagaStateMachineInstance>.Initially),
            typeof(IStateMachineEventActivitiesBuilder<>).MakeGenericType(saga), 0);
        AssertMethod(modifierType, nameof(IStateMachineModifier<ISagaStateMachineInstance>.DuringAny),
            typeof(IStateMachineEventActivitiesBuilder<>).MakeGenericType(saga), 0);

        AssertMethod(modifierType, nameof(IStateMachineModifier<ISagaStateMachineInstance>.Finally), modifierType, 0, untypedCallback);
        AssertMethod(modifierType, nameof(IStateMachineModifier<ISagaStateMachineInstance>.WhenEnter), modifierType, 0,
            typeof(IState), untypedCallback);
        AssertMethod(modifierType, nameof(IStateMachineModifier<ISagaStateMachineInstance>.WhenEnterAny), modifierType, 0, untypedCallback);
        AssertMethod(modifierType, nameof(IStateMachineModifier<ISagaStateMachineInstance>.WhenLeaveAny), modifierType, 0, untypedCallback);
        AssertMethod(modifierType, nameof(IStateMachineModifier<ISagaStateMachineInstance>.BeforeEnterAny), modifierType, 0, typedCallback);
        AssertMethod(modifierType, nameof(IStateMachineModifier<ISagaStateMachineInstance>.AfterLeaveAny), modifierType, 0, typedCallback);
        AssertMethod(modifierType, nameof(IStateMachineModifier<ISagaStateMachineInstance>.WhenLeave), modifierType, 0,
            typeof(IState), untypedCallback);
        AssertMethod(modifierType, nameof(IStateMachineModifier<ISagaStateMachineInstance>.BeforeEnter), modifierType, 0,
            typeof(IState), typedCallback);
        AssertMethod(modifierType, nameof(IStateMachineModifier<ISagaStateMachineInstance>.AfterLeave), modifierType, 0,
            typeof(IState), typedCallback);
        AssertMethod(modifierType, nameof(IStateMachineModifier<ISagaStateMachineInstance>.OnUnhandledEvent), modifierType, 0,
            typeof(UnhandledEventCallback<>).MakeGenericType(saga));
        AssertMethod(modifierType, nameof(IStateMachineModifier<ISagaStateMachineInstance>.Apply), typeof(void), 0);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "public-state-machine-visitor-contract-shape")]
    public void StateMachineVisitorContract_HasEveryExactVisitOverloadAndCallback()
    {
        Type visitorType = typeof(IStateMachineVisitor);
        AssertInterface(visitorType);
        AssertDeclaredSurface(visitorType, propertyCount: 0, methodCount: 10);

        AssertMethod(visitorType, nameof(IStateMachineVisitor.Visit), typeof(void), 0,
            typeof(IState), typeof(Action<IState>));
        AssertMethod(visitorType, nameof(IStateMachineVisitor.Visit), typeof(void), 0,
            typeof(IEvent), typeof(Action<IEvent>));

        MethodInfo eventVisit = AssertGenericMethodByFirstParameter(visitorType, "Visit", 1, 2, typeof(IEvent<>));
        Type message = eventVisit.GetGenericArguments()[0];
        AssertGenericParameter(message, "TMessage", GenericParameterAttributes.ReferenceTypeConstraint);
        Type typedEvent = typeof(IEvent<>).MakeGenericType(message);
        AssertSignature(eventVisit, typeof(void), typedEvent, typeof(Action<>).MakeGenericType(typedEvent));

        AssertMethod(visitorType, nameof(IStateMachineVisitor.Visit), typeof(void), 0, typeof(IStateMachineActivity));
        AssertMethod(visitorType, nameof(IStateMachineVisitor.Visit), typeof(void), 0,
            typeof(IStateMachineExceptionActivity), typeof(Action<IStateMachineExceptionActivity>));

        MethodInfo behaviorLeaf = AssertGenericMethodByFirstParameter(visitorType, "Visit", 1, 1, typeof(IBehavior<>));
        Type leafSaga = behaviorLeaf.GetGenericArguments()[0];
        AssertSagaParameter(leafSaga, "T");
        AssertSignature(behaviorLeaf, typeof(void), typeof(IBehavior<>).MakeGenericType(leafSaga));

        MethodInfo behaviorVisit = AssertGenericMethodByFirstParameter(visitorType, "Visit", 1, 2, typeof(IBehavior<>));
        Type visitSaga = behaviorVisit.GetGenericArguments()[0];
        AssertSagaParameter(visitSaga, "T");
        Type behavior = typeof(IBehavior<>).MakeGenericType(visitSaga);
        AssertSignature(behaviorVisit, typeof(void), behavior, typeof(Action<>).MakeGenericType(behavior));

        MethodInfo typedBehaviorLeaf = AssertGenericMethodByFirstParameter(visitorType, "Visit", 2, 1, typeof(IBehavior<,>));
        AssertTypedBehaviorVisit(typedBehaviorLeaf, hasContinuation: false);
        MethodInfo typedBehaviorVisit = AssertGenericMethodByFirstParameter(visitorType, "Visit", 2, 2, typeof(IBehavior<,>));
        AssertTypedBehaviorVisit(typedBehaviorVisit, hasContinuation: true);

        AssertMethod(visitorType, nameof(IStateMachineVisitor.Visit), typeof(void), 0,
            typeof(IStateMachineActivity), typeof(Action<IStateMachineActivity>));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "public-state-observer-contract-shape")]
    public void StateObserverContract_HasExactTransitionCallback()
    {
        Type observerType = typeof(IStateObserver<>);
        Type saga = observerType.GetGenericArguments()[0];
        AssertInterface(observerType);
        AssertSagaParameter(saga);
        AssertDeclaredSurface(observerType, propertyCount: 0, methodCount: 1);
        AssertMethod(
            observerType,
            nameof(IStateObserver<ISagaStateMachineInstance>.StateChangedAsync),
            typeof(Task),
            0,
            typeof(IBehaviorContext<>).MakeGenericType(saga),
            typeof(IState),
            typeof(IState));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "public-unhandled-event-context-contract-shape")]
    public void UnhandledEventContextContract_HasExactInheritanceMembersAndCancellationDefaults()
    {
        Type contextType = typeof(IUnhandledEventContext<>);
        Type saga = contextType.GetGenericArguments()[0];
        AssertInterface(contextType, typeof(IBehaviorContext<>).MakeGenericType(saga));
        AssertSagaParameter(saga);
        AssertDeclaredSurface(contextType, propertyCount: 1, methodCount: 2);
        AssertReadOnlyProperty(contextType, nameof(IUnhandledEventContext<ISagaStateMachineInstance>.CurrentState), typeof(IState));

        MethodInfo ignore = AssertMethod(
            contextType,
            nameof(IUnhandledEventContext<ISagaStateMachineInstance>.IgnoreAsync),
            typeof(Task),
            0,
            typeof(CancellationToken));
        AssertCancellationTokenDefault(ignore);
        MethodInfo @throw = AssertMethod(
            contextType,
            nameof(IUnhandledEventContext<ISagaStateMachineInstance>.ThrowAsync),
            typeof(Task),
            0,
            typeof(CancellationToken));
        AssertCancellationTokenDefault(@throw);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "public-visitable-contract-shape")]
    public void VisitableContract_HasExactInheritanceAndAcceptCallback()
    {
        Type visitableType = typeof(IVisitable);
        AssertInterface(visitableType, typeof(IProbeSite));
        AssertDeclaredSurface(visitableType, propertyCount: 0, methodCount: 1);
        AssertMethod(visitableType, nameof(IVisitable.Accept), typeof(void), 0, typeof(IStateMachineVisitor));
    }

    private static void AssertTypedObserverMethod(Type owner, string name, Type saga, bool hasException)
    {
        MethodInfo method = AssertGenericMethod(owner, name, genericArity: 1, parameterCount: hasException ? 2 : 1);
        Type data = method.GetGenericArguments()[0];
        AssertGenericParameter(data, "T", GenericParameterAttributes.ReferenceTypeConstraint);
        Type context = typeof(IBehaviorContext<,>).MakeGenericType(saga, data);
        if (hasException)
            AssertSignature(method, typeof(Task), context, typeof(Exception));
        else
            AssertSignature(method, typeof(Task), context);
    }

    private static void AssertTypedBehaviorVisit(MethodInfo method, bool hasContinuation)
    {
        Type[] arguments = method.GetGenericArguments();
        Type saga = arguments[0];
        Type message = arguments[1];
        AssertSagaParameter(saga, "T");
        AssertGenericParameter(message, "TMessage", GenericParameterAttributes.ReferenceTypeConstraint);
        Type behavior = typeof(IBehavior<,>).MakeGenericType(saga, message);
        if (hasContinuation)
            AssertSignature(method, typeof(void), behavior, typeof(Action<>).MakeGenericType(behavior));
        else
            AssertSignature(method, typeof(void), behavior);
    }

    private static void AssertInterface(Type type, params Type[] directInterfaces)
    {
        Assert.True(type.IsInterface);
        Assert.True(type.IsPublic);

        Type[] inheritedByOtherInterfaces = type.GetInterfaces()
            .SelectMany(static inherited => inherited.GetInterfaces())
            .Distinct()
            .ToArray();
        Type[] actual = type.GetInterfaces()
            .Except(inheritedByOtherInterfaces)
            .OrderBy(TypeIdentity, StringComparer.Ordinal)
            .ToArray();
        Type[] expected = directInterfaces.OrderBy(TypeIdentity, StringComparer.Ordinal).ToArray();

        Assert.Equal(expected, actual);
    }

    private static void AssertSagaParameter(Type parameter, string name = "TSaga") =>
        AssertGenericParameter(
            parameter,
            name,
            GenericParameterAttributes.ReferenceTypeConstraint,
            typeof(ISagaStateMachineInstance));

    private static void AssertGenericParameter(
        Type parameter,
        string name,
        GenericParameterAttributes attributes,
        params Type[] constraints)
    {
        const GenericParameterAttributes relevantAttributes =
            GenericParameterAttributes.VarianceMask | GenericParameterAttributes.SpecialConstraintMask;

        Assert.True(parameter.IsGenericParameter);
        Assert.Equal(name, parameter.Name);
        Assert.Equal(attributes, parameter.GenericParameterAttributes & relevantAttributes);
        Assert.Equal(
            constraints.OrderBy(TypeIdentity, StringComparer.Ordinal),
            parameter.GetGenericParameterConstraints().OrderBy(TypeIdentity, StringComparer.Ordinal));
    }

    private static void AssertDeclaredSurface(Type type, int propertyCount, int methodCount)
    {
        Assert.Equal(propertyCount, type.GetProperties(DeclaredPublicInstance).Length);
        Assert.Equal(methodCount, DeclaredMethods(type).Length);
        Assert.Empty(type.GetEvents(DeclaredPublicInstance));
        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
        Assert.Empty(type.GetNestedTypes(BindingFlags.Public));
    }

    private static PropertyInfo AssertReadOnlyProperty(Type owner, string name, Type propertyType)
    {
        PropertyInfo property = Assert.IsAssignableFrom<PropertyInfo>(owner.GetProperty(name, DeclaredPublicInstance));
        Assert.Equal(owner, property.DeclaringType);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.True(property.CanRead);
        Assert.True(property.GetMethod!.IsPublic);
        Assert.False(property.CanWrite);
        Assert.Null(property.SetMethod);
        return property;
    }

    private static MethodInfo AssertMethod(
        Type owner,
        string name,
        Type returnType,
        int genericArity,
        params Type[] parameterTypes)
    {
        MethodInfo method = Assert.Single(
            DeclaredMethods(owner),
            candidate => candidate.Name == name
                && candidate.GetGenericArguments().Length == genericArity
                && candidate.GetParameters().Select(static parameter => parameter.ParameterType).SequenceEqual(parameterTypes));
        AssertSignature(method, returnType, parameterTypes);
        return method;
    }

    private static MethodInfo AssertGenericMethod(Type owner, string name, int genericArity, int parameterCount)
    {
        MethodInfo method = Assert.Single(
            DeclaredMethods(owner),
            candidate => candidate.Name == name
                && candidate.IsGenericMethodDefinition
                && candidate.GetGenericArguments().Length == genericArity
                && candidate.GetParameters().Length == parameterCount);
        Assert.Equal(owner, method.DeclaringType);
        return method;
    }

    private static MethodInfo AssertGenericMethodByFirstParameter(
        Type owner,
        string name,
        int genericArity,
        int parameterCount,
        Type firstParameterGenericDefinition)
    {
        MethodInfo method = Assert.Single(
            DeclaredMethods(owner),
            candidate => candidate.Name == name
                && candidate.IsGenericMethodDefinition
                && candidate.GetGenericArguments().Length == genericArity
                && candidate.GetParameters().Length == parameterCount
                && candidate.GetParameters()[0].ParameterType.IsGenericType
                && candidate.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == firstParameterGenericDefinition);
        Assert.Equal(owner, method.DeclaringType);
        return method;
    }

    private static void AssertSignature(MethodInfo method, Type returnType, params Type[] parameterTypes)
    {
        Assert.Equal(returnType, method.ReturnType);
        Assert.Equal(parameterTypes, method.GetParameters().Select(static parameter => parameter.ParameterType));
    }

    private static void AssertCancellationTokenDefault(MethodInfo method)
    {
        ParameterInfo cancellationToken = Assert.Single(
            method.GetParameters(),
            static parameter => parameter.ParameterType == typeof(CancellationToken));
        Assert.Equal(method.GetParameters().Length - 1, cancellationToken.Position);
        Assert.True(cancellationToken.IsOptional);
        Assert.True(cancellationToken.HasDefaultValue);
        Assert.Null(cancellationToken.DefaultValue);
    }

    private static void AssertOutParameter(MethodInfo method, int index, Type elementType)
    {
        ParameterInfo parameter = method.GetParameters()[index];
        Assert.Equal(elementType.MakeByRefType(), parameter.ParameterType);
        Assert.True(parameter.IsOut);
        Assert.False(parameter.IsIn);
    }

    private static void AssertParamArray(MethodInfo method, int index, Type elementType)
    {
        ParameterInfo parameter = method.GetParameters()[index];
        Assert.Equal(elementType.MakeArrayType(), parameter.ParameterType);
        Assert.NotNull(parameter.GetCustomAttribute<ParamArrayAttribute>());
    }

    private static MethodInfo[] DeclaredMethods(Type type) =>
        type.GetMethods(DeclaredPublicInstance).Where(static method => !method.IsSpecialName).ToArray();

    private static Type ExpressionOf(Type delegateType) => typeof(Expression<>).MakeGenericType(delegateType);

    private static string TypeIdentity(Type type) => type.ToString();
}
