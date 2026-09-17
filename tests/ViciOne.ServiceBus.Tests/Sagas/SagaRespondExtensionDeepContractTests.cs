using System.Reflection;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaRespondExtensionDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-RESPOND-EXTENSIONS", "public-overload-census")]
    public void PublicSurface_ExposesTheExactThirteenRespondOverloads()
    {
        string[] actual = typeof(RespondExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(Describe)
            .Order(StringComparer.Ordinal)
            .ToArray();

        string[] expected =
        [
            "Respond<TInstance,TData,TException,TMessage>(IExceptionActivityBinder<TInstance,TData,TException>,TMessage,Action<SendContext<TMessage>>)",
            "Respond<TInstance,TData,TException,TMessage>(IExceptionActivityBinder<TInstance,TData,TException>,EventExceptionMessageFactory<TInstance,TData,TException,TMessage>,Action<SendContext<TMessage>>)",
            "Respond<TInstance,TData,TMessage>(IEventActivityBinder<TInstance,TData>,TMessage,Action<SendContext<TMessage>>)",
            "Respond<TInstance,TData,TMessage>(IEventActivityBinder<TInstance,TData>,EventMessageFactory<TInstance,TData,TMessage>,Action<SendContext<TMessage>>)",
            "Respond<TInstance,TException,TMessage>(IExceptionActivityBinder<TInstance,TException>,TMessage,Action<SendContext<TMessage>>)",
            "Respond<TInstance,TException,TMessage>(IExceptionActivityBinder<TInstance,TException>,EventExceptionMessageFactory<TInstance,TException,TMessage>,Action<SendContext<TMessage>>)",
            "RespondAwaited<TInstance,TData,TException,TMessage>(IExceptionActivityBinder<TInstance,TData,TException>,Task<TMessage>,Action<SendContext<TMessage>>)",
            "RespondAwaited<TInstance,TData,TException,TMessage>(IExceptionActivityBinder<TInstance,TData,TException>,AsyncEventExceptionMessageFactory<TInstance,TData,TException,TMessage>,Action<SendContext<TMessage>>)",
            "RespondAwaited<TInstance,TData,TMessage>(IEventActivityBinder<TInstance,TData>,Task<TMessage>,Action<SendContext<TMessage>>)",
            "RespondAwaited<TInstance,TData,TMessage>(IEventActivityBinder<TInstance,TData>,AsyncEventMessageFactory<TInstance,TData,TMessage>,Action<SendContext<TMessage>>)",
            "RespondAwaited<TInstance,TData,TMessage>(IEventActivityBinder<TInstance,TData>,Func<IBehaviorContext<TInstance,TData>,Task<InitializedMessage<TMessage>>>,Action<SendContext<TMessage>>)",
            "RespondAwaited<TInstance,TException,TMessage>(IExceptionActivityBinder<TInstance,TException>,Task<TMessage>,Action<SendContext<TMessage>>)",
            "RespondAwaited<TInstance,TException,TMessage>(IExceptionActivityBinder<TInstance,TException>,AsyncEventExceptionMessageFactory<TInstance,TException,TMessage>,Action<SendContext<TMessage>>)"
        ];

        Assert.Equal(expected.Order(StringComparer.Ordinal), actual);
        Assert.All(
            typeof(RespondExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly),
            method =>
            {
                ParameterInfo callback = method.GetParameters()[2];
                Assert.True(callback.IsOptional);
                Assert.Null(callback.DefaultValue);
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-RESPOND-EXTENSIONS", "data-binder-runtime-matrix")]
    public async Task DataBinderOverloads_PreserveBinderMessageFactoryAndCallbackIdentityAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IEventActivityBinder<TestSaga, InputMessage> result = CreateProxy<IEventActivityBinder<TestSaga, InputMessage>>(out _);
        IEventActivityBinder<TestSaga, InputMessage> binder =
            CreateProxy<IEventActivityBinder<TestSaga, InputMessage>>(out RecordingProxy recorder);
        recorder.ReturnValue = result;
        IBehaviorContext<TestSaga, InputMessage> context = CreateProxy<IBehaviorContext<TestSaga, InputMessage>>(out _);
        var directMessage = new ResponseMessage("direct");
        var taskMessage = new ResponseMessage("task");
        var syncMessage = new ResponseMessage("sync");
        var asyncMessage = new ResponseMessage("async");
        var initializedMessage = new ResponseMessage("initialized");
        var taskSource = new TaskCompletionSource<ResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackCount = 0;
        var syncFactoryCount = 0;
        var asyncFactoryCount = 0;
        var initializedFactoryCount = 0;
        object? syncContext = null;
        object? asyncContext = null;
        object? initializedContext = null;
        Action<SendContext<ResponseMessage>> callback = _ => callbackCount++;
        EventMessageFactory<TestSaga, InputMessage, ResponseMessage> syncFactory = factoryContext =>
        {
            syncFactoryCount++;
            syncContext = factoryContext;
            return syncMessage;
        };
        AsyncEventMessageFactory<TestSaga, InputMessage, ResponseMessage> asyncFactory = factoryContext =>
        {
            asyncFactoryCount++;
            asyncContext = factoryContext;
            return Task.FromResult(asyncMessage);
        };
        Func<IBehaviorContext<TestSaga, InputMessage>, Task<InitializedMessage<ResponseMessage>>> initializedFactory = factoryContext =>
        {
            initializedFactoryCount++;
            initializedContext = factoryContext;
            return Task.FromResult(new InitializedMessage<ResponseMessage>(initializedMessage));
        };

        Assert.Same(result, binder.Respond<TestSaga, InputMessage, ResponseMessage>(directMessage, callback));
        Assert.Same(result, binder.RespondAwaited<TestSaga, InputMessage, ResponseMessage>(taskSource.Task, callback));
        Assert.Same(result, binder.Respond<TestSaga, InputMessage, ResponseMessage>(syncFactory, callback));
        Assert.Same(result, binder.RespondAwaited<TestSaga, InputMessage, ResponseMessage>(asyncFactory, callback));
        Assert.Same(result, binder.RespondAwaited<TestSaga, InputMessage, ResponseMessage>(initializedFactory, callback));

        object[] activities = recorder.Arguments.ToArray();
        Assert.Equal(5, activities.Length);
        Assert.All(activities, activity => Assert.IsType<RespondActivity<TestSaga, InputMessage, ResponseMessage>>(activity));
        Assert.Equal(0, callbackCount);
        Assert.Equal(0, syncFactoryCount);
        Assert.Equal(0, asyncFactoryCount);
        Assert.Equal(0, initializedFactoryCount);
        Assert.All(activities, activity => AssertGraphContainsReference(activity, callback));
        AssertGraphContainsReference(activities[2], syncFactory);
        AssertGraphContainsReference(activities[3], asyncFactory);
        AssertGraphContainsReference(activities[4], initializedFactory);

        taskSource.SetResult(taskMessage);
        InitializedMessage<ResponseMessage> direct =
            await GetFactory<IBehaviorContext<TestSaga, InputMessage>>(activities[0]).GetMessageAsync(context, cancellationToken);
        InitializedMessage<ResponseMessage> tasked =
            await GetFactory<IBehaviorContext<TestSaga, InputMessage>>(activities[1]).GetMessageAsync(context, cancellationToken);
        InitializedMessage<ResponseMessage> synchronous =
            await GetFactory<IBehaviorContext<TestSaga, InputMessage>>(activities[2]).GetMessageAsync(context, cancellationToken);
        InitializedMessage<ResponseMessage> asynchronous =
            await GetFactory<IBehaviorContext<TestSaga, InputMessage>>(activities[3]).GetMessageAsync(context, cancellationToken);
        InitializedMessage<ResponseMessage> initialized =
            await GetFactory<IBehaviorContext<TestSaga, InputMessage>>(activities[4]).GetMessageAsync(context, cancellationToken);

        Assert.Same(directMessage, direct.Message);
        Assert.Same(taskMessage, tasked.Message);
        Assert.Same(syncMessage, synchronous.Message);
        Assert.Same(asyncMessage, asynchronous.Message);
        Assert.Same(initializedMessage, initialized.Message);
        Assert.Same(context, syncContext);
        Assert.Same(context, asyncContext);
        Assert.Same(context, initializedContext);
        Assert.Equal(1, syncFactoryCount);
        Assert.Equal(1, asyncFactoryCount);
        Assert.Equal(1, initializedFactoryCount);
        Assert.Equal(0, callbackCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-RESPOND-EXTENSIONS", "faulted-binder-runtime-matrix")]
    public async Task FaultedBinderOverloads_SelectFaultedActivitiesAndPreserveIdentitiesAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IExceptionActivityBinder<TestSaga, InvalidOperationException> result =
            CreateProxy<IExceptionActivityBinder<TestSaga, InvalidOperationException>>(out _);
        IExceptionActivityBinder<TestSaga, InvalidOperationException> binder =
            CreateProxy<IExceptionActivityBinder<TestSaga, InvalidOperationException>>(out RecordingProxy recorder);
        recorder.ReturnValue = result;
        IBehaviorExceptionContext<TestSaga, InvalidOperationException> context =
            CreateProxy<IBehaviorExceptionContext<TestSaga, InvalidOperationException>>(out _);
        var directMessage = new ResponseMessage("direct-fault");
        var taskMessage = new ResponseMessage("task-fault");
        var syncMessage = new ResponseMessage("sync-fault");
        var asyncMessage = new ResponseMessage("async-fault");
        var taskSource = new TaskCompletionSource<ResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackCount = 0;
        var syncFactoryCount = 0;
        var asyncFactoryCount = 0;
        object? syncContext = null;
        object? asyncContext = null;
        Action<SendContext<ResponseMessage>> callback = _ => callbackCount++;
        EventExceptionMessageFactory<TestSaga, InvalidOperationException, ResponseMessage> syncFactory = factoryContext =>
        {
            syncFactoryCount++;
            syncContext = factoryContext;
            return syncMessage;
        };
        AsyncEventExceptionMessageFactory<TestSaga, InvalidOperationException, ResponseMessage> asyncFactory = factoryContext =>
        {
            asyncFactoryCount++;
            asyncContext = factoryContext;
            return Task.FromResult(asyncMessage);
        };

        Assert.Same(result, binder.Respond<TestSaga, InvalidOperationException, ResponseMessage>(directMessage, callback));
        Assert.Same(result, binder.RespondAwaited<TestSaga, InvalidOperationException, ResponseMessage>(taskSource.Task, callback));
        Assert.Same(result, binder.Respond<TestSaga, InvalidOperationException, ResponseMessage>(syncFactory, callback));
        Assert.Same(result, binder.RespondAwaited<TestSaga, InvalidOperationException, ResponseMessage>(asyncFactory, callback));

        object[] activities = recorder.Arguments.ToArray();
        Assert.Equal(4, activities.Length);
        Assert.All(activities, activity =>
            Assert.IsType<FaultedRespondActivity<TestSaga, InvalidOperationException, ResponseMessage>>(activity));
        Assert.Equal(0, callbackCount);
        Assert.Equal(0, syncFactoryCount);
        Assert.Equal(0, asyncFactoryCount);
        Assert.All(activities, activity => AssertGraphContainsReference(activity, callback));
        AssertGraphContainsReference(activities[2], syncFactory);
        AssertGraphContainsReference(activities[3], asyncFactory);

        taskSource.SetResult(taskMessage);
        InitializedMessage<ResponseMessage> direct = await GetFactory<IBehaviorExceptionContext<TestSaga, InvalidOperationException>>(activities[0])
            .GetMessageAsync(context, cancellationToken);
        InitializedMessage<ResponseMessage> tasked = await GetFactory<IBehaviorExceptionContext<TestSaga, InvalidOperationException>>(activities[1])
            .GetMessageAsync(context, cancellationToken);
        InitializedMessage<ResponseMessage> synchronous = await GetFactory<IBehaviorExceptionContext<TestSaga, InvalidOperationException>>(activities[2])
            .GetMessageAsync(context, cancellationToken);
        InitializedMessage<ResponseMessage> asynchronous = await GetFactory<IBehaviorExceptionContext<TestSaga, InvalidOperationException>>(activities[3])
            .GetMessageAsync(context, cancellationToken);

        Assert.Same(directMessage, direct.Message);
        Assert.Same(taskMessage, tasked.Message);
        Assert.Same(syncMessage, synchronous.Message);
        Assert.Same(asyncMessage, asynchronous.Message);
        Assert.Same(context, syncContext);
        Assert.Same(context, asyncContext);
        Assert.Equal(1, syncFactoryCount);
        Assert.Equal(1, asyncFactoryCount);
        Assert.Equal(0, callbackCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-RESPOND-EXTENSIONS", "data-faulted-binder-runtime-matrix")]
    public async Task DataFaultedBinderOverloads_SelectFaultedActivitiesAndPreserveIdentitiesAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IExceptionActivityBinder<TestSaga, InputMessage, InvalidOperationException> result =
            CreateProxy<IExceptionActivityBinder<TestSaga, InputMessage, InvalidOperationException>>(out _);
        IExceptionActivityBinder<TestSaga, InputMessage, InvalidOperationException> binder =
            CreateProxy<IExceptionActivityBinder<TestSaga, InputMessage, InvalidOperationException>>(out RecordingProxy recorder);
        recorder.ReturnValue = result;
        IBehaviorExceptionContext<TestSaga, InputMessage, InvalidOperationException> context =
            CreateProxy<IBehaviorExceptionContext<TestSaga, InputMessage, InvalidOperationException>>(out _);
        var directMessage = new ResponseMessage("direct-data-fault");
        var taskMessage = new ResponseMessage("task-data-fault");
        var syncMessage = new ResponseMessage("sync-data-fault");
        var asyncMessage = new ResponseMessage("async-data-fault");
        var taskSource = new TaskCompletionSource<ResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackCount = 0;
        var syncFactoryCount = 0;
        var asyncFactoryCount = 0;
        object? syncContext = null;
        object? asyncContext = null;
        Action<SendContext<ResponseMessage>> callback = _ => callbackCount++;
        EventExceptionMessageFactory<TestSaga, InputMessage, InvalidOperationException, ResponseMessage> syncFactory = factoryContext =>
        {
            syncFactoryCount++;
            syncContext = factoryContext;
            return syncMessage;
        };
        AsyncEventExceptionMessageFactory<TestSaga, InputMessage, InvalidOperationException, ResponseMessage> asyncFactory = factoryContext =>
        {
            asyncFactoryCount++;
            asyncContext = factoryContext;
            return Task.FromResult(asyncMessage);
        };

        Assert.Same(result, binder.Respond<TestSaga, InputMessage, InvalidOperationException, ResponseMessage>(directMessage, callback));
        Assert.Same(result, binder.RespondAwaited<TestSaga, InputMessage, InvalidOperationException, ResponseMessage>(taskSource.Task, callback));
        Assert.Same(result, binder.Respond<TestSaga, InputMessage, InvalidOperationException, ResponseMessage>(syncFactory, callback));
        Assert.Same(result, binder.RespondAwaited<TestSaga, InputMessage, InvalidOperationException, ResponseMessage>(asyncFactory, callback));

        object[] activities = recorder.Arguments.ToArray();
        Assert.Equal(4, activities.Length);
        Assert.All(activities, activity =>
            Assert.IsType<FaultedRespondActivity<TestSaga, InputMessage, InvalidOperationException, ResponseMessage>>(activity));
        Assert.Equal(0, callbackCount);
        Assert.Equal(0, syncFactoryCount);
        Assert.Equal(0, asyncFactoryCount);
        Assert.All(activities, activity => AssertGraphContainsReference(activity, callback));
        AssertGraphContainsReference(activities[2], syncFactory);
        AssertGraphContainsReference(activities[3], asyncFactory);

        taskSource.SetResult(taskMessage);
        InitializedMessage<ResponseMessage> direct =
            await GetFactory<IBehaviorExceptionContext<TestSaga, InputMessage, InvalidOperationException>>(activities[0])
                .GetMessageAsync(context, cancellationToken);
        InitializedMessage<ResponseMessage> tasked =
            await GetFactory<IBehaviorExceptionContext<TestSaga, InputMessage, InvalidOperationException>>(activities[1])
                .GetMessageAsync(context, cancellationToken);
        InitializedMessage<ResponseMessage> synchronous =
            await GetFactory<IBehaviorExceptionContext<TestSaga, InputMessage, InvalidOperationException>>(activities[2])
                .GetMessageAsync(context, cancellationToken);
        InitializedMessage<ResponseMessage> asynchronous =
            await GetFactory<IBehaviorExceptionContext<TestSaga, InputMessage, InvalidOperationException>>(activities[3])
                .GetMessageAsync(context, cancellationToken);

        Assert.Same(directMessage, direct.Message);
        Assert.Same(taskMessage, tasked.Message);
        Assert.Same(syncMessage, synchronous.Message);
        Assert.Same(asyncMessage, asynchronous.Message);
        Assert.Same(context, syncContext);
        Assert.Same(context, asyncContext);
        Assert.Equal(1, syncFactoryCount);
        Assert.Equal(1, asyncFactoryCount);
        Assert.Equal(0, callbackCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-RESPOND-EXTENSIONS", "receiver-boundary-matrix")]
    public void AllOverloads_RejectNullReceiverBeforeInputOrCallbackEffects()
    {
        var message = new ResponseMessage("response");
        var callbackCount = 0;
        var factoryCount = 0;
        Action<SendContext<ResponseMessage>> callback = _ => callbackCount++;
        EventMessageFactory<TestSaga, InputMessage, ResponseMessage> sync = _ =>
        {
            factoryCount++;
            return message;
        };
        AsyncEventMessageFactory<TestSaga, InputMessage, ResponseMessage> async = _ =>
        {
            factoryCount++;
            return Task.FromResult(message);
        };
        Func<IBehaviorContext<TestSaga, InputMessage>, Task<InitializedMessage<ResponseMessage>>> initialized = _ =>
        {
            factoryCount++;
            return Task.FromResult(new InitializedMessage<ResponseMessage>(message));
        };
        EventExceptionMessageFactory<TestSaga, InvalidOperationException, ResponseMessage> faultSync = _ =>
        {
            factoryCount++;
            return message;
        };
        AsyncEventExceptionMessageFactory<TestSaga, InvalidOperationException, ResponseMessage> faultAsync = _ =>
        {
            factoryCount++;
            return Task.FromResult(message);
        };
        EventExceptionMessageFactory<TestSaga, InputMessage, InvalidOperationException, ResponseMessage> dataFaultSync = _ =>
        {
            factoryCount++;
            return message;
        };
        AsyncEventExceptionMessageFactory<TestSaga, InputMessage, InvalidOperationException, ResponseMessage> dataFaultAsync = _ =>
        {
            factoryCount++;
            return Task.FromResult(message);
        };

        AssertArgument("source", () => RespondExtensions.Respond<TestSaga, InputMessage, ResponseMessage>(
            (IEventActivityBinder<TestSaga, InputMessage>)null!, message, callback));
        AssertArgument("source", () => RespondExtensions.RespondAwaited<TestSaga, InputMessage, ResponseMessage>(
            (IEventActivityBinder<TestSaga, InputMessage>)null!, Task.FromResult(message), callback));
        AssertArgument("source", () => RespondExtensions.Respond<TestSaga, InputMessage, ResponseMessage>(
            (IEventActivityBinder<TestSaga, InputMessage>)null!, sync, callback));
        AssertArgument("source", () => RespondExtensions.RespondAwaited<TestSaga, InputMessage, ResponseMessage>(
            (IEventActivityBinder<TestSaga, InputMessage>)null!, async, callback));
        AssertArgument("source", () => RespondExtensions.RespondAwaited<TestSaga, InputMessage, ResponseMessage>(
            (IEventActivityBinder<TestSaga, InputMessage>)null!, initialized, callback));
        AssertArgument("source", () => RespondExtensions.Respond<TestSaga, InvalidOperationException, ResponseMessage>(
            (IExceptionActivityBinder<TestSaga, InvalidOperationException>)null!, message, callback));
        AssertArgument("source", () => RespondExtensions.RespondAwaited<TestSaga, InvalidOperationException, ResponseMessage>(
            (IExceptionActivityBinder<TestSaga, InvalidOperationException>)null!, Task.FromResult(message), callback));
        AssertArgument("source", () => RespondExtensions.Respond<TestSaga, InvalidOperationException, ResponseMessage>(
            (IExceptionActivityBinder<TestSaga, InvalidOperationException>)null!, faultSync, callback));
        AssertArgument("source", () => RespondExtensions.RespondAwaited<TestSaga, InvalidOperationException, ResponseMessage>(
            (IExceptionActivityBinder<TestSaga, InvalidOperationException>)null!, faultAsync, callback));
        AssertArgument("source", () => RespondExtensions.Respond<TestSaga, InputMessage, InvalidOperationException, ResponseMessage>(
            null!, message, callback));
        AssertArgument("source", () => RespondExtensions.RespondAwaited<TestSaga, InputMessage, InvalidOperationException, ResponseMessage>(
            null!, Task.FromResult(message), callback));
        AssertArgument("source", () => RespondExtensions.Respond<TestSaga, InputMessage, InvalidOperationException, ResponseMessage>(
            null!, dataFaultSync, callback));
        AssertArgument("source", () => RespondExtensions.RespondAwaited<TestSaga, InputMessage, InvalidOperationException, ResponseMessage>(
            null!, dataFaultAsync, callback));

        Assert.Equal(0, factoryCount);
        Assert.Equal(0, callbackCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-RESPOND-EXTENSIONS", "required-input-boundary-matrix")]
    public void AllOverloads_RejectNullRequiredInputBeforeBinderEffects()
    {
        IEventActivityBinder<TestSaga, InputMessage> dataBinder =
            CreateProxy<IEventActivityBinder<TestSaga, InputMessage>>(out RecordingProxy dataRecorder);
        IExceptionActivityBinder<TestSaga, InvalidOperationException> faultBinder =
            CreateProxy<IExceptionActivityBinder<TestSaga, InvalidOperationException>>(out RecordingProxy faultRecorder);
        IExceptionActivityBinder<TestSaga, InputMessage, InvalidOperationException> dataFaultBinder =
            CreateProxy<IExceptionActivityBinder<TestSaga, InputMessage, InvalidOperationException>>(out RecordingProxy dataFaultRecorder);

        AssertArgument("message", () => dataBinder.Respond<TestSaga, InputMessage, ResponseMessage>((ResponseMessage)null!));
        AssertArgument("message", () => dataBinder.RespondAwaited<TestSaga, InputMessage, ResponseMessage>((Task<ResponseMessage>)null!));
        AssertArgument("messageFactory", () => dataBinder.Respond<TestSaga, InputMessage, ResponseMessage>(
            (EventMessageFactory<TestSaga, InputMessage, ResponseMessage>)null!));
        AssertArgument("messageFactory", () => dataBinder.RespondAwaited<TestSaga, InputMessage, ResponseMessage>(
            (AsyncEventMessageFactory<TestSaga, InputMessage, ResponseMessage>)null!));
        AssertArgument("messageFactory", () => dataBinder.RespondAwaited<TestSaga, InputMessage, ResponseMessage>(
            (Func<IBehaviorContext<TestSaga, InputMessage>, Task<InitializedMessage<ResponseMessage>>>)null!));

        AssertArgument("message", () => faultBinder.Respond<TestSaga, InvalidOperationException, ResponseMessage>((ResponseMessage)null!));
        AssertArgument("message", () => faultBinder.RespondAwaited<TestSaga, InvalidOperationException, ResponseMessage>((Task<ResponseMessage>)null!));
        AssertArgument("messageFactory", () => faultBinder.Respond<TestSaga, InvalidOperationException, ResponseMessage>(
            (EventExceptionMessageFactory<TestSaga, InvalidOperationException, ResponseMessage>)null!));
        AssertArgument("messageFactory", () => faultBinder.RespondAwaited<TestSaga, InvalidOperationException, ResponseMessage>(
            (AsyncEventExceptionMessageFactory<TestSaga, InvalidOperationException, ResponseMessage>)null!));

        AssertArgument("message", () => dataFaultBinder.Respond<TestSaga, InputMessage, InvalidOperationException, ResponseMessage>(
            (ResponseMessage)null!));
        AssertArgument("message", () => dataFaultBinder.RespondAwaited<TestSaga, InputMessage, InvalidOperationException, ResponseMessage>(
            (Task<ResponseMessage>)null!));
        AssertArgument("messageFactory", () => dataFaultBinder.Respond<TestSaga, InputMessage, InvalidOperationException, ResponseMessage>(
            (EventExceptionMessageFactory<TestSaga, InputMessage, InvalidOperationException, ResponseMessage>)null!));
        AssertArgument("messageFactory", () => dataFaultBinder.RespondAwaited<TestSaga, InputMessage, InvalidOperationException, ResponseMessage>(
            (AsyncEventExceptionMessageFactory<TestSaga, InputMessage, InvalidOperationException, ResponseMessage>)null!));

        Assert.Empty(dataRecorder.Arguments);
        Assert.Empty(faultRecorder.Arguments);
        Assert.Empty(dataFaultRecorder.Arguments);
    }

    private static ContextMessageFactory<TContext, ResponseMessage> GetFactory<TContext>(object activity)
        where TContext : class, ConsumeContext =>
        GetField<ContextMessageFactory<TContext, ResponseMessage>>(activity, "_messageFactory");

    private static T GetField<T>(object source, string name)
    {
        Type? type = source.GetType();
        while (type is not null)
        {
            FieldInfo? field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field is not null)
                return Assert.IsType<T>(field.GetValue(source));

            type = type.BaseType;
        }

        throw new InvalidOperationException($"Field '{name}' was not found on {source.GetType()}.");
    }

    private static void AssertGraphContainsReference(object root, object expected)
    {
        var pending = new Queue<object>();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        pending.Enqueue(root);

        while (pending.Count > 0 && visited.Count < 4096)
        {
            object current = pending.Dequeue();
            if (ReferenceEquals(current, expected))
                return;

            if (!visited.Add(current))
                continue;

            if (current is Delegate callback)
            {
                foreach (Delegate invocation in callback.GetInvocationList())
                {
                    if (ReferenceEquals(invocation, expected))
                        return;

                    if (invocation.Target is not null)
                        pending.Enqueue(invocation.Target);
                }
            }

            Type? type = current.GetType();
            if (type.IsPrimitive || type.IsEnum || current is string or Type or MemberInfo)
                continue;

            while (type is not null)
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                                           BindingFlags.DeclaredOnly))
                {
                    object? value;
                    try
                    {
                        value = field.GetValue(current);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    if (value is not null)
                        pending.Enqueue(value);
                }

                type = type.BaseType;
            }
        }

        Assert.Fail($"The object graph rooted at {root.GetType()} does not contain the expected reference {expected.GetType()}.");
    }

    private static string Describe(MethodInfo method)
    {
        string genericParameters = string.Join(",", method.GetGenericArguments().Select(parameter => parameter.Name));
        string parameters = string.Join(",", method.GetParameters().Select(parameter => Describe(parameter.ParameterType)));
        return $"{method.Name}<{genericParameters}>({parameters})";
    }

    private static string Describe(Type type)
    {
        if (type.IsGenericParameter)
            return type.Name;

        if (!type.IsGenericType)
            return type.Name;

        string name = type.GetGenericTypeDefinition().Name;
        int arity = name.IndexOf('`', StringComparison.Ordinal);
        if (arity >= 0)
            name = name[..arity];

        return $"{name}<{string.Join(",", type.GetGenericArguments().Select(Describe))}>";
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

    private class RecordingProxy : DispatchProxy
    {
        public List<object> Arguments { get; } = [];
        public object? ReturnValue { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            MethodInfo method = targetMethod ?? throw new InvalidOperationException("A proxied invocation requires method metadata.");
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
    private sealed record ResponseMessage(string Value);
}
