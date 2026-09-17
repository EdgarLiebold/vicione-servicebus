using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaScheduleTimeSpanExtensionDeepContractTests
{
    static readonly DateTimeOffset UtcNow = new(2037, 4, 5, 6, 7, 8, TimeSpan.Zero);
    static readonly TimeSpan ScheduleDelay = TimeSpan.FromMinutes(11);
    static readonly TimeSpan ExplicitDelay = TimeSpan.FromSeconds(47);
    static readonly OutputMessage ExpectedMessage = new("scheduled");

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-SCHEDULE-TIMESPAN-EXTENSIONS", "exact-forty-four-overload-surface")]
    public void PublicSurface_ExposesExactlyFortyScheduleAndFourUnscheduleShapes()
    {
        MethodInfo[] schedules = GetMethods(nameof(ScheduleTimeSpanExtensions.Schedule));
        MethodInfo[] unschedules = GetMethods(nameof(ScheduleTimeSpanExtensions.Unschedule));

        Assert.Equal(40, schedules.Length);
        Assert.Equal(ExpectedScheduleVariants(), schedules.Select(Variant).Order(StringComparer.Ordinal));
        Assert.Equal(4, unschedules.Length);
        Assert.Equal(
            new[]
            {
                "data-faulted:Unschedule:g3", "data:Unschedule:g2", "faulted:Unschedule:g2", "normal:Unschedule:g1",
            },
            unschedules.Select(Variant).Order(StringComparer.Ordinal));

        foreach (MethodInfo method in schedules)
        {
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal("source", parameters[0].Name);
            Assert.Equal("schedule", parameters[1].Name);
            Assert.Equal(InputName(parameters[2].ParameterType), parameters[2].Name);
            Assert.Equal("callback", parameters[^1].Name);
            Assert.True(parameters[^1].HasDefaultValue);
            Assert.Null(parameters[^1].DefaultValue);
            Assert.Equal(parameters[0].ParameterType, method.ReturnType);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-SCHEDULE-TIMESPAN-EXTENSIONS", "required-boundaries-and-ordering")]
    public void EveryScheduleOverload_RejectsOwnedNullsInParameterOrderBeforeAnyEffect()
    {
        foreach (MethodInfo definition in GetMethods(nameof(ScheduleTimeSpanExtensions.Schedule)))
        {
            MethodInfo method = Close(definition);
            object binder = CreateProxy(method.GetParameters()[0].ParameterType, out RecordingProxy binderRecorder);
            object[] valid = CreateScheduleArguments(method, binder, out RecordingProxy scheduleRecorder,
                out InvocationTracker inputTracker, out InvocationTracker delayTracker);
            int[] guarded = GuardedParameterIndexes(method);

            for (var guardPosition = 0; guardPosition < guarded.Length; guardPosition++)
            {
                object?[] arguments = [.. valid];
                for (var index = guardPosition; index < guarded.Length; index++)
                    arguments[guarded[index]] = null;

                TargetInvocationException wrapper = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, arguments));
                ArgumentNullException exception = Assert.IsType<ArgumentNullException>(wrapper.InnerException);
                Assert.Equal(method.GetParameters()[guarded[guardPosition]].Name, exception.ParamName);
                Assert.Empty(binderRecorder.Arguments);
                Assert.Empty(scheduleRecorder.Invocations);
                Assert.Equal(0, inputTracker.Count);
                Assert.Equal(0, delayTracker.Count);
            }
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-SCHEDULE-TIMESPAN-EXTENSIONS", "activity-provider-context-and-time-matrix")]
    public async Task EveryScheduleOverload_PreservesActivityIdentityProviderLazinessContextAndUtcArithmeticAsync()
    {
        foreach (MethodInfo definition in GetMethods(nameof(ScheduleTimeSpanExtensions.Schedule)))
        {
            MethodInfo method = Close(definition);
            ParameterInfo[] parameters = method.GetParameters();
            object binder = CreateProxy(parameters[0].ParameterType, out RecordingProxy binderRecorder);
            object result = CreateProxy(parameters[0].ParameterType, out _);
            binderRecorder.ReturnValue = result;
            object[] arguments = CreateScheduleArguments(method, binder, out RecordingProxy scheduleRecorder,
                out InvocationTracker inputTracker, out InvocationTracker delayTracker);

            object? returned = method.Invoke(null, arguments);

            Assert.Same(result, returned);
            object activity = Assert.Single(binderRecorder.Arguments);
            Assert.Equal(ExpectedScheduleActivityType(parameters[0].ParameterType), activity.GetType());
            Assert.Same(arguments[1], GetField<object>(activity, "_schedule"));
            Assert.Equal(0, inputTracker.Count);
            Assert.Equal(0, delayTracker.Count);
            Assert.Empty(scheduleRecorder.Invocations);

            Delegate timeProvider = GetField<Delegate>(activity, "_timeProvider");
            Type contextType = timeProvider.GetType().GetMethod("Invoke")!.GetParameters()[0].ParameterType;
            object context = CreateContext(contextType, new FixedTimeProvider(UtcNow));
            DateTimeOffset dueAt = Assert.IsType<DateTimeOffset>(timeProvider.DynamicInvoke(context));
            bool hasExplicitDelay = parameters.Any(parameter => parameter.Name == "delayProvider");

            Assert.Equal(UtcNow + (hasExplicitDelay ? ExplicitDelay : ScheduleDelay), dueAt);
            Assert.Equal(hasExplicitDelay ? 1 : 0, delayTracker.Count);
            Assert.Same(hasExplicitDelay ? context : null, delayTracker.Context);
            MethodInfo[] delayCalls = scheduleRecorder.Invocations.Where(invocation => invocation.Name == nameof(ISchedule<TestSaga>.GetDelay)).ToArray();
            Assert.Equal(hasExplicitDelay ? 0 : 1, delayCalls.Length);
            if (!hasExplicitDelay)
                Assert.Same(context, scheduleRecorder.InvocationArguments.Single(arguments => arguments.Method.Name == nameof(ISchedule<TestSaga>.GetDelay)).Arguments![0]);

            object messageFactory = GetField<object>(activity, "_messageFactory");
            MethodInfo getMessage = messageFactory.GetType().GetMethod("GetMessageAsync")!;
            var messageTask = (Task<InitializedMessage<OutputMessage>>)getMessage.Invoke(
                messageFactory, [context, CancellationToken.None])!;
            InitializedMessage<OutputMessage> initialized = await messageTask;

            Assert.Same(ExpectedMessage, initialized.Message);
            bool delegateInput = IsDelegate(parameters[2].ParameterType);
            Assert.Equal(delegateInput ? 1 : 0, inputTracker.Count);
            Assert.Same(delegateInput ? context : null, inputTracker.Context);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-SCHEDULE-TIMESPAN-EXTENSIONS", "all-unschedule-binder-shapes")]
    public void EveryUnscheduleOverload_ValidatesInputsAndRegistersTheExactActivity()
    {
        foreach (MethodInfo definition in GetMethods(nameof(ScheduleTimeSpanExtensions.Unschedule)))
        {
            MethodInfo method = Close(definition);
            ParameterInfo[] parameters = method.GetParameters();
            object binder = CreateProxy(parameters[0].ParameterType, out RecordingProxy binderRecorder);
            object result = CreateProxy(parameters[0].ParameterType, out _);
            binderRecorder.ReturnValue = result;
            object schedule = CreateProxy(parameters[1].ParameterType, out _);

            AssertArgument(method, [null, null], "source");
            AssertArgument(method, [binder, null], "schedule");
            Assert.Empty(binderRecorder.Arguments);

            object? returned = method.Invoke(null, [binder, schedule]);

            Assert.Same(result, returned);
            object activity = Assert.Single(binderRecorder.Arguments);
            Assert.Equal(ExpectedUnscheduleActivityType(parameters[0].ParameterType), activity.GetType());
            Assert.Same(schedule, GetField<object>(activity, "_schedule"));
        }
    }

    static MethodInfo[] GetMethods(string name) =>
        typeof(ScheduleTimeSpanExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name == name)
            .OrderBy(Variant, StringComparer.Ordinal)
            .ToArray();

    static string[] ExpectedScheduleVariants()
    {
        string[] binders = ["normal", "data", "faulted", "data-faulted"];
        string[] inputs = ["message", "task", "sync", "async", "initialized"];
        return binders.SelectMany(binder => new[] { "schedule", "explicit" }
                .SelectMany(delay => inputs.Select(input => $"{binder}:Schedule:{delay}:{input}")))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    static string Variant(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        string binder = BinderKind(parameters[0].ParameterType);
        if (method.Name == nameof(ScheduleTimeSpanExtensions.Unschedule))
            return $"{binder}:Unschedule:g{method.GetGenericArguments().Length}";

        string delay = parameters.Any(parameter => parameter.Name == "delayProvider") ? "explicit" : "schedule";
        Type input = parameters[2].ParameterType;
        string inputKind = input.IsGenericParameter ? "message"
            : input.IsGenericType && input.GetGenericTypeDefinition() == typeof(Task<>) ? "task"
            : input.IsGenericType && input.GetGenericTypeDefinition() == typeof(Func<,>) ? "initialized"
            : input.Name.StartsWith("Async", StringComparison.Ordinal) ? "async"
            : "sync";
        return $"{binder}:Schedule:{delay}:{inputKind}";
    }

    static string BinderKind(Type type) => type.GetGenericTypeDefinition() switch
    {
        Type definition when definition == typeof(IEventActivityBinder<>) => "normal",
        Type definition when definition == typeof(IEventActivityBinder<,>) => "data",
        Type definition when definition == typeof(IExceptionActivityBinder<,>) => "faulted",
        Type definition when definition == typeof(IExceptionActivityBinder<,,>) => "data-faulted",
        _ => throw new InvalidOperationException($"Unexpected binder type {type}."),
    };

    static string InputName(Type type) => type.IsGenericParameter || type.GetGenericTypeDefinition() == typeof(Task<>)
        ? "message"
        : "messageFactory";

    static MethodInfo Close(MethodInfo method) => method.MakeGenericMethod(
        method.GetGenericArguments().Select(parameter => parameter.Name switch
        {
            "TSaga" => typeof(TestSaga),
            "TData" => typeof(InputMessage),
            "TException" => typeof(InvalidOperationException),
            "TMessage" => typeof(OutputMessage),
            _ => throw new InvalidOperationException(parameter.Name),
        }).ToArray());

    static object[] CreateScheduleArguments(MethodInfo method, object binder, out RecordingProxy scheduleRecorder,
        out InvocationTracker inputTracker, out InvocationTracker delayTracker)
    {
        ParameterInfo[] parameters = method.GetParameters();
        object schedule = CreateProxy(parameters[1].ParameterType, out scheduleRecorder);
        scheduleRecorder.Handler = (invocation, arguments) =>
            invocation.Name == nameof(ISchedule<TestSaga>.GetDelay) ? ScheduleDelay : DefaultValue(invocation.ReturnType);
        inputTracker = new InvocationTracker();
        delayTracker = new InvocationTracker();
        object?[] arguments = new object?[parameters.Length];
        arguments[0] = binder;
        arguments[1] = schedule;
        arguments[2] = CreateInput(parameters[2].ParameterType, inputTracker);
        for (var index = 3; index < parameters.Length; index++)
        {
            arguments[index] = parameters[index].Name == "delayProvider"
                ? CreateDelegate(parameters[index].ParameterType, ExplicitDelay, delayTracker)
                : (Action<SendContext<OutputMessage>>)(_ => { });
        }

        return arguments!;
    }

    static object CreateInput(Type type, InvocationTracker tracker)
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

    static Delegate CreateDelegate(Type delegateType, object returnValue, InvocationTracker tracker)
    {
        MethodInfo invoke = delegateType.GetMethod("Invoke")!;
        ParameterExpression context = Expression.Parameter(invoke.GetParameters()[0].ParameterType, "context");
        Expression body = Expression.Block(
            Expression.Call(Expression.Constant(tracker), nameof(InvocationTracker.Record), Type.EmptyTypes,
                Expression.Convert(context, typeof(object))),
            Expression.Constant(returnValue, invoke.ReturnType));
        return Expression.Lambda(delegateType, body, context).Compile();
    }

    static int[] GuardedParameterIndexes(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        var indexes = new List<int> { 0, 1 };
        if (parameters[2].ParameterType == typeof(Task<OutputMessage>) || IsDelegate(parameters[2].ParameterType))
            indexes.Add(2);
        int delay = Array.FindIndex(parameters, parameter => parameter.Name == "delayProvider");
        if (delay >= 0)
            indexes.Add(delay);
        return indexes.ToArray();
    }

    static bool IsDelegate(Type type) => typeof(Delegate).IsAssignableFrom(type);

    static Type ExpectedScheduleActivityType(Type binderType)
    {
        Type definition = binderType.GetGenericTypeDefinition();
        Type[] arguments = binderType.GetGenericArguments();
        if (definition == typeof(IEventActivityBinder<>))
            return typeof(ScheduleActivity<,>).MakeGenericType(arguments[0], typeof(OutputMessage));
        if (definition == typeof(IEventActivityBinder<,>))
            return typeof(ScheduleActivity<,,>).MakeGenericType(arguments[0], arguments[1], typeof(OutputMessage));

        string name = definition == typeof(IExceptionActivityBinder<,>)
            ? "ViciOne.ServiceBus.SagaStateMachine.FaultedScheduleActivity`3"
            : "ViciOne.ServiceBus.SagaStateMachine.FaultedScheduleActivity`4";
        Type activity = typeof(ScheduleTimeSpanExtensions).Assembly.GetType(name, throwOnError: true)!;
        return definition == typeof(IExceptionActivityBinder<,>)
            ? activity.MakeGenericType(arguments[0], arguments[1], typeof(OutputMessage))
            : activity.MakeGenericType(arguments[0], arguments[1], arguments[2], typeof(OutputMessage));
    }

    static Type ExpectedUnscheduleActivityType(Type binderType) =>
        binderType.GetGenericTypeDefinition() is Type definition
        && (definition == typeof(IEventActivityBinder<>) || definition == typeof(IEventActivityBinder<,>))
            ? typeof(UnscheduleActivity<TestSaga>)
            : typeof(FaultedUnscheduleActivity<TestSaga>);

    static object CreateContext(Type contextType, TimeProvider timeProvider)
    {
        object context = CreateProxy(contextType, out RecordingProxy recorder);
        recorder.Handler = (method, arguments) =>
        {
            if (method.Name == nameof(PipeContext.TryGetPayload)
                && method.GetGenericArguments() is [Type payloadType]
                && payloadType == typeof(TimeProvider))
            {
                arguments![0] = timeProvider;
                return true;
            }

            return DefaultValue(method.ReturnType);
        };
        return context;
    }

    static void AssertArgument(MethodInfo method, object?[] arguments, string parameterName)
    {
        TargetInvocationException wrapper = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, arguments));
        Assert.Equal(parameterName, Assert.IsType<ArgumentNullException>(wrapper.InnerException).ParamName);
    }

    static T GetField<T>(object instance, string name)
    {
        for (Type? type = instance.GetType(); type is not null; type = type.BaseType)
        {
            FieldInfo? field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field is not null)
                return (T)field.GetValue(instance)!;
        }

        throw new InvalidOperationException($"Field '{name}' was not found on {instance.GetType()}.");
    }

    static object CreateProxy(Type interfaceType, out RecordingProxy recorder)
    {
        object proxy = DispatchProxy.Create(interfaceType, typeof(RecordingProxy));
        recorder = (RecordingProxy)proxy;
        return proxy;
    }

    static object? DefaultValue(Type type) => type == typeof(void) ? null
        : type == typeof(Task) ? Task.CompletedTask
        : type.IsValueType ? Activator.CreateInstance(type)
        : null;

    sealed class InvocationTracker
    {
        public int Count { get; private set; }
        public object? Context { get; private set; }
        public void Record(object context)
        {
            Count++;
            Context = context;
        }
    }

    class RecordingProxy : DispatchProxy
    {
        public List<object> Arguments { get; } = [];
        public List<MethodInfo> Invocations { get; } = [];
        public List<(MethodInfo Method, object?[]? Arguments)> InvocationArguments { get; } = [];
        public object? ReturnValue { get; set; }
        public Func<MethodInfo, object?[]?, object?>? Handler { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            MethodInfo method = targetMethod ?? throw new InvalidOperationException("A proxied call requires method metadata.");
            Invocations.Add(method);
            InvocationArguments.Add((method, args));
            if (method.Name == "Add" && args is [not null])
                Arguments.Add(args[0]!);
            return Handler?.Invoke(method, args) ?? ReturnValue ?? DefaultValue(method.ReturnType);
        }
    }

    sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    sealed class TestSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = "Initial";
    }

    sealed record InputMessage(string Value = "input");
    sealed record OutputMessage(string Value);
}
