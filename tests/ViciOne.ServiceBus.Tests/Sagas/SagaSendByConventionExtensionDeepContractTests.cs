using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaSendByConventionExtensionDeepContractTests
{
    static readonly OutputMessage ExpectedMessage = new("expected");

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-SEND-BY-CONVENTION", "exact-forty-overload-surface")]
    public void PublicSurface_ContainsExactlyFortyConventionVariants()
    {
        MethodInfo[] methods = GetMethods();

        Assert.Equal(40, methods.Length);
        Assert.Equal(ExpectedVariants(), methods.Select(Describe).Order(StringComparer.Ordinal));

        foreach (MethodInfo method in methods)
        {
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(3, parameters.Length);
            Assert.Equal("source", parameters[0].Name);
            Assert.Equal(parameters[0].ParameterType, method.ReturnType);
            Assert.Equal(InputName(parameters[1].ParameterType), parameters[1].Name);
            Assert.Equal("callback", parameters[2].Name);

            Type expectedCallback = ExpectedCallbackType(method);
            Assert.Equal(expectedCallback, parameters[2].ParameterType);
            bool optionalAction = expectedCallback.GetGenericTypeDefinition() == typeof(Action<>);
            Assert.Equal(optionalAction, parameters[2].HasDefaultValue);
            if (optionalAction)
                Assert.Null(parameters[2].DefaultValue);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-SEND-BY-CONVENTION", "ordered-immediate-boundaries")]
    public void EveryOverload_ValidatesOwnedBoundariesInOrderBeforeBinderEffects()
    {
        foreach (MethodInfo definition in GetMethods())
        {
            MethodInfo method = Close(definition);
            object binder = CreateProxy(method.GetParameters()[0].ParameterType, out RecordingProxy recorder);
            object[] valid = CreateArguments(method, binder, out _, out _);

            object?[] allNull = new object?[valid.Length];
            AssertArgument(method, allNull, "source");

            object?[] nullInput = [.. valid];
            nullInput[1] = null;
            if (method.GetParameters()[1].ParameterType == typeof(OutputMessage))
                AssertArgument(method, nullInput, "message");
            else
            {
                nullInput[2] = null;
                AssertArgument(method, nullInput, InputName(method.GetParameters()[1].ParameterType));
            }

            if (IsContextCallback(method.GetParameters()[2].ParameterType))
            {
                object?[] nullCallback = [.. valid];
                nullCallback[2] = null;
                AssertArgument(method, nullCallback, "callback");
            }
            else
            {
                object?[] nullCallback = [.. valid];
                nullCallback[2] = null;
                Assert.Same(recorder.ReturnValue, method.Invoke(null, nullCallback));
                recorder.Arguments.Clear();
            }

            Assert.Empty(recorder.Arguments);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-SEND-BY-CONVENTION", "activity-result-route-factory-context-callback-identity")]
    public async Task EveryOverload_PreservesActivityResultAndRuntimeIdentityWithLazyRouteLookupAsync()
    {
        foreach (MethodInfo definition in GetMethods())
        {
            MethodInfo method = Close(definition);
            Type binderType = method.GetParameters()[0].ParameterType;
            object binder = CreateProxy(binderType, out RecordingProxy recorder);
            object result = CreateProxy(binderType, out _);
            recorder.ReturnValue = result;
            object[] arguments = CreateArguments(method, binder, out InvocationTracker factoryTracker,
                out InvocationTracker callbackTracker);

            object? returned = method.Invoke(null, arguments);

            Assert.Same(result, returned);
            object activity = Assert.Single(recorder.Arguments);
            Assert.Equal(ExpectedActivityType(binderType), activity.GetType());
            Assert.Empty(factoryTracker.Invocations);
            Assert.Empty(callbackTracker.Invocations);

            object messageFactory = GetField<object>(activity, "_messageFactory");
            MethodInfo getMessage = messageFactory.GetType().GetMethod("GetMessageAsync")!;
            object context = CreateBehaviorContext(getMessage.GetParameters()[0].ParameterType,
                out RecordingProxy contextRecorder, out RecordingProxy receiveRecorder);
            Delegate destinationProvider = GetField<Delegate>(activity, "_destinationAddressProvider");

            Assert.Empty(contextRecorder.Invocations);
            Assert.Empty(receiveRecorder.Invocations);
            TargetInvocationException routeFailure = Assert.Throws<TargetInvocationException>(() =>
                destinationProvider.DynamicInvoke(context));
            Assert.IsType<ConfigurationException>(routeFailure.InnerException);
            Assert.Single(contextRecorder.Invocations, invocation => invocation.Name == "get_ReceiveContext");
            Assert.Single(receiveRecorder.Invocations, invocation => invocation.Name == "get_SendEndpointProvider");

            var task = (Task<InitializedMessage<OutputMessage>>)getMessage.Invoke(
                messageFactory, [context, CancellationToken.None])!;
            InitializedMessage<OutputMessage> initialized = await task;

            Assert.Same(ExpectedMessage, initialized.Message);
            bool factoryInput = method.GetParameters()[1].ParameterType.IsSubclassOf(typeof(MulticastDelegate));
            Assert.Equal(factoryInput ? 1 : 0, factoryTracker.Invocations.Count);
            if (factoryInput)
                Assert.Same(context, Assert.Single(factoryTracker.Invocations)[0]);

            SendContext<OutputMessage> sendContext = CreateProxy<SendContext<OutputMessage>>(out _);
            await initialized.Pipe.SendAsync(sendContext);

            object?[] callbackArguments = Assert.Single(callbackTracker.Invocations);
            if (IsContextCallback(method.GetParameters()[2].ParameterType))
            {
                Assert.Equal(2, callbackArguments.Length);
                Assert.Same(context, callbackArguments[0]);
                Assert.Same(sendContext, callbackArguments[1]);
            }
            else
            {
                Assert.Single(callbackArguments);
                Assert.Same(sendContext, callbackArguments[0]);
            }
        }
    }

    private static MethodInfo[] GetMethods() =>
        typeof(SendByConventionExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name is nameof(SendByConventionExtensions.Send) or nameof(SendByConventionExtensions.SendAwaited))
            .OrderBy(Describe, StringComparer.Ordinal)
            .ToArray();

    private static string[] ExpectedVariants()
    {
        string[] binders = ["normal", "data", "faulted", "data-faulted"];
        string[] inputs = ["message", "task", "sync-factory", "async-factory", "initialized-factory"];
        string[] callbacks = ["action", "context"];
        return binders.SelectMany(binder => inputs.SelectMany(input => callbacks.Select(callback =>
                $"{binder}:{input}:{callback}")))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string Describe(MethodInfo method)
    {
        Type source = method.GetParameters()[0].ParameterType.GetGenericTypeDefinition();
        string binder = source == typeof(IEventActivityBinder<>) ? "normal"
            : source == typeof(IEventActivityBinder<,>) ? "data"
            : source == typeof(IExceptionActivityBinder<,>) ? "faulted"
            : "data-faulted";
        Type input = method.GetParameters()[1].ParameterType;
        string inputKind = input.IsGenericParameter ? "message"
            : input.IsGenericType && input.GetGenericTypeDefinition() == typeof(Task<>) ? "task"
            : input.IsGenericType && input.GetGenericTypeDefinition() == typeof(Func<,>) ? "initialized-factory"
            : input.Name.StartsWith("Async", StringComparison.Ordinal) ? "async-factory"
            : "sync-factory";
        string callback = IsContextCallback(method.GetParameters()[2].ParameterType) ? "context" : "action";
        return $"{binder}:{inputKind}:{callback}";
    }

    private static string InputName(Type input) =>
        input.IsGenericParameter || input == typeof(OutputMessage)
            || input.IsGenericType && input.GetGenericTypeDefinition() == typeof(Task<>)
            ? "message"
            : "messageFactory";

    private static bool IsContextCallback(Type callback) =>
        callback.GetGenericTypeDefinition() != typeof(Action<>);

    private static Type ExpectedCallbackType(MethodInfo method)
    {
        Type source = method.GetParameters()[0].ParameterType;
        Type definition = source.GetGenericTypeDefinition();
        Type[] sourceArguments = source.GetGenericArguments();
        Type message = method.GetGenericArguments()[^1];
        if (method.GetParameters()[2].ParameterType.GetGenericTypeDefinition() == typeof(Action<>))
            return typeof(Action<>).MakeGenericType(typeof(SendContext<>).MakeGenericType(message));
        if (definition == typeof(IEventActivityBinder<>))
            return typeof(SendContextCallback<,>).MakeGenericType(sourceArguments[0], message);
        if (definition == typeof(IEventActivityBinder<,>))
            return typeof(SendContextCallback<,,>).MakeGenericType(sourceArguments[0], sourceArguments[1], message);
        if (definition == typeof(IExceptionActivityBinder<,>))
            return typeof(SendExceptionContextCallback<,,>).MakeGenericType(sourceArguments[0], sourceArguments[1], message);
        return typeof(SendExceptionContextCallback<,,,>).MakeGenericType(
            sourceArguments[0], sourceArguments[1], sourceArguments[2], message);
    }

    private static MethodInfo Close(MethodInfo method) => method.MakeGenericMethod(
        method.GetGenericArguments().Select(parameter => parameter.Name switch
        {
            "TInstance" => typeof(TestSaga),
            "TData" => typeof(InputMessage),
            "TException" => typeof(InvalidOperationException),
            "TMessage" => typeof(OutputMessage),
            _ => throw new InvalidOperationException(parameter.Name),
        }).ToArray());

    private static object[] CreateArguments(MethodInfo method, object binder, out InvocationTracker factoryTracker,
        out InvocationTracker callbackTracker)
    {
        factoryTracker = new InvocationTracker();
        callbackTracker = new InvocationTracker();
        object input = CreateInput(method.GetParameters()[1].ParameterType, factoryTracker);
        Delegate callback = CreateDelegate(method.GetParameters()[2].ParameterType, null, callbackTracker);
        return [binder, input, callback];
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

    private static Delegate CreateDelegate(Type delegateType, object? returnValue, InvocationTracker tracker)
    {
        MethodInfo invoke = delegateType.GetMethod("Invoke")!;
        ParameterExpression[] parameters = invoke.GetParameters()
            .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
            .ToArray();
        Expression record = Expression.Call(Expression.Constant(tracker), nameof(InvocationTracker.Record), Type.EmptyTypes,
            Expression.NewArrayInit(typeof(object), parameters.Select(parameter => Expression.Convert(parameter, typeof(object)))));
        Expression body = invoke.ReturnType == typeof(void)
            ? record
            : Expression.Block(record, Expression.Constant(returnValue, invoke.ReturnType));
        return Expression.Lambda(delegateType, body, parameters).Compile();
    }

    private static object CreateBehaviorContext(Type contextType, out RecordingProxy contextRecorder,
        out RecordingProxy receiveRecorder)
    {
        ISendEndpointProvider provider = CreateProxy<ISendEndpointProvider>(out _);
        ReceiveContext receiveContext = CreateProxy<ReceiveContext>(out receiveRecorder);
        receiveRecorder.Handler = (method, _) => method.Name == "get_SendEndpointProvider"
            ? provider
            : DefaultValue(method.ReturnType);
        object context = CreateProxy(contextType, out contextRecorder);
        contextRecorder.Handler = (method, _) => method.Name == "get_ReceiveContext"
            ? receiveContext
            : DefaultValue(method.ReturnType);
        return context;
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

    private static void AssertArgument(MethodInfo method, object?[] arguments, string parameterName)
    {
        TargetInvocationException wrapper = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, arguments));
        ArgumentNullException exception = Assert.IsType<ArgumentNullException>(wrapper.InnerException);
        Assert.Equal(parameterName, exception.ParamName);
    }

    private static T GetField<T>(object instance, string name) =>
        (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;

    private static T CreateProxy<T>(out RecordingProxy recorder) where T : class
    {
        T proxy = DispatchProxy.Create<T, RecordingProxy>();
        recorder = (RecordingProxy)(object)proxy;
        return proxy;
    }

    private static object CreateProxy(Type interfaceType, out RecordingProxy recorder)
    {
        object proxy = DispatchProxy.Create(interfaceType, typeof(RecordingProxy));
        recorder = (RecordingProxy)proxy;
        return proxy;
    }

    private static object? DefaultValue(Type type) => type == typeof(void) ? null
        : type == typeof(Task) ? Task.CompletedTask
        : type.IsValueType ? Activator.CreateInstance(type)
        : null;

    private sealed class InvocationTracker
    {
        public List<object?[]> Invocations { get; } = [];
        public void Record(object?[] arguments) => Invocations.Add(arguments);
    }

    private class RecordingProxy : DispatchProxy
    {
        public List<MethodInfo> Invocations { get; } = [];
        public List<object> Arguments { get; } = [];
        public object? ReturnValue { get; set; }
        public Func<MethodInfo, object?[]?, object?>? Handler { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            MethodInfo method = targetMethod ?? throw new InvalidOperationException();
            Invocations.Add(method);
            if (method.Name == "Add" && args is [not null])
                Arguments.Add(args[0]!);
            return Handler?.Invoke(method, args) ?? ReturnValue ?? DefaultValue(method.ReturnType);
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
