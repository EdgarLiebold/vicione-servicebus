using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class SagaPublicContractShapeTests
{
    private const BindingFlags DeclaredPublicInstance = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "public-event-contract-shape")]
    public void EventContracts_HaveTheExactInheritanceVarianceConstraintsAndMembers()
    {
        Type eventType = typeof(IEvent);
        AssertInterface(eventType, typeof(IVisitable), typeof(IComparable<IEvent>));
        AssertDeclaredProperties(eventType, 1);
        AssertProperty(eventType, nameof(IEvent.Name), typeof(string), canWrite: false);
        AssertDeclaredMethods(eventType, 0);

        Type messageEventType = typeof(IEvent<>);
        AssertInterface(messageEventType, typeof(IEvent));
        AssertGenericParameter(
            messageEventType.GetGenericArguments()[0],
            "TMessage",
            GenericParameterAttributes.Covariant | GenericParameterAttributes.ReferenceTypeConstraint);
        AssertDeclaredProperties(messageEventType, 0);
        AssertDeclaredMethods(messageEventType, 0);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "public-request-and-settings-contract-shape")]
    public void RequestContracts_HaveTheExactHierarchyConstraintsMembersAndHiddenSettings()
    {
        Type requestType = typeof(IRequest<,,>);
        Type[] requestArguments = requestType.GetGenericArguments();
        Type saga = requestArguments[0];
        Type request = requestArguments[1];
        Type response = requestArguments[2];

        AssertInterface(requestType);
        AssertSagaParameter(saga);
        AssertReferenceTypeParameter(request, "TRequest");
        AssertReferenceTypeParameter(response, "TResponse");
        AssertDeclaredProperties(requestType, 6);
        AssertProperty(requestType, "Name", typeof(string), canWrite: false);
        PropertyInfo baseSettings = AssertProperty(
            requestType,
            "Settings",
            typeof(IRequestSettings<,,>).MakeGenericType(saga, request, response),
            canWrite: false);
        AssertProperty(
            requestType,
            "Completed",
            typeof(IEvent<>).MakeGenericType(response),
            canWrite: true);
        AssertProperty(
            requestType,
            "Faulted",
            typeof(IEvent<>).MakeGenericType(typeof(Fault<>).MakeGenericType(request)),
            canWrite: true);
        AssertProperty(
            requestType,
            "TimeoutExpired",
            typeof(IEvent<>).MakeGenericType(typeof(IRequestTimeoutExpired<>).MakeGenericType(request)),
            canWrite: true);
        AssertProperty(requestType, "Pending", typeof(IState), canWrite: true);
        AssertDeclaredMethods(requestType, 4);
        AssertMethod(requestType, "SetRequestId", typeof(void), 0, saga, typeof(Guid?));
        AssertMethod(requestType, "GetRequestId", typeof(Guid?), 0, saga);
        AssertMethod(requestType, "GenerateRequestId", typeof(Guid), 0, saga);
        AssertMethod(requestType, "SetSendContextHeaders", typeof(void), 0, typeof(SendContext<>).MakeGenericType(request));

        Type twoResponseRequestType = typeof(IRequest<,,,>);
        Type[] twoResponseArguments = twoResponseRequestType.GetGenericArguments();
        AssertInterface(
            twoResponseRequestType,
            typeof(IRequest<,,>).MakeGenericType(twoResponseArguments[0], twoResponseArguments[1], twoResponseArguments[2]));
        AssertSagaParameter(twoResponseArguments[0]);
        AssertReferenceTypeParameter(twoResponseArguments[1], "TRequest");
        AssertReferenceTypeParameter(twoResponseArguments[2], "TResponse");
        AssertReferenceTypeParameter(twoResponseArguments[3], "TResponse2");
        AssertDeclaredProperties(twoResponseRequestType, 2);
        PropertyInfo twoResponseSettings = AssertProperty(
            twoResponseRequestType,
            "Settings",
            typeof(IRequestSettings<,,,>).MakeGenericType(twoResponseArguments),
            canWrite: false);
        AssertProperty(
            twoResponseRequestType,
            "Completed2",
            typeof(IEvent<>).MakeGenericType(twoResponseArguments[3]),
            canWrite: true);
        AssertDeclaredMethods(twoResponseRequestType, 0);
        Assert.NotEqual(baseSettings.PropertyType, twoResponseSettings.PropertyType);

        Type threeResponseRequestType = typeof(IRequest<,,,,>);
        Type[] threeResponseArguments = threeResponseRequestType.GetGenericArguments();
        AssertInterface(
            threeResponseRequestType,
            typeof(IRequest<,,,>).MakeGenericType(
                threeResponseArguments[0],
                threeResponseArguments[1],
                threeResponseArguments[2],
                threeResponseArguments[3]));
        AssertSagaParameter(threeResponseArguments[0]);
        AssertReferenceTypeParameter(threeResponseArguments[1], "TRequest");
        AssertReferenceTypeParameter(threeResponseArguments[2], "TResponse");
        AssertReferenceTypeParameter(threeResponseArguments[3], "TResponse2");
        AssertReferenceTypeParameter(threeResponseArguments[4], "TResponse3");
        AssertDeclaredProperties(threeResponseRequestType, 2);
        PropertyInfo threeResponseSettings = AssertProperty(
            threeResponseRequestType,
            "Settings",
            typeof(IRequestSettings<,,,,>).MakeGenericType(threeResponseArguments),
            canWrite: false);
        AssertProperty(
            threeResponseRequestType,
            "Completed3",
            typeof(IEvent<>).MakeGenericType(threeResponseArguments[4]),
            canWrite: true);
        AssertDeclaredMethods(threeResponseRequestType, 0);
        Assert.NotEqual(twoResponseSettings.PropertyType, threeResponseSettings.PropertyType);

        Type settingsType = typeof(IRequestSettings<,,>);
        Type[] settingsArguments = settingsType.GetGenericArguments();
        Type settingsSaga = settingsArguments[0];
        Type settingsRequest = settingsArguments[1];
        Type settingsResponse = settingsArguments[2];
        AssertInterface(settingsType);
        AssertSagaParameter(settingsSaga);
        AssertReferenceTypeParameter(settingsRequest, "TRequest");
        AssertReferenceTypeParameter(settingsResponse, "TResponse");
        AssertDeclaredProperties(settingsType, 7);
        AssertProperty(settingsType, "ServiceAddress", typeof(Uri), canWrite: false);
        AssertProperty(settingsType, "Timeout", typeof(TimeSpan), canWrite: false);
        AssertProperty(settingsType, "ClearRequestIdOnFaulted", typeof(bool), canWrite: false);
        AssertProperty(settingsType, "TimeToLive", typeof(TimeSpan?), canWrite: false);
        AssertProperty(
            settingsType,
            "Completed",
            typeof(Action<>).MakeGenericType(typeof(IEventCorrelationConfigurator<,>).MakeGenericType(settingsSaga, settingsResponse)),
            canWrite: false);
        AssertProperty(
            settingsType,
            "Faulted",
            typeof(Action<>).MakeGenericType(
                typeof(IEventCorrelationConfigurator<,>).MakeGenericType(
                    settingsSaga,
                    typeof(Fault<>).MakeGenericType(settingsRequest))),
            canWrite: false);
        AssertProperty(
            settingsType,
            "TimeoutExpired",
            typeof(Action<>).MakeGenericType(
                typeof(IEventCorrelationConfigurator<,>).MakeGenericType(
                    settingsSaga,
                    typeof(IRequestTimeoutExpired<>).MakeGenericType(settingsRequest))),
            canWrite: false);
        AssertDeclaredMethods(settingsType, 0);

        Type twoResponseSettingsType = typeof(IRequestSettings<,,,>);
        Type[] twoResponseSettingsArguments = twoResponseSettingsType.GetGenericArguments();
        AssertInterface(
            twoResponseSettingsType,
            typeof(IRequestSettings<,,>).MakeGenericType(
                twoResponseSettingsArguments[0],
                twoResponseSettingsArguments[1],
                twoResponseSettingsArguments[2]));
        AssertSagaParameter(twoResponseSettingsArguments[0]);
        AssertReferenceTypeParameter(twoResponseSettingsArguments[1], "TRequest");
        AssertReferenceTypeParameter(twoResponseSettingsArguments[2], "TResponse");
        AssertReferenceTypeParameter(twoResponseSettingsArguments[3], "TResponse2");
        AssertDeclaredProperties(twoResponseSettingsType, 1);
        AssertProperty(
            twoResponseSettingsType,
            "Completed2",
            typeof(Action<>).MakeGenericType(
                typeof(IEventCorrelationConfigurator<,>).MakeGenericType(
                    twoResponseSettingsArguments[0],
                    twoResponseSettingsArguments[3])),
            canWrite: false);
        AssertDeclaredMethods(twoResponseSettingsType, 0);

        Type threeResponseSettingsType = typeof(IRequestSettings<,,,,>);
        Type[] threeResponseSettingsArguments = threeResponseSettingsType.GetGenericArguments();
        AssertInterface(
            threeResponseSettingsType,
            typeof(IRequestSettings<,,,>).MakeGenericType(
                threeResponseSettingsArguments[0],
                threeResponseSettingsArguments[1],
                threeResponseSettingsArguments[2],
                threeResponseSettingsArguments[3]));
        AssertSagaParameter(threeResponseSettingsArguments[0]);
        AssertReferenceTypeParameter(threeResponseSettingsArguments[1], "TRequest");
        AssertReferenceTypeParameter(threeResponseSettingsArguments[2], "TResponse");
        AssertReferenceTypeParameter(threeResponseSettingsArguments[3], "TResponse2");
        AssertReferenceTypeParameter(threeResponseSettingsArguments[4], "TResponse3");
        AssertDeclaredProperties(threeResponseSettingsType, 1);
        AssertProperty(
            threeResponseSettingsType,
            "Completed3",
            typeof(Action<>).MakeGenericType(
                typeof(IEventCorrelationConfigurator<,>).MakeGenericType(
                    threeResponseSettingsArguments[0],
                    threeResponseSettingsArguments[4])),
            canWrite: false);
        AssertDeclaredMethods(threeResponseSettingsType, 0);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "public-schedule-contract-shape")]
    public void ScheduleContracts_HaveTheExactHierarchyConstraintsAndMembers()
    {
        Type scheduleType = typeof(ISchedule<>);
        Type saga = scheduleType.GetGenericArguments()[0];
        AssertInterface(scheduleType);
        AssertSagaParameter(saga);
        AssertDeclaredProperties(scheduleType, 1);
        AssertProperty(scheduleType, "Name", typeof(string), canWrite: false);
        AssertDeclaredMethods(scheduleType, 3);
        AssertMethod(scheduleType, "GetDelay", typeof(TimeSpan), 0, typeof(IBehaviorContext<>).MakeGenericType(saga));
        AssertMethod(scheduleType, "GetTokenId", typeof(Guid?), 0, saga);
        AssertMethod(scheduleType, "SetTokenId", typeof(void), 0, saga, typeof(Guid?));

        Type messageScheduleType = typeof(ISchedule<,>);
        Type[] arguments = messageScheduleType.GetGenericArguments();
        AssertInterface(messageScheduleType, typeof(ISchedule<>).MakeGenericType(arguments[0]));
        AssertSagaParameter(arguments[0]);
        AssertReferenceTypeParameter(arguments[1], "TMessage");
        AssertDeclaredProperties(messageScheduleType, 2);
        Type messageEvent = typeof(IEvent<>).MakeGenericType(arguments[1]);
        AssertProperty(messageScheduleType, "Received", messageEvent, canWrite: true);
        AssertProperty(messageScheduleType, "AnyReceived", messageEvent, canWrite: true);
        AssertDeclaredMethods(messageScheduleType, 0);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "public-state-contract-shape")]
    public void StateContracts_HaveTheExactInheritanceConstraintsMembersAndCancellationDefaults()
    {
        Type stateType = typeof(IState);
        AssertInterface(stateType, typeof(IVisitable), typeof(INamedInitializerValue), typeof(IComparable<IState>));
        AssertDeclaredProperties(stateType, 5);
        AssertProperty(stateType, nameof(IState.Name), typeof(string), canWrite: false);
        AssertProperty(stateType, nameof(IState.Enter), typeof(IEvent), canWrite: false);
        AssertProperty(stateType, nameof(IState.Leave), typeof(IEvent), canWrite: false);
        AssertProperty(stateType, nameof(IState.BeforeEnter), typeof(IEvent<IState>), canWrite: false);
        AssertProperty(stateType, nameof(IState.AfterLeave), typeof(IEvent<IState>), canWrite: false);
        AssertDeclaredMethods(stateType, 0);

        Type genericStateType = typeof(IState<>);
        Type saga = genericStateType.GetGenericArguments()[0];
        AssertInterface(genericStateType, typeof(IState), typeof(INamedInitializerValue<>).MakeGenericType(saga));
        AssertSagaParameter(saga);
        AssertDeclaredProperties(genericStateType, 3);
        AssertProperty(genericStateType, "Events", typeof(IEnumerable<IEvent>), canWrite: false);
        AssertProperty(genericStateType, "DeclaredEvents", typeof(IEnumerable<IEvent>), canWrite: false);
        AssertProperty(genericStateType, "SuperState", genericStateType.MakeGenericType(saga), canWrite: false);
        AssertDeclaredMethods(genericStateType, 8);

        Type behaviorContext = typeof(IBehaviorContext<>).MakeGenericType(saga);
        MethodInfo raise = AssertMethod(genericStateType, "RaiseAsync", typeof(Task), 0, behaviorContext, typeof(CancellationToken));
        AssertCancellationTokenDefault(raise);
        MethodInfo typedRaise = AssertSingleGenericMethod(genericStateType, "RaiseAsync", 2);
        Type message = typedRaise.GetGenericArguments()[0];
        AssertReferenceTypeParameter(message, "TMessage");
        Assert.Equal(typeof(Task), typedRaise.ReturnType);
        Assert.Equal(typeof(IBehaviorContext<,>).MakeGenericType(saga, message), typedRaise.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), typedRaise.GetParameters()[1].ParameterType);
        AssertCancellationTokenDefault(typedRaise);
        AssertMethod(genericStateType, "Bind", typeof(void), 0, typeof(IEvent), typeof(IStateMachineActivity<>).MakeGenericType(saga));
        AssertMethod(genericStateType, "Ignore", typeof(void), 0, typeof(IEvent));
        MethodInfo typedIgnore = AssertSingleGenericMethod(genericStateType, "Ignore", 2);
        Type ignoredMessage = typedIgnore.GetGenericArguments()[0];
        AssertReferenceTypeParameter(ignoredMessage, "TMessage");
        Assert.Equal(typeof(void), typedIgnore.ReturnType);
        Assert.Equal(typeof(IEvent<>).MakeGenericType(ignoredMessage), typedIgnore.GetParameters()[0].ParameterType);
        Assert.Equal(
            typeof(StateMachineCondition<,>).MakeGenericType(saga, ignoredMessage),
            typedIgnore.GetParameters()[1].ParameterType);
        AssertMethod(genericStateType, "AddSubstate", typeof(void), 0, genericStateType.MakeGenericType(saga));
        AssertMethod(genericStateType, "HasState", typeof(bool), 0, genericStateType.MakeGenericType(saga));
        AssertMethod(genericStateType, "IsStateOf", typeof(bool), 0, genericStateType.MakeGenericType(saga));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "public-machine-and-saga-contract-shape")]
    public void StateMachineContracts_HaveTheExactInheritanceConstraintsMembersHiddenGetStateAndCancellationDefaults()
    {
        Type machineType = typeof(IStateMachine);
        AssertInterface(machineType, typeof(IVisitable));
        AssertDeclaredProperties(machineType, 6);
        AssertProperty(machineType, nameof(IStateMachine.Name), typeof(string), canWrite: false);
        AssertProperty(machineType, nameof(IStateMachine.Events), typeof(IEnumerable<IEvent>), canWrite: false);
        AssertProperty(machineType, nameof(IStateMachine.States), typeof(IEnumerable<IState>), canWrite: false);
        AssertProperty(machineType, nameof(IStateMachine.InstanceType), typeof(Type), canWrite: false);
        AssertProperty(machineType, nameof(IStateMachine.Initial), typeof(IState), canWrite: false);
        AssertProperty(machineType, nameof(IStateMachine.Final), typeof(IState), canWrite: false);
        AssertDeclaredMethods(machineType, 4);
        AssertMethod(machineType, nameof(IStateMachine.GetEvent), typeof(IEvent), 0, typeof(string));
        MethodInfo untypedGetState = AssertMethod(machineType, nameof(IStateMachine.GetState), typeof(IState), 0, typeof(string));
        AssertMethod(machineType, nameof(IStateMachine.NextEvents), typeof(IEnumerable<IEvent>), 0, typeof(IState));
        AssertMethod(machineType, nameof(IStateMachine.IsCompositeEvent), typeof(bool), 0, typeof(IEvent));

        Type genericMachineType = typeof(IStateMachine<>);
        Type saga = genericMachineType.GetGenericArguments()[0];
        AssertInterface(genericMachineType, typeof(IStateMachine));
        AssertSagaParameter(saga);
        AssertDeclaredProperties(genericMachineType, 1);
        AssertProperty(
            genericMachineType,
            "Accessor",
            typeof(IStateAccessor<>).MakeGenericType(saga),
            canWrite: false);
        AssertDeclaredMethods(genericMachineType, 6);
        Type typedState = typeof(IState<>).MakeGenericType(saga);
        MethodInfo typedGetState = AssertMethod(genericMachineType, "GetState", typedState, 0, typeof(string));
        Assert.Equal(untypedGetState.Name, typedGetState.Name);
        Assert.NotEqual(untypedGetState.ReturnType, typedGetState.ReturnType);

        Type behaviorContext = typeof(IBehaviorContext<>).MakeGenericType(saga);
        MethodInfo raise = AssertMethod(
            genericMachineType,
            "RaiseEventAsync",
            typeof(Task),
            0,
            behaviorContext,
            typeof(CancellationToken));
        AssertCancellationTokenDefault(raise);
        MethodInfo typedRaise = AssertSingleGenericMethod(genericMachineType, "RaiseEventAsync", 2);
        Type message = typedRaise.GetGenericArguments()[0];
        AssertReferenceTypeParameter(message, "TMessage");
        Assert.Equal(typeof(Task), typedRaise.ReturnType);
        Assert.Equal(typeof(IBehaviorContext<,>).MakeGenericType(saga, message), typedRaise.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), typedRaise.GetParameters()[1].ParameterType);
        AssertCancellationTokenDefault(typedRaise);
        AssertMethod(
            genericMachineType,
            "ConnectEventObserver",
            typeof(IDisposable),
            0,
            typeof(IEventObserver<>).MakeGenericType(saga));
        AssertMethod(
            genericMachineType,
            "ConnectEventObserver",
            typeof(IDisposable),
            0,
            typeof(IEvent),
            typeof(IEventObserver<>).MakeGenericType(saga));
        AssertMethod(
            genericMachineType,
            "ConnectStateObserver",
            typeof(IDisposable),
            0,
            typeof(IStateObserver<>).MakeGenericType(saga));

        Type sagaMachineType = typeof(ISagaStateMachine<>);
        Type sagaInstance = sagaMachineType.GetGenericArguments()[0];
        AssertInterface(sagaMachineType, typeof(IStateMachine<>).MakeGenericType(sagaInstance));
        AssertSagaParameter(sagaInstance);
        AssertDeclaredProperties(sagaMachineType, 1);
        AssertProperty(
            sagaMachineType,
            "Correlations",
            typeof(IEnumerable<IEventCorrelation>),
            canWrite: false);
        AssertDeclaredMethods(sagaMachineType, 1);
        MethodInfo isCompleted = AssertMethod(
            sagaMachineType,
            "IsCompletedAsync",
            typeof(Task<bool>),
            0,
            typeof(IBehaviorContext<>).MakeGenericType(sagaInstance),
            typeof(CancellationToken));
        AssertCancellationTokenDefault(isCompleted);
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

    private static void AssertSagaParameter(Type parameter) =>
        AssertGenericParameter(
            parameter,
            "TSaga",
            GenericParameterAttributes.ReferenceTypeConstraint,
            typeof(ISagaStateMachineInstance));

    private static void AssertReferenceTypeParameter(Type parameter, string name) =>
        AssertGenericParameter(parameter, name, GenericParameterAttributes.ReferenceTypeConstraint);

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

    private static void AssertDeclaredProperties(Type type, int expectedCount) =>
        Assert.Equal(expectedCount, type.GetProperties(DeclaredPublicInstance).Length);

    private static PropertyInfo AssertProperty(Type owner, string name, Type propertyType, bool canWrite)
    {
        PropertyInfo property = Assert.IsAssignableFrom<PropertyInfo>(owner.GetProperty(name, DeclaredPublicInstance));
        Assert.Equal(owner, property.DeclaringType);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.True(property.CanRead);
        Assert.True(property.GetMethod!.IsPublic);
        Assert.Equal(canWrite, property.CanWrite);
        if (canWrite)
            Assert.True(property.SetMethod!.IsPublic);
        else
            Assert.Null(property.SetMethod);

        return property;
    }

    private static void AssertDeclaredMethods(Type type, int expectedCount) =>
        Assert.Equal(expectedCount, DeclaredMethods(type).Length);

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

        Assert.Equal(owner, method.DeclaringType);
        Assert.Equal(returnType, method.ReturnType);
        return method;
    }

    private static MethodInfo AssertSingleGenericMethod(Type owner, string name, int parameterCount)
    {
        MethodInfo method = Assert.Single(
            DeclaredMethods(owner),
            candidate => candidate.Name == name
                && candidate.IsGenericMethodDefinition
                && candidate.GetParameters().Length == parameterCount);
        Assert.Equal(owner, method.DeclaringType);
        Assert.Single(method.GetGenericArguments());
        return method;
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

    private static MethodInfo[] DeclaredMethods(Type type) =>
        type.GetMethods(DeclaredPublicInstance).Where(static method => !method.IsSpecialName).ToArray();

    private static string TypeIdentity(Type type) => type.ToString();
}
