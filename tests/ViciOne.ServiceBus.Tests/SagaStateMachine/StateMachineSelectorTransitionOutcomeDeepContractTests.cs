using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.SagaStateMachine;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineSelectorTransitionOutcomeDeepContractTests
{
    static readonly Type OutcomeType = typeof(StateMachineActivitySelector<>).Assembly.GetType(
        "ViciOne.ServiceBus.SagaStateMachine.ForwardedRequestOutcome", throwOnError: true)!;
    static readonly Type TransitionType = typeof(StateMachineActivitySelector<>).Assembly.GetType(
        "ViciOne.ServiceBus.SagaStateMachine.TransitionActivity`1", throwOnError: true)!;

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-220-selector-transition-outcome-exact-surface-nullability")]
    public void Types_HaveExactVisibilitySurfaceGenericNamesConstraintsAndNullability()
    {
        AssertSelector(
            typeof(StateMachineActivitySelector<>),
            ["TSaga"],
            typeof(IEventActivityBinder<TestSaga>),
            ["OfType"]);
        AssertSelector(
            typeof(StateMachineActivitySelector<,>),
            ["TSaga", "TMessage"],
            typeof(IEventActivityBinder<TestSaga, Message>),
            ["OfInstanceType", "OfType"]);
        AssertSelector(
            typeof(StateMachineFaultedActivitySelector<,>),
            ["TSaga", "TException"],
            typeof(IExceptionActivityBinder<TestSaga, MarkerException>),
            ["OfType"]);
        AssertSelector(
            typeof(StateMachineFaultedActivitySelector<,,>),
            ["TSaga", "TMessage", "TException"],
            typeof(IExceptionActivityBinder<TestSaga, Message, MarkerException>),
            ["OfInstanceType", "OfType"]);

        Assert.True(TransitionType.IsNotPublic);
        Assert.True(TransitionType.IsSealed);
        Assert.Equal(["TSaga"], TransitionType.GetGenericArguments().Select(x => x.Name));
        AssertSagaConstraint(TransitionType.GetGenericArguments()[0]);

        Type closedTransition = TransitionType.MakeGenericType(typeof(TestSaga));
        ConstructorInfo transitionConstructor = Assert.Single(closedTransition.GetConstructors());
        AssertParameters(
            transitionConstructor.GetParameters(),
            ("toState", typeof(IState<TestSaga>)),
            ("currentStateAccessor", typeof(IStateAccessor<TestSaga>)));
        PropertyInfo toState = Assert.Single(closedTransition.GetProperties(BindingFlags.Instance | BindingFlags.Public));
        Assert.Equal("ToState", toState.Name);
        Assert.Equal(typeof(IState), toState.PropertyType);
        Assert.True(toState.CanRead);
        Assert.False(toState.CanWrite);

        MethodInfo[] allTransitionMethods = closedTransition.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        MethodInfo propertyGetter = Assert.Single(allTransitionMethods, method => method.IsSpecialName);
        Assert.Equal("get_ToState", propertyGetter.Name);
        MethodInfo[] transitionMethods = allTransitionMethods.Where(method => !method.IsSpecialName).ToArray();
        Assert.Equal(6, transitionMethods.Length);
        Assert.Equal(2, transitionMethods.Count(x => x.Name == "ExecuteAsync"));
        Assert.Equal(2, transitionMethods.Count(x => x.Name == "FaultedAsync"));
        Assert.All(transitionMethods.Where(x => typeof(Task).IsAssignableFrom(x.ReturnType)),
            method => Assert.EndsWith("Async", method.Name, StringComparison.Ordinal));
        Assert.Equal(["T"], Assert.Single(transitionMethods, x => x.Name == "ExecuteAsync" && x.IsGenericMethodDefinition)
            .GetGenericArguments().Select(x => x.Name));
        Assert.Contains(transitionMethods, x => x.Name == "FaultedAsync" &&
            x.GetGenericArguments().Select(argument => argument.Name).SequenceEqual(["TException"]));
        Assert.Contains(transitionMethods, x => x.Name == "FaultedAsync" &&
            x.GetGenericArguments().Select(argument => argument.Name).SequenceEqual(["T", "TException"]));
        Assert.All(transitionConstructor.GetParameters(), AssertNotNull);
        Assert.All(transitionMethods.SelectMany(x => x.GetParameters()), AssertNotNull);
        AssertNotNull(toState);

        Assert.True(OutcomeType.IsNotPublic);
        Assert.True(OutcomeType.IsSealed);
        Assert.False(OutcomeType.IsGenericType);
        ConstructorInfo outcomeConstructor = Assert.Single(OutcomeType.GetConstructors());
        AssertParameters(outcomeConstructor.GetParameters(), ("payload", typeof(object)), ("payloadTypes", typeof(string[])));
        PropertyInfo[] outcomeProperties = OutcomeType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        Assert.Equal(["Payload", "PayloadTypes"], outcomeProperties.Select(x => x.Name).OrderBy(x => x));
        Assert.Equal(typeof(object), outcomeProperties.Single(x => x.Name == "Payload").PropertyType);
        Assert.Equal(typeof(string[]), outcomeProperties.Single(x => x.Name == "PayloadTypes").PropertyType);
        Assert.All(outcomeProperties, property =>
        {
            Assert.True(property.CanRead);
            Assert.False(property.CanWrite);
            AssertNotNull(property);
        });
        Assert.All(outcomeConstructor.GetParameters(), AssertNotNull);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-220-selector-required-binder-and-exact-container-append")]
    public async Task Selectors_RejectMissingBindersAndAppendExactFreshContainersReturningBinderIdentityAsync()
    {
        AssertArgument("binder", () => new StateMachineActivitySelector<TestSaga>(null!));
        AssertArgument("binder", () => new StateMachineActivitySelector<TestSaga, Message>(null!));
        AssertArgument("binder", () => new StateMachineFaultedActivitySelector<TestSaga, MarkerException>(null!));
        AssertArgument("binder", () => new StateMachineFaultedActivitySelector<TestSaga, Message, MarkerException>(null!));

        AssertSelectorAppend<IEventActivityBinder<TestSaga>>(
            binder => new StateMachineActivitySelector<TestSaga>(binder).OfType<UntypedActivity>(),
            typeof(ContainerFactoryActivity<TestSaga, UntypedActivity>));
        AssertSelectorAppend<IEventActivityBinder<TestSaga, Message>>(
            binder => new StateMachineActivitySelector<TestSaga, Message>(binder).OfType<TypedActivity>(),
            typeof(ContainerFactoryActivity<TestSaga, Message, TypedActivity>));
        AssertSelectorAppend<IEventActivityBinder<TestSaga, Message>>(
            binder => new StateMachineActivitySelector<TestSaga, Message>(binder).OfInstanceType<UntypedActivity>(),
            typeof(ContainerFactoryActivity<TestSaga, UntypedActivity>));
        AssertSelectorAppend<IExceptionActivityBinder<TestSaga, MarkerException>>(
            binder => new StateMachineFaultedActivitySelector<TestSaga, MarkerException>(binder).OfType<UntypedActivity>(),
            typeof(FaultedContainerFactoryActivity<TestSaga, MarkerException, UntypedActivity>));
        AssertSelectorAppend<IExceptionActivityBinder<TestSaga, Message, MarkerException>>(
            binder => new StateMachineFaultedActivitySelector<TestSaga, Message, MarkerException>(binder).OfType<TypedActivity>(),
            typeof(FaultedContainerFactoryActivity<TestSaga, Message, MarkerException, TypedActivity>));
        AssertSelectorAppend<IExceptionActivityBinder<TestSaga, Message, MarkerException>>(
            binder => new StateMachineFaultedActivitySelector<TestSaga, Message, MarkerException>(binder).OfInstanceType<UntypedActivity>(),
            typeof(FaultedContainerFactoryActivity<TestSaga, MarkerException, UntypedActivity>));

        var appended = new ConcurrentQueue<object>();
        IEventActivityBinder<TestSaga> sentinel = CreateStrict<IEventActivityBinder<TestSaga>>();
        IEventActivityBinder<TestSaga> binder = CreateStrict<IEventActivityBinder<TestSaga>>((method, arguments) =>
        {
            Assert.Equal("Add", method.Name);
            appended.Enqueue(Assert.Single(arguments)!);
            return sentinel;
        });
        var selector = new StateMachineActivitySelector<TestSaga>(binder);

        IEventActivityBinder<TestSaga>[] results = await Task.WhenAll(Enumerable.Range(0, 32)
            .Select(_ => Task.Run(selector.OfType<UntypedActivity>, TestContext.Current.CancellationToken)));

        Assert.All(results, result => Assert.Same(sentinel, result));
        Assert.Equal(32, appended.Count);
        Assert.All(appended, activity => Assert.IsType<ContainerFactoryActivity<TestSaga, UntypedActivity>>(activity));
        Assert.Equal(32, appended.Distinct(ReferenceEqualityComparer.Instance).Count());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-220-transition-self-sibling-parent-child-disjoint-order")]
    public async Task Transition_ExecutesExactLifecycleOrderForEveryHierarchyRelationshipAsync()
    {
        await AssertTransitionOrderAsync(
            hierarchy => (hierarchy.AChild, hierarchy.AChild),
            ["get", "next"]);
        await AssertTransitionOrderAsync(
            hierarchy => (hierarchy.AChild, hierarchy.ASibling),
            ["get", "AChild.Leave", "ASibling.BeforeEnter", "set:ASibling", "AChild.AfterLeave", "ASibling.Enter", "next"]);
        await AssertTransitionOrderAsync(
            hierarchy => (hierarchy.ARoot, hierarchy.AChild),
            ["get", "AChild.BeforeEnter", "set:AChild", "AChild.Enter", "next"]);
        await AssertTransitionOrderAsync(
            hierarchy => (hierarchy.AChild, hierarchy.ARoot),
            ["get", "AChild.Leave", "set:ARoot", "AChild.AfterLeave", "next"]);
        await AssertTransitionOrderAsync(
            hierarchy => (hierarchy.AChild, hierarchy.BLeaf),
            [
                "get", "AChild.Leave", "ARoot.Leave",
                "BRoot.BeforeEnter", "BMid.BeforeEnter", "BLeaf.BeforeEnter", "set:BLeaf",
                "AChild.AfterLeave", "ARoot.AfterLeave",
                "BRoot.Enter", "BMid.Enter", "BLeaf.Enter", "next"
            ]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-FAULT", "iteration-220-transition-failure-cancellation-and-partial-state")]
    public async Task Transition_PreservesExactFailureCancellationAndPartialStateBoundariesAsync()
    {
        var getFailureLog = new ConcurrentQueue<string>();
        Hierarchy getFailureHierarchy = CreateHierarchy(getFailureLog);
        var getFailure = new MarkerException();
        var getFailureAccessor = new RecordingAccessor(getFailureHierarchy.AChild, getFailureLog)
        {
            GetFailure = getFailure
        };
        using var getFailureCancellation = new CancellationTokenSource();
        IBehaviorContext<TestSaga> getFailureContext = CreateContext(getFailureCancellation.Token);
        var getFailureNext = new RecordingBehavior(getFailureLog);
        IStateMachineActivity<TestSaga> getFailureTransition =
            SagaStateMachineExecutionTestDriver.CreateTransition(getFailureHierarchy.BLeaf, getFailureAccessor);

        MarkerException caughtGetFailure = await Assert.ThrowsAsync<MarkerException>(() =>
            getFailureTransition.ExecuteAsync(getFailureContext, getFailureNext));

        Assert.Same(getFailure, caughtGetFailure);
        Assert.Equal(1, getFailureAccessor.GetCount);
        Assert.Equal(0, getFailureAccessor.SetCount);
        Assert.Equal(getFailureCancellation.Token, getFailureAccessor.GetToken);
        Assert.Same(getFailureContext, getFailureAccessor.GetContext);
        Assert.Null(getFailureAccessor.SetContext);
        Assert.Same(getFailureHierarchy.AChild, getFailureAccessor.Current);
        Assert.Equal(["get"], getFailureLog);
        Assert.Empty(getFailureHierarchy.TypedLifecycleCalls);
        Assert.Equal(0, getFailureNext.ExecuteCount);
        Assert.Null(getFailureNext.LastExecuteContext);

        var log = new ConcurrentQueue<string>();
        Hierarchy hierarchy = CreateHierarchy(log);
        var accessor = new RecordingAccessor(hierarchy.AChild, log);
        IBehaviorContext<TestSaga> context = CreateContext(TestContext.Current.CancellationToken);
        var next = new RecordingBehavior(log);
        IStateMachineActivity<TestSaga> transition = SagaStateMachineExecutionTestDriver.CreateTransition(hierarchy.BLeaf, accessor);
        var failure = new MarkerException();
        hierarchy.LifecycleFailure = failure;
        hierarchy.FailureEventName = "AChild.AfterLeave";

        Exception caught = await Assert.ThrowsAsync<MarkerException>(() => transition.ExecuteAsync(context, next));

        Assert.Same(failure, caught);
        Assert.Same(hierarchy.BLeaf, accessor.Current);
        Assert.Equal(
            [
                "get", "AChild.Leave", "ARoot.Leave",
                "BRoot.BeforeEnter", "BMid.BeforeEnter", "BLeaf.BeforeEnter", "set:BLeaf",
                "AChild.AfterLeave"
            ],
            log);
        Assert.Equal(0, next.ExecuteCount);

        log.Clear();
        accessor.Current = hierarchy.AChild;
        hierarchy.LifecycleFailure = null;
        hierarchy.FailureEventName = null;
        accessor.SetFailure = failure;

        caught = await Assert.ThrowsAsync<MarkerException>(() => transition.ExecuteAsync(context, next));

        Assert.Same(failure, caught);
        Assert.Same(hierarchy.AChild, accessor.Current);
        Assert.Equal(
            [
                "get", "AChild.Leave", "ARoot.Leave",
                "BRoot.BeforeEnter", "BMid.BeforeEnter", "BLeaf.BeforeEnter", "set:BLeaf"
            ],
            log);
        Assert.Equal(0, next.ExecuteCount);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        log.Clear();
        accessor.SetFailure = null;
        context = CreateContext(cancellation.Token);

        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            transition.ExecuteAsync(context, next));

        Assert.Equal(cancellation.Token, canceled.CancellationToken);
        Assert.Empty(log);
        Assert.Same(hierarchy.AChild, accessor.Current);
        Assert.Equal(0, next.ExecuteCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-220-transition-context-fault-visitor-probe-boundaries")]
    public async Task Transition_PreservesContextFaultTaskVisitorProbeAndRequiredArgumentIdentityAsync()
    {
        var log = new ConcurrentQueue<string>();
        Hierarchy hierarchy = CreateHierarchy(log);
        var accessor = new RecordingAccessor(hierarchy.AChild, log);
        IStateMachineActivity<TestSaga> transition = SagaStateMachineExecutionTestDriver.CreateTransition(hierarchy.ASibling, accessor);
        IBehaviorContext<TestSaga> context = CreateContext(TestContext.Current.CancellationToken);
        IBehaviorContext<TestSaga, Message> typedContext = CreateContext(TestContext.Current.CancellationToken, new Message());
        IBehaviorExceptionContext<TestSaga, MarkerException> faultContext = CreateStrict<IBehaviorExceptionContext<TestSaga, MarkerException>>();
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> typedFaultContext =
            CreateStrict<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
        var next = new RecordingBehavior(log);
        var typedNext = new RecordingTypedBehavior(log);

        await AssertArgumentAsync("context", () => transition.ExecuteAsync(null!, next));
        await AssertArgumentAsync("next", () => transition.ExecuteAsync(context, null!));
        await AssertArgumentAsync("context", () => transition.ExecuteAsync<Message>(null!, typedNext));
        await AssertArgumentAsync("next", () => transition.ExecuteAsync(typedContext, null!));
        AssertArgument("context", () => transition.FaultedAsync<MarkerException>(null!, next));
        AssertArgument("next", () => transition.FaultedAsync(faultContext, null!));
        AssertArgument("context", () => transition.FaultedAsync<Message, MarkerException>(null!, typedNext));
        AssertArgument("next", () => transition.FaultedAsync(typedFaultContext, null!));
        AssertArgument("visitor", () => transition.Accept(null!));
        AssertArgument("context", () => transition.Probe(null!));
        AssertArgument("toState", () => SagaStateMachineExecutionTestDriver.CreateTransition<TestSaga>(null!, accessor));
        AssertArgument("currentStateAccessor", () => SagaStateMachineExecutionTestDriver.CreateTransition(hierarchy.ASibling, null!));

        Task faultTask = NewIncompleteTaskAsync();
        next.FaultTask = faultTask;
        Assert.Same(faultTask, transition.FaultedAsync(faultContext, next));
        Assert.Same(faultContext, next.LastFaultContext);

        Task typedFaultTask = NewIncompleteTaskAsync();
        typedNext.FaultTask = typedFaultTask;
        Assert.Same(typedFaultTask, transition.FaultedAsync(typedFaultContext, typedNext));
        Assert.Same(typedFaultContext, typedNext.LastFaultContext);
        Assert.Empty(log);

        using var typedCancellation = new CancellationTokenSource();
        var message = new Message();
        typedContext = CreateContext(typedCancellation.Token, message);

        await transition.ExecuteAsync(typedContext, typedNext);

        Assert.Equal(
            ["get", "AChild.Leave", "ASibling.BeforeEnter", "set:ASibling", "AChild.AfterLeave", "ASibling.Enter", "typed-next"],
            log);
        Assert.Equal(1, accessor.GetCount);
        Assert.Equal(1, accessor.SetCount);
        Assert.Equal(typedCancellation.Token, accessor.GetToken);
        Assert.Equal(typedCancellation.Token, accessor.SetToken);
        Assert.Same(typedContext, accessor.GetContext);
        Assert.Same(typedContext, accessor.SetContext);
        Assert.Same(hierarchy.ASibling, accessor.Current);
        Assert.Equal(1, typedNext.ExecuteCount);
        Assert.Same(typedContext, typedNext.LastExecuteContext);
        Assert.All(hierarchy.TypedLifecycleCalls, call => Assert.Same(call.State, call.Payload));

        await AssertLifecycleCancellationForwardingAsync();

        object? visited = null;
        IStateMachineVisitor visitor = CreateStrict<IStateMachineVisitor>((method, arguments) =>
        {
            Assert.Equal("Visit", method.Name);
            Assert.Single(arguments);
            visited = arguments[0];
            return null;
        });
        transition.Accept(visitor);
        Assert.Same(transition, visited);

        var probeCalls = new List<(string Method, object?[] Arguments)>();
        ProbeContext? child = null;
        child = CreateStrict<ProbeContext>((method, arguments) =>
        {
            probeCalls.Add((method.Name, arguments));
            return null;
        });
        ProbeContext root = CreateStrict<ProbeContext>((method, arguments) =>
        {
            Assert.Equal("CreateScope", method.Name);
            Assert.Equal("transition", Assert.Single(arguments));
            return child;
        });
        transition.Probe(root);
        (string probeMethod, object?[] probeArguments) = Assert.Single(probeCalls);
        Assert.Equal("Add", probeMethod);
        Assert.Equal("toState", probeArguments[0]);
        Assert.Equal("ASibling", probeArguments[1]);

        var visitorFailure = new MarkerException();
        visitor = CreateStrict<IStateMachineVisitor>((_, _) => throw visitorFailure);
        Assert.Same(visitorFailure, Assert.Throws<MarkerException>(() => transition.Accept(visitor)));
        ProbeContext probe = CreateStrict<ProbeContext>((_, _) => throw visitorFailure);
        Assert.Same(visitorFailure, Assert.Throws<MarkerException>(() => transition.Probe(probe)));

    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "iteration-220-forwarded-outcome-validation-payload-and-snapshot-ownership")]
    public void ForwardedOutcome_ValidatesInputsAndOwnsEveryPayloadTypeSnapshot()
    {
        object payload = new();
        string[] supplied = ["urn:message:a", "urn:message:b"];
        object outcome = CreateOutcome(payload, supplied);

        Assert.Same(payload, GetOutcomePayload(outcome));
        supplied[0] = "mutated-source";
        string[] firstRead = GetOutcomePayloadTypes(outcome);
        Assert.Equal(["urn:message:a", "urn:message:b"], firstRead);

        firstRead[1] = "mutated-result";
        string[] secondRead = GetOutcomePayloadTypes(outcome);
        Assert.Equal(["urn:message:a", "urn:message:b"], secondRead);
        Assert.NotSame(firstRead, secondRead);

        AssertOutcomeArgument<ArgumentNullException>("payload", null!, ["a"]);
        AssertOutcomeArgument<ArgumentNullException>("payloadTypes", payload, null!);
        AssertOutcomeArgument<ArgumentException>("payloadTypes", payload, []);
        AssertOutcomeArgument<ArgumentException>("payloadTypes", payload, ["a", null!]);

        string[][] concurrentReads = Enumerable.Range(0, 32)
            .AsParallel()
            .Select(_ => GetOutcomePayloadTypes(outcome))
            .ToArray();
        Assert.All(concurrentReads, snapshot => Assert.Equal(["urn:message:a", "urn:message:b"], snapshot));
        Assert.Equal(concurrentReads.Length, concurrentReads.Distinct(ReferenceEqualityComparer.Instance).Count());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-220-transition-concurrent-context-isolation")]
    public async Task Transition_IsolatesConcurrentContextsTokensStateAndContinuationAsync()
    {
        const int count = 24;
        var histories = new ConcurrentDictionary<int, ConcurrentQueue<string>>();
        var target = CreateState("Target", null, histories);
        var accessor = new ContextMappedAccessor(histories);
        IStateMachineActivity<TestSaga> transition = SagaStateMachineExecutionTestDriver.CreateTransition(target, accessor);
        var scenarios = Enumerable.Range(0, count).Select(index =>
        {
            var cancellation = new CancellationTokenSource();
            IBehaviorContext<TestSaga> context = CreateContext(cancellation.Token, index);
            return new ConcurrentScenario(index, cancellation, context, new ContextRecordingBehavior(histories));
        }).ToArray();

        await Task.WhenAll(scenarios.Select(scenario => transition.ExecuteAsync(scenario.Context, scenario.Next)));

        foreach (ConcurrentScenario scenario in scenarios)
        {
            Assert.Same(target, accessor.GetState(scenario.Context));
            Assert.Equal(scenario.Cancellation.Token, accessor.GetToken(scenario.Context));
            Assert.Same(scenario.Context, scenario.Next.LastContext);
            Assert.Equal(["get", "Target.BeforeEnter", "set:Target", "Target.Enter", "next"], histories[scenario.Id]);
            scenario.Cancellation.Dispose();
        }
    }

    static async Task AssertTransitionOrderAsync(
        Func<Hierarchy, (IState<TestSaga> Current, IState<TestSaga> Target)> select,
        string[] expected)
    {
        var log = new ConcurrentQueue<string>();
        Hierarchy hierarchy = CreateHierarchy(log);
        (IState<TestSaga> current, IState<TestSaga> target) = select(hierarchy);
        var accessor = new RecordingAccessor(current, log);
        using var cancellation = new CancellationTokenSource();
        IBehaviorContext<TestSaga> context = CreateContext(cancellation.Token);
        var next = new RecordingBehavior(log);
        IStateMachineActivity<TestSaga> transition = SagaStateMachineExecutionTestDriver.CreateTransition(target, accessor);

        await transition.ExecuteAsync(context, next);

        Assert.Equal(expected, log);
        Assert.Same(target, accessor.Current);
        Assert.Equal(1, accessor.GetCount);
        Assert.Equal(cancellation.Token, accessor.GetToken);
        Assert.Same(context, accessor.GetContext);
        if (!ReferenceEquals(current, target))
        {
            Assert.Equal(1, accessor.SetCount);
            Assert.Equal(cancellation.Token, accessor.SetToken);
            Assert.Same(context, accessor.SetContext);
        }
        else
        {
            Assert.Equal(0, accessor.SetCount);
            Assert.Null(accessor.SetContext);
        }
        Assert.Same(context, next.LastExecuteContext);
        Assert.All(hierarchy.TypedLifecycleCalls, call => Assert.Same(call.State, call.Payload));
    }

    static async Task AssertLifecycleCancellationForwardingAsync()
    {
        var log = new ConcurrentQueue<string>();
        var raises = new ConcurrentQueue<(string EventName, CancellationToken Token)>();
        IState<TestSaga> current = CreateRecordingState("Current", log, raises);
        IState<TestSaga> target = CreateRecordingState("Target", log, raises);
        var accessor = new RecordingAccessor(current, log);
        using var cancellation = new CancellationTokenSource();
        IBehaviorContext<TestSaga> context = CreateContext(cancellation.Token);
        var next = new RecordingBehavior(log);
        IStateMachineActivity<TestSaga> transition = SagaStateMachineExecutionTestDriver.CreateTransition(target, accessor);

        await transition.ExecuteAsync(context, next);

        Assert.Equal(
            ["get", "Current.Leave", "Target.BeforeEnter", "set:Target", "Current.AfterLeave", "Target.Enter", "next"],
            log);
        Assert.Equal(
            ["Current.Leave", "Target.BeforeEnter", "Current.AfterLeave", "Target.Enter"],
            raises.Select(call => call.EventName));
        Assert.All(raises, call => Assert.Equal(cancellation.Token, call.Token));
        Assert.Equal(cancellation.Token, accessor.GetToken);
        Assert.Equal(cancellation.Token, accessor.SetToken);
        Assert.Same(context, accessor.GetContext);
        Assert.Same(context, accessor.SetContext);
        Assert.Same(context, next.LastExecuteContext);
    }

    static IState<TestSaga> CreateRecordingState(
        string name,
        ConcurrentQueue<string> log,
        ConcurrentQueue<(string EventName, CancellationToken Token)> raises)
    {
        IEvent enter = CreateNamedEvent<IEvent>(name + ".Enter");
        IEvent leave = CreateNamedEvent<IEvent>(name + ".Leave");
        IEvent<IState> beforeEnter = CreateNamedEvent<IEvent<IState>>(name + ".BeforeEnter");
        IEvent<IState> afterLeave = CreateNamedEvent<IEvent<IState>>(name + ".AfterLeave");
        IState<TestSaga>? state = null;
        state = CreateStrict<IState<TestSaga>>((method, arguments) =>
        {
            if (method.Name == "RaiseAsync")
            {
                var context = (IBehaviorContext<TestSaga>)arguments[0]!;
                var token = (CancellationToken)arguments[1]!;
                string eventName = context.Event.Name;
                log.Enqueue(eventName);
                raises.Enqueue((eventName, token));
                return Task.CompletedTask;
            }

            return method.Name switch
            {
                "get_Name" => name,
                "get_Enter" => enter,
                "get_Leave" => leave,
                "get_BeforeEnter" => beforeEnter,
                "get_AfterLeave" => afterLeave,
                "get_SuperState" => null,
                "get_Events" or "get_DeclaredEvents" => Array.Empty<IEvent>(),
                "HasState" or "IsStateOf" => ReferenceEquals(state, arguments[0]),
                "Equals" => ReferenceEquals(state, arguments[0]),
                "GetHashCode" => name.GetHashCode(StringComparison.Ordinal),
                "ToString" => name,
                _ => throw Unexpected()
            };
        });
        return state;
    }

    static TEvent CreateNamedEvent<TEvent>(string name)
        where TEvent : class, IEvent => CreateStrict<TEvent>((method, _) =>
            method.Name == "get_Name" ? name : throw Unexpected());

    static Hierarchy CreateHierarchy(ConcurrentQueue<string> log)
    {
        var hierarchy = new Hierarchy(log);
        hierarchy.ARoot = CreateState("ARoot", null, hierarchy);
        hierarchy.AChild = CreateState("AChild", hierarchy.ARoot, hierarchy);
        hierarchy.ASibling = CreateState("ASibling", hierarchy.ARoot, hierarchy);
        hierarchy.BRoot = CreateState("BRoot", null, hierarchy);
        hierarchy.BMid = CreateState("BMid", hierarchy.BRoot, hierarchy);
        hierarchy.BLeaf = CreateState("BLeaf", hierarchy.BMid, hierarchy);
        return hierarchy;
    }

    static ViciOneServiceBusStateMachine<TestSaga>.StateMachineState CreateState(
        string name,
        IState<TestSaga>? parent,
        Hierarchy hierarchy)
    {
        var state = new ViciOneServiceBusStateMachine<TestSaga>.StateMachineState(
            (_, _) => throw Unexpected(),
            name,
            new NoopObserver(),
            parent);
        var activity = new LifecycleActivity(state, hierarchy);
        state.Bind(state.Enter, activity);
        state.Bind(state.Leave, activity);
        state.Bind(state.BeforeEnter, activity);
        state.Bind(state.AfterLeave, activity);
        return state;
    }

    static ViciOneServiceBusStateMachine<TestSaga>.StateMachineState CreateState(
        string name,
        IState<TestSaga>? parent,
        ConcurrentDictionary<int, ConcurrentQueue<string>> histories)
    {
        var state = new ViciOneServiceBusStateMachine<TestSaga>.StateMachineState(
            (_, _) => throw Unexpected(),
            name,
            new NoopObserver(),
            parent);
        var activity = new ConcurrentLifecycleActivity(histories);
        state.Bind(state.Enter, activity);
        state.Bind(state.BeforeEnter, activity);
        return state;
    }

    static void AssertSelectorAppend<TBinder>(Func<TBinder, TBinder> select, Type expectedActivityType)
        where TBinder : class
    {
        object? appended = null;
        TBinder sentinel = CreateStrict<TBinder>();
        TBinder binder = CreateStrict<TBinder>((method, arguments) =>
        {
            Assert.Equal("Add", method.Name);
            appended = Assert.Single(arguments);
            return sentinel;
        });

        TBinder result = select(binder);

        Assert.Same(sentinel, result);
        Assert.NotNull(appended);
        Assert.Equal(expectedActivityType, appended.GetType());
    }

    static void AssertSelector(Type openType, string[] genericNames, Type closedBinderType, string[] methodNames)
    {
        Assert.True(openType.IsPublic);
        Assert.False(openType.IsAbstract);
        Assert.False(openType.IsSealed);
        Assert.Equal(genericNames, openType.GetGenericArguments().Select(x => x.Name));
        AssertSagaConstraint(openType.GetGenericArguments()[0]);
        for (var index = 1; index < openType.GetGenericArguments().Length; index++)
        {
            Type argument = openType.GetGenericArguments()[index];
            if (argument.Name == "TException")
                Assert.Equal(typeof(Exception), Assert.Single(argument.GetGenericParameterConstraints()));
            else
                Assert.True(argument.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        }

        Type[] closedArguments = openType.GetGenericArguments().Select(argument => argument.Name switch
        {
            "TSaga" => typeof(TestSaga),
            "TMessage" => typeof(Message),
            "TException" => typeof(MarkerException),
            _ => throw Unexpected()
        }).ToArray();
        Type closedType = openType.MakeGenericType(closedArguments);
        ConstructorInfo constructor = Assert.Single(closedType.GetConstructors(BindingFlags.Instance | BindingFlags.Public));
        ParameterInfo binder = Assert.Single(constructor.GetParameters());
        Assert.Equal("binder", binder.Name);
        Assert.Equal(closedBinderType, binder.ParameterType);
        AssertNotNull(binder);

        MethodInfo[] methods = closedType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.Equal(methodNames, methods.Select(x => x.Name).OrderBy(x => x));
        Assert.All(methods, method =>
        {
            Assert.True(method.IsGenericMethodDefinition);
            Type activity = Assert.Single(method.GetGenericArguments());
            Assert.Equal("TActivity", activity.Name);
            Assert.True(activity.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
            Assert.Contains(activity.GetGenericParameterConstraints(), constraint =>
                constraint.IsGenericType && constraint.GetGenericTypeDefinition().Name.StartsWith("IStateMachineActivity", StringComparison.Ordinal));
            Assert.Equal(closedBinderType, method.ReturnType);
            AssertNotNull(method.ReturnParameter);
        });
    }

    static void AssertSagaConstraint(Type argument)
    {
        Assert.True(argument.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        Assert.Equal(typeof(ISagaStateMachineInstance), Assert.Single(argument.GetGenericParameterConstraints()));
    }

    static void AssertParameters(ParameterInfo[] actual, params (string Name, Type Type)[] expected)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            Assert.Equal(expected[index].Name, actual[index].Name);
            Assert.Equal(expected[index].Type, actual[index].ParameterType);
        }
    }

    static void AssertNotNull(ParameterInfo parameter) =>
        Assert.Equal(NullabilityState.NotNull, new NullabilityInfoContext().Create(parameter).ReadState);

    static void AssertNotNull(PropertyInfo property) =>
        Assert.Equal(NullabilityState.NotNull, new NullabilityInfoContext().Create(property).ReadState);

    static void AssertArgument(string parameterName, Action action) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);

    static async Task AssertArgumentAsync(string parameterName, Func<Task> action) =>
        Assert.Equal(parameterName, (await Assert.ThrowsAsync<ArgumentNullException>(action)).ParamName);

    static object CreateOutcome(object payload, string[] payloadTypes) =>
        Assert.Single(OutcomeType.GetConstructors()).Invoke([payload, payloadTypes]);

    static object GetOutcomePayload(object outcome) => OutcomeType.GetProperty("Payload")!.GetValue(outcome)!;

    static string[] GetOutcomePayloadTypes(object outcome) => (string[])OutcomeType.GetProperty("PayloadTypes")!.GetValue(outcome)!;

    static void AssertOutcomeArgument<TException>(string parameterName, object payload, string[] payloadTypes)
        where TException : ArgumentException
    {
        TargetInvocationException wrapper = Assert.Throws<TargetInvocationException>(() => CreateOutcome(payload, payloadTypes));
        TException exception = Assert.IsType<TException>(wrapper.InnerException);
        Assert.Equal(parameterName, exception.ParamName);
    }

    static T CreateStrict<T>(Func<MethodInfo, object?[], object?>? handler = null)
        where T : class
    {
        T proxy = DispatchProxy.Create<T, StrictProxy>();
        ((StrictProxy)(object)proxy).Handler = handler;
        return proxy;
    }

    static IBehaviorContext<TestSaga> CreateContext(CancellationToken token, int id = -1) =>
        CreateContextCore<IBehaviorContext<TestSaga>>(token, null, null, id);

    static IBehaviorContext<TestSaga, T> CreateContext<T>(CancellationToken token, T message, int id = -1)
        where T : class => CreateContextCore<IBehaviorContext<TestSaga, T>>(token, null, message, id);

    static TContext CreateContextCore<TContext>(CancellationToken token, IEvent? @event, object? message, int id)
        where TContext : class
    {
        TContext context = DispatchProxy.Create<TContext, BehaviorContextProxy>();
        var proxy = (BehaviorContextProxy)(object)context;
        proxy.Token = token;
        proxy.Event = @event;
        proxy.Message = message;
        proxy.Id = id;
        return context;
    }

    static Task NewIncompleteTaskAsync() => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task;

    static InvalidOperationException Unexpected() => new("Unexpected test-double call.");

    public sealed class TestSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed record Message;

    public sealed class MarkerException : Exception;

    public sealed class UntypedActivity : IStateMachineActivity<TestSaga>
    {
        public void Accept(IStateMachineVisitor visitor) => throw Unexpected();
        public void Probe(ProbeContext context) => throw Unexpected();
        public Task ExecuteAsync(IBehaviorContext<TestSaga> context, IBehavior<TestSaga> next) => throw Unexpected();
        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context, IBehavior<TestSaga, T> next) where T : class => throw Unexpected();
        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context, IBehavior<TestSaga> next)
            where TException : Exception => throw Unexpected();
        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context, IBehavior<TestSaga, T> next)
            where T : class where TException : Exception => throw Unexpected();
    }

    public sealed class TypedActivity : IStateMachineActivity<TestSaga, Message>
    {
        public void Accept(IStateMachineVisitor visitor) => throw Unexpected();
        public void Probe(ProbeContext context) => throw Unexpected();
        public Task ExecuteAsync(IBehaviorContext<TestSaga, Message> context, IBehavior<TestSaga, Message> next) => throw Unexpected();
        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, Message, TException> context,
            IBehavior<TestSaga, Message> next) where TException : Exception => throw Unexpected();
    }

    sealed class Hierarchy(ConcurrentQueue<string> log)
    {
        public ViciOneServiceBusStateMachine<TestSaga>.StateMachineState ARoot { get; set; } = null!;
        public ViciOneServiceBusStateMachine<TestSaga>.StateMachineState AChild { get; set; } = null!;
        public ViciOneServiceBusStateMachine<TestSaga>.StateMachineState ASibling { get; set; } = null!;
        public ViciOneServiceBusStateMachine<TestSaga>.StateMachineState BRoot { get; set; } = null!;
        public ViciOneServiceBusStateMachine<TestSaga>.StateMachineState BMid { get; set; } = null!;
        public ViciOneServiceBusStateMachine<TestSaga>.StateMachineState BLeaf { get; set; } = null!;
        public ConcurrentQueue<string> Log { get; } = log;
        public ConcurrentQueue<(IState State, object Payload)> TypedLifecycleCalls { get; } = new();
        public string? FailureEventName { get; set; }
        public Exception? LifecycleFailure { get; set; }
    }

    sealed class LifecycleActivity(IState state, Hierarchy hierarchy) : IStateMachineActivity<TestSaga>
    {
        public void Accept(IStateMachineVisitor visitor) => throw Unexpected();
        public void Probe(ProbeContext context) => throw Unexpected();

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context, IBehavior<TestSaga> next)
        {
            Record(context.Event.Name);
            return next.ExecuteAsync(context);
        }

        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context, IBehavior<TestSaga, T> next)
            where T : class
        {
            Record(context.Event.Name);
            hierarchy.TypedLifecycleCalls.Enqueue((state, context.Message));
            return next.ExecuteAsync(context);
        }

        void Record(string eventName)
        {
            hierarchy.Log.Enqueue(eventName);
            if (hierarchy.FailureEventName == eventName && hierarchy.LifecycleFailure is not null)
                throw hierarchy.LifecycleFailure;
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context, IBehavior<TestSaga> next)
            where TException : Exception => throw Unexpected();

        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context, IBehavior<TestSaga, T> next)
            where T : class where TException : Exception => throw Unexpected();
    }

    sealed class ConcurrentLifecycleActivity(ConcurrentDictionary<int, ConcurrentQueue<string>> histories) : IStateMachineActivity<TestSaga>
    {
        public void Accept(IStateMachineVisitor visitor) => throw Unexpected();
        public void Probe(ProbeContext context) => throw Unexpected();

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context, IBehavior<TestSaga> next)
        {
            histories.GetOrAdd(((BehaviorContextProxy)(object)context).Id, _ => new()).Enqueue(context.Event.Name);
            return next.ExecuteAsync(context);
        }

        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context, IBehavior<TestSaga, T> next) where T : class
        {
            histories.GetOrAdd(((BehaviorContextProxy)(object)context).Id, _ => new()).Enqueue(context.Event.Name);
            return next.ExecuteAsync(context);
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context, IBehavior<TestSaga> next)
            where TException : Exception => throw Unexpected();
        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context, IBehavior<TestSaga, T> next)
            where T : class where TException : Exception => throw Unexpected();
    }

    sealed class RecordingAccessor(IState<TestSaga>? current, ConcurrentQueue<string> log) : IStateAccessor<TestSaga>
    {
        public IState<TestSaga>? Current { get; set; } = current;
        public Exception? GetFailure { get; set; }
        public Exception? SetFailure { get; set; }
        public IBehaviorContext<TestSaga>? GetContext { get; private set; }
        public IBehaviorContext<TestSaga>? SetContext { get; private set; }
        public CancellationToken GetToken { get; private set; }
        public CancellationToken SetToken { get; private set; }
        public int GetCount { get; private set; }
        public int SetCount { get; private set; }

        public Task<IState<TestSaga>?> GetAsync(IBehaviorContext<TestSaga> context, CancellationToken cancellationToken = default)
        {
            GetCount++;
            GetContext = context;
            GetToken = cancellationToken;
            log.Enqueue("get");
            if (GetFailure is not null)
                return Task.FromException<IState<TestSaga>?>(GetFailure);
            return Task.FromResult(Current);
        }

        public Task SetAsync(IBehaviorContext<TestSaga> context, IState<TestSaga> state, CancellationToken cancellationToken = default)
        {
            SetCount++;
            SetContext = context;
            SetToken = cancellationToken;
            log.Enqueue("set:" + state.Name);
            if (SetFailure is not null)
                return Task.FromException(SetFailure);
            Current = state;
            return Task.CompletedTask;
        }

        public Expression<Func<TestSaga, bool>> GetStateExpression(params IState[] states) => throw Unexpected();
        public void Probe(ProbeContext context) => throw Unexpected();
    }

    sealed class ContextMappedAccessor(ConcurrentDictionary<int, ConcurrentQueue<string>> histories) : IStateAccessor<TestSaga>
    {
        readonly ConcurrentDictionary<IBehaviorContext<TestSaga>, IState<TestSaga>> _states = new(ReferenceEqualityComparer.Instance);
        readonly ConcurrentDictionary<IBehaviorContext<TestSaga>, CancellationToken> _tokens = new(ReferenceEqualityComparer.Instance);

        public Task<IState<TestSaga>?> GetAsync(IBehaviorContext<TestSaga> context, CancellationToken cancellationToken = default)
        {
            int id = ((BehaviorContextProxy)(object)context).Id;
            histories.GetOrAdd(id, _ => new()).Enqueue("get");
            _tokens[context] = cancellationToken;
            return Task.FromResult<IState<TestSaga>?>(null);
        }

        public Task SetAsync(IBehaviorContext<TestSaga> context, IState<TestSaga> state, CancellationToken cancellationToken = default)
        {
            int id = ((BehaviorContextProxy)(object)context).Id;
            histories[id].Enqueue("set:" + state.Name);
            _states[context] = state;
            _tokens[context] = cancellationToken;
            return Task.CompletedTask;
        }

        public IState<TestSaga> GetState(IBehaviorContext<TestSaga> context) => _states[context];
        public CancellationToken GetToken(IBehaviorContext<TestSaga> context) => _tokens[context];
        public Expression<Func<TestSaga, bool>> GetStateExpression(params IState[] states) => throw Unexpected();
        public void Probe(ProbeContext context) => throw Unexpected();
    }

    sealed class RecordingBehavior(ConcurrentQueue<string> log) : IBehavior<TestSaga>
    {
        public int ExecuteCount { get; private set; }
        public object? LastExecuteContext { get; private set; }
        public object? LastFaultContext { get; private set; }
        public Task FaultTask { get; set; } = Task.CompletedTask;
        public void Accept(IStateMachineVisitor visitor) => throw Unexpected();
        public void Probe(ProbeContext context) => throw Unexpected();

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context)
        {
            ExecuteCount++;
            LastExecuteContext = context;
            log.Enqueue("next");
            return Task.CompletedTask;
        }

        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context) where T : class => throw Unexpected();
        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context)
            where T : class where TException : Exception => throw Unexpected();
        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context) where TException : Exception
        {
            LastFaultContext = context;
            return FaultTask;
        }
    }

    sealed class RecordingTypedBehavior(ConcurrentQueue<string>? log = null) : IBehavior<TestSaga, Message>
    {
        public int ExecuteCount { get; private set; }
        public IBehaviorContext<TestSaga, Message>? LastExecuteContext { get; private set; }
        public object? LastFaultContext { get; private set; }
        public Task FaultTask { get; set; } = Task.CompletedTask;
        public void Accept(IStateMachineVisitor visitor) => throw Unexpected();
        public void Probe(ProbeContext context) => throw Unexpected();
        public Task ExecuteAsync(IBehaviorContext<TestSaga, Message> context)
        {
            ExecuteCount++;
            LastExecuteContext = context;
            log?.Enqueue("typed-next");
            return Task.CompletedTask;
        }
        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, Message, TException> context)
            where TException : Exception
        {
            LastFaultContext = context;
            return FaultTask;
        }
    }

    sealed class ContextRecordingBehavior(ConcurrentDictionary<int, ConcurrentQueue<string>> histories) : IBehavior<TestSaga>
    {
        public IBehaviorContext<TestSaga>? LastContext { get; private set; }
        public void Accept(IStateMachineVisitor visitor) => throw Unexpected();
        public void Probe(ProbeContext context) => throw Unexpected();
        public Task ExecuteAsync(IBehaviorContext<TestSaga> context)
        {
            LastContext = context;
            histories[((BehaviorContextProxy)(object)context).Id].Enqueue("next");
            return Task.CompletedTask;
        }
        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context) where T : class => throw Unexpected();
        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context)
            where T : class where TException : Exception => throw Unexpected();
        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context)
            where TException : Exception => throw Unexpected();
    }

    sealed class NoopObserver : IEventObserver<TestSaga>
    {
        public Task PreExecuteAsync(IBehaviorContext<TestSaga> context) => Task.CompletedTask;
        public Task PreExecuteAsync<T>(IBehaviorContext<TestSaga, T> context) where T : class => Task.CompletedTask;
        public Task PostExecuteAsync(IBehaviorContext<TestSaga> context) => Task.CompletedTask;
        public Task PostExecuteAsync<T>(IBehaviorContext<TestSaga, T> context) where T : class => Task.CompletedTask;
        public Task ExecuteFaultAsync(IBehaviorContext<TestSaga> context, Exception exception) => Task.CompletedTask;
        public Task ExecuteFaultAsync<T>(IBehaviorContext<TestSaga, T> context, Exception exception) where T : class => Task.CompletedTask;
    }

    sealed record ConcurrentScenario(
        int Id,
        CancellationTokenSource Cancellation,
        IBehaviorContext<TestSaga> Context,
        ContextRecordingBehavior Next);

    public class StrictProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?>? Handler { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (Handler is null)
                throw Unexpected();

            return Handler(targetMethod ?? throw Unexpected(), args ?? []);
        }
    }

    public class BehaviorContextProxy : DispatchProxy
    {
        public CancellationToken Token { get; set; }
        public IEvent? Event { get; set; }
        public object? Message { get; set; }
        public int Id { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            MethodInfo method = targetMethod ?? throw Unexpected();
            object?[] arguments = args ?? [];
            return method.Name switch
            {
                "get_CancellationToken" => Token,
                "get_Event" => Event ?? throw Unexpected(),
                "get_Message" => Message ?? throw Unexpected(),
                "CreateProxy" => CreateChild(method.ReturnType, (IEvent)arguments[0]!, arguments.Length > 1 ? arguments[1] : null),
                _ => throw Unexpected()
            };
        }

        object CreateChild(Type contextType, IEvent @event, object? message)
        {
            object context = DispatchProxy.Create(contextType, typeof(BehaviorContextProxy));
            var proxy = (BehaviorContextProxy)context;
            proxy.Token = Token;
            proxy.Event = @event;
            proxy.Message = message;
            proxy.Id = Id;
            return context;
        }
    }
}
