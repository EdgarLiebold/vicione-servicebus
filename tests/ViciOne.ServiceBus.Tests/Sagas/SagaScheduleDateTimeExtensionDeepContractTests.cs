using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaScheduleDateTimeExtensionDeepContractTests
{
    private static readonly DateTimeOffset DueTime = new(2035, 4, 3, 2, 1, 0, TimeSpan.Zero);
    private static readonly OutputMessage ExpectedMessage = new("scheduled");

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-SCHEDULE-DATETIME-EXTENSIONS", "exact-public-overload-census")]
    public void PublicSurface_ContainsExactlyTwentyScheduleOverloadsAcrossFourBinderShapesAndFiveInputs()
    {
        string[] actual = GetScheduleMethods().Select(Describe).Order(StringComparer.Ordinal).ToArray();
        string[] expected =
        [
            "data:async-factory:g3", "data:initialized-factory:g3", "data:message:g3", "data:sync-factory:g3", "data:task:g3",
            "data-faulted:async-factory:g4", "data-faulted:initialized-factory:g4", "data-faulted:message:g4",
            "data-faulted:sync-factory:g4", "data-faulted:task:g4",
            "faulted:async-factory:g3", "faulted:initialized-factory:g3", "faulted:message:g3", "faulted:sync-factory:g3",
            "faulted:task:g3",
            "normal:async-factory:g2", "normal:initialized-factory:g2", "normal:message:g2", "normal:sync-factory:g2",
            "normal:task:g2"
        ];

        Assert.Equal(expected.Order(StringComparer.Ordinal), actual);

        foreach (MethodInfo method in GetScheduleMethods())
        {
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(5, parameters.Length);
            Assert.Equal("source", parameters[0].Name);
            Assert.Equal(parameters[0].ParameterType, method.ReturnType);
            Assert.Equal("schedule", parameters[1].Name);
            Assert.Equal(InputParameterName(parameters[2].ParameterType), parameters[2].Name);
            Assert.Equal("timeProvider", parameters[3].Name);
            Assert.Equal("callback", parameters[4].Name);
            Assert.True(parameters[4].HasDefaultValue);
            Assert.Null(parameters[4].DefaultValue);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-SCHEDULE-DATETIME-EXTENSIONS", "receiver-required-input-boundary-matrix")]
    public void EveryOverload_RejectsReceiverScheduleMessageOrFactoryAndTimeProviderBeforeBinderEffects()
    {
        foreach (MethodInfo definition in GetScheduleMethods())
        {
            MethodInfo method = Close(definition);
            object binder = CreateProxy(method.GetParameters()[0].ParameterType, out RecordingProxy recorder);
            object[] arguments = CreateArguments(method, binder, out _, out _, out _);

            AssertArgument(method, arguments, 0, "source");
            AssertArgument(method, arguments, 1, "schedule");
            AssertArgument(method, arguments, 2, InputParameterName(method.GetParameters()[2].ParameterType));
            AssertArgument(method, arguments, 3, "timeProvider");
            Assert.Empty(recorder.Arguments);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-SCHEDULE-DATETIME-EXTENSIONS", "four-binder-five-input-runtime-matrix")]
    public async Task EveryOverload_RegistersExactActivityPreservesIdentityAndDefersRuntimeProvidersAsync()
    {
        foreach (MethodInfo definition in GetScheduleMethods())
        {
            MethodInfo method = Close(definition);
            object binder = CreateProxy(method.GetParameters()[0].ParameterType, out RecordingProxy recorder);
            recorder.ReturnValue = binder;
            object[] arguments = CreateArguments(method, binder, out InvocationTracker factoryTracker,
                out InvocationTracker timeTracker, out CallbackTracker callbackTracker);

            object? result = method.Invoke(null, arguments);

            Assert.Same(binder, result);
            object activity = Assert.Single(recorder.Arguments);
            Assert.Equal(ExpectedActivityType(method.GetParameters()[0].ParameterType), activity.GetType());
            Assert.Equal(0, factoryTracker.Count);
            Assert.Equal(0, timeTracker.Count);
            Assert.Equal(0, callbackTracker.Count);

            object messageFactory = GetField<object>(activity, "_messageFactory");
            MethodInfo getMessage = messageFactory.GetType().GetMethod("GetMessageAsync")!;
            Type contextType = getMessage.GetParameters()[0].ParameterType;
            object context = CreateProxy(contextType, out _);
            var messageTask = (Task<InitializedMessage<OutputMessage>>)getMessage.Invoke(
                messageFactory, [context, CancellationToken.None])!;
            InitializedMessage<OutputMessage> initialized = await messageTask;

            Assert.Same(ExpectedMessage, initialized.Message);
            bool factoryInput = IsFactoryInput(method.GetParameters()[2].ParameterType);
            Assert.Equal(factoryInput ? 1 : 0, factoryTracker.Count);
            if (factoryInput)
                Assert.Same(context, factoryTracker.Context);
            else
                Assert.Null(factoryTracker.Context);
            Assert.Equal(0, timeTracker.Count);

            var timeProvider = GetField<Delegate>(activity, "_timeProvider");
            Assert.Equal(DueTime, timeProvider.DynamicInvoke(context));
            Assert.Equal(1, timeTracker.Count);
            Assert.Same(context, timeTracker.Context);

            SendContext<OutputMessage> sendContext = CreateProxy<SendContext<OutputMessage>>(out _);
            await initialized.Pipe.SendAsync(sendContext);
            Assert.Equal(1, callbackTracker.Count);
            Assert.Same(sendContext, callbackTracker.Context);
        }
    }

    private static MethodInfo[] GetScheduleMethods() => typeof(ScheduleDateTimeExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Where(method => method.Name == nameof(ScheduleDateTimeExtensions.Schedule))
        .OrderBy(Describe, StringComparer.Ordinal)
        .ToArray();

    private static string Describe(MethodInfo method)
    {
        Type binder = method.GetParameters()[0].ParameterType.GetGenericTypeDefinition();
        string shape = binder == typeof(IEventActivityBinder<>)
            ? "normal"
            : binder == typeof(IEventActivityBinder<,>)
                ? "data"
                : binder == typeof(IExceptionActivityBinder<,>)
                    ? "faulted"
                    : "data-faulted";
        return $"{shape}:{InputKind(method.GetParameters()[2].ParameterType)}:g{method.GetGenericArguments().Length}";
    }

    private static string InputKind(Type type)
    {
        if (type.IsGenericParameter || type == typeof(OutputMessage))
            return "message";
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            return "task";
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Func<,>))
            return "initialized-factory";
        return type.Name.StartsWith("Async", StringComparison.Ordinal) ? "async-factory" : "sync-factory";
    }

    private static string InputParameterName(Type type) => InputKind(type) is "message" or "task" ? "message" : "messageFactory";

    private static bool IsFactoryInput(Type type) => InputKind(type) is not ("message" or "task");

    private static MethodInfo Close(MethodInfo definition)
    {
        Type binder = definition.GetParameters()[0].ParameterType.GetGenericTypeDefinition();
        Type[] arguments = binder == typeof(IEventActivityBinder<>)
            ? [typeof(TestSaga), typeof(OutputMessage)]
            : binder == typeof(IEventActivityBinder<,>)
                ? [typeof(TestSaga), typeof(InputMessage), typeof(OutputMessage)]
                : binder == typeof(IExceptionActivityBinder<,>)
                    ? [typeof(TestSaga), typeof(InvalidOperationException), typeof(OutputMessage)]
                    : [typeof(TestSaga), typeof(InputMessage), typeof(InvalidOperationException), typeof(OutputMessage)];
        return definition.MakeGenericMethod(arguments);
    }

    private static object[] CreateArguments(MethodInfo method, object binder, out InvocationTracker factoryTracker,
        out InvocationTracker timeTracker, out CallbackTracker callbackTracker)
    {
        ParameterInfo[] parameters = method.GetParameters();
        object schedule = CreateProxy(parameters[1].ParameterType, out _);
        factoryTracker = new InvocationTracker();
        timeTracker = new InvocationTracker();
        callbackTracker = new CallbackTracker();
        object input = CreateInput(parameters[2].ParameterType, factoryTracker);
        Delegate timeProvider = CreateDelegate(parameters[3].ParameterType, timeTracker, DueTime);
        Action<SendContext<OutputMessage>> callback = callbackTracker.Record;
        return [binder, schedule, input, timeProvider, callback];
    }

    private static object CreateInput(Type type, InvocationTracker tracker)
    {
        string kind = InputKind(type);
        return kind switch
        {
            "message" => ExpectedMessage,
            "task" => Task.FromResult(ExpectedMessage),
            "initialized-factory" => CreateDelegate(type, tracker,
                Task.FromResult(new InitializedMessage<OutputMessage>(ExpectedMessage))),
            "async-factory" => CreateDelegate(type, tracker, Task.FromResult(ExpectedMessage)),
            _ => CreateDelegate(type, tracker, ExpectedMessage)
        };
    }

    private static Delegate CreateDelegate(Type delegateType, InvocationTracker tracker, object returnValue)
    {
        MethodInfo invoke = delegateType.GetMethod("Invoke")!;
        ParameterExpression context = Expression.Parameter(invoke.GetParameters()[0].ParameterType, "context");
        MethodCallExpression record = Expression.Call(Expression.Constant(tracker), nameof(InvocationTracker.Record), null,
            Expression.Convert(context, typeof(object)));
        BlockExpression body = Expression.Block(record, Expression.Constant(returnValue, invoke.ReturnType));
        return Expression.Lambda(delegateType, body, context).Compile();
    }

    private static void AssertArgument(MethodInfo method, object[] arguments, int index, string parameterName)
    {
        object?[] invocation = [.. arguments];
        invocation[index] = null;
        TargetInvocationException wrapper = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, invocation));
        ArgumentNullException exception = Assert.IsType<ArgumentNullException>(wrapper.InnerException);
        Assert.Equal(parameterName, exception.ParamName);
    }

    private static Type ExpectedActivityType(Type binderType)
    {
        Type binder = binderType.GetGenericTypeDefinition();
        if (binder == typeof(IEventActivityBinder<>))
            return typeof(ScheduleActivity<TestSaga, OutputMessage>);
        if (binder == typeof(IEventActivityBinder<,>))
            return typeof(ScheduleActivity<TestSaga, InputMessage, OutputMessage>);

        string name = binder == typeof(IExceptionActivityBinder<,>)
            ? "ViciOne.ServiceBus.SagaStateMachine.FaultedScheduleActivity`3"
            : "ViciOne.ServiceBus.SagaStateMachine.FaultedScheduleActivity`4";
        Type definition = typeof(ScheduleDateTimeExtensions).Assembly.GetType(name, throwOnError: true)!;
        return binder == typeof(IExceptionActivityBinder<,>)
            ? definition.MakeGenericType(typeof(TestSaga), typeof(InvalidOperationException), typeof(OutputMessage))
            : definition.MakeGenericType(typeof(TestSaga), typeof(InputMessage), typeof(InvalidOperationException), typeof(OutputMessage));
    }

    private static T GetField<T>(object instance, string name) =>
        (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;

    private static T CreateProxy<T>(out RecordingProxy recorder)
        where T : class
    {
        T proxy = DispatchProxy.Create<T, RecordingProxy>();
        recorder = (RecordingProxy)(object)proxy;
        return proxy;
    }

    private static object CreateProxy(Type interfaceType, out RecordingProxy recorder)
    {
        MethodInfo create = typeof(SagaScheduleDateTimeExtensionDeepContractTests).GetMethod(
            nameof(CreateProxyObject), BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(interfaceType);
        object proxy = create.Invoke(null, null)!;
        recorder = (RecordingProxy)proxy;
        return proxy;
    }

    private static object CreateProxyObject<T>() where T : class => DispatchProxy.Create<T, RecordingProxy>();

    private sealed class InvocationTracker
    {
        public int Count { get; private set; }
        public object? Context { get; private set; }
        public void Record(object context)
        {
            Count++;
            Context = context;
        }
    }

    private sealed class CallbackTracker
    {
        public int Count { get; private set; }
        public SendContext<OutputMessage>? Context { get; private set; }
        public void Record(SendContext<OutputMessage> context)
        {
            Count++;
            Context = context;
        }
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
