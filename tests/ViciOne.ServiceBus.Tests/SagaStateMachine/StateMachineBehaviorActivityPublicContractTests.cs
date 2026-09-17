using System.Reflection;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineBehaviorActivityPublicContractTests
{
    private const BindingFlags DeclaredPublicInstance =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "public-behavior-contract-shape")]
    public void BehaviorContracts_HaveExactInheritanceVarianceConstraintsAndTaskShapedOverloads()
    {
        Type behaviorType = typeof(IBehavior<>);
        Type instance = AssertSingleGenericParameter(behaviorType, "TInstance");
        AssertInterface(behaviorType, typeof(IVisitable));
        AssertSagaParameter(instance, "TInstance");
        AssertDeclaredProperties(behaviorType, 0);
        AssertDeclaredMethods(behaviorType, 4);
        AssertMethod(
            behaviorType,
            nameof(IBehavior<ISagaStateMachineInstance>.ExecuteAsync),
            typeof(Task),
            0,
            typeof(IBehaviorContext<>).MakeGenericType(instance));

        MethodInfo executeWithMessage = AssertGenericMethod(behaviorType, "ExecuteAsync", 1, 1);
        Type executeMessage = executeWithMessage.GetGenericArguments()[0];
        AssertReferenceTypeParameter(executeMessage, "T");
        AssertMethodShape(
            executeWithMessage,
            typeof(Task),
            typeof(IBehaviorContext<,>).MakeGenericType(instance, executeMessage));

        MethodInfo faultWithMessage = AssertGenericMethod(behaviorType, "FaultedAsync", 2, 1);
        Type[] faultWithMessageArguments = faultWithMessage.GetGenericArguments();
        AssertReferenceTypeParameter(faultWithMessageArguments[0], "T");
        AssertExceptionParameter(faultWithMessageArguments[1], "TException");
        AssertMethodShape(
            faultWithMessage,
            typeof(Task),
            typeof(IBehaviorExceptionContext<,,>).MakeGenericType(
                instance,
                faultWithMessageArguments[0],
                faultWithMessageArguments[1]));

        MethodInfo faultWithoutMessage = AssertGenericMethod(behaviorType, "FaultedAsync", 1, 1);
        Type faultException = faultWithoutMessage.GetGenericArguments()[0];
        AssertExceptionParameter(faultException, "TException");
        AssertMethodShape(
            faultWithoutMessage,
            typeof(Task),
            typeof(IBehaviorExceptionContext<,>).MakeGenericType(instance, faultException));

        Type messageBehaviorType = typeof(IBehavior<,>);
        Type[] messageBehaviorArguments = messageBehaviorType.GetGenericArguments();
        Type saga = messageBehaviorArguments[0];
        Type message = messageBehaviorArguments[1];
        AssertInterface(messageBehaviorType, typeof(IVisitable));
        AssertSagaParameter(saga, "TSaga");
        AssertGenericParameter(
            message,
            "TMessage",
            GenericParameterAttributes.Contravariant | GenericParameterAttributes.ReferenceTypeConstraint);
        AssertDeclaredProperties(messageBehaviorType, 0);
        AssertDeclaredMethods(messageBehaviorType, 2);
        AssertMethod(
            messageBehaviorType,
            "ExecuteAsync",
            typeof(Task),
            0,
            typeof(IBehaviorContext<,>).MakeGenericType(saga, message));

        MethodInfo typedFault = AssertGenericMethod(messageBehaviorType, "FaultedAsync", 1, 1);
        Type typedFaultException = typedFault.GetGenericArguments()[0];
        AssertExceptionParameter(typedFaultException, "TException");
        AssertMethodShape(
            typedFault,
            typeof(Task),
            typeof(IBehaviorExceptionContext<,,>).MakeGenericType(saga, message, typedFaultException));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "public-behavior-context-contract-shape")]
    public void BehaviorContextContracts_HaveExactInheritanceVarianceHiddenMembersAndTaskShapes()
    {
        Type contextType = typeof(IBehaviorContext<>);
        Type saga = AssertSingleGenericParameter(contextType, "TSaga");
        AssertInterface(contextType, typeof(SagaConsumeContext<>).MakeGenericType(saga));
        AssertSagaParameter(saga, "TSaga");
        AssertDeclaredProperties(contextType, 2);
        AssertProperty(contextType, "StateMachine", typeof(IStateMachine<>).MakeGenericType(saga));
        PropertyInfo untypedEvent = AssertProperty(contextType, "Event", typeof(IEvent));
        AssertDeclaredMethods(contextType, 5);

        MethodInfo raise = AssertMethod(
            contextType,
            "RaiseAsync",
            typeof(Task),
            0,
            typeof(IEvent),
            typeof(CancellationToken));
        AssertCancellationTokenDefault(raise);

        MethodInfo typedRaise = AssertGenericMethod(contextType, "RaiseAsync", 1, 3);
        Type raisedMessage = typedRaise.GetGenericArguments()[0];
        AssertReferenceTypeParameter(raisedMessage, "TMessage");
        AssertMethodShape(
            typedRaise,
            typeof(Task),
            typeof(IEvent<>).MakeGenericType(raisedMessage),
            raisedMessage,
            typeof(CancellationToken));
        AssertCancellationTokenDefault(typedRaise);

        MethodInfo initialize = AssertGenericMethod(contextType, "InitAsync", 1, 2);
        Type initializedMessage = initialize.GetGenericArguments()[0];
        AssertReferenceTypeParameter(initializedMessage, "TMessage");
        AssertMethodShape(
            initialize,
            typeof(Task<>).MakeGenericType(typeof(InitializedMessage<>).MakeGenericType(initializedMessage)),
            typeof(object),
            typeof(CancellationToken));
        AssertCancellationTokenDefault(initialize);

        AssertMethod(
            contextType,
            "CreateProxy",
            typeof(IBehaviorContext<>).MakeGenericType(saga),
            0,
            typeof(IEvent));

        MethodInfo createTypedProxy = AssertGenericMethod(contextType, "CreateProxy", 1, 2);
        Type proxyMessage = createTypedProxy.GetGenericArguments()[0];
        AssertReferenceTypeParameter(proxyMessage, "TMessage");
        AssertMethodShape(
            createTypedProxy,
            typeof(IBehaviorContext<,>).MakeGenericType(saga, proxyMessage),
            typeof(IEvent<>).MakeGenericType(proxyMessage),
            proxyMessage);

        Type typedContextType = typeof(IBehaviorContext<,>);
        Type[] typedContextArguments = typedContextType.GetGenericArguments();
        Type typedSaga = typedContextArguments[0];
        Type message = typedContextArguments[1];
        AssertInterface(
            typedContextType,
            typeof(SagaConsumeContext<,>).MakeGenericType(typedSaga, message),
            typeof(IBehaviorContext<>).MakeGenericType(typedSaga));
        AssertSagaParameter(typedSaga, "TSaga");
        AssertGenericParameter(
            message,
            "TMessage",
            GenericParameterAttributes.Covariant | GenericParameterAttributes.ReferenceTypeConstraint);
        AssertDeclaredProperties(typedContextType, 1);
        PropertyInfo typedEvent = AssertProperty(
            typedContextType,
            "Event",
            typeof(IEvent<>).MakeGenericType(message));
        Assert.Equal(untypedEvent.Name, typedEvent.Name);
        Assert.NotEqual(untypedEvent.PropertyType, typedEvent.PropertyType);
        AssertDeclaredMethods(typedContextType, 1);

        MethodInfo typedInitialize = AssertGenericMethod(typedContextType, "InitAsync", 1, 2);
        Type result = typedInitialize.GetGenericArguments()[0];
        AssertReferenceTypeParameter(result, "TResult");
        AssertMethodShape(
            typedInitialize,
            typeof(Task<>).MakeGenericType(typeof(InitializedMessage<>).MakeGenericType(result)),
            typeof(object),
            typeof(CancellationToken));
        AssertCancellationTokenDefault(typedInitialize);
        Assert.Equal(initialize.Name, typedInitialize.Name);
        Assert.Equal(initialize.GetParameters().Select(static parameter => parameter.ParameterType),
            typedInitialize.GetParameters().Select(static parameter => parameter.ParameterType));
        Assert.NotEqual(initialize.DeclaringType, typedInitialize.DeclaringType);

        Type exceptionContextType = typeof(IBehaviorExceptionContext<,>);
        Type[] exceptionContextArguments = exceptionContextType.GetGenericArguments();
        Type exceptionSaga = exceptionContextArguments[0];
        Type exception = exceptionContextArguments[1];
        AssertInterface(
            exceptionContextType,
            typeof(IBehaviorContext<>).MakeGenericType(exceptionSaga));
        AssertSagaParameter(exceptionSaga, "TSaga");
        AssertGenericParameter(
            exception,
            "TException",
            GenericParameterAttributes.Covariant,
            typeof(Exception));
        AssertDeclaredProperties(exceptionContextType, 1);
        AssertProperty(exceptionContextType, "Exception", exception);
        AssertDeclaredMethods(exceptionContextType, 1);
        MethodInfo exceptionProxy = AssertGenericMethod(exceptionContextType, "CreateProxy", 1, 2);
        Type exceptionProxyMessage = exceptionProxy.GetGenericArguments()[0];
        AssertReferenceTypeParameter(exceptionProxyMessage, "TMessage");
        AssertMethodShape(
            exceptionProxy,
            typeof(IBehaviorExceptionContext<,,>).MakeGenericType(
                exceptionSaga,
                exceptionProxyMessage,
                exception),
            typeof(IEvent<>).MakeGenericType(exceptionProxyMessage),
            exceptionProxyMessage);
        AssertHiddenGenericMethod(exceptionProxy, createTypedProxy);

        Type typedExceptionContextType = typeof(IBehaviorExceptionContext<,,>);
        Type[] typedExceptionContextArguments = typedExceptionContextType.GetGenericArguments();
        Type typedExceptionSaga = typedExceptionContextArguments[0];
        Type typedExceptionMessage = typedExceptionContextArguments[1];
        Type typedException = typedExceptionContextArguments[2];
        AssertInterface(
            typedExceptionContextType,
            typeof(IBehaviorContext<,>).MakeGenericType(typedExceptionSaga, typedExceptionMessage),
            typeof(IBehaviorExceptionContext<,>).MakeGenericType(typedExceptionSaga, typedException));
        AssertSagaParameter(typedExceptionSaga, "TSaga");
        AssertGenericParameter(
            typedExceptionMessage,
            "TMessage",
            GenericParameterAttributes.Covariant | GenericParameterAttributes.ReferenceTypeConstraint);
        AssertGenericParameter(
            typedException,
            "TException",
            GenericParameterAttributes.Covariant,
            typeof(Exception));
        AssertDeclaredProperties(typedExceptionContextType, 0);
        AssertDeclaredMethods(typedExceptionContextType, 1);
        MethodInfo nestedExceptionProxy = AssertGenericMethod(typedExceptionContextType, "CreateProxy", 1, 2);
        Type nestedMessage = nestedExceptionProxy.GetGenericArguments()[0];
        AssertReferenceTypeParameter(nestedMessage, "TNestedMessage");
        AssertMethodShape(
            nestedExceptionProxy,
            typeof(IBehaviorExceptionContext<,,>).MakeGenericType(
                typedExceptionSaga,
                nestedMessage,
                typedException),
            typeof(IEvent<>).MakeGenericType(nestedMessage),
            nestedMessage);
        AssertHiddenGenericMethod(nestedExceptionProxy, exceptionProxy);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "public-activity-contract-shape")]
    public void ActivityContracts_HaveExactInheritanceConstraintsOverloadsAndTaskShapes()
    {
        Type activityMarkerType = typeof(IStateMachineActivity);
        AssertInterface(activityMarkerType, typeof(IVisitable));
        AssertDeclaredProperties(activityMarkerType, 0);
        AssertDeclaredMethods(activityMarkerType, 0);

        Type activityType = typeof(IStateMachineActivity<>);
        Type saga = AssertSingleGenericParameter(activityType, "TSaga");
        AssertInterface(activityType, activityMarkerType);
        AssertSagaParameter(saga, "TSaga");
        AssertDeclaredProperties(activityType, 0);
        AssertDeclaredMethods(activityType, 4);
        AssertMethod(
            activityType,
            "ExecuteAsync",
            typeof(Task),
            0,
            typeof(IBehaviorContext<>).MakeGenericType(saga),
            typeof(IBehavior<>).MakeGenericType(saga));

        MethodInfo typedExecute = AssertGenericMethod(activityType, "ExecuteAsync", 1, 2);
        Type executeMessage = typedExecute.GetGenericArguments()[0];
        AssertReferenceTypeParameter(executeMessage, "T");
        AssertMethodShape(
            typedExecute,
            typeof(Task),
            typeof(IBehaviorContext<,>).MakeGenericType(saga, executeMessage),
            typeof(IBehavior<,>).MakeGenericType(saga, executeMessage));

        MethodInfo untypedFault = AssertGenericMethod(activityType, "FaultedAsync", 1, 2);
        Type untypedException = untypedFault.GetGenericArguments()[0];
        AssertExceptionParameter(untypedException, "TException");
        AssertMethodShape(
            untypedFault,
            typeof(Task),
            typeof(IBehaviorExceptionContext<,>).MakeGenericType(saga, untypedException),
            typeof(IBehavior<>).MakeGenericType(saga));

        MethodInfo typedFault = AssertGenericMethod(activityType, "FaultedAsync", 2, 2);
        Type[] typedFaultArguments = typedFault.GetGenericArguments();
        AssertReferenceTypeParameter(typedFaultArguments[0], "T");
        AssertExceptionParameter(typedFaultArguments[1], "TException");
        AssertMethodShape(
            typedFault,
            typeof(Task),
            typeof(IBehaviorExceptionContext<,,>).MakeGenericType(
                saga,
                typedFaultArguments[0],
                typedFaultArguments[1]),
            typeof(IBehavior<,>).MakeGenericType(saga, typedFaultArguments[0]));

        Type messageActivityType = typeof(IStateMachineActivity<,>);
        Type[] messageActivityArguments = messageActivityType.GetGenericArguments();
        Type messageSaga = messageActivityArguments[0];
        Type message = messageActivityArguments[1];
        AssertInterface(messageActivityType, activityMarkerType);
        AssertSagaParameter(messageSaga, "TSaga");
        AssertReferenceTypeParameter(message, "TMessage");
        AssertDeclaredProperties(messageActivityType, 0);
        AssertDeclaredMethods(messageActivityType, 2);
        AssertMethod(
            messageActivityType,
            "ExecuteAsync",
            typeof(Task),
            0,
            typeof(IBehaviorContext<,>).MakeGenericType(messageSaga, message),
            typeof(IBehavior<,>).MakeGenericType(messageSaga, message));

        MethodInfo messageFault = AssertGenericMethod(messageActivityType, "FaultedAsync", 1, 2);
        Type messageException = messageFault.GetGenericArguments()[0];
        AssertExceptionParameter(messageException, "TException");
        AssertMethodShape(
            messageFault,
            typeof(Task),
            typeof(IBehaviorExceptionContext<,,>).MakeGenericType(messageSaga, message, messageException),
            typeof(IBehavior<,>).MakeGenericType(messageSaga, message));

        Type exceptionActivityType = typeof(IStateMachineExceptionActivity);
        AssertInterface(exceptionActivityType, activityMarkerType);
        AssertDeclaredProperties(exceptionActivityType, 1);
        AssertProperty(exceptionActivityType, "ExceptionType", typeof(Type));
        AssertDeclaredMethods(exceptionActivityType, 0);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "public-event-activity-binder-contract-shape")]
    public void EventActivityBinderContracts_HaveExactInheritanceConstraintsPropertiesAndOverloads()
    {
        Type activitiesType = typeof(IEventActivities<>);
        Type activitiesInstance = AssertSingleGenericParameter(activitiesType, "TInstance");
        AssertInterface(activitiesType);
        AssertSagaParameter(activitiesInstance, "TInstance");
        AssertDeclaredProperties(activitiesType, 0);
        AssertDeclaredMethods(activitiesType, 1);
        AssertMethod(
            activitiesType,
            "GetStateActivityBinders",
            typeof(IEnumerable<>).MakeGenericType(typeof(IActivityBinder<>).MakeGenericType(activitiesInstance)),
            0);

        Type binderType = typeof(IEventActivityBinder<>);
        Type saga = AssertSingleGenericParameter(binderType, "TSaga");
        Type binder = binderType.MakeGenericType(saga);
        Type callback = SameTypeFunc(binder);
        AssertInterface(binderType, typeof(IEventActivities<>).MakeGenericType(saga));
        AssertSagaParameter(saga, "TSaga");
        AssertDeclaredProperties(binderType, 2);
        AssertProperty(binderType, "StateMachine", typeof(IStateMachine<>).MakeGenericType(saga));
        AssertProperty(binderType, "Event", typeof(IEvent));
        AssertDeclaredMethods(binderType, 7);
        AssertMethod(binderType, "Add", binder, 0, typeof(IStateMachineActivity<>).MakeGenericType(saga));

        MethodInfo catchMethod = AssertGenericMethod(binderType, "Catch", 1, 1);
        Type caughtException = catchMethod.GetGenericArguments()[0];
        AssertExceptionParameter(caughtException, "TException");
        Type exceptionBinder = typeof(IExceptionActivityBinder<,>).MakeGenericType(saga, caughtException);
        AssertMethodShape(catchMethod, binder, SameTypeFunc(exceptionBinder));

        AssertMethod(binderType, "Retry", binder, 0, typeof(Action<IRetryConfigurator>), callback);
        AssertMethod(binderType, "If", binder, 0, typeof(StateMachineCondition<>).MakeGenericType(saga), callback);
        AssertMethod(
            binderType,
            "IfAwaited",
            binder,
            0,
            typeof(StateMachineAsyncCondition<>).MakeGenericType(saga),
            callback);
        AssertMethod(
            binderType,
            "IfElse",
            binder,
            0,
            typeof(StateMachineCondition<>).MakeGenericType(saga),
            callback,
            callback);
        AssertMethod(
            binderType,
            "IfElseAwaited",
            binder,
            0,
            typeof(StateMachineAsyncCondition<>).MakeGenericType(saga),
            callback,
            callback);

        Type messageBinderType = typeof(IEventActivityBinder<,>);
        Type[] messageBinderArguments = messageBinderType.GetGenericArguments();
        Type messageSaga = messageBinderArguments[0];
        Type message = messageBinderArguments[1];
        Type messageBinder = messageBinderType.MakeGenericType(messageSaga, message);
        Type messageCallback = SameTypeFunc(messageBinder);
        AssertInterface(messageBinderType, typeof(IEventActivities<>).MakeGenericType(messageSaga));
        AssertSagaParameter(messageSaga, "TSaga");
        AssertReferenceTypeParameter(message, "TMessage");
        AssertDeclaredProperties(messageBinderType, 2);
        AssertProperty(messageBinderType, "StateMachine", typeof(IStateMachine<>).MakeGenericType(messageSaga));
        AssertProperty(messageBinderType, "Event", typeof(IEvent<>).MakeGenericType(message));
        AssertDeclaredMethods(messageBinderType, 8);
        AssertMethod(
            messageBinderType,
            "Add",
            messageBinder,
            0,
            typeof(IStateMachineActivity<>).MakeGenericType(messageSaga));
        AssertMethod(
            messageBinderType,
            "Add",
            messageBinder,
            0,
            typeof(IStateMachineActivity<,>).MakeGenericType(messageSaga, message));

        MethodInfo messageCatch = AssertGenericMethod(messageBinderType, "Catch", 1, 1);
        Type messageCaughtException = messageCatch.GetGenericArguments()[0];
        AssertExceptionParameter(messageCaughtException, "TException");
        Type messageExceptionBinder = typeof(IExceptionActivityBinder<,,>).MakeGenericType(
            messageSaga,
            message,
            messageCaughtException);
        AssertMethodShape(messageCatch, messageBinder, SameTypeFunc(messageExceptionBinder));

        AssertMethod(
            messageBinderType,
            "Retry",
            messageBinder,
            0,
            typeof(Action<IRetryConfigurator>),
            messageCallback);
        AssertMethod(
            messageBinderType,
            "If",
            messageBinder,
            0,
            typeof(StateMachineCondition<,>).MakeGenericType(messageSaga, message),
            messageCallback);
        AssertMethod(
            messageBinderType,
            "IfAwaited",
            messageBinder,
            0,
            typeof(StateMachineAsyncCondition<,>).MakeGenericType(messageSaga, message),
            messageCallback);
        AssertMethod(
            messageBinderType,
            "IfElse",
            messageBinder,
            0,
            typeof(StateMachineCondition<,>).MakeGenericType(messageSaga, message),
            messageCallback,
            messageCallback);
        AssertMethod(
            messageBinderType,
            "IfElseAwaited",
            messageBinder,
            0,
            typeof(StateMachineAsyncCondition<,>).MakeGenericType(messageSaga, message),
            messageCallback,
            messageCallback);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CATCH", "public-exception-activity-binder-contract-shape")]
    public void ExceptionActivityBinderContracts_HaveExactInheritanceConstraintsPropertiesAndOverloads()
    {
        Type binderType = typeof(IExceptionActivityBinder<,>);
        Type[] arguments = binderType.GetGenericArguments();
        Type saga = arguments[0];
        Type exception = arguments[1];
        Type binder = binderType.MakeGenericType(saga, exception);
        Type callback = SameTypeFunc(binder);
        AssertInterface(binderType, typeof(IEventActivities<>).MakeGenericType(saga));
        AssertSagaParameter(saga, "TSaga");
        AssertExceptionParameter(exception, "TException");
        AssertDeclaredProperties(binderType, 2);
        AssertProperty(binderType, "StateMachine", typeof(IStateMachine<>).MakeGenericType(saga));
        AssertProperty(binderType, "Event", typeof(IEvent));
        AssertDeclaredMethods(binderType, 6);
        AssertMethod(binderType, "Add", binder, 0, typeof(IStateMachineActivity<>).MakeGenericType(saga));

        MethodInfo catchMethod = AssertGenericMethod(binderType, "Catch", 1, 1);
        Type nestedException = catchMethod.GetGenericArguments()[0];
        AssertExceptionParameter(nestedException, "TNestedException");
        Type nestedBinder = typeof(IExceptionActivityBinder<,>).MakeGenericType(saga, nestedException);
        AssertMethodShape(catchMethod, binder, SameTypeFunc(nestedBinder));

        AssertMethod(
            binderType,
            "If",
            binder,
            0,
            typeof(StateMachineExceptionCondition<,>).MakeGenericType(saga, exception),
            callback);
        AssertMethod(
            binderType,
            "IfAwaited",
            binder,
            0,
            typeof(StateMachineAsyncExceptionCondition<,>).MakeGenericType(saga, exception),
            callback);
        AssertMethod(
            binderType,
            "IfElse",
            binder,
            0,
            typeof(StateMachineExceptionCondition<,>).MakeGenericType(saga, exception),
            callback,
            callback);
        AssertMethod(
            binderType,
            "IfElseAwaited",
            binder,
            0,
            typeof(StateMachineAsyncExceptionCondition<,>).MakeGenericType(saga, exception),
            callback,
            callback);

        Type messageBinderType = typeof(IExceptionActivityBinder<,,>);
        Type[] messageArguments = messageBinderType.GetGenericArguments();
        Type messageSaga = messageArguments[0];
        Type message = messageArguments[1];
        Type messageException = messageArguments[2];
        Type messageBinder = messageBinderType.MakeGenericType(messageSaga, message, messageException);
        Type messageCallback = SameTypeFunc(messageBinder);
        AssertInterface(messageBinderType, typeof(IEventActivities<>).MakeGenericType(messageSaga));
        AssertSagaParameter(messageSaga, "TSaga");
        AssertReferenceTypeParameter(message, "TMessage");
        AssertExceptionParameter(messageException, "TException");
        AssertDeclaredProperties(messageBinderType, 2);
        AssertProperty(messageBinderType, "StateMachine", typeof(IStateMachine<>).MakeGenericType(messageSaga));
        AssertProperty(messageBinderType, "Event", typeof(IEvent<>).MakeGenericType(message));
        AssertDeclaredMethods(messageBinderType, 7);
        AssertMethod(
            messageBinderType,
            "Add",
            messageBinder,
            0,
            typeof(IStateMachineActivity<>).MakeGenericType(messageSaga));
        AssertMethod(
            messageBinderType,
            "Add",
            messageBinder,
            0,
            typeof(IStateMachineActivity<,>).MakeGenericType(messageSaga, message));

        MethodInfo messageCatch = AssertGenericMethod(messageBinderType, "Catch", 1, 1);
        Type nestedMessageException = messageCatch.GetGenericArguments()[0];
        AssertExceptionParameter(nestedMessageException, "TNestedException");
        Type nestedMessageBinder = typeof(IExceptionActivityBinder<,,>).MakeGenericType(
            messageSaga,
            message,
            nestedMessageException);
        AssertMethodShape(messageCatch, messageBinder, SameTypeFunc(nestedMessageBinder));

        AssertMethod(
            messageBinderType,
            "If",
            messageBinder,
            0,
            typeof(StateMachineExceptionCondition<,,>).MakeGenericType(messageSaga, message, messageException),
            messageCallback);
        AssertMethod(
            messageBinderType,
            "IfAwaited",
            messageBinder,
            0,
            typeof(StateMachineAsyncExceptionCondition<,,>).MakeGenericType(messageSaga, message, messageException),
            messageCallback);
        AssertMethod(
            messageBinderType,
            "IfElse",
            messageBinder,
            0,
            typeof(StateMachineExceptionCondition<,,>).MakeGenericType(messageSaga, message, messageException),
            messageCallback,
            messageCallback);
        AssertMethod(
            messageBinderType,
            "IfElseAwaited",
            messageBinder,
            0,
            typeof(StateMachineAsyncExceptionCondition<,,>).MakeGenericType(messageSaga, message, messageException),
            messageCallback,
            messageCallback);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "public-selector-and-builder-contract-shape")]
    public void SelectorAndBuilderContracts_HaveExactInheritanceConstraintsPropertiesAndOverloads()
    {
        Type messageSelectorType = typeof(IStateMachineActivitySelector<,>);
        Type[] messageSelectorArguments = messageSelectorType.GetGenericArguments();
        Type selectorInstance = messageSelectorArguments[0];
        Type selectorData = messageSelectorArguments[1];
        AssertInterface(messageSelectorType);
        AssertSagaParameter(selectorInstance, "TInstance");
        AssertReferenceTypeParameter(selectorData, "TData");
        AssertDeclaredProperties(messageSelectorType, 0);
        AssertDeclaredMethods(messageSelectorType, 2);
        MethodInfo ofType = AssertGenericMethod(messageSelectorType, "OfType", 1, 0);
        AssertActivityParameter(
            ofType.GetGenericArguments()[0],
            typeof(IStateMachineActivity<,>).MakeGenericType(selectorInstance, selectorData));
        AssertMethodShape(
            ofType,
            typeof(IEventActivityBinder<,>).MakeGenericType(selectorInstance, selectorData));
        MethodInfo ofInstanceType = AssertGenericMethod(messageSelectorType, "OfInstanceType", 1, 0);
        AssertActivityParameter(
            ofInstanceType.GetGenericArguments()[0],
            typeof(IStateMachineActivity<>).MakeGenericType(selectorInstance));
        AssertMethodShape(
            ofInstanceType,
            typeof(IEventActivityBinder<,>).MakeGenericType(selectorInstance, selectorData));

        Type selectorType = typeof(IStateMachineActivitySelector<>);
        Type instance = AssertSingleGenericParameter(selectorType, "TInstance");
        AssertInterface(selectorType);
        AssertSagaParameter(instance, "TInstance");
        AssertDeclaredProperties(selectorType, 0);
        AssertDeclaredMethods(selectorType, 1);
        MethodInfo instanceOfType = AssertGenericMethod(selectorType, "OfType", 1, 0);
        AssertActivityParameter(
            instanceOfType.GetGenericArguments()[0],
            typeof(IStateMachineActivity<>).MakeGenericType(instance));
        AssertMethodShape(instanceOfType, typeof(IEventActivityBinder<>).MakeGenericType(instance));

        Type messageFaultSelectorType = typeof(IStateMachineFaultedActivitySelector<,,>);
        Type[] messageFaultSelectorArguments = messageFaultSelectorType.GetGenericArguments();
        Type faultInstance = messageFaultSelectorArguments[0];
        Type faultData = messageFaultSelectorArguments[1];
        Type faultException = messageFaultSelectorArguments[2];
        AssertInterface(messageFaultSelectorType);
        AssertSagaParameter(faultInstance, "TInstance");
        AssertReferenceTypeParameter(faultData, "TData");
        AssertExceptionParameter(faultException, "TException");
        AssertDeclaredProperties(messageFaultSelectorType, 0);
        AssertDeclaredMethods(messageFaultSelectorType, 2);
        MethodInfo faultOfType = AssertGenericMethod(messageFaultSelectorType, "OfType", 1, 0);
        AssertActivityParameter(
            faultOfType.GetGenericArguments()[0],
            typeof(IStateMachineActivity<,>).MakeGenericType(faultInstance, faultData));
        AssertMethodShape(
            faultOfType,
            typeof(IExceptionActivityBinder<,,>).MakeGenericType(faultInstance, faultData, faultException));
        MethodInfo faultOfInstanceType = AssertGenericMethod(messageFaultSelectorType, "OfInstanceType", 1, 0);
        AssertActivityParameter(
            faultOfInstanceType.GetGenericArguments()[0],
            typeof(IStateMachineActivity<>).MakeGenericType(faultInstance));
        AssertMethodShape(
            faultOfInstanceType,
            typeof(IExceptionActivityBinder<,,>).MakeGenericType(faultInstance, faultData, faultException));

        Type faultSelectorType = typeof(IStateMachineFaultedActivitySelector<,>);
        Type[] faultSelectorArguments = faultSelectorType.GetGenericArguments();
        Type untypedFaultInstance = faultSelectorArguments[0];
        Type untypedFaultException = faultSelectorArguments[1];
        AssertInterface(faultSelectorType);
        AssertSagaParameter(untypedFaultInstance, "TInstance");
        AssertExceptionParameter(untypedFaultException, "TException");
        AssertDeclaredProperties(faultSelectorType, 0);
        AssertDeclaredMethods(faultSelectorType, 1);
        MethodInfo untypedFaultOfType = AssertGenericMethod(faultSelectorType, "OfType", 1, 0);
        AssertActivityParameter(
            untypedFaultOfType.GetGenericArguments()[0],
            typeof(IStateMachineActivity<>).MakeGenericType(untypedFaultInstance));
        AssertMethodShape(
            untypedFaultOfType,
            typeof(IExceptionActivityBinder<,>).MakeGenericType(untypedFaultInstance, untypedFaultException));

        Type builderType = typeof(IStateMachineEventActivitiesBuilder<>);
        Type builderSaga = AssertSingleGenericParameter(builderType, "TSaga");
        Type builder = builderType.MakeGenericType(builderSaga);
        Type untypedBinder = typeof(IEventActivityBinder<>).MakeGenericType(builderSaga);
        Type untypedCallback = SameTypeFunc(untypedBinder);
        AssertInterface(builderType, typeof(IStateMachineModifier<>).MakeGenericType(builderSaga));
        AssertSagaParameter(builderSaga, "TSaga");
        AssertDeclaredProperties(builderType, 1);
        AssertProperty(builderType, "IsCommitted", typeof(bool));
        AssertDeclaredMethods(builderType, 8);
        AssertMethod(builderType, "When", builder, 0, typeof(IEvent), untypedCallback);
        AssertMethod(
            builderType,
            "When",
            builder,
            0,
            typeof(IEvent),
            typeof(StateMachineCondition<>).MakeGenericType(builderSaga),
            untypedCallback);

        MethodInfo typedWhen = AssertGenericMethod(builderType, "When", 1, 2);
        Type whenData = typedWhen.GetGenericArguments()[0];
        AssertReferenceTypeParameter(whenData, "TData");
        Type whenBinder = typeof(IEventActivityBinder<,>).MakeGenericType(builderSaga, whenData);
        AssertMethodShape(
            typedWhen,
            builder,
            typeof(IEvent<>).MakeGenericType(whenData),
            SameTypeFunc(whenBinder));

        MethodInfo filteredWhen = AssertGenericMethod(builderType, "When", 1, 3);
        Type filteredData = filteredWhen.GetGenericArguments()[0];
        AssertReferenceTypeParameter(filteredData, "TData");
        Type filteredBinder = typeof(IEventActivityBinder<,>).MakeGenericType(builderSaga, filteredData);
        AssertMethodShape(
            filteredWhen,
            builder,
            typeof(IEvent<>).MakeGenericType(filteredData),
            typeof(StateMachineCondition<,>).MakeGenericType(builderSaga, filteredData),
            SameTypeFunc(filteredBinder));

        AssertMethod(builderType, "Ignore", builder, 0, typeof(IEvent));
        MethodInfo typedIgnore = AssertGenericMethod(builderType, "Ignore", 1, 1);
        Type ignoredMessage = typedIgnore.GetGenericArguments()[0];
        AssertReferenceTypeParameter(ignoredMessage, "TMessage");
        AssertMethodShape(typedIgnore, builder, typeof(IEvent<>).MakeGenericType(ignoredMessage));
        MethodInfo filteredIgnore = AssertGenericMethod(builderType, "Ignore", 1, 2);
        Type filteredMessage = filteredIgnore.GetGenericArguments()[0];
        AssertReferenceTypeParameter(filteredMessage, "TMessage");
        AssertMethodShape(
            filteredIgnore,
            builder,
            typeof(IEvent<>).MakeGenericType(filteredMessage),
            typeof(StateMachineCondition<,>).MakeGenericType(builderSaga, filteredMessage));
        AssertMethod(
            builderType,
            "CommitActivities",
            typeof(IStateMachineModifier<>).MakeGenericType(builderSaga),
            0);
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

    private static Type AssertSingleGenericParameter(Type owner, string name)
    {
        Type parameter = Assert.Single(owner.GetGenericArguments());
        Assert.Equal(name, parameter.Name);
        return parameter;
    }

    private static void AssertSagaParameter(Type parameter, string name) =>
        AssertGenericParameter(
            parameter,
            name,
            GenericParameterAttributes.ReferenceTypeConstraint,
            typeof(ISagaStateMachineInstance));

    private static void AssertReferenceTypeParameter(Type parameter, string name) =>
        AssertGenericParameter(parameter, name, GenericParameterAttributes.ReferenceTypeConstraint);

    private static void AssertExceptionParameter(Type parameter, string name) =>
        AssertGenericParameter(parameter, name, GenericParameterAttributes.None, typeof(Exception));

    private static void AssertActivityParameter(Type parameter, Type activityConstraint) =>
        AssertGenericParameter(
            parameter,
            "TActivity",
            GenericParameterAttributes.ReferenceTypeConstraint,
            activityConstraint);

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

    private static PropertyInfo AssertProperty(Type owner, string name, Type propertyType)
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
                && candidate.GetParameters().Select(static parameter => parameter.ParameterType)
                    .SequenceEqual(parameterTypes));

        Assert.Equal(owner, method.DeclaringType);
        AssertMethodShape(method, returnType, parameterTypes);
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

    private static void AssertMethodShape(MethodInfo method, Type returnType, params Type[] parameterTypes)
    {
        Assert.NotNull(method.DeclaringType);
        Assert.True(method.IsPublic);
        Assert.False(method.IsStatic);
        Assert.Equal(returnType, method.ReturnType);
        Assert.Equal(parameterTypes, method.GetParameters().Select(static parameter => parameter.ParameterType));
    }

    private static void AssertHiddenGenericMethod(MethodInfo hidden, MethodInfo inherited)
    {
        Assert.Equal(inherited.Name, hidden.Name);
        Assert.Equal(inherited.GetGenericArguments().Length, hidden.GetGenericArguments().Length);
        MethodInfo closedHidden = hidden.MakeGenericMethod(typeof(string));
        MethodInfo closedInherited = inherited.MakeGenericMethod(typeof(string));
        Assert.Equal(
            closedInherited.GetParameters().Select(static parameter => parameter.ParameterType),
            closedHidden.GetParameters().Select(static parameter => parameter.ParameterType));
        Assert.NotEqual(inherited.DeclaringType, hidden.DeclaringType);
        Assert.NotEqual(closedInherited.ReturnType, closedHidden.ReturnType);
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

    private static Type SameTypeFunc(Type type) => typeof(Func<,>).MakeGenericType(type, type);

    private static string TypeIdentity(Type type) => type.ToString();
}
