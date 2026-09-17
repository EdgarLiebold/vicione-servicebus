using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaSendExtensionDeepContractTests
{
    static readonly Uri Destination = new("loopback://send-extension-contract");

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-SEND-EXTENSIONS", "exact-public-overload-census")]
    public void PublicSurface_ExposesTheExactFortySendVariants()
    {
        MethodInfo[] methods = GetSendMethods();

        Assert.Equal(40, methods.Length);
        Assert.Equal(ExpectedVariants(), methods.Select(Variant).Order(StringComparer.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-SEND-EXTENSIONS", "required-boundary-matrix")]
    public void EveryOverload_RejectsReceiverDestinationAndInputBeforeBinderEffects()
    {
        foreach (MethodInfo definition in GetSendMethods())
        {
            MethodInfo method = Close(definition);
            ParameterInfo[] parameters = method.GetParameters();
            object binder = CreateProxy(parameters[0].ParameterType, out RecordingProxy recorder);
            object[] valid = CreateArguments(method, binder, out _, out _, out _);

            AssertArgument(method, valid, 0, "source");
            AssertArgument(method, valid, 1, parameters[1].Name!);
            AssertArgument(method, valid, 2, parameters[2].Name!);
            Assert.Empty(recorder.Arguments);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-SEND-EXTENSIONS", "runtime-identity-matrix")]
    public async Task EveryOverload_RegistersTheExactActivityAndPreservesAllRuntimeInputsAsync()
    {
        foreach (MethodInfo definition in GetSendMethods())
        {
            MethodInfo method = Close(definition);
            ParameterInfo[] parameters = method.GetParameters();
            object binder = CreateProxy(parameters[0].ParameterType, out RecordingProxy recorder);
            object result = CreateProxy(parameters[0].ParameterType, out _);
            recorder.ReturnValue = result;
            object[] arguments = CreateArguments(method, binder, out InvocationTracker destinationTracker,
                out InvocationTracker factoryTracker, out CallbackTracker callbackTracker);

            object? returned = method.Invoke(null, arguments);

            Assert.Same(result, returned);
            object activity = Assert.Single(recorder.Arguments);
            Assert.Equal(ExpectedActivityType(parameters[0].ParameterType), activity.GetType());
            Assert.Equal(0, destinationTracker.Count);
            Assert.Equal(0, factoryTracker.Count);

            Delegate destinationProvider = GetField<Delegate>(activity, "_destinationAddressProvider");
            if (parameters[1].ParameterType != typeof(Uri))
                Assert.Same(arguments[1], destinationProvider);

            object messageFactory = GetField<object>(activity, "_messageFactory");
            MethodInfo getMessage = messageFactory.GetType().GetMethod("GetMessageAsync")!;
            Type contextType = getMessage.GetParameters()[0].ParameterType;
            object context = CreateProxy(contextType, out _);
            Assert.Same(Destination, destinationProvider.DynamicInvoke(context));
            Assert.Equal(parameters[1].ParameterType == typeof(Uri) ? 0 : 1, destinationTracker.Count);

            var task = (Task<InitializedMessage<OutputMessage>>)getMessage.Invoke(
                messageFactory, [context, CancellationToken.None])!;
            InitializedMessage<OutputMessage> initialized = await task;

            Assert.Same(arguments[2] is OutputMessage direct ? direct : ExpectedMessage, initialized.Message);
            Assert.Equal(parameters[2].ParameterType.IsSubclassOf(typeof(MulticastDelegate)) ? 1 : 0, factoryTracker.Count);

            SendContext<OutputMessage> sendContext = CreateProxy<SendContext<OutputMessage>>(out _);
            await initialized.Pipe.SendAsync(sendContext);
            Assert.Same(sendContext, callbackTracker.Context);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-SEND-EXTENSIONS", "compiled-binder-activity-selection")]
    public void CompiledMatrix_SelectsNormalDataFaultedAndDataFaultedActivities()
    {
        IEventActivityBinder<TestSaga> normal = CreateBinder<IEventActivityBinder<TestSaga>>(out RecordingProxy normalRecorder,
            out IEventActivityBinder<TestSaga> normalResult);
        IEventActivityBinder<TestSaga, InputMessage> data = CreateBinder<IEventActivityBinder<TestSaga, InputMessage>>(
            out RecordingProxy dataRecorder, out IEventActivityBinder<TestSaga, InputMessage> dataResult);
        IExceptionActivityBinder<TestSaga, InvalidOperationException> faulted =
            CreateBinder<IExceptionActivityBinder<TestSaga, InvalidOperationException>>(
                out RecordingProxy faultedRecorder, out IExceptionActivityBinder<TestSaga, InvalidOperationException> faultedResult);
        IExceptionActivityBinder<TestSaga, InputMessage, InvalidOperationException> dataFaulted =
            CreateBinder<IExceptionActivityBinder<TestSaga, InputMessage, InvalidOperationException>>(
                out RecordingProxy dataFaultedRecorder,
                out IExceptionActivityBinder<TestSaga, InputMessage, InvalidOperationException> dataFaultedResult);
        var message = new OutputMessage("compiled");
        DestinationAddressProvider<TestSaga, InputMessage> dataProvider = _ => Destination;
        AsyncEventExceptionMessageFactory<TestSaga, InputMessage, InvalidOperationException, OutputMessage> asyncFactory =
            _ => Task.FromResult(message);

        Assert.Same(normalResult, normal.Send(Destination, message));
        Assert.IsType<SendActivity<TestSaga, OutputMessage>>(Assert.Single(normalRecorder.Arguments));
        Assert.Same(dataResult, data.SendAwaited(dataProvider, Task.FromResult(message)));
        Assert.IsType<SendActivity<TestSaga, InputMessage, OutputMessage>>(Assert.Single(dataRecorder.Arguments));
        Assert.Same(faultedResult,
            faulted.Send(Destination, (EventExceptionMessageFactory<TestSaga, InvalidOperationException, OutputMessage>)(_ => message)));
        Assert.IsType<FaultedSendActivity<TestSaga, InvalidOperationException, OutputMessage>>(Assert.Single(faultedRecorder.Arguments));
        Assert.Same(dataFaultedResult, dataFaulted.SendAwaited(dataProvider, asyncFactory));
        Assert.IsType<FaultedSendActivity<TestSaga, InputMessage, InvalidOperationException, OutputMessage>>(
            Assert.Single(dataFaultedRecorder.Arguments));

        Func<IBehaviorContext<TestSaga>, Task<InitializedMessage<OutputMessage>>> initializedFactory =
            _ => Task.FromResult(new InitializedMessage<OutputMessage>(message));
        normalRecorder.Arguments.Clear();
        Assert.Same(normalResult, normal.SendAwaited(Destination, initializedFactory));
        Assert.IsType<SendActivity<TestSaga, OutputMessage>>(Assert.Single(normalRecorder.Arguments));
    }

    private static MethodInfo[] GetSendMethods() =>
        typeof(SendExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name is nameof(SendExtensions.Send) or nameof(SendExtensions.SendAwaited))
            .OrderBy(Variant, StringComparer.Ordinal)
            .ToArray();

    private static string[] ExpectedVariants()
    {
        string[] binders = ["event", "event-data", "fault", "fault-data"];
        string[] variants =
        [
            "Send:fixed:message", "SendAwaited:fixed:task", "Send:provider:message", "SendAwaited:provider:task",
            "Send:fixed:sync", "SendAwaited:fixed:async", "SendAwaited:fixed:initialized",
            "Send:provider:sync", "SendAwaited:provider:async", "SendAwaited:provider:initialized",
        ];

        return binders.SelectMany(binder => variants.Select(variant => $"{binder}:{variant}"))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string Variant(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        Type binder = parameters[0].ParameterType.GetGenericTypeDefinition();
        string binderKind = binder == typeof(IEventActivityBinder<>) ? "event"
            : binder == typeof(IEventActivityBinder<,>) ? "event-data"
            : binder == typeof(IExceptionActivityBinder<,>) ? "fault"
            : "fault-data";
        string destination = parameters[1].ParameterType == typeof(Uri) ? "fixed" : "provider";
        Type input = parameters[2].ParameterType;
        string inputKind = input.IsGenericParameter ? "message"
            : input.IsGenericType && input.GetGenericTypeDefinition() == typeof(Task<>) ? "task"
            : input.IsGenericType && input.GetGenericTypeDefinition() == typeof(Func<,>) ? "initialized"
            : input.Name.StartsWith("Async", StringComparison.Ordinal) ? "async"
            : "sync";
        return $"{binderKind}:{method.Name}:{destination}:{inputKind}";
    }

    private static MethodInfo Close(MethodInfo method) => method.MakeGenericMethod(
        method.GetGenericArguments().Select(parameter => parameter.Name switch
        {
            "TSaga" => typeof(TestSaga),
            "TData" => typeof(InputMessage),
            "TException" => typeof(InvalidOperationException),
            "TMessage" => typeof(OutputMessage),
            _ => throw new InvalidOperationException(parameter.Name),
        }).ToArray());

    private static object[] CreateArguments(MethodInfo method, object binder, out InvocationTracker destinationTracker,
        out InvocationTracker factoryTracker, out CallbackTracker callbackTracker)
    {
        ParameterInfo[] parameters = method.GetParameters();
        destinationTracker = new InvocationTracker();
        factoryTracker = new InvocationTracker();
        callbackTracker = new CallbackTracker();
        object destination = parameters[1].ParameterType == typeof(Uri)
            ? Destination
            : CreateDelegate(parameters[1].ParameterType, Destination, destinationTracker);
        object input = CreateInput(parameters[2].ParameterType, factoryTracker);
        Action<SendContext<OutputMessage>> callback = callbackTracker.Record;
        return [binder, destination, input, callback];
    }

    private static object CreateInput(Type type, InvocationTracker tracker)
    {
        if (type == typeof(OutputMessage))
            return ExpectedMessage;
        if (type == typeof(Task<OutputMessage>))
            return Task.FromResult(ExpectedMessage);

        Type returnType = type.GetMethod("Invoke")!.ReturnType;
        object value = returnType == typeof(OutputMessage) ? ExpectedMessage
            : returnType == typeof(Task<OutputMessage>) ? Task.FromResult(ExpectedMessage)
            : Task.FromResult(new InitializedMessage<OutputMessage>(ExpectedMessage));
        return CreateDelegate(type, value, tracker);
    }

    private static Delegate CreateDelegate(Type delegateType, object returnValue, InvocationTracker tracker)
    {
        MethodInfo invoke = delegateType.GetMethod("Invoke")!;
        ParameterExpression[] parameters = invoke.GetParameters()
            .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
            .ToArray();
        Expression body = Expression.Block(
            Expression.Call(Expression.Constant(tracker), nameof(InvocationTracker.Increment), Type.EmptyTypes),
            Expression.Constant(returnValue, invoke.ReturnType));
        return Expression.Lambda(delegateType, body, parameters).Compile();
    }

    private static Type ExpectedActivityType(Type binderType)
    {
        Type definition = binderType.GetGenericTypeDefinition();
        Type[] arguments = binderType.GetGenericArguments();
        return definition == typeof(IEventActivityBinder<>)
            ? typeof(SendActivity<,>).MakeGenericType(arguments[0], typeof(OutputMessage))
            : definition == typeof(IEventActivityBinder<,>)
                ? typeof(SendActivity<,,>).MakeGenericType(arguments[0], arguments[1], typeof(OutputMessage))
                : definition == typeof(IExceptionActivityBinder<,>)
                    ? typeof(FaultedSendActivity<,,>).MakeGenericType(arguments[0], arguments[1], typeof(OutputMessage))
                    : typeof(FaultedSendActivity<,,,>).MakeGenericType(arguments[0], arguments[1], arguments[2], typeof(OutputMessage));
    }

    private static void AssertArgument(MethodInfo method, object[] valid, int index, string parameterName)
    {
        object?[] arguments = [.. valid];
        arguments[index] = null;
        TargetInvocationException outer = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, arguments));
        ArgumentNullException exception = Assert.IsType<ArgumentNullException>(outer.InnerException);
        Assert.Equal(parameterName, exception.ParamName);
    }

    private static T GetField<T>(object instance, string name) =>
        (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;

    private static T CreateBinder<T>(out RecordingProxy recorder, out T result)
        where T : class
    {
        T binder = CreateProxy<T>(out recorder);
        result = CreateProxy<T>(out _);
        recorder.ReturnValue = result;
        return binder;
    }

    private static T CreateProxy<T>(out RecordingProxy recorder)
        where T : class
    {
        T proxy = DispatchProxy.Create<T, RecordingProxy>();
        recorder = (RecordingProxy)(object)proxy;
        return proxy;
    }

    private static object CreateProxy(Type interfaceType, out RecordingProxy recorder)
    {
        MethodInfo create = typeof(SagaSendExtensionDeepContractTests).GetMethod(
            nameof(CreateProxyObject), BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(interfaceType);
        object proxy = create.Invoke(null, null)!;
        recorder = (RecordingProxy)proxy;
        return proxy;
    }

    private static object CreateProxyObject<T>()
        where T : class => DispatchProxy.Create<T, RecordingProxy>();

    private static readonly OutputMessage ExpectedMessage = new("reflected");

    private sealed class InvocationTracker
    {
        public int Count { get; private set; }
        public void Increment() => Count++;
    }

    private sealed class CallbackTracker
    {
        public SendContext<OutputMessage>? Context { get; private set; }
        public void Record(SendContext<OutputMessage> context) => Context = context;
    }

    private class RecordingProxy : DispatchProxy
    {
        public List<object> Arguments { get; } = [];
        public object? ReturnValue { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "Add" && args is [not null])
                Arguments.Add(args[0]!);
            return ReturnValue;
        }
    }

    private sealed class TestSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = "Initial";
    }

    private sealed record InputMessage(string Value = "input");
    private sealed record OutputMessage(string Value);
}
