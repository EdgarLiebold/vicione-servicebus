using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaPublishExtensionDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PUBLISH-EXTENSIONS", "exact-public-overload-census")]
    public void PublicSurface_ContainsExactlyTwentyPublishOverloadsAcrossFourBinderShapesAndFiveInputs()
    {
        string[] actual = GetPublishMethods().Select(Describe).Order(StringComparer.Ordinal).ToArray();
        string[] expected =
        [
            "data:Publish:message:g3",
            "data:Publish:sync-factory:g3",
            "data:PublishAwaited:async-factory:g3",
            "data:PublishAwaited:initialized-factory:g3",
            "data:PublishAwaited:task:g3",
            "data-faulted:Publish:message:g4",
            "data-faulted:Publish:sync-factory:g4",
            "data-faulted:PublishAwaited:async-factory:g4",
            "data-faulted:PublishAwaited:initialized-factory:g4",
            "data-faulted:PublishAwaited:task:g4",
            "faulted:Publish:message:g3",
            "faulted:Publish:sync-factory:g3",
            "faulted:PublishAwaited:async-factory:g3",
            "faulted:PublishAwaited:initialized-factory:g3",
            "faulted:PublishAwaited:task:g3",
            "normal:Publish:message:g2",
            "normal:Publish:sync-factory:g2",
            "normal:PublishAwaited:async-factory:g2",
            "normal:PublishAwaited:initialized-factory:g2",
            "normal:PublishAwaited:task:g2"
        ];

        Assert.Equal(expected.Order(StringComparer.Ordinal), actual);

        foreach (MethodInfo method in GetPublishMethods())
        {
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(3, parameters.Length);
            Assert.Equal("source", parameters[0].Name);
            Assert.Equal(parameters[0].ParameterType, method.ReturnType);
            Assert.Equal(InputParameterName(parameters[1].ParameterType), parameters[1].Name);
            Assert.Equal("callback", parameters[2].Name);
            Assert.True(parameters[2].HasDefaultValue);
            Assert.Null(parameters[2].DefaultValue);

            Type messageType = method.GetGenericArguments()[^1];
            Type expectedCallback = typeof(Action<>).MakeGenericType(typeof(PublishContext<>).MakeGenericType(messageType));
            Assert.Equal(expectedCallback, parameters[2].ParameterType);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PUBLISH-EXTENSIONS", "receiver-and-input-boundary-ownership")]
    public void EveryOverload_RejectsNullReceiverBeforeInputAndNullInputBeforeBinderEffects()
    {
        foreach (MethodInfo definition in GetPublishMethods())
        {
            MethodInfo method = Close(definition);

            ArgumentNullException receiver = InvokeNullBoundary(method, null, null);
            Assert.Equal("source", receiver.ParamName);

            object binder = CreateProxy(method.GetParameters()[0].ParameterType, out RecordingProxy recorder);
            ArgumentNullException input = InvokeNullBoundary(method, binder, null);
            Assert.Equal(InputParameterName(method.GetParameters()[1].ParameterType), input.ParamName);
            Assert.Empty(recorder.Arguments);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PUBLISH-EXTENSIONS", "normal-runtime-matrix")]
    public async Task NormalBinder_PreservesResultActivityMessagesFactoriesAndContextsAcrossFiveInputsAsync()
    {
        IEventActivityBinder<TestSaga> binder = CreateProxy<IEventActivityBinder<TestSaga>>(out RecordingProxy recorder);
        recorder.ReturnValue = binder;
        var message = new OutputMessage("normal");
        IBehaviorContext<TestSaga> context = CreateProxy<IBehaviorContext<TestSaga>>(out _);
        var syncCount = 0;
        var asyncCount = 0;
        var initializedCount = 0;
        object? syncContext = null;
        object? asyncContext = null;
        object? initializedContext = null;
        EventMessageFactory<TestSaga, OutputMessage> syncFactory = value =>
        {
            syncCount++;
            syncContext = value;
            return message;
        };
        AsyncEventMessageFactory<TestSaga, OutputMessage> asyncFactory = value =>
        {
            asyncCount++;
            asyncContext = value;
            return Task.FromResult(message);
        };
        Func<IBehaviorContext<TestSaga>, Task<InitializedMessage<OutputMessage>>> initializedFactory = value =>
        {
            initializedCount++;
            initializedContext = value;
            return Task.FromResult(new InitializedMessage<OutputMessage>(message));
        };

        Assert.Same(binder, binder.Publish(message));
        Assert.Same(binder, binder.PublishAwaited(Task.FromResult(message)));
        Assert.Same(binder, binder.Publish(syncFactory));
        Assert.Same(binder, binder.PublishAwaited(asyncFactory));
        Assert.Same(binder, binder.PublishAwaited(initializedFactory));

        await AssertRuntimeMatrixAsync<PublishActivity<TestSaga, OutputMessage>, IBehaviorContext<TestSaga>>(
            recorder, context, message);
        AssertFactoryObservations(context, syncContext, asyncContext, initializedContext, syncCount, asyncCount, initializedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PUBLISH-EXTENSIONS", "data-runtime-matrix")]
    public async Task DataBinder_PreservesResultActivityMessagesFactoriesAndContextsAcrossFiveInputsAsync()
    {
        IEventActivityBinder<TestSaga, InputMessage> binder =
            CreateProxy<IEventActivityBinder<TestSaga, InputMessage>>(out RecordingProxy recorder);
        recorder.ReturnValue = binder;
        var message = new OutputMessage("data");
        IBehaviorContext<TestSaga, InputMessage> context = CreateProxy<IBehaviorContext<TestSaga, InputMessage>>(out _);
        var syncCount = 0;
        var asyncCount = 0;
        var initializedCount = 0;
        object? syncContext = null;
        object? asyncContext = null;
        object? initializedContext = null;
        EventMessageFactory<TestSaga, InputMessage, OutputMessage> syncFactory = value =>
        {
            syncCount++;
            syncContext = value;
            return message;
        };
        AsyncEventMessageFactory<TestSaga, InputMessage, OutputMessage> asyncFactory = value =>
        {
            asyncCount++;
            asyncContext = value;
            return Task.FromResult(message);
        };
        Func<IBehaviorContext<TestSaga, InputMessage>, Task<InitializedMessage<OutputMessage>>> initializedFactory = value =>
        {
            initializedCount++;
            initializedContext = value;
            return Task.FromResult(new InitializedMessage<OutputMessage>(message));
        };

        Assert.Same(binder, binder.Publish(message));
        Assert.Same(binder, binder.PublishAwaited(Task.FromResult(message)));
        Assert.Same(binder, binder.Publish(syncFactory));
        Assert.Same(binder, binder.PublishAwaited(asyncFactory));
        Assert.Same(binder, binder.PublishAwaited(initializedFactory));

        await AssertRuntimeMatrixAsync<PublishActivity<TestSaga, InputMessage, OutputMessage>, IBehaviorContext<TestSaga, InputMessage>>(
            recorder, context, message);
        AssertFactoryObservations(context, syncContext, asyncContext, initializedContext, syncCount, asyncCount, initializedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PUBLISH-EXTENSIONS", "faulted-runtime-matrix")]
    public async Task FaultedBinder_PreservesResultActivityMessagesFactoriesAndContextsAcrossFiveInputsAsync()
    {
        IExceptionActivityBinder<TestSaga, InvalidOperationException> binder =
            CreateProxy<IExceptionActivityBinder<TestSaga, InvalidOperationException>>(out RecordingProxy recorder);
        recorder.ReturnValue = binder;
        var message = new OutputMessage("faulted");
        IBehaviorExceptionContext<TestSaga, InvalidOperationException> context =
            CreateProxy<IBehaviorExceptionContext<TestSaga, InvalidOperationException>>(out _);
        var syncCount = 0;
        var asyncCount = 0;
        var initializedCount = 0;
        object? syncContext = null;
        object? asyncContext = null;
        object? initializedContext = null;
        EventExceptionMessageFactory<TestSaga, InvalidOperationException, OutputMessage> syncFactory = value =>
        {
            syncCount++;
            syncContext = value;
            return message;
        };
        AsyncEventExceptionMessageFactory<TestSaga, InvalidOperationException, OutputMessage> asyncFactory = value =>
        {
            asyncCount++;
            asyncContext = value;
            return Task.FromResult(message);
        };
        Func<IBehaviorExceptionContext<TestSaga, InvalidOperationException>, Task<InitializedMessage<OutputMessage>>> initializedFactory = value =>
        {
            initializedCount++;
            initializedContext = value;
            return Task.FromResult(new InitializedMessage<OutputMessage>(message));
        };

        Assert.Same(binder, binder.Publish(message));
        Assert.Same(binder, binder.PublishAwaited(Task.FromResult(message)));
        Assert.Same(binder, binder.Publish(syncFactory));
        Assert.Same(binder, binder.PublishAwaited(asyncFactory));
        Assert.Same(binder, binder.PublishAwaited(initializedFactory));

        await AssertRuntimeMatrixAsync<FaultedPublishActivity<TestSaga, InvalidOperationException, OutputMessage>,
            IBehaviorExceptionContext<TestSaga, InvalidOperationException>>(recorder, context, message);
        AssertFactoryObservations(context, syncContext, asyncContext, initializedContext, syncCount, asyncCount, initializedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PUBLISH-EXTENSIONS", "data-faulted-runtime-matrix")]
    public async Task DataFaultedBinder_PreservesResultActivityMessagesFactoriesAndContextsAcrossFiveInputsAsync()
    {
        IExceptionActivityBinder<TestSaga, InputMessage, InvalidOperationException> binder =
            CreateProxy<IExceptionActivityBinder<TestSaga, InputMessage, InvalidOperationException>>(out RecordingProxy recorder);
        recorder.ReturnValue = binder;
        var message = new OutputMessage("data-faulted");
        IBehaviorExceptionContext<TestSaga, InputMessage, InvalidOperationException> context =
            CreateProxy<IBehaviorExceptionContext<TestSaga, InputMessage, InvalidOperationException>>(out _);
        var syncCount = 0;
        var asyncCount = 0;
        var initializedCount = 0;
        object? syncContext = null;
        object? asyncContext = null;
        object? initializedContext = null;
        EventExceptionMessageFactory<TestSaga, InputMessage, InvalidOperationException, OutputMessage> syncFactory = value =>
        {
            syncCount++;
            syncContext = value;
            return message;
        };
        AsyncEventExceptionMessageFactory<TestSaga, InputMessage, InvalidOperationException, OutputMessage> asyncFactory = value =>
        {
            asyncCount++;
            asyncContext = value;
            return Task.FromResult(message);
        };
        Func<IBehaviorExceptionContext<TestSaga, InputMessage, InvalidOperationException>, Task<InitializedMessage<OutputMessage>>>
            initializedFactory = value =>
            {
                initializedCount++;
                initializedContext = value;
                return Task.FromResult(new InitializedMessage<OutputMessage>(message));
            };

        Assert.Same(binder, binder.Publish(message));
        Assert.Same(binder, binder.PublishAwaited(Task.FromResult(message)));
        Assert.Same(binder, binder.Publish(syncFactory));
        Assert.Same(binder, binder.PublishAwaited(asyncFactory));
        Assert.Same(binder, binder.PublishAwaited(initializedFactory));

        await AssertRuntimeMatrixAsync<FaultedPublishActivity<TestSaga, InputMessage, InvalidOperationException, OutputMessage>,
            IBehaviorExceptionContext<TestSaga, InputMessage, InvalidOperationException>>(recorder, context, message);
        AssertFactoryObservations(context, syncContext, asyncContext, initializedContext, syncCount, asyncCount, initializedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PUBLISH-EXTENSIONS", "publish-context-callback-uplift")]
    public void CallbackUplift_PreservesNullAndDeliversTheExactPublishPayloadOnceWithoutEarlyInvocation()
    {
        MethodInfo uplift = typeof(PublishExtensions).GetMethod("Uplift", BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(OutputMessage));

        Assert.Null(uplift.Invoke(null, [null]));

        PublishContext<OutputMessage> payload = CreateProxy<PublishContext<OutputMessage>>(out _);
        object? observed = null;
        var callbackCount = 0;
        Action<PublishContext<OutputMessage>> callback = value =>
        {
            callbackCount++;
            observed = value;
        };

        var uplifted = Assert.IsType<Action<SendContext<OutputMessage>>>(uplift.Invoke(null, [callback]));
        Assert.Equal(0, callbackCount);

        SendContext<OutputMessage> sendContext = CreateProxy<SendContext<OutputMessage>>(out RecordingProxy recorder);
        recorder.Handler = (method, arguments) =>
        {
            if (method.Name == nameof(PipeContext.TryGetPayload)
                && method.GetGenericArguments() is [Type payloadType]
                && payloadType == typeof(PublishContext<OutputMessage>))
            {
                arguments![0] = payload;
                return true;
            }

            return DefaultValue(method.ReturnType);
        };

        uplifted(sendContext);

        Assert.Equal(1, callbackCount);
        Assert.Same(payload, observed);
        Assert.Single(recorder.Invocations, method => method.Name == nameof(PipeContext.TryGetPayload));
    }

    private static MethodInfo[] GetPublishMethods() =>
        typeof(PublishExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name is nameof(PublishExtensions.Publish) or nameof(PublishExtensions.PublishAwaited))
            .ToArray();

    private static string Describe(MethodInfo method)
    {
        Type sourceDefinition = method.GetParameters()[0].ParameterType.GetGenericTypeDefinition();
        string binder = sourceDefinition == typeof(IEventActivityBinder<>)
            ? "normal"
            : sourceDefinition == typeof(IEventActivityBinder<,>)
                ? "data"
                : sourceDefinition == typeof(IExceptionActivityBinder<,>)
                    ? "faulted"
                    : sourceDefinition == typeof(IExceptionActivityBinder<,,>)
                        ? "data-faulted"
                        : throw new InvalidOperationException($"Unexpected binder {sourceDefinition}.");

        return $"{binder}:{method.Name}:{DescribeInput(method.GetParameters()[1].ParameterType)}:g{method.GetGenericArguments().Length}";
    }

    private static string DescribeInput(Type input)
    {
        if (input.IsGenericParameter || input == typeof(OutputMessage))
            return "message";

        Type definition = input.GetGenericTypeDefinition();
        if (definition == typeof(Task<>))
            return "task";
        if (definition == typeof(EventMessageFactory<,>)
            || definition == typeof(EventMessageFactory<,,>)
            || definition == typeof(EventExceptionMessageFactory<,,>)
            || definition == typeof(EventExceptionMessageFactory<,,,>))
            return "sync-factory";
        if (definition == typeof(AsyncEventMessageFactory<,>)
            || definition == typeof(AsyncEventMessageFactory<,,>)
            || definition == typeof(AsyncEventExceptionMessageFactory<,,>)
            || definition == typeof(AsyncEventExceptionMessageFactory<,,,>))
            return "async-factory";
        if (definition == typeof(Func<,>))
            return "initialized-factory";

        throw new InvalidOperationException($"Unexpected publish input {input}.");
    }

    private static string InputParameterName(Type input) =>
        DescribeInput(input) is "message" or "task" ? "message" : "messageFactory";

    private static MethodInfo Close(MethodInfo definition)
    {
        Type sourceDefinition = definition.GetParameters()[0].ParameterType.GetGenericTypeDefinition();
        Type[] arguments = sourceDefinition == typeof(IEventActivityBinder<>)
            ? [typeof(TestSaga), typeof(OutputMessage)]
            : sourceDefinition == typeof(IEventActivityBinder<,>)
                ? [typeof(TestSaga), typeof(InputMessage), typeof(OutputMessage)]
                : sourceDefinition == typeof(IExceptionActivityBinder<,>)
                    ? [typeof(TestSaga), typeof(InvalidOperationException), typeof(OutputMessage)]
                    : [typeof(TestSaga), typeof(InputMessage), typeof(InvalidOperationException), typeof(OutputMessage)];

        return definition.MakeGenericMethod(arguments);
    }

    private static ArgumentNullException InvokeNullBoundary(MethodInfo method, object? source, object? input)
    {
        TargetInvocationException wrapper = Assert.Throws<TargetInvocationException>(() =>
            method.Invoke(null, [source, input, null]));
        return Assert.IsType<ArgumentNullException>(wrapper.InnerException);
    }

    private static async Task AssertRuntimeMatrixAsync<TActivity, TContext>(
        RecordingProxy recorder,
        TContext context,
        OutputMessage message)
        where TContext : class, ConsumeContext
    {
        Assert.Equal(5, recorder.Arguments.Count);
        Assert.All(recorder.Arguments, activity => Assert.IsType<TActivity>(activity));

        foreach (object activity in recorder.Arguments)
        {
            FieldInfo field = activity.GetType().GetField("_messageFactory", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var factory = Assert.IsType<ContextMessageFactory<TContext, OutputMessage>>(field.GetValue(activity));
            InitializedMessage<OutputMessage> initialized = await factory.GetMessageAsync(context);
            Assert.Same(message, initialized.Message);
        }
    }

    private static void AssertFactoryObservations(
        object context,
        object? syncContext,
        object? asyncContext,
        object? initializedContext,
        int syncCount,
        int asyncCount,
        int initializedCount)
    {
        Assert.Same(context, syncContext);
        Assert.Same(context, asyncContext);
        Assert.Same(context, initializedContext);
        Assert.Equal(1, syncCount);
        Assert.Equal(1, asyncCount);
        Assert.Equal(1, initializedCount);
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
        object proxy = DispatchProxy.Create(interfaceType, typeof(RecordingProxy));
        recorder = (RecordingProxy)proxy;
        return proxy;
    }

    private class RecordingProxy : DispatchProxy
    {
        public List<MethodInfo> Invocations { get; } = [];
        public List<object> Arguments { get; } = [];
        public object? ReturnValue { get; set; }
        public Func<MethodInfo, object?[]?, object?>? Handler { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            MethodInfo method = targetMethod ?? throw new InvalidOperationException("A proxied invocation requires method metadata.");
            Invocations.Add(method);

            if (args is { Length: > 0 } && args[0] is not null)
                Arguments.Add(args[0]!);

            return Handler?.Invoke(method, args) ?? ReturnValue ?? DefaultValue(method.ReturnType);
        }
    }

    private static object? DefaultValue(Type type) =>
        type == typeof(void)
            ? null
            : type == typeof(Task)
                ? Task.CompletedTask
                : type.IsValueType
                    ? Activator.CreateInstance(type)
                    : null;

    private sealed class TestSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = "Initial";
    }

    private sealed record InputMessage(string Value = "input");
    private sealed record OutputMessage(string Value);
}
