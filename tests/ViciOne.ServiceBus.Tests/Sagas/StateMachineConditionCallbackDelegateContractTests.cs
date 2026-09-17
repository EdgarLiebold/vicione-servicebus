using System.Reflection;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class StateMachineConditionCallbackDelegateContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DELEGATE-CONTRACTS", "synchronous-conditions-preserve-context-and-results")]
    public void SynchronousConditions_PreserveContextIdentityAndBooleanResults()
    {
        IBehaviorContext<DelegateSaga> stateContext = CreateContext<IBehaviorContext<DelegateSaga>>();
        IBehaviorContext<DelegateSaga, InputMessage> messageContext = CreateContext<IBehaviorContext<DelegateSaga, InputMessage>>();
        IBehaviorExceptionContext<DelegateSaga, InvalidOperationException> exceptionContext =
            CreateContext<IBehaviorExceptionContext<DelegateSaga, InvalidOperationException>>();
        IBehaviorExceptionContext<DelegateSaga, InputMessage, InvalidOperationException> messageExceptionContext =
            CreateContext<IBehaviorExceptionContext<DelegateSaga, InputMessage, InvalidOperationException>>();
        object? observedStateContext = null;
        object? observedMessageContext = null;
        object? observedExceptionContext = null;
        object? observedMessageExceptionContext = null;

        StateMachineCondition<DelegateSaga> stateCondition = context =>
        {
            observedStateContext = context;
            return true;
        };
        StateMachineCondition<DelegateSaga, InputMessage> messageCondition = context =>
        {
            observedMessageContext = context;
            return false;
        };
        StateMachineExceptionCondition<DelegateSaga, InvalidOperationException> exceptionCondition = context =>
        {
            observedExceptionContext = context;
            return false;
        };
        StateMachineExceptionCondition<DelegateSaga, InputMessage, InvalidOperationException> messageExceptionCondition = context =>
        {
            observedMessageExceptionContext = context;
            return true;
        };

        Assert.True(stateCondition(stateContext));
        Assert.Same(stateContext, observedStateContext);
        Assert.False(messageCondition(messageContext));
        Assert.Same(messageContext, observedMessageContext);
        Assert.False(exceptionCondition(exceptionContext));
        Assert.Same(exceptionContext, observedExceptionContext);
        Assert.True(messageExceptionCondition(messageExceptionContext));
        Assert.Same(messageExceptionContext, observedMessageExceptionContext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DELEGATE-CONTRACTS", "asynchronous-conditions-preserve-context-task-and-results")]
    public async Task AsynchronousConditions_PreserveContextTaskIdentityAndBooleanResultsAsync()
    {
        IBehaviorContext<DelegateSaga> stateContext = CreateContext<IBehaviorContext<DelegateSaga>>();
        IBehaviorContext<DelegateSaga, InputMessage> messageContext = CreateContext<IBehaviorContext<DelegateSaga, InputMessage>>();
        IBehaviorExceptionContext<DelegateSaga, InvalidOperationException> exceptionContext =
            CreateContext<IBehaviorExceptionContext<DelegateSaga, InvalidOperationException>>();
        IBehaviorExceptionContext<DelegateSaga, InputMessage, InvalidOperationException> messageExceptionContext =
            CreateContext<IBehaviorExceptionContext<DelegateSaga, InputMessage, InvalidOperationException>>();
        Task<bool> stateTask = Task.FromResult(true);
        Task<bool> messageTask = Task.FromResult(false);
        Task<bool> exceptionTask = Task.FromResult(false);
        Task<bool> messageExceptionTask = Task.FromResult(true);
        object? observedStateContext = null;
        object? observedMessageContext = null;
        object? observedExceptionContext = null;
        object? observedMessageExceptionContext = null;

        StateMachineAsyncCondition<DelegateSaga> stateCondition = context =>
        {
            observedStateContext = context;
            return stateTask;
        };
        StateMachineAsyncCondition<DelegateSaga, InputMessage> messageCondition = context =>
        {
            observedMessageContext = context;
            return messageTask;
        };
        StateMachineAsyncExceptionCondition<DelegateSaga, InvalidOperationException> exceptionCondition = context =>
        {
            observedExceptionContext = context;
            return exceptionTask;
        };
        StateMachineAsyncExceptionCondition<DelegateSaga, InputMessage, InvalidOperationException> messageExceptionCondition = context =>
        {
            observedMessageExceptionContext = context;
            return messageExceptionTask;
        };

        Task<bool> returnedStateTask = stateCondition(stateContext);
        Assert.Same(stateTask, returnedStateTask);
        Assert.True(await returnedStateTask);
        Assert.Same(stateContext, observedStateContext);

        Task<bool> returnedMessageTask = messageCondition(messageContext);
        Assert.Same(messageTask, returnedMessageTask);
        Assert.False(await returnedMessageTask);
        Assert.Same(messageContext, observedMessageContext);

        Task<bool> returnedExceptionTask = exceptionCondition(exceptionContext);
        Assert.Same(exceptionTask, returnedExceptionTask);
        Assert.False(await returnedExceptionTask);
        Assert.Same(exceptionContext, observedExceptionContext);

        Task<bool> returnedMessageExceptionTask = messageExceptionCondition(messageExceptionContext);
        Assert.Same(messageExceptionTask, returnedMessageExceptionTask);
        Assert.True(await returnedMessageExceptionTask);
        Assert.Same(messageExceptionContext, observedMessageExceptionContext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DELEGATE-CONTRACTS", "asynchronous-conditions-preserve-fault-cancellation-and-null-task-shapes")]
    public async Task AsynchronousConditions_PreserveFaultCancellationAndNullTaskShapesAsync()
    {
        IBehaviorContext<DelegateSaga> stateContext = CreateContext<IBehaviorContext<DelegateSaga>>();
        IBehaviorContext<DelegateSaga, InputMessage> messageContext = CreateContext<IBehaviorContext<DelegateSaga, InputMessage>>();
        IBehaviorExceptionContext<DelegateSaga, InvalidOperationException> exceptionContext =
            CreateContext<IBehaviorExceptionContext<DelegateSaga, InvalidOperationException>>();
        IBehaviorExceptionContext<DelegateSaga, InputMessage, InvalidOperationException> messageExceptionContext =
            CreateContext<IBehaviorExceptionContext<DelegateSaga, InputMessage, InvalidOperationException>>();
        var expectedFailure = new InvalidOperationException("condition-fault");
        var expectedExceptionFailure = new ApplicationException("exception-condition-fault");
        Task<bool> faultedTask = Task.FromException<bool>(expectedFailure);
        Task<bool> exceptionFaultedTask = Task.FromException<bool>(expectedExceptionFailure);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Task<bool> canceledTask = Task.FromCanceled<bool>(cancellation.Token);

        StateMachineAsyncCondition<DelegateSaga> faultedCondition = _ => faultedTask;
        StateMachineAsyncCondition<DelegateSaga, InputMessage> canceledCondition = _ => canceledTask;
        StateMachineAsyncExceptionCondition<DelegateSaga, InvalidOperationException> nullTaskCondition = _ => null!;
        StateMachineAsyncExceptionCondition<DelegateSaga, InputMessage, InvalidOperationException> exceptionFaultedCondition = _ =>
            exceptionFaultedTask;

        Task<bool> returnedFaultedTask = faultedCondition(stateContext);
        Assert.Same(faultedTask, returnedFaultedTask);
        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => returnedFaultedTask);
        Assert.Same(expectedFailure, failure);

        Task<bool> returnedCanceledTask = canceledCondition(messageContext);
        Assert.Same(canceledTask, returnedCanceledTask);
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => returnedCanceledTask);
        Assert.Equal(cancellation.Token, canceled.CancellationToken);

        Assert.Null(nullTaskCondition(exceptionContext));

        Task<bool> returnedExceptionFaultedTask = exceptionFaultedCondition(messageExceptionContext);
        Assert.Same(exceptionFaultedTask, returnedExceptionFaultedTask);
        ApplicationException exceptionFailure = await Assert.ThrowsAsync<ApplicationException>(() => returnedExceptionFaultedTask);
        Assert.Same(expectedExceptionFailure, exceptionFailure);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DELEGATE-CONTRACTS", "send-callbacks-preserve-behavior-and-send-context-identity")]
    public void SendCallbacks_PreserveBehaviorAndSendContextIdentity()
    {
        IBehaviorContext<DelegateSaga> stateContext = CreateContext<IBehaviorContext<DelegateSaga>>();
        IBehaviorContext<DelegateSaga, InputMessage> messageContext = CreateContext<IBehaviorContext<DelegateSaga, InputMessage>>();
        SendContext<OutputMessage> stateSendContext = CreateContext<SendContext<OutputMessage>>();
        SendContext<OutputMessage> messageSendContext = CreateContext<SendContext<OutputMessage>>();
        object? observedStateContext = null;
        object? observedStateSendContext = null;
        object? observedMessageContext = null;
        object? observedMessageSendContext = null;

        SendContextCallback<DelegateSaga, OutputMessage> stateCallback = (context, sendContext) =>
        {
            observedStateContext = context;
            observedStateSendContext = sendContext;
        };
        SendContextCallback<DelegateSaga, InputMessage, OutputMessage> messageCallback = (context, sendContext) =>
        {
            observedMessageContext = context;
            observedMessageSendContext = sendContext;
        };

        stateCallback(stateContext, stateSendContext);
        messageCallback(messageContext, messageSendContext);

        Assert.Same(stateContext, observedStateContext);
        Assert.Same(stateSendContext, observedStateSendContext);
        Assert.Same(messageContext, observedMessageContext);
        Assert.Same(messageSendContext, observedMessageSendContext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DELEGATE-CONTRACTS", "send-exception-callbacks-preserve-exception-and-send-context-identity")]
    public void SendExceptionCallbacks_PreserveExceptionAndSendContextIdentity()
    {
        IBehaviorExceptionContext<DelegateSaga, InvalidOperationException> exceptionContext =
            CreateContext<IBehaviorExceptionContext<DelegateSaga, InvalidOperationException>>();
        IBehaviorExceptionContext<DelegateSaga, InputMessage, InvalidOperationException> messageExceptionContext =
            CreateContext<IBehaviorExceptionContext<DelegateSaga, InputMessage, InvalidOperationException>>();
        SendContext<OutputMessage> exceptionSendContext = CreateContext<SendContext<OutputMessage>>();
        SendContext<OutputMessage> messageExceptionSendContext = CreateContext<SendContext<OutputMessage>>();
        object? observedExceptionContext = null;
        object? observedExceptionSendContext = null;
        object? observedMessageExceptionContext = null;
        object? observedMessageExceptionSendContext = null;

        SendExceptionContextCallback<DelegateSaga, InvalidOperationException, OutputMessage> exceptionCallback = (context, sendContext) =>
        {
            observedExceptionContext = context;
            observedExceptionSendContext = sendContext;
        };
        SendExceptionContextCallback<DelegateSaga, InputMessage, InvalidOperationException, OutputMessage> messageExceptionCallback =
            (context, sendContext) =>
            {
                observedMessageExceptionContext = context;
                observedMessageExceptionSendContext = sendContext;
            };

        exceptionCallback(exceptionContext, exceptionSendContext);
        messageExceptionCallback(messageExceptionContext, messageExceptionSendContext);

        Assert.Same(exceptionContext, observedExceptionContext);
        Assert.Same(exceptionSendContext, observedExceptionSendContext);
        Assert.Same(messageExceptionContext, observedMessageExceptionContext);
        Assert.Same(messageExceptionSendContext, observedMessageExceptionSendContext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DELEGATE-CONTRACTS", "unhandled-event-callback-preserves-context-and-task-terminal-shapes")]
    public async Task UnhandledEventCallback_PreservesContextTaskIdentityAndTerminalShapesAsync()
    {
        IUnhandledEventContext<DelegateSaga> context = CreateContext<IUnhandledEventContext<DelegateSaga>>();
        Task completedTask = Task.CompletedTask;
        object? observedContext = null;
        UnhandledEventCallback<DelegateSaga> completed = callbackContext =>
        {
            observedContext = callbackContext;
            return completedTask;
        };

        Task returnedCompletedTask = completed(context);
        Assert.Same(completedTask, returnedCompletedTask);
        await returnedCompletedTask;
        Assert.Same(context, observedContext);

        var expectedFailure = new InvalidOperationException("unhandled-event-fault");
        Task faultedTask = Task.FromException(expectedFailure);
        UnhandledEventCallback<DelegateSaga> faulted = _ => faultedTask;
        Task returnedFaultedTask = faulted(context);
        Assert.Same(faultedTask, returnedFaultedTask);
        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => returnedFaultedTask);
        Assert.Same(expectedFailure, failure);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Task canceledTask = Task.FromCanceled(cancellation.Token);
        UnhandledEventCallback<DelegateSaga> canceled = _ => canceledTask;
        Task returnedCanceledTask = canceled(context);
        Assert.Same(canceledTask, returnedCanceledTask);
        OperationCanceledException canceledFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => returnedCanceledTask);
        Assert.Equal(cancellation.Token, canceledFailure.CancellationToken);

        UnhandledEventCallback<DelegateSaga> nullTask = _ => null!;
        Assert.Null(nullTask(context));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DELEGATE-CONTRACTS", "condition-callback-signatures-variance-and-constraints-remain-compatible")]
    public void DelegateDefinitions_DeclareExactSignaturesVarianceAndGenericConstraints()
    {
        DelegateContract[] contracts =
        [
            Contract(typeof(StateMachineAsyncCondition<>), typeof(Task<bool>), [typeof(IBehaviorContext<>)], Saga()),
            Contract(typeof(StateMachineAsyncCondition<,>), typeof(Task<bool>), [typeof(IBehaviorContext<,>)], Saga(), MessageInput()),
            Contract(typeof(StateMachineAsyncExceptionCondition<,>), typeof(Task<bool>), [typeof(IBehaviorExceptionContext<,>)],
                Saga(), ExceptionInput()),
            Contract(typeof(StateMachineAsyncExceptionCondition<,,>), typeof(Task<bool>), [typeof(IBehaviorExceptionContext<,,>)],
                Saga(), MessageInput(), ExceptionInput()),
            Contract(typeof(StateMachineCondition<>), typeof(bool), [typeof(IBehaviorContext<>)], Saga()),
            Contract(typeof(StateMachineCondition<,>), typeof(bool), [typeof(IBehaviorContext<,>)], Saga(), MessageInput()),
            Contract(typeof(StateMachineExceptionCondition<,>), typeof(bool), [typeof(IBehaviorExceptionContext<,>)],
                Saga(), ExceptionInput()),
            Contract(typeof(StateMachineExceptionCondition<,,>), typeof(bool), [typeof(IBehaviorExceptionContext<,,>)],
                Saga(), MessageInput(), ExceptionInput()),
            Contract(typeof(SendContextCallback<,>), typeof(void), [typeof(IBehaviorContext<>), typeof(SendContext<>)],
                Saga(), OutgoingMessageInput()),
            Contract(typeof(SendContextCallback<,,>), typeof(void), [typeof(IBehaviorContext<,>), typeof(SendContext<>)],
                Saga(), MessageInput(), OutgoingMessageInput()),
            Contract(typeof(SendExceptionContextCallback<,,>), typeof(void),
                [typeof(IBehaviorExceptionContext<,>), typeof(SendContext<>)], Saga(), ExceptionInput(), OutgoingMessageInput()),
            Contract(typeof(SendExceptionContextCallback<,,,>), typeof(void),
                [typeof(IBehaviorExceptionContext<,,>), typeof(SendContext<>)], Saga(), MessageInput(), ExceptionInput(), OutgoingMessageInput()),
            Contract(typeof(UnhandledEventCallback<>), typeof(Task), [typeof(IUnhandledEventContext<>)], Saga()),
        ];

        Assert.Equal(13, contracts.Length);
        foreach (DelegateContract contract in contracts)
        {
            Assert.True(contract.Definition.IsGenericTypeDefinition, contract.Definition.FullName);
            Assert.Equal(typeof(MulticastDelegate), contract.Definition.BaseType);
            MethodInfo invoke = Assert.Single(contract.Definition.GetMethods(), method => method.Name == "Invoke");
            Assert.Equal(contract.ReturnType, invoke.ReturnType);
            Assert.Equal(contract.ParameterTypeDefinitions.Length, invoke.GetParameters().Length);

            for (var index = 0; index < contract.ParameterTypeDefinitions.Length; index++)
            {
                Type actualParameterType = invoke.GetParameters()[index].ParameterType;
                Assert.True(actualParameterType.IsGenericType, $"{contract.Definition}.Invoke parameter {index}");
                Assert.Equal(contract.ParameterTypeDefinitions[index], actualParameterType.GetGenericTypeDefinition());
            }

            Type[] genericParameters = contract.Definition.GetGenericArguments();
            Assert.Equal(contract.GenericParameters.Length, genericParameters.Length);
            for (var index = 0; index < genericParameters.Length; index++)
            {
                GenericParameterContract expected = contract.GenericParameters[index];
                Type actual = genericParameters[index];
                Assert.Equal(expected.Attributes, actual.GenericParameterAttributes);
                Assert.Equal(expected.TypeConstraints, actual.GetGenericParameterConstraints());
            }
        }
    }

    private static TContext CreateContext<TContext>()
        where TContext : class => DispatchProxy.Create<TContext, ContextProxy>();

    private static DelegateContract Contract(Type definition, Type returnType, Type[] parameterTypeDefinitions,
        params GenericParameterContract[] genericParameters) =>
        new(definition, returnType, parameterTypeDefinitions, genericParameters);

    private static GenericParameterContract Saga() =>
        new(GenericParameterAttributes.ReferenceTypeConstraint, [typeof(ISagaStateMachineInstance)]);

    private static GenericParameterContract MessageInput() =>
        new(GenericParameterAttributes.Contravariant | GenericParameterAttributes.ReferenceTypeConstraint, []);

    private static GenericParameterContract ExceptionInput() =>
        new(GenericParameterAttributes.Contravariant, [typeof(Exception)]);

    private static GenericParameterContract OutgoingMessageInput() =>
        new(GenericParameterAttributes.Contravariant | GenericParameterAttributes.ReferenceTypeConstraint, []);

    private sealed record DelegateContract(Type Definition, Type ReturnType, Type[] ParameterTypeDefinitions,
        GenericParameterContract[] GenericParameters);

    private sealed record GenericParameterContract(GenericParameterAttributes Attributes, Type[] TypeConstraints);

    private class ContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException($"The context member '{targetMethod?.Name}' is not used by these delegate contracts.");
    }

    private sealed class DelegateSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = "Initial";
    }

    private sealed record InputMessage(string Value = "input");

    private sealed record OutputMessage(string Value = "output");
}
