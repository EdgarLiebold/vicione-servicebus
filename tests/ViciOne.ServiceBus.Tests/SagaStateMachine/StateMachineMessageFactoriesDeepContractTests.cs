using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineMessageFactoriesDeepContractTests
{
    const string HeaderName = "message-factory-stage";

    static readonly NullabilityInfoContext Nullability = new();

    static readonly string[] ExpectedCreateSignatures =
    [
        "TaskMessageFactory<OutputMessage> Create(OutputMessage)",
        "TaskMessageFactory<OutputMessage> Create(OutputMessage,IPipe<SendContext<OutputMessage>>)",
        "TaskMessageFactory<OutputMessage> Create(OutputMessage,Action<SendContext<OutputMessage>>)",
        "TaskMessageFactory<OutputMessage> Create(Task<OutputMessage>)",
        "TaskMessageFactory<OutputMessage> Create(Task<OutputMessage>,IPipe<SendContext<OutputMessage>>)",
        "TaskMessageFactory<OutputMessage> Create(Task<OutputMessage>,Action<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorContext<TSaga,TMessage>,OutputMessage> Create<TSaga,TMessage>(OutputMessage,SendContextCallback<TSaga,TMessage,OutputMessage>)",
        "ContextMessageFactory<IBehaviorContext<TSaga,TMessage>,OutputMessage> Create<TSaga,TMessage>(Task<OutputMessage>,SendContextCallback<TSaga,TMessage,OutputMessage>)",
        "ContextMessageFactory<IBehaviorContext<TSaga,TMessage>,OutputMessage> Create<TSaga,TMessage>(Func<IBehaviorContext<TSaga,TMessage>,Task<InitializedMessage<OutputMessage>>>)",
        "ContextMessageFactory<IBehaviorContext<TSaga,TMessage>,OutputMessage> Create<TSaga,TMessage>(Func<IBehaviorContext<TSaga,TMessage>,Task<InitializedMessage<OutputMessage>>>,Action<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorContext<TSaga,TMessage>,OutputMessage> Create<TSaga,TMessage>(Func<IBehaviorContext<TSaga,TMessage>,Task<InitializedMessage<OutputMessage>>>,SendContextCallback<TSaga,TMessage,OutputMessage>)",
        "ContextMessageFactory<IBehaviorContext<TSaga,TMessage>,OutputMessage> Create<TSaga,TMessage>(AsyncEventMessageFactory<TSaga,TMessage,OutputMessage>)",
        "ContextMessageFactory<IBehaviorContext<TSaga,TMessage>,OutputMessage> Create<TSaga,TMessage>(AsyncEventMessageFactory<TSaga,TMessage,OutputMessage>,IPipe<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorContext<TSaga,TMessage>,OutputMessage> Create<TSaga,TMessage>(AsyncEventMessageFactory<TSaga,TMessage,OutputMessage>,Action<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorContext<TSaga,TMessage>,OutputMessage> Create<TSaga,TMessage>(AsyncEventMessageFactory<TSaga,TMessage,OutputMessage>,SendContextCallback<TSaga,TMessage,OutputMessage>)",
        "ContextMessageFactory<IBehaviorContext<TSaga,TMessage>,OutputMessage> Create<TSaga,TMessage>(EventMessageFactory<TSaga,TMessage,OutputMessage>)",
        "ContextMessageFactory<IBehaviorContext<TSaga,TMessage>,OutputMessage> Create<TSaga,TMessage>(EventMessageFactory<TSaga,TMessage,OutputMessage>,IPipe<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorContext<TSaga,TMessage>,OutputMessage> Create<TSaga,TMessage>(EventMessageFactory<TSaga,TMessage,OutputMessage>,Action<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorContext<TSaga,TMessage>,OutputMessage> Create<TSaga,TMessage>(EventMessageFactory<TSaga,TMessage,OutputMessage>,SendContextCallback<TSaga,TMessage,OutputMessage>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TMessage,TException>,OutputMessage> Create<TSaga,TMessage,TException>(OutputMessage,SendExceptionContextCallback<TSaga,TMessage,TException,OutputMessage>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TMessage,TException>,OutputMessage> Create<TSaga,TMessage,TException>(Task<OutputMessage>,SendExceptionContextCallback<TSaga,TMessage,TException,OutputMessage>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TMessage,TException>,OutputMessage> Create<TSaga,TMessage,TException>(Func<IBehaviorExceptionContext<TSaga,TMessage,TException>,Task<InitializedMessage<OutputMessage>>>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TMessage,TException>,OutputMessage> Create<TSaga,TMessage,TException>(Func<IBehaviorExceptionContext<TSaga,TMessage,TException>,Task<InitializedMessage<OutputMessage>>>,Action<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TMessage,TException>,OutputMessage> Create<TSaga,TMessage,TException>(Func<IBehaviorExceptionContext<TSaga,TMessage,TException>,Task<InitializedMessage<OutputMessage>>>,SendExceptionContextCallback<TSaga,TMessage,TException,OutputMessage>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TMessage,TException>,OutputMessage> Create<TSaga,TMessage,TException>(AsyncEventExceptionMessageFactory<TSaga,TMessage,TException,OutputMessage>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TMessage,TException>,OutputMessage> Create<TSaga,TMessage,TException>(AsyncEventExceptionMessageFactory<TSaga,TMessage,TException,OutputMessage>,IPipe<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TMessage,TException>,OutputMessage> Create<TSaga,TMessage,TException>(AsyncEventExceptionMessageFactory<TSaga,TMessage,TException,OutputMessage>,Action<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TMessage,TException>,OutputMessage> Create<TSaga,TMessage,TException>(AsyncEventExceptionMessageFactory<TSaga,TMessage,TException,OutputMessage>,SendExceptionContextCallback<TSaga,TMessage,TException,OutputMessage>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TMessage,TException>,OutputMessage> Create<TSaga,TMessage,TException>(EventExceptionMessageFactory<TSaga,TMessage,TException,OutputMessage>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TMessage,TException>,OutputMessage> Create<TSaga,TMessage,TException>(EventExceptionMessageFactory<TSaga,TMessage,TException,OutputMessage>,IPipe<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TMessage,TException>,OutputMessage> Create<TSaga,TMessage,TException>(EventExceptionMessageFactory<TSaga,TMessage,TException,OutputMessage>,Action<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TMessage,TException>,OutputMessage> Create<TSaga,TMessage,TException>(EventExceptionMessageFactory<TSaga,TMessage,TException,OutputMessage>,SendExceptionContextCallback<TSaga,TMessage,TException,OutputMessage>)",
        "ContextMessageFactory<IBehaviorContext<TSaga>,OutputMessage> Create<TSaga>(OutputMessage,SendContextCallback<TSaga,OutputMessage>)",
        "ContextMessageFactory<IBehaviorContext<TSaga>,OutputMessage> Create<TSaga>(Task<OutputMessage>,SendContextCallback<TSaga,OutputMessage>)",
        "ContextMessageFactory<IBehaviorContext<TSaga>,OutputMessage> Create<TSaga>(Func<IBehaviorContext<TSaga>,Task<InitializedMessage<OutputMessage>>>)",
        "ContextMessageFactory<IBehaviorContext<TSaga>,OutputMessage> Create<TSaga>(Func<IBehaviorContext<TSaga>,Task<InitializedMessage<OutputMessage>>>,Action<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorContext<TSaga>,OutputMessage> Create<TSaga>(Func<IBehaviorContext<TSaga>,Task<InitializedMessage<OutputMessage>>>,SendContextCallback<TSaga,OutputMessage>)",
        "ContextMessageFactory<IBehaviorContext<TSaga>,OutputMessage> Create<TSaga>(AsyncEventMessageFactory<TSaga,OutputMessage>)",
        "ContextMessageFactory<IBehaviorContext<TSaga>,OutputMessage> Create<TSaga>(AsyncEventMessageFactory<TSaga,OutputMessage>,IPipe<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorContext<TSaga>,OutputMessage> Create<TSaga>(AsyncEventMessageFactory<TSaga,OutputMessage>,Action<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorContext<TSaga>,OutputMessage> Create<TSaga>(AsyncEventMessageFactory<TSaga,OutputMessage>,SendContextCallback<TSaga,OutputMessage>)",
        "ContextMessageFactory<IBehaviorContext<TSaga>,OutputMessage> Create<TSaga>(EventMessageFactory<TSaga,OutputMessage>)",
        "ContextMessageFactory<IBehaviorContext<TSaga>,OutputMessage> Create<TSaga>(EventMessageFactory<TSaga,OutputMessage>,IPipe<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorContext<TSaga>,OutputMessage> Create<TSaga>(EventMessageFactory<TSaga,OutputMessage>,Action<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorContext<TSaga>,OutputMessage> Create<TSaga>(EventMessageFactory<TSaga,OutputMessage>,SendContextCallback<TSaga,OutputMessage>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TException>,OutputMessage> Create<TSaga,TException>(OutputMessage,SendExceptionContextCallback<TSaga,TException,OutputMessage>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TException>,OutputMessage> Create<TSaga,TException>(Task<OutputMessage>,SendExceptionContextCallback<TSaga,TException,OutputMessage>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TException>,OutputMessage> Create<TSaga,TException>(Func<IBehaviorExceptionContext<TSaga,TException>,Task<InitializedMessage<OutputMessage>>>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TException>,OutputMessage> Create<TSaga,TException>(Func<IBehaviorExceptionContext<TSaga,TException>,Task<InitializedMessage<OutputMessage>>>,Action<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TException>,OutputMessage> Create<TSaga,TException>(Func<IBehaviorExceptionContext<TSaga,TException>,Task<InitializedMessage<OutputMessage>>>,SendExceptionContextCallback<TSaga,TException,OutputMessage>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TException>,OutputMessage> Create<TSaga,TException>(AsyncEventExceptionMessageFactory<TSaga,TException,OutputMessage>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TException>,OutputMessage> Create<TSaga,TException>(AsyncEventExceptionMessageFactory<TSaga,TException,OutputMessage>,IPipe<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TException>,OutputMessage> Create<TSaga,TException>(AsyncEventExceptionMessageFactory<TSaga,TException,OutputMessage>,Action<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TException>,OutputMessage> Create<TSaga,TException>(AsyncEventExceptionMessageFactory<TSaga,TException,OutputMessage>,SendExceptionContextCallback<TSaga,TException,OutputMessage>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TException>,OutputMessage> Create<TSaga,TException>(EventExceptionMessageFactory<TSaga,TException,OutputMessage>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TException>,OutputMessage> Create<TSaga,TException>(EventExceptionMessageFactory<TSaga,TException,OutputMessage>,IPipe<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TException>,OutputMessage> Create<TSaga,TException>(EventExceptionMessageFactory<TSaga,TException,OutputMessage>,Action<SendContext<OutputMessage>>)",
        "ContextMessageFactory<IBehaviorExceptionContext<TSaga,TException>,OutputMessage> Create<TSaga,TException>(EventExceptionMessageFactory<TSaga,TException,OutputMessage>,SendExceptionContextCallback<TSaga,TException,OutputMessage>)"
    ];

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-214-message-factory-public-surface-nullability-async-names")]
    public void PublicSurface_UsesExactFactoryCountNullabilityAndAsyncNames()
    {
        Type messageFactory = typeof(MessageFactory<OutputMessage>);
        MethodInfo[] createMethods = messageFactory.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        Assert.True(messageFactory.IsPublic);
        Assert.True(messageFactory.IsAbstract);
        Assert.True(messageFactory.IsSealed);
        AssertReferenceTypeParameter(messageFactory.GetGenericTypeDefinition(), "T");
        Assert.Empty(messageFactory.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
        Assert.Empty(messageFactory.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
        Assert.Empty(messageFactory.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
        Assert.Empty(messageFactory.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
        Assert.Equal(58, createMethods.Length);
        Assert.Equal(ExpectedCreateSignatures.OrderBy(value => value, StringComparer.Ordinal),
            createMethods.Select(DescribeMethod).OrderBy(value => value, StringComparer.Ordinal));
        Assert.All(createMethods, method =>
        {
            Assert.Equal("Create", method.Name);
            Assert.False(IsTask(method.ReturnType));
            Assert.Equal(NullabilityState.NotNull, Nullability.Create(method.ReturnParameter).ReadState);
            AssertNotNullableRecursively(Nullability.Create(method.ReturnParameter));
            AssertGenericConstraints(method);

            ParameterInfo[] parameters = method.GetParameters();
            Assert.InRange(parameters.Length, 1, 2);
            Assert.Equal(parameters[0].ParameterType == typeof(OutputMessage) ? "message" : "factory", parameters[0].Name);
            Assert.Equal(NullabilityState.NotNull, Nullability.Create(parameters[0]).ReadState);
            AssertNotNullableRecursively(Nullability.Create(parameters[0]));
            if (parameters.Length == 2)
            {
                Assert.Equal(IsPipe(parameters[1].ParameterType) ? "pipe" : "callback", parameters[1].Name);
                NullabilityInfo optional = Nullability.Create(parameters[1]);
                Assert.Equal(NullabilityState.Nullable, optional.ReadState);
                Assert.Equal(NullabilityState.Nullable, optional.WriteState);
                AssertNestedGenericArgumentsNotNullable(optional);
            }
        });

        AssertFactorySurface(
            typeof(TaskMessageFactory<OutputMessage>),
            [
                "Task<InitializedMessage<OutputMessage>> GetMessageAsync(CancellationToken)",
                "Task UseAsync(Func<InitializedMessage<OutputMessage>,Task>,CancellationToken)"
            ],
            [typeof(Task<InitializedMessage<OutputMessage>>)]);
        AssertReferenceTypeParameter(typeof(TaskMessageFactory<>), "T");
        Type contextFactory = typeof(ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage>);
        AssertReferenceTypeParameter(typeof(ContextMessageFactory<,>), "TContext", typeof(ConsumeContext));
        AssertReferenceTypeParameter(typeof(ContextMessageFactory<,>), "T");
        Assert.Empty(typeof(TaskMessageFactory<OutputMessage>)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));
        MethodInfo conversion = Assert.Single(
            contextFactory.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));
        Assert.True(conversion.IsSpecialName);
        Assert.Equal(
            "ContextMessageFactory<IBehaviorContext<TestSaga,InputMessage>,OutputMessage> op_Implicit(TaskMessageFactory<OutputMessage>)",
            DescribeMethod(conversion));
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(conversion.ReturnParameter).ReadState);
        AssertNotNullableRecursively(Nullability.Create(conversion.ReturnParameter));
        ParameterInfo conversionFactory = Assert.Single(conversion.GetParameters());
        Assert.Equal("factory", conversionFactory.Name);
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(conversionFactory).ReadState);
        AssertNotNullableRecursively(Nullability.Create(conversionFactory));
        Assert.False(conversion.IsVirtual);
        Assert.False(conversion.IsFinal);
        Assert.Same(conversion, conversion.GetBaseDefinition());
        AssertFactorySurface(
            contextFactory,
            [
                "Task<InitializedMessage<OutputMessage>> GetMessageAsync(IBehaviorContext<TestSaga,InputMessage>,CancellationToken)",
                "Task UseAsync(IBehaviorContext<TestSaga,InputMessage>,Func<IBehaviorContext<TestSaga,InputMessage>,InitializedMessage<OutputMessage>,Task>,CancellationToken)",
                "Task<TResult> UseAsync<TResult>(IBehaviorContext<TestSaga,InputMessage>,Func<IBehaviorContext<TestSaga,InputMessage>,InitializedMessage<OutputMessage>,Task<TResult>>,CancellationToken)"
            ],
            [typeof(Func<IBehaviorContext<TestSaga, InputMessage>, Task<InitializedMessage<OutputMessage>>>)]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-214-message-factory-all-overloads-context-pipe-callback-compatibility")]
    public async Task CreateOverloads_PreserveMessagesContextsPipesCallbacksAndOptionalNullCompatibilityAsync()
    {
        MethodInfo[] methods = typeof(MessageFactory<OutputMessage>)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        foreach (MethodInfo definition in methods)
        {
            MethodInfo method = Close(definition);
            await VerifySuccessfulOverloadAsync(method, configureOptionalParameter: true);
            if (HasAsynchronousSource(method))
                await VerifyPendingSuccessfulOverloadAsync(method, configureOptionalParameter: true);

            if (method.GetParameters().Length == 2)
            {
                await VerifySuccessfulOverloadAsync(method, configureOptionalParameter: false);
                if (HasAsynchronousSource(method))
                    await VerifyPendingSuccessfulOverloadAsync(method, configureOptionalParameter: false);
            }
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-214-message-factory-async-source-failure-cancellation-identity")]
    public async Task AsyncCreateOverloads_PreserveFactoryFailureAndCancellationIdentityAsync()
    {
        MethodInfo[] methods = typeof(MessageFactory<OutputMessage>)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(Close)
            .Where(HasAsynchronousSource)
            .ToArray();
        var expectedFailure = new InvalidOperationException("factory failed");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        foreach (MethodInfo method in methods)
        {
            var failureRecorder = new InvocationRecorder();
            object failedFactory = method.Invoke(null,
                CreateArguments(method, failureRecorder, SourceOutcome.Faulted, true, expectedFailure, TestContext.Current.CancellationToken))!;
            InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => GetMessageOnlyAsync(failedFactory));
            Assert.Same(expectedFailure, failure);
            Assert.Empty(failureRecorder.Stages);
            Assert.Equal(IsFactoryDelegate(method.GetParameters()[0].ParameterType) ? 1 : 0, failureRecorder.FactoryCalls);

            var canceledRecorder = new InvocationRecorder();
            object canceledFactory = method.Invoke(null,
                CreateArguments(method, canceledRecorder, SourceOutcome.Canceled, true, null, cancellation.Token))!;
            OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => GetMessageOnlyAsync(canceledFactory));
            Assert.Equal(cancellation.Token, canceled.CancellationToken);
            Assert.Empty(canceledRecorder.Stages);
            Assert.Equal(IsFactoryDelegate(method.GetParameters()[0].ParameterType) ? 1 : 0, canceledRecorder.FactoryCalls);
        }

        MethodInfo[] synchronousFactories = typeof(MessageFactory<OutputMessage>)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(Close)
            .Where(HasSynchronousFactorySource)
            .ToArray();
        foreach (MethodInfo method in synchronousFactories)
        {
            var recorder = new InvocationRecorder();
            var failure = new InvalidOperationException($"{DescribeMethod(method)} failed");
            object?[] arguments = CreateThrowingFactoryArguments(method, recorder, failure, configureOptionalParameter: true);
            object factory = method.Invoke(null, arguments)!;
            Assert.Equal(0, recorder.FactoryCalls);

            InvalidOperationException observed = await Assert.ThrowsAsync<InvalidOperationException>(() => GetMessageOnlyAsync(factory));

            Assert.Same(failure, observed);
            Assert.Equal(1, recorder.FactoryCalls);
            Assert.Empty(recorder.Stages);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-214-message-factory-async-factory-null-task-boundary")]
    public async Task ContextCreateOverloads_RejectNullTasksReturnedByEveryAsyncFactoryShapeAsync()
    {
        MethodInfo[] methods = typeof(MessageFactory<OutputMessage>)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(Close)
            .Where(method =>
            {
                Type sourceType = method.GetParameters()[0].ParameterType;
                return IsFactoryDelegate(sourceType) && IsTask(sourceType.GetMethod("Invoke")!.ReturnType);
            })
            .ToArray();

        foreach (MethodInfo method in methods)
        {
            var recorder = new InvocationRecorder();
            object factory = method.Invoke(null,
                CreateArguments(method, recorder, SourceOutcome.NullTask, true, null, TestContext.Current.CancellationToken))!;
            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => GetMessageOnlyAsync(factory));
            Assert.Equal("The message factory returned no task.", error.Message);
            Assert.Empty(recorder.Stages);
            Assert.Equal(1, recorder.FactoryCalls);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-214-message-factory-all-overload-required-input-boundaries")]
    public void EveryCreateOverload_RejectsItsRequiredMessageTaskOrFactoryImmediately()
    {
        MethodInfo[] methods = typeof(MessageFactory<OutputMessage>)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(Close)
            .ToArray();

        foreach (MethodInfo method in methods)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object?[] arguments = new object?[parameters.Length];
            TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, arguments));
            var error = Assert.IsType<ArgumentNullException>(invocation.InnerException);
            Assert.Equal(parameters[0].ParameterType == typeof(OutputMessage) ? "message" : "factory", error.ParamName);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-214-task-message-factory-task-result-callback-identity")]
    public async Task TaskFactory_PreservesTaskResultFailureCancellationAndCallbackIdentityAsync()
    {
        var message = new OutputMessage("task");
        var initialized = new InitializedMessage<OutputMessage>(message);
        Task<InitializedMessage<OutputMessage>> completed = Task.FromResult(initialized);
        var factory = new TaskMessageFactory<OutputMessage>(completed);
        CancellationToken testToken = TestContext.Current.CancellationToken;

        Assert.Same(completed, factory.GetMessageAsync(testToken));
        Assert.Same(message, (await factory.GetMessageAsync(testToken)).Message);

        InitializedMessage<OutputMessage> callbackMessage = default;
        var fastCallbackCalls = 0;
        var callbackCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task use = factory.UseAsync(value =>
        {
            fastCallbackCalls++;
            callbackMessage = value;
            return callbackCompletion.Task;
        }, testToken);
        Assert.Same(callbackCompletion.Task, use);
        Assert.Same(message, callbackMessage.Message);
        callbackCompletion.SetResult();
        await use;
        Assert.Equal(1, fastCallbackCalls);

        var callbackFailure = new InvalidOperationException("callback failed");
        Task callbackFailureTask = Task.FromException(callbackFailure);
        Task failedCallback = factory.UseAsync(_ => callbackFailureTask, testToken);
        Assert.Same(callbackFailureTask, failedCallback);
        Assert.Same(callbackFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => failedCallback));

        using var callbackCancellation = new CancellationTokenSource();
        callbackCancellation.Cancel();
        Task callbackCanceledTask = Task.FromCanceled(callbackCancellation.Token);
        Task canceledCallback = factory.UseAsync(_ => callbackCanceledTask, testToken);
        Assert.Same(callbackCanceledTask, canceledCallback);
        OperationCanceledException canceledCallbackError = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledCallback);
        Assert.Equal(callbackCancellation.Token, canceledCallbackError.CancellationToken);

        var thrownCallbackFailure = new InvalidOperationException("callback threw");
        InvalidOperationException thrownCallbackError = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = factory.UseAsync(_ => throw thrownCallbackFailure, testToken);
        });
        Assert.Same(thrownCallbackFailure, thrownCallbackError);

        var expectedFailure = new InvalidOperationException("source failed");
        Task<InitializedMessage<OutputMessage>> failed = Task.FromException<InitializedMessage<OutputMessage>>(expectedFailure);
        var failedFactory = new TaskMessageFactory<OutputMessage>(failed);
        Assert.Same(failed, failedFactory.GetMessageAsync(testToken));
        Assert.Same(expectedFailure, await Assert.ThrowsAsync<InvalidOperationException>(
            () => failedFactory.UseAsync(_ => Task.CompletedTask, testToken)));

        using var sourceCancellation = new CancellationTokenSource();
        sourceCancellation.Cancel();
        Task<InitializedMessage<OutputMessage>> sourceCanceled = Task.FromCanceled<InitializedMessage<OutputMessage>>(sourceCancellation.Token);
        var sourceCanceledFactory = new TaskMessageFactory<OutputMessage>(sourceCanceled);
        Assert.Same(sourceCanceled, sourceCanceledFactory.GetMessageAsync(testToken));
        OperationCanceledException sourceCanceledError = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sourceCanceledFactory.UseAsync(_ => Task.CompletedTask, testToken));
        Assert.Equal(sourceCancellation.Token, sourceCanceledError.CancellationToken);

        using var callerCancellation = new CancellationTokenSource();
        callerCancellation.Cancel();
        OperationCanceledException canceledGetError = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => failedFactory.GetMessageAsync(callerCancellation.Token));
        Assert.Equal(callerCancellation.Token, canceledGetError.CancellationToken);
        var callbackCalls = 0;
        Task callerCanceled = factory.UseAsync(_ =>
        {
            callbackCalls++;
            return Task.CompletedTask;
        }, callerCancellation.Token);
        OperationCanceledException callerCanceledError = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => callerCanceled);
        Assert.Equal(callerCancellation.Token, callerCanceledError.CancellationToken);
        Assert.Equal(0, callbackCalls);

        await VerifyTaskFactoryPendingCallbacksAsync(initialized, testToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-214-context-message-factory-context-task-use-identity")]
    public async Task ContextFactory_PreservesContextTaskResultFailureCancellationAndUseOutcomesAsync()
    {
        IBehaviorContext<TestSaga, InputMessage> context = CreateContext<IBehaviorContext<TestSaga, InputMessage>>();
        var message = new OutputMessage("context");
        var initialized = new InitializedMessage<OutputMessage>(message);
        Task<InitializedMessage<OutputMessage>> completed = Task.FromResult(initialized);
        CancellationToken testToken = TestContext.Current.CancellationToken;
        object? observedContext = null;
        var factory = new ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage>(value =>
        {
            observedContext = value;
            return completed;
        });

        Assert.Same(completed, factory.GetMessageAsync(context, testToken));
        Assert.Same(context, observedContext);

        var useCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fastUseCalls = 0;
        Task use = factory.UseAsync(context, (callbackContext, value) =>
        {
            fastUseCalls++;
            Assert.Same(context, callbackContext);
            Assert.Same(message, value.Message);
            return useCompletion.Task;
        }, testToken);
        Assert.Same(useCompletion.Task, use);
        useCompletion.SetResult();
        await use;
        Assert.Equal(1, fastUseCalls);

        Task<int> resultTask = Task.FromResult(73);
        var fastResultCalls = 0;
        Task<int> result = factory.UseAsync(context, (callbackContext, value) =>
        {
            fastResultCalls++;
            Assert.Same(context, callbackContext);
            Assert.Same(message, value.Message);
            return resultTask;
        }, testToken);
        Assert.Same(resultTask, result);
        Assert.Equal(73, await result);
        Assert.Equal(1, fastResultCalls);

        var callbackFailure = new InvalidOperationException("context callback failed");
        Task failedCallbackTask = Task.FromException(callbackFailure);
        Task failedCallback = factory.UseAsync(context, (_, _) => failedCallbackTask, testToken);
        Assert.Same(failedCallbackTask, failedCallback);
        Assert.Same(callbackFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => failedCallback));
        Task<int> failedResultTask = Task.FromException<int>(callbackFailure);
        Task<int> failedResult = factory.UseAsync(context, (_, _) => failedResultTask, testToken);
        Assert.Same(failedResultTask, failedResult);
        Assert.Same(callbackFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => failedResult));

        using var callbackCancellation = new CancellationTokenSource();
        callbackCancellation.Cancel();
        Task canceledCallbackTask = Task.FromCanceled(callbackCancellation.Token);
        Task canceledCallback = factory.UseAsync(context, (_, _) => canceledCallbackTask, testToken);
        Assert.Same(canceledCallbackTask, canceledCallback);
        OperationCanceledException canceledCallbackError = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledCallback);
        Assert.Equal(callbackCancellation.Token, canceledCallbackError.CancellationToken);
        Task<int> canceledResultTask = Task.FromCanceled<int>(callbackCancellation.Token);
        Task<int> canceledResult = factory.UseAsync(context, (_, _) => canceledResultTask, testToken);
        Assert.Same(canceledResultTask, canceledResult);
        OperationCanceledException canceledResultError = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledResult);
        Assert.Equal(callbackCancellation.Token, canceledResultError.CancellationToken);

        var thrownCallbackFailure = new InvalidOperationException("context callback threw");
        InvalidOperationException thrownCallbackError = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = factory.UseAsync(context, (_, _) => throw thrownCallbackFailure, testToken);
        });
        Assert.Same(thrownCallbackFailure, thrownCallbackError);
        InvalidOperationException thrownResultCallbackError = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = factory.UseAsync<int>(context, (_, _) => throw thrownCallbackFailure, testToken);
        });
        Assert.Same(thrownCallbackFailure, thrownResultCallbackError);

        var pendingSource = new TaskCompletionSource<InitializedMessage<OutputMessage>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingFactory = new ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage>(_ => pendingSource.Task);
        Task<InitializedMessage<OutputMessage>> pending = pendingFactory.GetMessageAsync(context, testToken);
        Assert.NotSame(pendingSource.Task, pending);
        pendingSource.SetResult(initialized);
        Assert.Same(message, (await pending).Message);

        var expectedFailure = new InvalidOperationException("context source failed");
        var failedFactory = new ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage>(
            _ => Task.FromException<InitializedMessage<OutputMessage>>(expectedFailure));
        Assert.Same(expectedFailure,
            await Assert.ThrowsAsync<InvalidOperationException>(() => failedFactory.GetMessageAsync(context, testToken)));

        using var sourceCancellation = new CancellationTokenSource();
        sourceCancellation.Cancel();
        var sourceCanceledFactory = new ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage>(
            _ => Task.FromCanceled<InitializedMessage<OutputMessage>>(sourceCancellation.Token));
        OperationCanceledException sourceCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sourceCanceledFactory.GetMessageAsync(context, testToken));
        Assert.Equal(sourceCancellation.Token, sourceCanceled.CancellationToken);

        using var callerCancellation = new CancellationTokenSource();
        callerCancellation.Cancel();
        var calls = 0;
        var preemptedFactory = new ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage>(_ =>
        {
            calls++;
            return completed;
        });
        Task<InitializedMessage<OutputMessage>> callerCanceled = preemptedFactory.GetMessageAsync(context, callerCancellation.Token);
        OperationCanceledException callerCanceledError = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => callerCanceled);
        Assert.Equal(callerCancellation.Token, callerCanceledError.CancellationToken);
        Assert.Equal(0, calls);

        var useCalls = 0;
        var callbackCalls = 0;
        var canceledUseFactory = new ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage>(_ =>
        {
            useCalls++;
            return completed;
        });
        Task canceledUse = canceledUseFactory.UseAsync(context, (_, _) =>
        {
            callbackCalls++;
            return Task.CompletedTask;
        }, callerCancellation.Token);
        OperationCanceledException canceledUseError = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledUse);
        Assert.Equal(callerCancellation.Token, canceledUseError.CancellationToken);
        Task<int> canceledResultUse = canceledUseFactory.UseAsync(context, (_, _) =>
        {
            callbackCalls++;
            return Task.FromResult(1);
        }, callerCancellation.Token);
        OperationCanceledException canceledResultUseError = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledResultUse);
        Assert.Equal(callerCancellation.Token, canceledResultUseError.CancellationToken);
        Assert.Equal(0, useCalls);
        Assert.Equal(0, callbackCalls);

        await VerifyContextFactoryPendingCallbacksAsync(context, initialized, testToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-214-direct-message-factories-null-collaborator-conversion")]
    public async Task DirectFactories_RejectNullCollaboratorsAndNullTaskResultsWithoutSideEffectsAsync()
    {
        IBehaviorContext<TestSaga, InputMessage> context = CreateContext<IBehaviorContext<TestSaga, InputMessage>>();
        Task<InitializedMessage<OutputMessage>> completed = Task.FromResult(
            new InitializedMessage<OutputMessage>(new OutputMessage("valid")));
        CancellationToken testToken = TestContext.Current.CancellationToken;

        AssertArgument("messageFactory", () => new TaskMessageFactory<OutputMessage>(null!));
        AssertArgument("messageFactory", () =>
            new ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage>(null!));

        var taskFactory = new TaskMessageFactory<OutputMessage>(completed);
        AssertArgument("callback", () => taskFactory.UseAsync(null!, testToken));

        var contextFactory = new ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage>(_ => completed);
        AssertArgument("context", () => contextFactory.GetMessageAsync(null!, testToken));
        AssertArgument("context", () => contextFactory.UseAsync(null!, (_, _) => Task.CompletedTask, testToken));
        AssertArgument("callback", () => contextFactory.UseAsync(context,
            (Func<IBehaviorContext<TestSaga, InputMessage>, InitializedMessage<OutputMessage>, Task>)null!, testToken));
        AssertArgument("callback", () => contextFactory.UseAsync<int>(context,
            (Func<IBehaviorContext<TestSaga, InputMessage>, InitializedMessage<OutputMessage>, Task<int>>)null!, testToken));

        using var canceledArguments = new CancellationTokenSource();
        canceledArguments.Cancel();
        AssertArgument("callback", () => taskFactory.UseAsync(null!, canceledArguments.Token));
        AssertArgument("context", () => contextFactory.GetMessageAsync(null!, canceledArguments.Token));
        AssertArgument("context", () => contextFactory.UseAsync(null!,
            (Func<IBehaviorContext<TestSaga, InputMessage>, InitializedMessage<OutputMessage>, Task>)null!, canceledArguments.Token));
        AssertArgument("context", () => contextFactory.UseAsync<int>(null!,
            (Func<IBehaviorContext<TestSaga, InputMessage>, InitializedMessage<OutputMessage>, Task<int>>)null!, canceledArguments.Token));
        AssertArgument("callback", () => contextFactory.UseAsync(context,
            (Func<IBehaviorContext<TestSaga, InputMessage>, InitializedMessage<OutputMessage>, Task>)null!, canceledArguments.Token));
        AssertArgument("callback", () => contextFactory.UseAsync<int>(context,
            (Func<IBehaviorContext<TestSaga, InputMessage>, InitializedMessage<OutputMessage>, Task<int>>)null!, canceledArguments.Token));

        var nullTaskFactory = new ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage>(_ => null!);
        InvalidOperationException missingFactoryTask = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = nullTaskFactory.GetMessageAsync(context, testToken);
        });
        Assert.Equal("The message factory returned no task.", missingFactoryTask.Message);
        var nullSourceCallbackCalls = 0;
        InvalidOperationException missingUseFactoryTask = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = nullTaskFactory.UseAsync(context, (_, _) =>
            {
                nullSourceCallbackCalls++;
                return Task.CompletedTask;
            }, testToken);
        });
        Assert.Equal("The message factory returned no task.", missingUseFactoryTask.Message);
        InvalidOperationException missingResultUseFactoryTask = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = nullTaskFactory.UseAsync(context, (_, _) =>
            {
                nullSourceCallbackCalls++;
                return Task.FromResult(1);
            }, testToken);
        });
        Assert.Equal("The message factory returned no task.", missingResultUseFactoryTask.Message);
        Assert.Equal(0, nullSourceCallbackCalls);

        InvalidOperationException missingTaskCallback = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = taskFactory.UseAsync(_ => null!, testToken);
        });
        Assert.Equal("The callback returned no task.", missingTaskCallback.Message);
        InvalidOperationException missingContextCallback = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = contextFactory.UseAsync(context, (_, _) => null!, testToken);
        });
        Assert.Equal("The callback returned no task.", missingContextCallback.Message);
        InvalidOperationException missingResultCallback = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = contextFactory.UseAsync<int>(context, (_, _) => null!, testToken);
        });
        Assert.Equal("The callback returned no task.", missingResultCallback.Message);

        TaskMessageFactory<OutputMessage> source = new(completed);
        ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage> converted = source;
        Assert.Same(completed, converted.GetMessageAsync(context, testToken));
        Assert.Same(completed, converted.GetMessageAsync(CreateContext<IBehaviorContext<TestSaga, InputMessage>>(), testToken));

        TaskMessageFactory<OutputMessage> nullSource = null!;
        AssertArgument("factory", () =>
        {
            ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage> _ = nullSource;
        });

        await completed;
    }

    static async Task VerifySuccessfulOverloadAsync(MethodInfo method, bool configureOptionalParameter)
    {
        var recorder = new InvocationRecorder();
        object?[] arguments = CreateArguments(method, recorder, SourceOutcome.Success, configureOptionalParameter, null,
            TestContext.Current.CancellationToken);
        object factory = method.Invoke(null, arguments)!;
        Assert.Equal(0, recorder.FactoryCalls);
        (InitializedMessage<OutputMessage> initialized, object? context) = await GetMessageAsync(factory);

        Assert.Same(recorder.Message, initialized.Message);
        if (IsFactoryDelegate(method.GetParameters()[0].ParameterType))
        {
            Assert.Same(context, recorder.FactoryContext);
            Assert.Equal(1, recorder.FactoryCalls);
        }

        ParameterInfo[] parameters = method.GetParameters();
        bool initializedFactory = ReturnsInitializedMessageTask(parameters[0].ParameterType);
        bool configuredPipe = configureOptionalParameter && parameters.Length == 2 && IsPipe(parameters[1].ParameterType);
        bool configuredCallback = configureOptionalParameter && parameters.Length == 2 && !IsPipe(parameters[1].ParameterType);

        if (configuredPipe || initializedFactory && !configuredCallback)
            Assert.Same(recorder.BasePipe, initialized.Pipe);

        var sendContext = new MessageSendContext<OutputMessage>(recorder.Message);
        await initialized.Pipe.SendAsync(sendContext);

        var expectedStages = new List<string>();
        if (initializedFactory || configuredPipe)
            expectedStages.Add("base");
        if (configuredCallback)
            expectedStages.Add("callback");
        Assert.Equal(expectedStages, recorder.Stages);

        if (expectedStages.Count > 0)
        {
            Assert.True(sendContext.Headers.TryGetHeader(HeaderName, out object? finalStage));
            Assert.Equal(expectedStages[^1], finalStage);
        }
        else
            Assert.False(sendContext.Headers.TryGetHeader(HeaderName, out _));

        if (configuredCallback)
        {
            Assert.Same(sendContext, recorder.CallbackSendContext);
            Type callbackType = parameters[1].ParameterType;
            if (callbackType.GetMethod("Invoke")!.GetParameters().Length == 2)
                Assert.Same(context, recorder.CallbackBehaviorContext);
            else
                Assert.Null(recorder.CallbackBehaviorContext);
        }
    }

    static async Task VerifyPendingSuccessfulOverloadAsync(MethodInfo method, bool configureOptionalParameter)
    {
        var recorder = new InvocationRecorder();
        object?[] arguments = CreatePendingArguments(method, recorder, configureOptionalParameter, out PendingSource pending);
        object factory = method.Invoke(null, arguments)!;
        Assert.Equal(0, recorder.FactoryCalls);

        Task<(InitializedMessage<OutputMessage> Message, object? Context)> getMessage = GetMessageAsync(factory);
        Assert.False(
            getMessage.IsCompleted,
            $"{DescribeMethod(method)}; optional={configureOptionalParameter}; status={getMessage.Status}; error={getMessage.Exception}");
        Assert.Empty(recorder.Stages);
        if (IsFactoryDelegate(method.GetParameters()[0].ParameterType))
            Assert.Equal(1, recorder.FactoryCalls);

        pending.Complete();
        (InitializedMessage<OutputMessage> initialized, object? context) = await getMessage;
        Assert.Same(recorder.Message, initialized.Message);
        if (IsFactoryDelegate(method.GetParameters()[0].ParameterType))
        {
            Assert.Same(context, recorder.FactoryContext);
            Assert.Equal(1, recorder.FactoryCalls);
        }

        ParameterInfo[] parameters = method.GetParameters();
        bool initializedFactory = ReturnsInitializedMessageTask(parameters[0].ParameterType);
        bool configuredPipe = configureOptionalParameter && parameters.Length == 2 && IsPipe(parameters[1].ParameterType);
        bool configuredCallback = configureOptionalParameter && parameters.Length == 2 && !IsPipe(parameters[1].ParameterType);
        if (configuredPipe || initializedFactory && !configuredCallback)
            Assert.Same(recorder.BasePipe, initialized.Pipe);
        var sendContext = new MessageSendContext<OutputMessage>(recorder.Message);
        await initialized.Pipe.SendAsync(sendContext);

        var expectedStages = new List<string>();
        if (initializedFactory || configuredPipe)
            expectedStages.Add("base");
        if (configuredCallback)
            expectedStages.Add("callback");
        Assert.Equal(expectedStages, recorder.Stages);
        if (configuredCallback)
        {
            Assert.Same(sendContext, recorder.CallbackSendContext);
            Type callbackType = parameters[1].ParameterType;
            if (callbackType.GetMethod("Invoke")!.GetParameters().Length == 2)
                Assert.Same(context, recorder.CallbackBehaviorContext);
            else
                Assert.Null(recorder.CallbackBehaviorContext);
        }
    }

    static async Task VerifyTaskFactoryPendingCallbacksAsync(InitializedMessage<OutputMessage> initialized,
        CancellationToken cancellationToken)
    {
        TaskMessageFactory<OutputMessage> successFactory = CreatePendingTaskFactory(out var successSource);
        var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        InitializedMessage<OutputMessage> observed = default;
        Task success = successFactory.UseAsync(message =>
        {
            observed = message;
            callbackEntered.SetResult();
            return callbackCompletion.Task;
        }, cancellationToken);
        Assert.False(success.IsCompleted);
        successSource.SetResult(initialized);
        await callbackEntered.Task;
        Assert.Same(initialized.Message, observed.Message);
        Assert.Same(initialized.Pipe, observed.Pipe);
        Assert.False(success.IsCompleted);
        Assert.NotSame(callbackCompletion.Task, success);
        callbackCompletion.SetResult();
        await success;

        var callbackFailure = new InvalidOperationException("pending task callback failed");
        TaskMessageFactory<OutputMessage> failedFactory = CreatePendingTaskFactory(out var failedSource);
        Task failed = failedFactory.UseAsync(_ => Task.FromException(callbackFailure), cancellationToken);
        Assert.False(failed.IsCompleted);
        failedSource.SetResult(initialized);
        Assert.Same(callbackFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => failed));

        using var callbackCancellation = new CancellationTokenSource();
        callbackCancellation.Cancel();
        TaskMessageFactory<OutputMessage> canceledFactory = CreatePendingTaskFactory(out var canceledSource);
        Task canceled = canceledFactory.UseAsync(_ => Task.FromCanceled(callbackCancellation.Token), cancellationToken);
        Assert.False(canceled.IsCompleted);
        canceledSource.SetResult(initialized);
        OperationCanceledException canceledError = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);
        Assert.Equal(callbackCancellation.Token, canceledError.CancellationToken);

        TaskMessageFactory<OutputMessage> nullFactory = CreatePendingTaskFactory(out var nullSource);
        Task missing = nullFactory.UseAsync(_ => null!, cancellationToken);
        Assert.False(missing.IsCompleted);
        nullSource.SetResult(initialized);
        InvalidOperationException missingError = await Assert.ThrowsAsync<InvalidOperationException>(() => missing);
        Assert.Equal("The callback returned no task.", missingError.Message);

        var synchronousFailure = new InvalidOperationException("pending task callback threw");
        TaskMessageFactory<OutputMessage> throwingFactory = CreatePendingTaskFactory(out var throwingSource);
        Task throwing = throwingFactory.UseAsync(_ => throw synchronousFailure, cancellationToken);
        Assert.False(throwing.IsCompleted);
        throwingSource.SetResult(initialized);
        Assert.Same(synchronousFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => throwing));

        var sourceFailure = new InvalidOperationException("pending task source failed");
        TaskMessageFactory<OutputMessage> sourceFailedFactory = CreatePendingTaskFactory(out var sourceFailed);
        var callbackCalls = 0;
        Task sourceFailureUse = sourceFailedFactory.UseAsync(_ =>
        {
            callbackCalls++;
            return Task.CompletedTask;
        }, cancellationToken);
        sourceFailed.SetException(sourceFailure);
        Assert.Same(sourceFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => sourceFailureUse));

        using var sourceCancellation = new CancellationTokenSource();
        sourceCancellation.Cancel();
        TaskMessageFactory<OutputMessage> sourceCanceledFactory = CreatePendingTaskFactory(out var sourceCanceled);
        Task sourceCanceledUse = sourceCanceledFactory.UseAsync(_ =>
        {
            callbackCalls++;
            return Task.CompletedTask;
        }, cancellationToken);
        sourceCanceled.SetCanceled(sourceCancellation.Token);
        OperationCanceledException sourceCanceledError = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sourceCanceledUse);
        Assert.Equal(sourceCancellation.Token, sourceCanceledError.CancellationToken);
        Assert.Equal(0, callbackCalls);
    }

    static async Task VerifyContextFactoryPendingCallbacksAsync(
        IBehaviorContext<TestSaga, InputMessage> context,
        InitializedMessage<OutputMessage> initialized,
        CancellationToken cancellationToken)
    {
        ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage> successFactory =
            CreatePendingContextFactory(out var successSource);
        var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task success = successFactory.UseAsync(context, (observedContext, observedMessage) =>
        {
            Assert.Same(context, observedContext);
            Assert.Same(initialized.Message, observedMessage.Message);
            Assert.Same(initialized.Pipe, observedMessage.Pipe);
            callbackEntered.SetResult();
            return callbackCompletion.Task;
        }, cancellationToken);
        Assert.False(success.IsCompleted);
        successSource.SetResult(initialized);
        await callbackEntered.Task;
        Assert.False(success.IsCompleted);
        Assert.NotSame(callbackCompletion.Task, success);
        callbackCompletion.SetResult();
        await success;

        ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage> resultFactory =
            CreatePendingContextFactory(out var resultSource);
        var resultCallbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resultCompletion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<int> result = resultFactory.UseAsync(context, (observedContext, observedMessage) =>
        {
            Assert.Same(context, observedContext);
            Assert.Same(initialized.Message, observedMessage.Message);
            Assert.Same(initialized.Pipe, observedMessage.Pipe);
            resultCallbackEntered.SetResult();
            return resultCompletion.Task;
        }, cancellationToken);
        Assert.False(result.IsCompleted);
        resultSource.SetResult(initialized);
        await resultCallbackEntered.Task;
        Assert.False(result.IsCompleted);
        Assert.NotSame(resultCompletion.Task, result);
        resultCompletion.SetResult(91);
        Assert.Equal(91, await result);

        var callbackFailure = new InvalidOperationException("pending context callback failed");
        ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage> failedFactory =
            CreatePendingContextFactory(out var failedSource);
        Task failed = failedFactory.UseAsync(context, (_, _) => Task.FromException(callbackFailure), cancellationToken);
        Assert.False(failed.IsCompleted);
        failedSource.SetResult(initialized);
        Assert.Same(callbackFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => failed));

        ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage> failedResultFactory =
            CreatePendingContextFactory(out var failedResultSource);
        Task<int> failedResult = failedResultFactory.UseAsync(context,
            (_, _) => Task.FromException<int>(callbackFailure), cancellationToken);
        Assert.False(failedResult.IsCompleted);
        failedResultSource.SetResult(initialized);
        Assert.Same(callbackFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => failedResult));

        using var callbackCancellation = new CancellationTokenSource();
        callbackCancellation.Cancel();
        ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage> canceledFactory =
            CreatePendingContextFactory(out var canceledSource);
        Task canceled = canceledFactory.UseAsync(context,
            (_, _) => Task.FromCanceled(callbackCancellation.Token), cancellationToken);
        Assert.False(canceled.IsCompleted);
        canceledSource.SetResult(initialized);
        OperationCanceledException canceledError = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);
        Assert.Equal(callbackCancellation.Token, canceledError.CancellationToken);

        ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage> canceledResultFactory =
            CreatePendingContextFactory(out var canceledResultSource);
        Task<int> canceledResult = canceledResultFactory.UseAsync(context,
            (_, _) => Task.FromCanceled<int>(callbackCancellation.Token), cancellationToken);
        Assert.False(canceledResult.IsCompleted);
        canceledResultSource.SetResult(initialized);
        OperationCanceledException canceledResultError = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledResult);
        Assert.Equal(callbackCancellation.Token, canceledResultError.CancellationToken);

        ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage> nullFactory =
            CreatePendingContextFactory(out var nullSource);
        Task missing = nullFactory.UseAsync(context, (_, _) => null!, cancellationToken);
        Assert.False(missing.IsCompleted);
        nullSource.SetResult(initialized);
        InvalidOperationException missingError = await Assert.ThrowsAsync<InvalidOperationException>(() => missing);
        Assert.Equal("The callback returned no task.", missingError.Message);

        ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage> nullResultFactory =
            CreatePendingContextFactory(out var nullResultSource);
        Task<int> missingResult = nullResultFactory.UseAsync<int>(context, (_, _) => null!, cancellationToken);
        Assert.False(missingResult.IsCompleted);
        nullResultSource.SetResult(initialized);
        InvalidOperationException missingResultError = await Assert.ThrowsAsync<InvalidOperationException>(() => missingResult);
        Assert.Equal("The callback returned no task.", missingResultError.Message);

        var synchronousFailure = new InvalidOperationException("pending context callback threw");
        ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage> throwingFactory =
            CreatePendingContextFactory(out var throwingSource);
        Task throwing = throwingFactory.UseAsync(context, (_, _) => throw synchronousFailure, cancellationToken);
        Assert.False(throwing.IsCompleted);
        throwingSource.SetResult(initialized);
        Assert.Same(synchronousFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => throwing));

        ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage> throwingResultFactory =
            CreatePendingContextFactory(out var throwingResultSource);
        Task<int> throwingResult = throwingResultFactory.UseAsync<int>(context,
            (_, _) => throw synchronousFailure, cancellationToken);
        Assert.False(throwingResult.IsCompleted);
        throwingResultSource.SetResult(initialized);
        Assert.Same(synchronousFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => throwingResult));

        var sourceFailure = new InvalidOperationException("pending context source failed");
        var callbackCalls = 0;
        ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage> sourceFailedFactory =
            CreatePendingContextFactory(out var sourceFailed);
        Task sourceFailureUse = sourceFailedFactory.UseAsync(context, (_, _) =>
        {
            callbackCalls++;
            return Task.CompletedTask;
        }, cancellationToken);
        sourceFailed.SetException(sourceFailure);
        Assert.Same(sourceFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => sourceFailureUse));

        ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage> sourceFailedResultFactory =
            CreatePendingContextFactory(out var sourceFailedResult);
        Task<int> sourceFailureResultUse = sourceFailedResultFactory.UseAsync(context, (_, _) =>
        {
            callbackCalls++;
            return Task.FromResult(1);
        }, cancellationToken);
        sourceFailedResult.SetException(sourceFailure);
        Assert.Same(sourceFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => sourceFailureResultUse));

        using var sourceCancellation = new CancellationTokenSource();
        sourceCancellation.Cancel();
        ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage> sourceCanceledFactory =
            CreatePendingContextFactory(out var sourceCanceled);
        Task sourceCanceledUse = sourceCanceledFactory.UseAsync(context, (_, _) =>
        {
            callbackCalls++;
            return Task.CompletedTask;
        }, cancellationToken);
        sourceCanceled.SetCanceled(sourceCancellation.Token);
        OperationCanceledException sourceCanceledError = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sourceCanceledUse);
        Assert.Equal(sourceCancellation.Token, sourceCanceledError.CancellationToken);

        ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage> sourceCanceledResultFactory =
            CreatePendingContextFactory(out var sourceCanceledResult);
        Task<int> sourceCanceledResultUse = sourceCanceledResultFactory.UseAsync(context, (_, _) =>
        {
            callbackCalls++;
            return Task.FromResult(1);
        }, cancellationToken);
        sourceCanceledResult.SetCanceled(sourceCancellation.Token);
        OperationCanceledException sourceCanceledResultError = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sourceCanceledResultUse);
        Assert.Equal(sourceCancellation.Token, sourceCanceledResultError.CancellationToken);
        Assert.Equal(0, callbackCalls);
    }

    static TaskMessageFactory<OutputMessage> CreatePendingTaskFactory(
        out TaskCompletionSource<InitializedMessage<OutputMessage>> source)
    {
        source = new TaskCompletionSource<InitializedMessage<OutputMessage>>(TaskCreationOptions.RunContinuationsAsynchronously);
        return new TaskMessageFactory<OutputMessage>(source.Task);
    }

    static ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage> CreatePendingContextFactory(
        out TaskCompletionSource<InitializedMessage<OutputMessage>> source)
    {
        var completion = new TaskCompletionSource<InitializedMessage<OutputMessage>>(TaskCreationOptions.RunContinuationsAsynchronously);
        source = completion;
        return new ContextMessageFactory<IBehaviorContext<TestSaga, InputMessage>, OutputMessage>(_ => completion.Task);
    }

    static object?[] CreateArguments(MethodInfo method, InvocationRecorder recorder, SourceOutcome outcome,
        bool configureOptionalParameter, Exception? failure, CancellationToken cancellationToken)
    {
        ParameterInfo[] parameters = method.GetParameters();
        object?[] arguments = new object?[parameters.Length];
        arguments[0] = CreateSource(parameters[0].ParameterType, recorder, outcome, failure, cancellationToken);
        if (parameters.Length == 2 && configureOptionalParameter)
            arguments[1] = IsPipe(parameters[1].ParameterType)
                ? recorder.BasePipe
                : CreateCallback(parameters[1].ParameterType, recorder);
        return arguments;
    }

    static object?[] CreatePendingArguments(MethodInfo method, InvocationRecorder recorder,
        bool configureOptionalParameter, out PendingSource pending)
    {
        ParameterInfo[] parameters = method.GetParameters();
        Type sourceType = parameters[0].ParameterType;
        Type resultType = sourceType == typeof(Task<OutputMessage>)
            ? typeof(OutputMessage)
            : sourceType.GetMethod("Invoke")!.ReturnType.GetGenericArguments()[0];
        pending = new PendingSource(recorder, resultType);

        object?[] arguments = new object?[parameters.Length];
        arguments[0] = sourceType == typeof(Task<OutputMessage>)
            ? pending.Task
            : CreateRecordingFactory(sourceType, recorder, pending.Task);
        if (parameters.Length == 2 && configureOptionalParameter)
            arguments[1] = IsPipe(parameters[1].ParameterType)
                ? recorder.BasePipe
                : CreateCallback(parameters[1].ParameterType, recorder);
        return arguments;
    }

    static object?[] CreateThrowingFactoryArguments(MethodInfo method, InvocationRecorder recorder,
        Exception failure, bool configureOptionalParameter)
    {
        ParameterInfo[] parameters = method.GetParameters();
        Type sourceType = parameters[0].ParameterType;
        MethodInfo invoke = sourceType.GetMethod("Invoke")!;
        ParameterExpression[] factoryParameters = invoke.GetParameters()
            .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
            .ToArray();
        Expression recordContext = Expression.Call(
            Expression.Constant(recorder),
            typeof(InvocationRecorder).GetMethod(nameof(InvocationRecorder.RecordFactoryContext))!,
            Expression.Convert(factoryParameters[0], typeof(object)));
        Expression body = Expression.Block(recordContext,
            Expression.Throw(Expression.Constant(failure), invoke.ReturnType));

        object?[] arguments = new object?[parameters.Length];
        arguments[0] = Expression.Lambda(sourceType, body, factoryParameters).Compile();
        if (parameters.Length == 2 && configureOptionalParameter)
            arguments[1] = IsPipe(parameters[1].ParameterType)
                ? recorder.BasePipe
                : CreateCallback(parameters[1].ParameterType, recorder);
        return arguments;
    }

    static object CreateRecordingFactory(Type factoryType, InvocationRecorder recorder, object result)
    {
        MethodInfo invoke = factoryType.GetMethod("Invoke")!;
        ParameterExpression[] parameters = invoke.GetParameters()
            .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
            .ToArray();
        Expression recordContext = Expression.Call(
            Expression.Constant(recorder),
            typeof(InvocationRecorder).GetMethod(nameof(InvocationRecorder.RecordFactoryContext))!,
            Expression.Convert(parameters[0], typeof(object)));
        Expression body = Expression.Block(recordContext, Expression.Constant(result, invoke.ReturnType));
        return Expression.Lambda(factoryType, body, parameters).Compile();
    }

    static object CreateSource(Type parameterType, InvocationRecorder recorder, SourceOutcome outcome,
        Exception? failure, CancellationToken cancellationToken)
    {
        if (parameterType == typeof(OutputMessage))
            return recorder.Message;
        if (parameterType == typeof(Task<OutputMessage>))
            return CreateTask(typeof(OutputMessage), recorder, outcome, failure, cancellationToken)!;

        MethodInfo invoke = parameterType.GetMethod("Invoke")!;
        ParameterExpression[] parameters = invoke.GetParameters()
            .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
            .ToArray();
        object? result = invoke.ReturnType == typeof(OutputMessage)
            ? recorder.Message
            : CreateTask(
                invoke.ReturnType.GetGenericArguments()[0], recorder, outcome, failure, cancellationToken);
        Expression recordContext = Expression.Call(
            Expression.Constant(recorder),
            typeof(InvocationRecorder).GetMethod(nameof(InvocationRecorder.RecordFactoryContext))!,
            Expression.Convert(parameters[0], typeof(object)));
        Expression body = Expression.Block(recordContext, Expression.Constant(result, invoke.ReturnType));
        return Expression.Lambda(parameterType, body, parameters).Compile();
    }

    static object? CreateTask(Type resultType, InvocationRecorder recorder, SourceOutcome outcome,
        Exception? failure, CancellationToken cancellationToken)
    {
        if (resultType == typeof(OutputMessage))
        {
            return outcome switch
            {
                SourceOutcome.Success => Task.FromResult(recorder.Message),
                SourceOutcome.Faulted => Task.FromException<OutputMessage>(failure!),
                SourceOutcome.Canceled => Task.FromCanceled<OutputMessage>(cancellationToken),
                SourceOutcome.NullTask => null,
                _ => throw new ArgumentOutOfRangeException(nameof(outcome))
            };
        }

        Assert.Equal(typeof(InitializedMessage<OutputMessage>), resultType);
        var initialized = new InitializedMessage<OutputMessage>(recorder.Message, recorder.BasePipe);
        return outcome switch
        {
            SourceOutcome.Success => Task.FromResult(initialized),
            SourceOutcome.Faulted => Task.FromException<InitializedMessage<OutputMessage>>(failure!),
            SourceOutcome.Canceled => Task.FromCanceled<InitializedMessage<OutputMessage>>(cancellationToken),
            SourceOutcome.NullTask => null,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome))
        };
    }

    static object CreateCallback(Type callbackType, InvocationRecorder recorder)
    {
        MethodInfo invoke = callbackType.GetMethod("Invoke")!;
        ParameterExpression[] parameters = invoke.GetParameters()
            .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
            .ToArray();
        Expression behaviorContext = parameters.Length == 2
            ? Expression.Convert(parameters[0], typeof(object))
            : Expression.Constant(null, typeof(object));
        Expression sendContext = Expression.Convert(parameters[^1], typeof(SendContext<OutputMessage>));
        Expression body = Expression.Call(
            Expression.Constant(recorder),
            typeof(InvocationRecorder).GetMethod(nameof(InvocationRecorder.RecordCallback))!,
            behaviorContext,
            sendContext);
        return Expression.Lambda(callbackType, body, parameters).Compile();
    }

    static async Task<(InitializedMessage<OutputMessage> Message, object? Context)> GetMessageAsync(object factory)
    {
        if (factory is TaskMessageFactory<OutputMessage> taskFactory)
            return (await taskFactory.GetMessageAsync(TestContext.Current.CancellationToken), null);

        Type factoryType = factory.GetType();
        Type contextType = factoryType.GetGenericArguments()[0];
        object context = DispatchProxy.Create(contextType, typeof(StrictDispatchProxy));
        MethodInfo getMessage = Assert.Single(factoryType.GetMethods(BindingFlags.Public | BindingFlags.Instance),
            method => method.Name == "GetMessageAsync");
        Task<InitializedMessage<OutputMessage>> task;
        try
        {
            task = Assert.IsAssignableFrom<Task<InitializedMessage<OutputMessage>>>(
                getMessage.Invoke(factory, [context, TestContext.Current.CancellationToken]));
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }

        return (await task, context);
    }

    static async Task GetMessageOnlyAsync(object factory)
    {
        await GetMessageAsync(factory);
    }

    static bool HasAsynchronousSource(MethodInfo method)
    {
        Type sourceType = method.GetParameters()[0].ParameterType;
        return sourceType == typeof(Task<OutputMessage>)
            || IsFactoryDelegate(sourceType) && IsTask(sourceType.GetMethod("Invoke")!.ReturnType);
    }

    static bool HasSynchronousFactorySource(MethodInfo method)
    {
        Type sourceType = method.GetParameters()[0].ParameterType;
        return IsFactoryDelegate(sourceType) && !IsTask(sourceType.GetMethod("Invoke")!.ReturnType);
    }

    static bool ReturnsInitializedMessageTask(Type sourceType) =>
        IsFactoryDelegate(sourceType)
        && sourceType.GetMethod("Invoke")!.ReturnType == typeof(Task<InitializedMessage<OutputMessage>>);

    static bool IsFactoryDelegate(Type type) => typeof(Delegate).IsAssignableFrom(type);

    static bool IsPipe(Type type) => typeof(IPipe<SendContext<OutputMessage>>).IsAssignableFrom(type);

    static bool IsTask(Type type) => type == typeof(Task)
        || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>);

    static MethodInfo Close(MethodInfo method)
    {
        if (!method.IsGenericMethodDefinition)
            return method;

        Type[] arguments = method.GetGenericArguments().Select(parameter => parameter.Name switch
        {
            "TSaga" => typeof(TestSaga),
            "TMessage" => typeof(InputMessage),
            "TException" => typeof(InvalidOperationException),
            _ => throw new InvalidOperationException($"Unexpected generic parameter: {parameter.Name}.")
        }).ToArray();
        return method.MakeGenericMethod(arguments);
    }

    static void AssertFactorySurface(Type type, string[] methodSignatures, Type[] constructorParameters)
    {
        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.False(type.IsAbstract);
        Assert.False(type.IsSealed);
        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
        Assert.Empty(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
        Assert.Empty(type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
        ConstructorInfo constructor = Assert.Single(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Equal(constructorParameters, constructor.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal("messageFactory", Assert.Single(constructor.GetParameters()).Name);
        Assert.All(constructor.GetParameters(), parameter =>
        {
            NullabilityInfo constructorNullability = Nullability.Create(parameter);
            Assert.Equal(NullabilityState.NotNull, constructorNullability.ReadState);
            if (parameter.ParameterType.IsGenericType
                && parameter.ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
                AssertContextFactoryConstructorNullability(constructorNullability);
            else
                AssertNotNullableRecursively(constructorNullability);
        });

        MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();
        Assert.Equal(methodSignatures.OrderBy(value => value, StringComparer.Ordinal),
            methods.Select(DescribeMethod).OrderBy(value => value, StringComparer.Ordinal));
        Assert.All(methods, method =>
        {
            Assert.True(IsTask(method.ReturnType));
            Assert.EndsWith("Async", method.Name, StringComparison.Ordinal);
            Assert.Equal(NullabilityState.NotNull, Nullability.Create(method.ReturnParameter).ReadState);
            AssertNotNullableRecursively(Nullability.Create(method.ReturnParameter));
            Assert.False(method.IsVirtual);
            Assert.False(method.IsFinal);
            Assert.Same(method, method.GetBaseDefinition());
            if (method.IsGenericMethodDefinition)
            {
                Type resultParameter = Assert.Single(method.GetGenericArguments());
                Assert.Equal("TResult", resultParameter.Name);
                Assert.Equal(
                    GenericParameterAttributes.None,
                    resultParameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
                Assert.Empty(resultParameter.GetGenericParameterConstraints());
            }
            else
                Assert.False(method.IsGenericMethod);
            ParameterInfo cancellationToken = Assert.Single(method.GetParameters(),
                parameter => parameter.ParameterType == typeof(CancellationToken));
            Assert.Equal("cancellationToken", cancellationToken.Name);
            Assert.True(cancellationToken.IsOptional);
            Assert.True(cancellationToken.HasDefaultValue);
            Assert.Null(cancellationToken.DefaultValue);
            AssertNotNullableRecursively(Nullability.Create(cancellationToken));
            ParameterInfo[] nonCancellationParameters = method.GetParameters()
                .Where(parameter => parameter.ParameterType != typeof(CancellationToken))
                .ToArray();
            string[] expectedParameterNames = (method.Name, nonCancellationParameters.Length) switch
            {
                ("GetMessageAsync", 0) => [],
                ("GetMessageAsync", 1) => ["context"],
                ("UseAsync", 1) => ["callback"],
                ("UseAsync", 2) => ["context", "callback"],
                _ => throw new InvalidOperationException($"Unexpected async surface: {DescribeMethod(method)}")
            };
            Assert.Equal(
                expectedParameterNames,
                nonCancellationParameters.Select(parameter => parameter.Name));
            Assert.All(nonCancellationParameters, parameter =>
            {
                Assert.False(parameter.IsOptional);
                Assert.Equal(NullabilityState.NotNull, Nullability.Create(parameter).ReadState);
                NullabilityInfo parameterNullability = Nullability.Create(parameter);
                if (parameter.Name == "callback"
                    && parameter.ParameterType.IsGenericType
                    && parameter.ParameterType.GetGenericTypeDefinition() == typeof(Func<,,>))
                    AssertContextFactoryCallbackNullability(parameterNullability);
                else
                    AssertNotNullableRecursively(parameterNullability);
            });
        });
    }

    static void AssertNotNullableRecursively(NullabilityInfo info)
    {
        NullabilityState expected = info.Type.IsGenericParameter
            ? info.Type.Name switch
            {
                "TResult" => NullabilityState.Nullable,
                "TException" => NullabilityState.Unknown,
                _ => NullabilityState.NotNull
            }
            : NullabilityState.NotNull;
        Assert.Equal(expected, info.ReadState);
        Assert.Equal(expected, info.WriteState);
        AssertNestedGenericArgumentsNotNullable(info);
    }

    static void AssertContextFactoryConstructorNullability(NullabilityInfo info)
    {
        Assert.Equal(NullabilityState.NotNull, info.ReadState);
        Assert.Equal(NullabilityState.NotNull, info.WriteState);
        Assert.Equal(2, info.GenericTypeArguments.Length);
        Assert.Equal(NullabilityState.NotNull, info.GenericTypeArguments[0].ReadState);
        Assert.Equal(NullabilityState.NotNull, info.GenericTypeArguments[0].WriteState);
        AssertNotNullableRecursively(info.GenericTypeArguments[1]);
    }

    static void AssertContextFactoryCallbackNullability(NullabilityInfo info)
    {
        Assert.Equal(NullabilityState.NotNull, info.ReadState);
        Assert.Equal(NullabilityState.NotNull, info.WriteState);
        Assert.Equal(3, info.GenericTypeArguments.Length);
        Assert.Equal(NullabilityState.NotNull, info.GenericTypeArguments[0].ReadState);
        Assert.Equal(NullabilityState.NotNull, info.GenericTypeArguments[0].WriteState);
        AssertNotNullableRecursively(info.GenericTypeArguments[1]);
        AssertNotNullableRecursively(info.GenericTypeArguments[2]);
    }

    static void AssertNestedGenericArgumentsNotNullable(NullabilityInfo info)
    {
        foreach (NullabilityInfo argument in info.GenericTypeArguments)
            AssertNotNullableRecursively(argument);
        if (info.ElementType is not null)
            AssertNotNullableRecursively(info.ElementType);
    }

    static string DescribeMethod(MethodInfo method)
    {
        string genericArguments = method.IsGenericMethodDefinition
            ? $"<{string.Join(",", method.GetGenericArguments().Select(argument => argument.Name))}>"
            : string.Empty;
        string parameters = string.Join(",", method.GetParameters().Select(parameter => DescribeType(parameter.ParameterType)));
        return $"{DescribeType(method.ReturnType)} {method.Name}{genericArguments}({parameters})";
    }

    static string DescribeType(Type type)
    {
        if (type.IsGenericParameter)
            return type.Name;
        if (!type.IsGenericType)
            return type.Name;

        string name = type.GetGenericTypeDefinition().Name;
        int arityMarker = name.IndexOf('`');
        if (arityMarker >= 0)
            name = name[..arityMarker];
        return $"{name}<{string.Join(",", type.GetGenericArguments().Select(DescribeType))}>";
    }

    static void AssertGenericConstraints(MethodInfo method)
    {
        foreach (Type parameter in method.GetGenericArguments())
        {
            string[] constraints = parameter.GetGenericParameterConstraints()
                .Select(DescribeType)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            GenericParameterAttributes attributes = parameter.GenericParameterAttributes
                & GenericParameterAttributes.SpecialConstraintMask;
            switch (parameter.Name)
            {
                case "TSaga":
                    Assert.Equal(GenericParameterAttributes.ReferenceTypeConstraint, attributes);
                    Assert.Equal([nameof(ISagaStateMachineInstance)], constraints);
                    break;
                case "TMessage":
                    Assert.Equal(GenericParameterAttributes.ReferenceTypeConstraint, attributes);
                    Assert.Empty(constraints);
                    break;
                case "TException":
                    Assert.Equal(GenericParameterAttributes.None, attributes);
                    Assert.Equal([nameof(Exception)], constraints);
                    break;
                default:
                    Assert.Fail($"Unexpected generic parameter: {parameter.Name}.");
                    break;
            }
        }
    }

    static void AssertReferenceTypeParameter(Type genericType, string parameterName, params Type[] expectedConstraints)
    {
        Type parameter = Assert.Single(genericType.GetGenericArguments(), argument => argument.Name == parameterName);
        GenericParameterAttributes attributes = parameter.GenericParameterAttributes
            & GenericParameterAttributes.SpecialConstraintMask;
        Assert.Equal(GenericParameterAttributes.ReferenceTypeConstraint, attributes);
        Assert.Equal(expectedConstraints.OrderBy(DescribeType, StringComparer.Ordinal),
            parameter.GetGenericParameterConstraints().OrderBy(DescribeType, StringComparer.Ordinal));
    }

    static TContext CreateContext<TContext>()
        where TContext : class => DispatchProxy.Create<TContext, StrictDispatchProxy>();

    static void AssertArgument(string parameterName, Action action) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);

    enum SourceOutcome
    {
        Success,
        Faulted,
        Canceled,
        NullTask
    }

    sealed class PendingSource
    {
        readonly TaskCompletionSource<InitializedMessage<OutputMessage>>? _initialized;
        readonly InvocationRecorder _recorder;
        readonly TaskCompletionSource<OutputMessage>? _message;

        public PendingSource(InvocationRecorder recorder, Type resultType)
        {
            _recorder = recorder;
            if (resultType == typeof(OutputMessage))
            {
                _message = new TaskCompletionSource<OutputMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                Task = _message.Task;
            }
            else
            {
                Assert.Equal(typeof(InitializedMessage<OutputMessage>), resultType);
                _initialized = new TaskCompletionSource<InitializedMessage<OutputMessage>>(TaskCreationOptions.RunContinuationsAsynchronously);
                Task = _initialized.Task;
            }
        }

        public object Task { get; }

        public void Complete()
        {
            if (_message is not null)
                _message.SetResult(_recorder.Message);
            else
                _initialized!.SetResult(new InitializedMessage<OutputMessage>(_recorder.Message, _recorder.BasePipe));
        }
    }

    sealed class InvocationRecorder
    {
        public InvocationRecorder()
        {
            BasePipe = new RecordingPipe(this);
        }

        public OutputMessage Message { get; } = new("matrix");

        public RecordingPipe BasePipe { get; }

        public List<string> Stages { get; } = [];

        public object? FactoryContext { get; private set; }

        public int FactoryCalls { get; private set; }

        public object? CallbackBehaviorContext { get; private set; }

        public SendContext<OutputMessage>? CallbackSendContext { get; private set; }

        public void RecordFactoryContext(object context)
        {
            FactoryCalls++;
            FactoryContext = context;
        }

        public void RecordBase(SendContext<OutputMessage> sendContext)
        {
            CallbackSendContext ??= sendContext;
            Stages.Add("base");
            sendContext.Headers.Set(HeaderName, "base");
        }

        public void RecordCallback(object? behaviorContext, SendContext<OutputMessage> sendContext)
        {
            CallbackBehaviorContext = behaviorContext;
            CallbackSendContext = sendContext;
            Stages.Add("callback");
            sendContext.Headers.Set(HeaderName, "callback");
        }
    }

    sealed class RecordingPipe(InvocationRecorder recorder) : IPipe<SendContext<OutputMessage>>
    {
        public Task SendAsync(SendContext<OutputMessage> context)
        {
            recorder.RecordBase(context);
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    public class StrictDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected context call: {targetMethod?.Name}.");
    }

    public sealed class TestSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed record InputMessage;

    public sealed record OutputMessage(string Value);
}
