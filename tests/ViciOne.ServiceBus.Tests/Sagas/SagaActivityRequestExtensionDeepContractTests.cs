using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaActivityRequestExtensionDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-ACTIVITY-REQUEST-EXTENSIONS", "container-overload-selector-binder-identity")]
    public void ContainerActivityOverloads_PreserveSelectorBinderAndCallbackResultIdentity()
    {
        IEventActivityBinder<TestSaga> stateResult = CreateProxy<IEventActivityBinder<TestSaga>>(out _);
        IEventActivityBinder<TestSaga> stateBinder = CreateProxy<IEventActivityBinder<TestSaga>>(out RecordingProxy stateRecorder);
        stateRecorder.ReturnValue = stateResult;

        IEventActivityBinder<TestSaga> returnedState = ContainerActivityExtensions.Activity<TestSaga>(
            stateBinder,
            selector => selector.OfType<ActionActivity<TestSaga>>());

        Assert.Same(stateResult, returnedState);
        Assert.IsType<ContainerFactoryActivity<TestSaga, ActionActivity<TestSaga>>>(Assert.Single(stateRecorder.Arguments));

        IEventActivityBinder<TestSaga, InputMessage> messageResult =
            CreateProxy<IEventActivityBinder<TestSaga, InputMessage>>(out _);
        IEventActivityBinder<TestSaga, InputMessage> messageBinder =
            CreateProxy<IEventActivityBinder<TestSaga, InputMessage>>(out RecordingProxy messageRecorder);
        messageRecorder.ReturnValue = messageResult;

        IEventActivityBinder<TestSaga, InputMessage> returnedMessage = ContainerActivityExtensions.Activity<TestSaga, InputMessage>(
            messageBinder,
            selector => selector.OfType<RequestStartedActivity<TestSaga, InputMessage>>());

        Assert.Same(messageResult, returnedMessage);
        Assert.IsType<ContainerFactoryActivity<TestSaga, InputMessage, RequestStartedActivity<TestSaga, InputMessage>>>(
            Assert.Single(messageRecorder.Arguments));

        IExceptionActivityBinder<TestSaga, InvalidOperationException> faultResult =
            CreateProxy<IExceptionActivityBinder<TestSaga, InvalidOperationException>>(out _);
        IExceptionActivityBinder<TestSaga, InvalidOperationException> faultBinder =
            CreateProxy<IExceptionActivityBinder<TestSaga, InvalidOperationException>>(out RecordingProxy faultRecorder);
        faultRecorder.ReturnValue = faultResult;

        IExceptionActivityBinder<TestSaga, InvalidOperationException> returnedFault =
            ContainerActivityExtensions.Activity<TestSaga, InvalidOperationException>(
                faultBinder,
                selector => selector.OfType<ActionActivity<TestSaga>>());

        Assert.Same(faultResult, returnedFault);
        Assert.IsType<FaultedContainerFactoryActivity<TestSaga, InvalidOperationException, ActionActivity<TestSaga>>>(
            Assert.Single(faultRecorder.Arguments));

        IExceptionActivityBinder<TestSaga, InputMessage, InvalidOperationException> messageFaultResult =
            CreateProxy<IExceptionActivityBinder<TestSaga, InputMessage, InvalidOperationException>>(out _);
        IExceptionActivityBinder<TestSaga, InputMessage, InvalidOperationException> messageFaultBinder =
            CreateProxy<IExceptionActivityBinder<TestSaga, InputMessage, InvalidOperationException>>(out RecordingProxy messageFaultRecorder);
        messageFaultRecorder.ReturnValue = messageFaultResult;

        IExceptionActivityBinder<TestSaga, InputMessage, InvalidOperationException> returnedMessageFault =
            ContainerActivityExtensions.Activity<TestSaga, InputMessage, InvalidOperationException>(
                messageFaultBinder,
                selector => selector.OfType<RequestStartedActivity<TestSaga, InputMessage>>());

        Assert.Same(messageFaultResult, returnedMessageFault);
        Assert.IsType<FaultedContainerFactoryActivity<TestSaga, InputMessage, InvalidOperationException,
            RequestStartedActivity<TestSaga, InputMessage>>>(Assert.Single(messageFaultRecorder.Arguments));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-ACTIVITY-REQUEST-EXTENSIONS", "container-null-boundaries-before-callback")]
    public void ContainerActivityOverloads_RejectNullInputsBeforeCallbackOrBinderEffects()
    {
        var callbackCount = 0;
        IEventActivityBinder<TestSaga> stateBinder = CreateProxy<IEventActivityBinder<TestSaga>>(out RecordingProxy stateRecorder);
        IEventActivityBinder<TestSaga, InputMessage> messageBinder =
            CreateProxy<IEventActivityBinder<TestSaga, InputMessage>>(out RecordingProxy messageRecorder);
        IExceptionActivityBinder<TestSaga, InvalidOperationException> faultBinder =
            CreateProxy<IExceptionActivityBinder<TestSaga, InvalidOperationException>>(out RecordingProxy faultRecorder);
        IExceptionActivityBinder<TestSaga, InputMessage, InvalidOperationException> messageFaultBinder =
            CreateProxy<IExceptionActivityBinder<TestSaga, InputMessage, InvalidOperationException>>(out RecordingProxy messageFaultRecorder);

        AssertArgument("binder", () => ContainerActivityExtensions.Activity<TestSaga>(null!, selector =>
        {
            callbackCount++;
            return stateBinder;
        }));
        AssertArgument("binder", () => ContainerActivityExtensions.Activity<TestSaga, InputMessage>(null!, selector =>
        {
            callbackCount++;
            return messageBinder;
        }));
        AssertArgument("binder", () => ContainerActivityExtensions.Activity<TestSaga, InvalidOperationException>(null!, selector =>
        {
            callbackCount++;
            return faultBinder;
        }));
        AssertArgument("binder", () => ContainerActivityExtensions.Activity<TestSaga, InputMessage, InvalidOperationException>(null!, selector =>
        {
            callbackCount++;
            return messageFaultBinder;
        }));

        AssertArgument("configure", () => ContainerActivityExtensions.Activity<TestSaga>(stateBinder, null!));
        AssertArgument("configure", () => ContainerActivityExtensions.Activity<TestSaga, InputMessage>(messageBinder, null!));
        AssertArgument("configure", () =>
            ContainerActivityExtensions.Activity<TestSaga, InvalidOperationException>(faultBinder, null!));
        AssertArgument("configure", () =>
            ContainerActivityExtensions.Activity<TestSaga, InputMessage, InvalidOperationException>(messageFaultBinder, null!));

        Assert.Equal(0, callbackCount);
        Assert.Empty(stateRecorder.Arguments);
        Assert.Empty(messageRecorder.Arguments);
        Assert.Empty(faultRecorder.Arguments);
        Assert.Empty(messageFaultRecorder.Arguments);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-ACTIVITY-REQUEST-EXTENSIONS", "request-activity-registration-and-return-identity")]
    public void RequestExtensions_RegisterExactLifecycleActivitiesAndReturnBinderResults()
    {
        IEventActivityBinder<TestSaga, InputMessage> startedResult = CreateProxy<IEventActivityBinder<TestSaga, InputMessage>>(out _);
        IEventActivityBinder<TestSaga, InputMessage> startedBinder = CreateProxy<IEventActivityBinder<TestSaga, InputMessage>>(
            out RecordingProxy startedRecorder);
        startedRecorder.ReturnValue = startedResult;

        Assert.Same(startedResult, startedBinder.RequestStarted());
        Assert.IsType<RequestStartedActivity<TestSaga, InputMessage>>(Assert.Single(startedRecorder.Arguments));

        IEventActivityBinder<TestSaga, InputMessage> completedResult = CreateProxy<IEventActivityBinder<TestSaga, InputMessage>>(out _);
        IEventActivityBinder<TestSaga, InputMessage> completedBinder = CreateProxy<IEventActivityBinder<TestSaga, InputMessage>>(
            out RecordingProxy completedRecorder);
        completedRecorder.ReturnValue = completedResult;

        Assert.Same(completedResult, completedBinder.RequestCompleted());
        Assert.IsType<RequestCompletedActivity<TestSaga, InputMessage>>(Assert.Single(completedRecorder.Arguments));

        IEventActivityBinder<TestSaga, InputMessage> factoryResult = CreateProxy<IEventActivityBinder<TestSaga, InputMessage>>(out _);
        IEventActivityBinder<TestSaga, InputMessage> factoryBinder = CreateProxy<IEventActivityBinder<TestSaga, InputMessage>>(
            out RecordingProxy factoryRecorder);
        factoryRecorder.ReturnValue = factoryResult;
        AsyncEventMessageFactory<TestSaga, InputMessage, ResponseMessage> factory = _ => Task.FromResult(new ResponseMessage("response"));

        Assert.Same(factoryResult, factoryBinder.RequestCompleted(factory));
        Assert.IsType<RequestCompletedActivity<TestSaga, InputMessage, ResponseMessage>>(Assert.Single(factoryRecorder.Arguments));

        IEventActivityBinder<TestSaga, InputMessage> faultedResult = CreateProxy<IEventActivityBinder<TestSaga, InputMessage>>(out _);
        IEventActivityBinder<TestSaga, InputMessage> faultedBinder = CreateProxy<IEventActivityBinder<TestSaga, InputMessage>>(
            out RecordingProxy faultedRecorder);
        faultedRecorder.ReturnValue = faultedResult;
        IEvent<RequestMessage> requestEvent = CreateProxy<IEvent<RequestMessage>>(out _);

        Assert.Same(faultedResult, faultedBinder.RequestFaulted(requestEvent));
        Assert.IsType<RequestFaultedActivity<TestSaga, InputMessage, RequestMessage>>(Assert.Single(faultedRecorder.Arguments));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-ACTIVITY-REQUEST-EXTENSIONS", "request-required-boundaries-before-binder-effects")]
    public void RequestExtensions_RejectEveryNullRequiredInputBeforeBinderEffects()
    {
        IEventActivityBinder<TestSaga, InputMessage> binder =
            CreateProxy<IEventActivityBinder<TestSaga, InputMessage>>(out RecordingProxy recorder);
        AsyncEventMessageFactory<TestSaga, InputMessage, ResponseMessage> factory = _ => Task.FromResult(new ResponseMessage("response"));
        IEvent<RequestMessage> requestEvent = CreateProxy<IEvent<RequestMessage>>(out _);

        AssertArgument("source", () => RequestEventExtensions.RequestStarted<TestSaga, InputMessage>(null!));
        AssertArgument("source", () => RequestEventExtensions.RequestCompleted<TestSaga, InputMessage>(null!));
        AssertArgument("source", () => RequestEventExtensions.RequestCompleted<TestSaga, InputMessage, ResponseMessage>(null!, factory));
        AssertArgument("source", () => RequestEventExtensions.RequestCompleted<TestSaga, InputMessage, ResponseMessage>(null!, null!));
        AssertArgument("source", () => RequestEventExtensions.RequestFaulted<TestSaga, InputMessage, RequestMessage>(null!, requestEvent));
        AssertArgument("source", () => RequestEventExtensions.RequestFaulted<TestSaga, InputMessage, RequestMessage>(null!, null!));
        AssertArgument("messageFactory", () => binder.RequestCompleted<TestSaga, InputMessage, ResponseMessage>(null!));
        AssertArgument("requestEvent", () => binder.RequestFaulted<TestSaga, InputMessage, RequestMessage>(null!));

        Assert.Empty(recorder.Arguments);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-ACTIVITY-REQUEST-EXTENSIONS", "message-factory-and-request-event-ownership")]
    public void RequestInputs_DetermineTheExactOwnedFactoryAndRequestContractWithoutEarlyInvocation()
    {
        var factoryInvocationCount = 0;
        AsyncEventMessageFactory<TestSaga, InputMessage, ResponseMessage> factory = _ =>
        {
            factoryInvocationCount++;
            return Task.FromResult(new ResponseMessage("response"));
        };
        IEventActivityBinder<TestSaga, InputMessage> completedBinder = CreateProxy<IEventActivityBinder<TestSaga, InputMessage>>(
            out RecordingProxy completedRecorder);
        completedRecorder.ReturnValue = completedBinder;

        completedBinder.RequestCompleted(factory);

        object completedActivity = Assert.Single(completedRecorder.Arguments);
        FieldInfo factoryField = completedActivity.GetType().GetField("_messageFactory", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.Same(factory, factoryField.GetValue(completedActivity));
        Assert.Equal(0, factoryInvocationCount);

        IEvent<RequestMessage> requestEvent = CreateProxy<IEvent<RequestMessage>>(out RecordingProxy requestRecorder);
        IEventActivityBinder<TestSaga, InputMessage> faultedBinder = CreateProxy<IEventActivityBinder<TestSaga, InputMessage>>(
            out RecordingProxy faultedRecorder);
        faultedRecorder.ReturnValue = faultedBinder;

        faultedBinder.RequestFaulted(requestEvent);

        Type activityType = Assert.Single(faultedRecorder.Arguments).GetType();
        Assert.Equal(typeof(RequestFaultedActivity<TestSaga, InputMessage, RequestMessage>), activityType);
        Assert.Empty(requestRecorder.Invocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-ACTIVITY-REQUEST-EXTENSIONS", "service-address-exception-provider-shape-and-invocation")]
    public void ServiceAddressExceptionProviders_ExposeExactDelegateShapeAndPreserveInvocationInputs()
    {
        AssertDelegateShape(
            typeof(ServiceAddressExceptionProvider<TestSaga, InvalidOperationException>),
            typeof(IBehaviorExceptionContext<TestSaga, InvalidOperationException>));
        AssertDelegateShape(
            typeof(ServiceAddressExceptionProvider<TestSaga, InputMessage, InvalidOperationException>),
            typeof(IBehaviorExceptionContext<TestSaga, InputMessage, InvalidOperationException>));

        Type[] stateParameters = typeof(ServiceAddressExceptionProvider<,>).GetGenericArguments();
        AssertGenericParameter(stateParameters[0], GenericParameterAttributes.ReferenceTypeConstraint, typeof(ISagaStateMachineInstance));
        AssertGenericParameter(stateParameters[1], GenericParameterAttributes.Contravariant, typeof(Exception));

        Type[] messageParameters = typeof(ServiceAddressExceptionProvider<,,>).GetGenericArguments();
        AssertGenericParameter(messageParameters[0], GenericParameterAttributes.ReferenceTypeConstraint, typeof(ISagaStateMachineInstance));
        AssertGenericParameter(
            messageParameters[1],
            GenericParameterAttributes.Contravariant | GenericParameterAttributes.ReferenceTypeConstraint);
        AssertGenericParameter(messageParameters[2], GenericParameterAttributes.Contravariant, typeof(Exception));

        IBehaviorExceptionContext<TestSaga, InvalidOperationException> stateContext =
            CreateProxy<IBehaviorExceptionContext<TestSaga, InvalidOperationException>>(out _);
        IBehaviorExceptionContext<TestSaga, InputMessage, InvalidOperationException> messageContext =
            CreateProxy<IBehaviorExceptionContext<TestSaga, InputMessage, InvalidOperationException>>(out _);
        var stateAddress = new Uri("loopback://state-exception-service");
        var messageAddress = new Uri("loopback://message-exception-service");
        object? observedStateContext = null;
        object? observedMessageContext = null;

        ServiceAddressExceptionProvider<TestSaga, InvalidOperationException> stateProvider = context =>
        {
            observedStateContext = context;
            return stateAddress;
        };
        ServiceAddressExceptionProvider<TestSaga, InputMessage, InvalidOperationException> messageProvider = context =>
        {
            observedMessageContext = context;
            return messageAddress;
        };

        Assert.Same(stateAddress, stateProvider(stateContext));
        Assert.Same(stateContext, observedStateContext);
        Assert.Same(messageAddress, messageProvider(messageContext));
        Assert.Same(messageContext, observedMessageContext);
    }

    private static T CreateProxy<T>(out RecordingProxy recorder)
        where T : class
    {
        T proxy = DispatchProxy.Create<T, RecordingProxy>();
        recorder = (RecordingProxy)(object)proxy;
        return proxy;
    }

    private static void AssertArgument(string parameterName, Action action)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    private static void AssertDelegateShape(Type delegateType, Type contextType)
    {
        MethodInfo invoke = delegateType.GetMethod("Invoke")!;
        Assert.Equal(typeof(Uri), invoke.ReturnType);
        ParameterInfo parameter = Assert.Single(invoke.GetParameters());
        Assert.Equal("context", parameter.Name);
        Assert.Equal(contextType, parameter.ParameterType);
    }

    private static void AssertGenericParameter(
        Type parameter,
        GenericParameterAttributes attributes,
        params Type[] constraints)
    {
        Assert.True(parameter.IsGenericParameter);
        Assert.Equal(attributes, parameter.GenericParameterAttributes);
        Assert.Equal(constraints, parameter.GetGenericParameterConstraints());
    }

    private class RecordingProxy : DispatchProxy
    {
        public List<MethodInfo> Invocations { get; } = [];
        public List<object> Arguments { get; } = [];
        public object? ReturnValue { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            MethodInfo method = targetMethod ?? throw new InvalidOperationException("A proxied invocation requires method metadata.");
            Invocations.Add(method);

            if (args is { Length: > 0 } && args[0] is not null)
                Arguments.Add(args[0]!);

            return ReturnValue ?? DefaultValue(method.ReturnType);
        }

        private static object? DefaultValue(Type type) =>
            type == typeof(void)
                ? null
                : type == typeof(Task)
                    ? Task.CompletedTask
                    : type.IsValueType
                        ? Activator.CreateInstance(type)
                        : null;
    }

    private sealed class TestSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = "Initial";
    }

    private sealed record InputMessage(string Value = "input");
    private sealed record RequestMessage(string Value = "request");
    private sealed record ResponseMessage(string Value);
}
