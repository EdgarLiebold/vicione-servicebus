using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaBehaviorTransitionExtensionDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-BEHAVIOR-EXTENSIONS", "required-boundaries-before-collaborator-effects")]
    public void RequiredBoundaries_RejectEveryNullBeforeAnyBinderOrConfiguratorEffect()
    {
        var stateMachine = CreateStateMachine(out _, out _, out _, out _);
        IEventActivityBinder<TestSaga> eventBinder =
            CreateBinder<IEventActivityBinder<TestSaga>>(stateMachine, out BinderRecorder eventRecorder);
        IEventActivityBinder<TestSaga, TestMessage> dataBinder =
            CreateBinder<IEventActivityBinder<TestSaga, TestMessage>>(stateMachine, out BinderRecorder dataRecorder);
        IExceptionActivityBinder<TestSaga, InvalidOperationException> exceptionBinder =
            CreateBinder<IExceptionActivityBinder<TestSaga, InvalidOperationException>>(stateMachine, out BinderRecorder exceptionRecorder);
        IExceptionActivityBinder<TestSaga, TestMessage, InvalidOperationException> dataExceptionBinder =
            CreateBinder<IExceptionActivityBinder<TestSaga, TestMessage, InvalidOperationException>>(
                stateMachine, out BinderRecorder dataExceptionRecorder);
        IState targetState = CreateState("Target");

        AssertArgument("action", () => ThenExtensions.Then<TestSaga>(eventBinder, null!));
        AssertArgument("action", () => ThenExtensions.Then<TestSaga, InvalidOperationException>(exceptionBinder, null!));
        AssertArgument("asyncAction", () => ThenExtensions.ThenAwaited<TestSaga, InvalidOperationException>(exceptionBinder, null!));
        AssertArgument("action", () => ThenExtensions.ThenAwaited<TestSaga>(eventBinder, null!));
        AssertArgument("action", () => ThenExtensions.Then<TestSaga, TestMessage>(dataBinder, null!));
        AssertArgument("action", () => ThenExtensions.Then<TestSaga, TestMessage, InvalidOperationException>(dataExceptionBinder, null!));
        AssertArgument("asyncAction", () =>
            ThenExtensions.ThenAwaited<TestSaga, TestMessage, InvalidOperationException>(dataExceptionBinder, null!));
        AssertArgument("action", () => ThenExtensions.ThenAwaited<TestSaga, TestMessage>(dataBinder, null!));
        AssertArgument("activityFactory", () => ThenExtensions.Execute<TestSaga>(eventBinder,
            (Func<IBehaviorContext<TestSaga>, IStateMachineActivity<TestSaga>>)null!));
        AssertArgument("activity", () => ThenExtensions.Execute<TestSaga>(eventBinder, (IStateMachineActivity<TestSaga>)null!));
        AssertArgument("activityFactory", () => ThenExtensions.ExecuteAwaited<TestSaga>(eventBinder, null!));
        AssertArgument("activityFactory", () => ThenExtensions.Execute<TestSaga, TestMessage>(dataBinder,
            (Func<IBehaviorContext<TestSaga, TestMessage>, IStateMachineActivity<TestSaga, TestMessage>>)null!));
        AssertArgument("activityFactory", () => ThenExtensions.ExecuteAwaited<TestSaga, TestMessage>(dataBinder,
            (Func<IBehaviorContext<TestSaga, TestMessage>, Task<IStateMachineActivity<TestSaga, TestMessage>>>)null!));
        AssertArgument("activityFactory", () => ThenExtensions.Execute<TestSaga, TestMessage>(dataBinder,
            (Func<IBehaviorContext<TestSaga, TestMessage>, IStateMachineActivity<TestSaga>>)null!));
        AssertArgument("activityFactory", () => ThenExtensions.ExecuteAwaited<TestSaga, TestMessage>(dataBinder,
            (Func<IBehaviorContext<TestSaga, TestMessage>, Task<IStateMachineActivity<TestSaga>>>)null!));

        AssertArgument("toState", () => TransitionExtensions.TransitionTo<TestSaga>(eventBinder, null!));
        AssertArgument("toState", () =>
            TransitionExtensions.TransitionTo<TestSaga, InvalidOperationException>(exceptionBinder, null!));
        AssertArgument("toState", () => TransitionExtensions.TransitionTo<TestSaga, TestMessage>(dataBinder, null!));
        AssertArgument("toState", () =>
            TransitionExtensions.TransitionTo<TestSaga, TestMessage, InvalidOperationException>(dataExceptionBinder, null!));

        AssertArgument("binder", () => ThenExtensions.Then<TestSaga>(null!, _ => { }));
        AssertArgument("binder", () =>
            ThenExtensions.Then<TestSaga, InvalidOperationException>(
                (IExceptionActivityBinder<TestSaga, InvalidOperationException>)null!, _ => { }));
        AssertArgument("binder", () =>
            ThenExtensions.ThenAwaited<TestSaga, InvalidOperationException>(
                (IExceptionActivityBinder<TestSaga, InvalidOperationException>)null!, _ => Task.CompletedTask));
        AssertArgument("binder", () => ThenExtensions.ThenAwaited<TestSaga>(null!, _ => Task.CompletedTask));
        AssertArgument("binder", () => ThenExtensions.Then<TestSaga, TestMessage>(null!, _ => { }));
        AssertArgument("binder", () =>
            ThenExtensions.Then<TestSaga, TestMessage, InvalidOperationException>(null!, _ => { }));
        AssertArgument("binder", () =>
            ThenExtensions.ThenAwaited<TestSaga, TestMessage, InvalidOperationException>(null!, _ => Task.CompletedTask));
        AssertArgument("binder", () => ThenExtensions.ThenAwaited<TestSaga, TestMessage>(null!, _ => Task.CompletedTask));
        AssertArgument("binder", () => ThenExtensions.Execute<TestSaga>(null!, _ => CreateActivity()));
        AssertArgument("binder", () => ThenExtensions.Execute<TestSaga>(null!, CreateActivity()));
        AssertArgument("binder", () =>
            ThenExtensions.ExecuteAwaited<TestSaga>(null!, _ => Task.FromResult(CreateActivity())));
        AssertArgument("binder", () => ThenExtensions.Execute<TestSaga, TestMessage>(null!,
            (Func<IBehaviorContext<TestSaga, TestMessage>, IStateMachineActivity<TestSaga, TestMessage>>)(_ => CreateDataActivity())));
        AssertArgument("binder", () => ThenExtensions.ExecuteAwaited<TestSaga, TestMessage>(null!,
            (Func<IBehaviorContext<TestSaga, TestMessage>, Task<IStateMachineActivity<TestSaga, TestMessage>>>)(
                _ => Task.FromResult(CreateDataActivity()))));
        AssertArgument("binder", () => ThenExtensions.Execute<TestSaga, TestMessage>(null!,
            (Func<IBehaviorContext<TestSaga, TestMessage>, IStateMachineActivity<TestSaga>>)(_ => CreateActivity())));
        AssertArgument("binder", () => ThenExtensions.ExecuteAwaited<TestSaga, TestMessage>(null!,
            (Func<IBehaviorContext<TestSaga, TestMessage>, Task<IStateMachineActivity<TestSaga>>>)(
                _ => Task.FromResult(CreateActivity()))));

        AssertArgument("source", () => TransitionExtensions.TransitionTo<TestSaga>(null!, targetState));
        AssertArgument("source", () =>
            TransitionExtensions.TransitionTo<TestSaga, InvalidOperationException>(
                (IExceptionActivityBinder<TestSaga, InvalidOperationException>)null!, targetState));
        AssertArgument("source", () => TransitionExtensions.TransitionTo<TestSaga, TestMessage>(null!, targetState));
        AssertArgument("source", () =>
            TransitionExtensions.TransitionTo<TestSaga, TestMessage, InvalidOperationException>(null!, targetState));
        AssertArgument("source", () => TransitionExtensions.Finalize<TestSaga>(null!));
        AssertArgument("source", () => TransitionExtensions.Finalize<TestSaga, InvalidOperationException>(
            (IExceptionActivityBinder<TestSaga, InvalidOperationException>)null!));
        AssertArgument("source", () => TransitionExtensions.Finalize<TestSaga, TestMessage>(null!));
        AssertArgument("source", () =>
            TransitionExtensions.Finalize<TestSaga, TestMessage, InvalidOperationException>(null!));

        var missing = new RecordingMissingInstanceConfigurator();
        AssertArgument("configurator", () =>
            MissingInstanceRedeliveryExtensions.Redeliver<TestSaga, TestMessage>(null!, _ => { }));
        AssertArgument("configure", () => missing.Redeliver<TestSaga, TestMessage>(null!));

        Assert.Equal(0, eventRecorder.InvocationCount);
        Assert.Equal(0, dataRecorder.InvocationCount);
        Assert.Equal(0, exceptionRecorder.InvocationCount);
        Assert.Equal(0, dataExceptionRecorder.InvocationCount);
        Assert.Equal(0, missing.DiscardCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-MISSING-REDELIVERY", "configure-validation-build-and-wrapper-ownership")]
    public void Redeliver_OwnsConfigureValidationBuildAndBuildFailureWrapping()
    {
        var successful = new RecordingMissingInstanceConfigurator();
        var configureCount = 0;

        IPipe<ConsumeContext<TestMessage>> pipe = successful.Redeliver<TestSaga, TestMessage>(configurator =>
        {
            configureCount++;
            configurator.ConfigureMessageScheduler = false;
            configurator.SetRetryPolicy(_ => Retry.None);
        });

        Assert.Equal(1, configureCount);
        Assert.Equal(1, successful.DiscardCount);
        Assert.Contains("MissingInstanceRedeliveryPipe", pipe.GetType().Name, StringComparison.Ordinal);

        var validation = new RecordingMissingInstanceConfigurator();
        ConfigurationException validationFailure = Assert.Throws<ConfigurationException>(() =>
            validation.Redeliver<TestSaga, TestMessage>(_ => { }));
        ValidationResult validationResult = Assert.Single(validationFailure.Results);
        Assert.Equal(ValidationResultDisposition.Failure, validationResult.Disposition);
        Assert.Contains("RetryPolicy", validationResult.ToString(), StringComparison.Ordinal);
        Assert.Null(validationFailure.InnerException);
        Assert.Equal(1, validation.DiscardCount);

        var configure = new RecordingMissingInstanceConfigurator();
        var configureFailure = new InvalidOperationException("configure-owned");
        InvalidOperationException propagated = Assert.Throws<InvalidOperationException>(() =>
            configure.Redeliver<TestSaga, TestMessage>(_ => throw configureFailure));
        Assert.Same(configureFailure, propagated);
        Assert.Equal(1, configure.DiscardCount);

        var build = new RecordingMissingInstanceConfigurator();
        var buildFailure = new InvalidOperationException("build-owned");
        ConfigurationException wrapped = Assert.Throws<ConfigurationException>(() =>
            build.Redeliver<TestSaga, TestMessage>(configurator =>
                configurator.SetRetryPolicy(_ => throw buildFailure)));
        Assert.Same(buildFailure, wrapped.InnerException);
        Assert.Empty(wrapped.Results);
        Assert.Contains("missing instance redelivery configuration was invalid", wrapped.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, build.DiscardCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-BEHAVIOR-EXTENSIONS", "then-callback-identity-across-normal-faulted-and-data-shapes")]
    public void ThenOverloads_PreserveBinderCallbackAndActivityIdentityAcrossEveryShape()
    {
        var stateMachine = CreateStateMachine(out _, out _, out _, out _);
        IEventActivityBinder<TestSaga> eventBinder =
            CreateBinder<IEventActivityBinder<TestSaga>>(stateMachine, out BinderRecorder eventRecorder);
        IEventActivityBinder<TestSaga, TestMessage> dataBinder =
            CreateBinder<IEventActivityBinder<TestSaga, TestMessage>>(stateMachine, out BinderRecorder dataRecorder);
        IExceptionActivityBinder<TestSaga, InvalidOperationException> exceptionBinder =
            CreateBinder<IExceptionActivityBinder<TestSaga, InvalidOperationException>>(stateMachine, out BinderRecorder exceptionRecorder);
        IExceptionActivityBinder<TestSaga, TestMessage, InvalidOperationException> dataExceptionBinder =
            CreateBinder<IExceptionActivityBinder<TestSaga, TestMessage, InvalidOperationException>>(
                stateMachine, out BinderRecorder dataExceptionRecorder);

        Action<IBehaviorContext<TestSaga>> action = _ => { };
        Func<IBehaviorContext<TestSaga>, Task> asyncAction = _ => Task.CompletedTask;
        Action<IBehaviorExceptionContext<TestSaga, InvalidOperationException>> faultedAction = _ => { };
        Func<IBehaviorExceptionContext<TestSaga, InvalidOperationException>, Task> asyncFaultedAction = _ => Task.CompletedTask;
        Action<IBehaviorContext<TestSaga, TestMessage>> dataAction = _ => { };
        Func<IBehaviorContext<TestSaga, TestMessage>, Task> asyncDataAction = _ => Task.CompletedTask;
        Action<IBehaviorExceptionContext<TestSaga, TestMessage, InvalidOperationException>> dataFaultedAction = _ => { };
        Func<IBehaviorExceptionContext<TestSaga, TestMessage, InvalidOperationException>, Task> asyncDataFaultedAction = _ => Task.CompletedTask;

        Assert.Same(eventBinder, eventBinder.Then(action));
        Assert.Same(eventBinder, eventBinder.ThenAwaited(asyncAction));
        Assert.Same(exceptionBinder, exceptionBinder.Then(faultedAction));
        Assert.Same(exceptionBinder, exceptionBinder.ThenAwaited(asyncFaultedAction));
        Assert.Same(dataBinder, dataBinder.Then(dataAction));
        Assert.Same(dataBinder, dataBinder.ThenAwaited(asyncDataAction));
        Assert.Same(dataExceptionBinder, dataExceptionBinder.Then(dataFaultedAction));
        Assert.Same(dataExceptionBinder, dataExceptionBinder.ThenAwaited(asyncDataFaultedAction));

        AssertDelegate<Action<IBehaviorContext<TestSaga>>>(eventRecorder.Activities[0], "_action", action);
        AssertDelegate<Func<IBehaviorContext<TestSaga>, Task>>(eventRecorder.Activities[1], "_asyncAction", asyncAction);
        AssertDelegate<Action<IBehaviorExceptionContext<TestSaga, InvalidOperationException>>>(
            exceptionRecorder.Activities[0], "_action", faultedAction);
        AssertDelegate<Func<IBehaviorExceptionContext<TestSaga, InvalidOperationException>, Task>>(
            exceptionRecorder.Activities[1], "_asyncAction", asyncFaultedAction);
        AssertDelegate<Action<IBehaviorContext<TestSaga, TestMessage>>>(dataRecorder.Activities[0], "_action", dataAction);
        AssertDelegate<Func<IBehaviorContext<TestSaga, TestMessage>, Task>>(
            dataRecorder.Activities[1], "_asyncAction", asyncDataAction);
        AssertDelegate<Action<IBehaviorExceptionContext<TestSaga, TestMessage, InvalidOperationException>>>(
            dataExceptionRecorder.Activities[0], "_action", dataFaultedAction);
        AssertDelegate<Func<IBehaviorExceptionContext<TestSaga, TestMessage, InvalidOperationException>, Task>>(
            dataExceptionRecorder.Activities[1], "_asyncAction", asyncDataFaultedAction);

        Assert.IsType<ActionActivity<TestSaga>>(eventRecorder.Activities[0]);
        Assert.IsType<AsyncActivity<TestSaga>>(eventRecorder.Activities[1]);
        Assert.IsType<FaultedActionActivity<TestSaga, InvalidOperationException>>(exceptionRecorder.Activities[0]);
        Assert.IsType<AsyncFaultedActionActivity<TestSaga, InvalidOperationException>>(exceptionRecorder.Activities[1]);
        Assert.IsType<ActionActivity<TestSaga, TestMessage>>(dataRecorder.Activities[0]);
        Assert.IsType<AsyncActivity<TestSaga, TestMessage>>(dataRecorder.Activities[1]);
        Assert.IsType<FaultedActionActivity<TestSaga, TestMessage, InvalidOperationException>>(dataExceptionRecorder.Activities[0]);
        Assert.IsType<AsyncFaultedActionActivity<TestSaga, TestMessage, InvalidOperationException>>(dataExceptionRecorder.Activities[1]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-BEHAVIOR-EXTENSIONS", "execute-factory-activity-identity-and-data-slim-adaptation")]
    public async Task ExecuteOverloads_PreserveFactoriesActivitiesAndSlimAdaptationAsync()
    {
        var stateMachine = CreateStateMachine(out _, out _, out _, out _);
        IEventActivityBinder<TestSaga> eventBinder =
            CreateBinder<IEventActivityBinder<TestSaga>>(stateMachine, out BinderRecorder eventRecorder);
        IEventActivityBinder<TestSaga, TestMessage> dataBinder =
            CreateBinder<IEventActivityBinder<TestSaga, TestMessage>>(stateMachine, out BinderRecorder dataRecorder);
        IStateMachineActivity<TestSaga> activity = CreateActivity();
        IStateMachineActivity<TestSaga, TestMessage> dataActivity = CreateDataActivity();
        Func<IBehaviorContext<TestSaga>, IStateMachineActivity<TestSaga>> factory = _ => activity;
        Func<IBehaviorContext<TestSaga>, Task<IStateMachineActivity<TestSaga>>> asyncFactory = _ => Task.FromResult(activity);
        Func<IBehaviorContext<TestSaga, TestMessage>, IStateMachineActivity<TestSaga, TestMessage>> dataFactory = _ => dataActivity;
        Func<IBehaviorContext<TestSaga, TestMessage>, Task<IStateMachineActivity<TestSaga, TestMessage>>> asyncDataFactory =
            _ => Task.FromResult(dataActivity);
        IBehaviorContext<TestSaga, TestMessage> context = CreateProxy<IBehaviorContext<TestSaga, TestMessage>>(
            (method, _) => throw Unused(method));
        object? slimFactoryContext = null;
        object? asyncSlimFactoryContext = null;
        Func<IBehaviorContext<TestSaga, TestMessage>, IStateMachineActivity<TestSaga>> slimFactory = factoryContext =>
        {
            slimFactoryContext = factoryContext;
            return activity;
        };
        Func<IBehaviorContext<TestSaga, TestMessage>, Task<IStateMachineActivity<TestSaga>>> asyncSlimFactory = factoryContext =>
        {
            asyncSlimFactoryContext = factoryContext;
            return Task.FromResult(activity);
        };

        Assert.Same(eventBinder, eventBinder.Execute(factory));
        Assert.Same(eventBinder, eventBinder.Execute(activity));
        Assert.Same(eventBinder, eventBinder.ExecuteAwaited(asyncFactory));
        Assert.Same(dataBinder, dataBinder.Execute(dataFactory));
        Assert.Same(dataBinder, dataBinder.ExecuteAwaited(asyncDataFactory));
        Assert.Same(dataBinder, dataBinder.Execute(slimFactory));
        Assert.Same(dataBinder, dataBinder.ExecuteAwaited(asyncSlimFactory));

        AssertDelegate<Func<IBehaviorContext<TestSaga>, IStateMachineActivity<TestSaga>>>(
            eventRecorder.Activities[0], "_activityFactory", factory);
        Assert.Same(activity, eventRecorder.Activities[1]);
        AssertDelegate<Func<IBehaviorContext<TestSaga>, Task<IStateMachineActivity<TestSaga>>>>(
            eventRecorder.Activities[2], "_activityFactory", asyncFactory);
        AssertDelegate<Func<IBehaviorContext<TestSaga, TestMessage>, IStateMachineActivity<TestSaga, TestMessage>>>(
            dataRecorder.Activities[0], "_activityFactory", dataFactory);
        AssertDelegate<Func<IBehaviorContext<TestSaga, TestMessage>, Task<IStateMachineActivity<TestSaga, TestMessage>>>>(
            dataRecorder.Activities[1], "_activityFactory", asyncDataFactory);

        var wrappedFactory = Assert.IsType<FactoryActivity<TestSaga, TestMessage>>(dataRecorder.Activities[2]);
        var producedFactory = GetField<Func<IBehaviorContext<TestSaga, TestMessage>, IStateMachineActivity<TestSaga, TestMessage>>>(
            wrappedFactory, "_activityFactory");
        var slim = Assert.IsType<SlimActivity<TestSaga, TestMessage>>(producedFactory(context));
        Assert.Same(context, slimFactoryContext);
        Assert.Same(activity, GetField<IStateMachineActivity<TestSaga>>(slim, "_activity"));

        var wrappedAsyncFactory = Assert.IsType<AsyncFactoryActivity<TestSaga, TestMessage>>(dataRecorder.Activities[3]);
        var producedAsyncFactory = GetField<Func<IBehaviorContext<TestSaga, TestMessage>, Task<IStateMachineActivity<TestSaga, TestMessage>>>>(
            wrappedAsyncFactory, "_activityFactory");
        var asyncSlim = Assert.IsType<SlimActivity<TestSaga, TestMessage>>(await producedAsyncFactory(context));
        Assert.Same(context, asyncSlimFactoryContext);
        Assert.Same(activity, GetField<IStateMachineActivity<TestSaga>>(asyncSlim, "_activity"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-TRANSITION-EXTENSIONS", "target-lookup-accessor-wrapper-and-returned-binder-identity")]
    public void TransitionAndFinalize_ResolveTargetsPreserveAccessorAndSelectNormalOrFaultedWrappers()
    {
        IState<TestSaga> resolvedTarget = CreateState("Target");
        IState<TestSaga> final = CreateState("Final");
        IStateAccessor<TestSaga> accessor = CreateProxy<IStateAccessor<TestSaga>>((method, _) => throw Unused(method));
        var machineRecorder = new StateMachineRecorder(accessor, resolvedTarget, final);
        IStateMachine<TestSaga> stateMachine = machineRecorder.CreateProxy();
        IState suppliedTarget = CreateState("Target");
        IEventActivityBinder<TestSaga> eventBinder =
            CreateBinder<IEventActivityBinder<TestSaga>>(stateMachine, out BinderRecorder eventRecorder);
        IEventActivityBinder<TestSaga, TestMessage> dataBinder =
            CreateBinder<IEventActivityBinder<TestSaga, TestMessage>>(stateMachine, out BinderRecorder dataRecorder);
        IExceptionActivityBinder<TestSaga, InvalidOperationException> exceptionBinder =
            CreateBinder<IExceptionActivityBinder<TestSaga, InvalidOperationException>>(stateMachine, out BinderRecorder exceptionRecorder);
        IExceptionActivityBinder<TestSaga, TestMessage, InvalidOperationException> dataExceptionBinder =
            CreateBinder<IExceptionActivityBinder<TestSaga, TestMessage, InvalidOperationException>>(
                stateMachine, out BinderRecorder dataExceptionRecorder);

        Assert.Same(eventBinder, eventBinder.TransitionTo(suppliedTarget));
        Assert.Same(eventBinder, eventBinder.Finalize());
        Assert.Same(dataBinder, dataBinder.TransitionTo(suppliedTarget));
        Assert.Same(dataBinder, dataBinder.Finalize());
        Assert.Same(exceptionBinder, exceptionBinder.TransitionTo(suppliedTarget));
        Assert.Same(exceptionBinder, exceptionBinder.Finalize());
        Assert.Same(dataExceptionBinder, dataExceptionBinder.TransitionTo(suppliedTarget));
        Assert.Same(dataExceptionBinder, dataExceptionBinder.Finalize());

        Assert.Equal(["Target", "Final", "Target", "Final", "Target", "Final", "Target", "Final"], machineRecorder.RequestedStates);
        AssertTransition(eventRecorder.Activities[0], resolvedTarget, accessor, faulted: false);
        AssertTransition(eventRecorder.Activities[1], final, accessor, faulted: false);
        AssertTransition(dataRecorder.Activities[0], resolvedTarget, accessor, faulted: false);
        AssertTransition(dataRecorder.Activities[1], final, accessor, faulted: false);
        AssertTransition(exceptionRecorder.Activities[0], resolvedTarget, accessor, faulted: true);
        AssertTransition(exceptionRecorder.Activities[1], final, accessor, faulted: true);
        AssertTransition(dataExceptionRecorder.Activities[0], resolvedTarget, accessor, faulted: true);
        AssertTransition(dataExceptionRecorder.Activities[1], final, accessor, faulted: true);
        Assert.NotSame(suppliedTarget, resolvedTarget);
    }

    private static void AssertArgument(string parameterName, Action action) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);

    private static void AssertDelegate<TDelegate>(object activity, string fieldName, TDelegate expected)
        where TDelegate : Delegate =>
        Assert.Same(expected, GetField<TDelegate>(activity, fieldName));

    private static void AssertTransition(object addedActivity, IState<TestSaga> expectedState,
        IStateAccessor<TestSaga> expectedAccessor, bool faulted)
    {
        object transition = addedActivity;
        if (faulted)
        {
            var wrapper = Assert.IsType<ExecuteOnFaultedActivity<TestSaga>>(addedActivity);
            transition = GetField<IStateMachineActivity<TestSaga>>(wrapper, "_activity");
        }

        Assert.Equal("TransitionActivity`1", transition.GetType().Name);
        Assert.Same(expectedState, GetField<IState<TestSaga>>(transition, "_toState"));
        Assert.Same(expectedAccessor, GetField<IStateAccessor<TestSaga>>(transition, "_currentStateAccessor"));
    }

    private static TField GetField<TField>(object instance, string name)
    {
        FieldInfo? field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        object? value = field.GetValue(instance);
        Assert.NotNull(value);
        return Assert.IsAssignableFrom<TField>(value);
    }

    private static IStateMachineActivity<TestSaga> CreateActivity() =>
        CreateProxy<IStateMachineActivity<TestSaga>>((method, _) => throw Unused(method));

    private static IStateMachineActivity<TestSaga, TestMessage> CreateDataActivity() =>
        CreateProxy<IStateMachineActivity<TestSaga, TestMessage>>((method, _) => throw Unused(method));

    private static IState<TestSaga> CreateState(string name) =>
        CreateProxy<IState<TestSaga>>((method, _) => method.Name == "get_Name" ? name : throw Unused(method));

    private static IStateMachine<TestSaga> CreateStateMachine(out StateMachineRecorder recorder,
        out IState<TestSaga> target, out IState<TestSaga> final, out IStateAccessor<TestSaga> accessor)
    {
        target = CreateState("Target");
        final = CreateState("Final");
        accessor = CreateProxy<IStateAccessor<TestSaga>>((method, _) => throw Unused(method));
        recorder = new StateMachineRecorder(accessor, target, final);
        return recorder.CreateProxy();
    }

    private static TBinder CreateBinder<TBinder>(IStateMachine<TestSaga> stateMachine, out BinderRecorder recorder)
        where TBinder : class
    {
        recorder = new BinderRecorder();
        BinderRecorder capturedRecorder = recorder;
        TBinder binder = null!;
        binder = CreateProxy<TBinder>((method, arguments) =>
        {
            capturedRecorder.InvocationCount++;
            if (method.Name == "get_StateMachine")
                return stateMachine;
            if (method.Name == "Add")
            {
                object? activity = Assert.Single(arguments!);
                Assert.NotNull(activity);
                capturedRecorder.Activities.Add(activity);
                return binder;
            }

            throw Unused(method);
        });
        return binder;
    }

    private static T CreateProxy<T>(Func<MethodInfo, object?[]?, object?> handler)
        where T : class
    {
        T proxy = DispatchProxy.Create<T, ForwardingProxy>();
        ((ForwardingProxy)(object)proxy).Handler = handler;
        return proxy;
    }

    private static NotSupportedException Unused(MethodInfo method) =>
        new($"The member '{method.Name}' is not used by this focused contract.");

    private sealed class BinderRecorder
    {
        public List<object> Activities { get; } = [];
        public int InvocationCount { get; set; }
    }

    private sealed class StateMachineRecorder(
        IStateAccessor<TestSaga> accessor,
        IState<TestSaga> target,
        IState<TestSaga> final)
    {
        public List<string> RequestedStates { get; } = [];

        public IStateMachine<TestSaga> CreateProxy() =>
            SagaBehaviorTransitionExtensionDeepContractTests.CreateProxy<IStateMachine<TestSaga>>((method, arguments) =>
            {
                if (method.Name == "get_Accessor")
                    return accessor;
                if (method.Name == "get_Final")
                    return final;
                if (method.Name == "GetState" && method.ReturnType == typeof(IState<TestSaga>))
                {
                    string name = Assert.IsType<string>(Assert.Single(arguments!));
                    RequestedStates.Add(name);
                    return name == final.Name ? final : target;
                }

                throw Unused(method);
            });
    }

    private class ForwardingProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.NotNull(targetMethod);
            return Handler(targetMethod, args);
        }
    }

    private sealed class RecordingMissingInstanceConfigurator : IMissingInstanceConfigurator<TestSaga, TestMessage>
    {
        readonly IPipe<ConsumeContext<TestMessage>> _pipe = Pipe.Execute<ConsumeContext<TestMessage>>(_ => { });

        public int DiscardCount { get; private set; }

        public IPipe<ConsumeContext<TestMessage>> Discard()
        {
            DiscardCount++;
            return _pipe;
        }

        public IPipe<ConsumeContext<TestMessage>> Fault() => _pipe;

        public IPipe<ConsumeContext<TestMessage>> ExecuteAwaited(Func<ConsumeContext<TestMessage>, Task> callback) =>
            Pipe.ExecuteAwaited(callback);

        public IPipe<ConsumeContext<TestMessage>> Execute(Action<ConsumeContext<TestMessage>> callback) =>
            Pipe.Execute(callback);
    }

    private sealed class TestSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed record TestMessage(string Value = "value");
}
