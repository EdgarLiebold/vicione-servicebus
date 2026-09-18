using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineCoreActivityBindersDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-core-binder-constructor-null-contract")]
    public void Constructors_RejectEveryNullDependencyWithTheExactParameterName()
    {
        var @event = new TriggerEvent("Observed");
        var activity = new RecordingActivity("activity", []);
        var activities = new MutableEventActivities();
        IRetryPolicy retryPolicy = Retry.None;
        StateMachineCondition<TestSaga, Message> filter = _ => true;

        AssertParam("event", () => _ = new ExecuteActivityBinder<TestSaga>(null!, activity));
        AssertParam("activity", () => _ = new ExecuteActivityBinder<TestSaga>(@event, null!));
        AssertParam("event", () => _ = new IgnoreEventActivityBinder<TestSaga>(null!));
        AssertParam("event", () => _ = new IgnoreEventActivityBinder<TestSaga, Message>(null!, filter));
        AssertParam("filter", () => _ = new IgnoreEventActivityBinder<TestSaga, Message>(new MessageEvent<Message>("Message"), null!));
        AssertParam("event", () => _ = new CatchActivityBinder<TestSaga, MarkerException>(null!, activities));
        AssertParam("activities", () => _ = new CatchActivityBinder<TestSaga, MarkerException>(@event, null!));
        AssertParam("event", () => _ = new RetryActivityBinder<TestSaga>(null!, retryPolicy, activities));
        AssertParam("retryPolicy", () => _ = new RetryActivityBinder<TestSaga>(@event, null!, activities));
        AssertParam("retryActivities", () => _ = new RetryActivityBinder<TestSaga>(@event, retryPolicy, null!));
        AssertParam("event", () => _ = new RetryActivityBinder<TestSaga, Message>(null!, retryPolicy, activities));
        AssertParam("retryPolicy", () => _ = new RetryActivityBinder<TestSaga, Message>(@event, null!, activities));
        AssertParam("retryActivities", () => _ = new RetryActivityBinder<TestSaga, Message>(@event, retryPolicy, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-core-binder-method-null-contract")]
    public void EveryBinder_RejectsNullStateAndBuilderBeforeAnyDelegation()
    {
        var @event = new TriggerEvent("Observed");
        var typedEvent = new MessageEvent<Message>("Message");
        var activity = new RecordingActivity("activity", []);
        var activities = new MutableEventActivities();
        StateMachineCondition<TestSaga, Message> filter = _ => true;
        IActivityBinder<TestSaga>[] binders =
        [
            new ExecuteActivityBinder<TestSaga>(@event, activity),
            new IgnoreEventActivityBinder<TestSaga>(@event),
            new IgnoreEventActivityBinder<TestSaga, Message>(typedEvent, filter),
            new CatchActivityBinder<TestSaga, MarkerException>(@event, activities),
            new RetryActivityBinder<TestSaga>(@event, Retry.None, activities),
            new RetryActivityBinder<TestSaga, Message>(typedEvent, Retry.None, activities),
        ];

        foreach (IActivityBinder<TestSaga> binder in binders)
        {
            AssertParam("state", () => binder.IsStateTransitionEvent(null!));
            AssertParam("state", () => binder.Bind((IState<TestSaga>)null!));
            AssertParam("builder", () => binder.Bind((IBehaviorBuilder<TestSaga>)null!));
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-core-binder-transition-classification")]
    public void EveryBinder_ClassifiesAllFourLifecycleEventsAndRejectsAnOrdinaryEvent()
    {
        var state = new TransitionState();
        IEvent[] lifecycleEvents = [state.Enter, state.BeforeEnter, state.AfterLeave, state.Leave];

        foreach (IEvent lifecycleEvent in lifecycleEvents)
        {
            foreach (IActivityBinder<TestSaga> binder in CreateBinders(lifecycleEvent))
                Assert.True(binder.IsStateTransitionEvent(state));
        }

        foreach (IActivityBinder<TestSaga> binder in CreateBinders(new TriggerEvent("Ordinary")))
            Assert.False(binder.IsStateTransitionEvent(state));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-typed-ignore-transition-matrix")]
    public void TypedIgnoreBinder_ClassifiesEveryCompatibleLifecycleSlotAndRejectsOtherEvents()
    {
        var enter = new MessageEvent<Message>("Enter");
        var leave = new MessageEvent<Message>("Leave");
        var beforeEnter = new MessageEvent<IState>("BeforeEnter");
        var afterLeave = new MessageEvent<IState>("AfterLeave");
        var state = new TransitionState(enter, leave, beforeEnter, afterLeave);

        Assert.True(new IgnoreEventActivityBinder<TestSaga, Message>(enter, _ => true).IsStateTransitionEvent(state));
        Assert.True(new IgnoreEventActivityBinder<TestSaga, Message>(leave, _ => true).IsStateTransitionEvent(state));
        Assert.True(new IgnoreEventActivityBinder<TestSaga, IState>(beforeEnter, _ => true).IsStateTransitionEvent(state));
        Assert.True(new IgnoreEventActivityBinder<TestSaga, IState>(afterLeave, _ => true).IsStateTransitionEvent(state));
        Assert.False(new IgnoreEventActivityBinder<TestSaga, Message>(
            new MessageEvent<Message>("Ordinary"), _ => true).IsStateTransitionEvent(state));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-core-binder-public-api-contract")]
    public void PublicSurface_ExposesExactBinderContractsNullabilityConstraintsAndMembers()
    {
        AssertBinderInterfaceSurface();

        (Type Type, Type[] ConstructorParameters, string[] ConstructorParameterNames)[] contracts =
        [
            (typeof(ExecuteActivityBinder<TestSaga>),
                [typeof(IEvent), typeof(IStateMachineActivity<TestSaga>)], ["event", "activity"]),
            (typeof(IgnoreEventActivityBinder<TestSaga>), [typeof(IEvent)], ["event"]),
            (typeof(IgnoreEventActivityBinder<TestSaga, Message>),
                [typeof(IEvent<Message>), typeof(StateMachineCondition<TestSaga, Message>)], ["event", "filter"]),
            (typeof(CatchActivityBinder<TestSaga, MarkerException>),
                [typeof(IEvent), typeof(IEventActivities<TestSaga>)], ["event", "activities"]),
            (typeof(RetryActivityBinder<TestSaga>),
                [typeof(IEvent), typeof(IRetryPolicy), typeof(IEventActivities<TestSaga>)],
                ["event", "retryPolicy", "retryActivities"]),
            (typeof(RetryActivityBinder<TestSaga, Message>),
                [typeof(IEvent), typeof(IRetryPolicy), typeof(IEventActivities<TestSaga>)],
                ["event", "retryPolicy", "retryActivities"]),
        ];

        foreach ((Type type, Type[] constructorParameters, string[] constructorParameterNames) in contracts)
            AssertConcreteBinderSurface(type, constructorParameters, constructorParameterNames);

        AssertSagaConstraint(typeof(IActivityBinder<>), 0);
        AssertSagaConstraint(typeof(ExecuteActivityBinder<>), 0);
        AssertSagaConstraint(typeof(IgnoreEventActivityBinder<>), 0);
        AssertSagaConstraint(typeof(IgnoreEventActivityBinder<,>), 0);
        AssertSagaConstraint(typeof(CatchActivityBinder<,>), 0);
        AssertSagaConstraint(typeof(RetryActivityBinder<>), 0);
        AssertSagaConstraint(typeof(RetryActivityBinder<,>), 0);
        AssertReferenceConstraint(typeof(IgnoreEventActivityBinder<,>), 1);
        AssertReferenceConstraint(typeof(RetryActivityBinder<,>), 1);
        Assert.Equal([typeof(Exception)], typeof(CatchActivityBinder<,>).GetGenericArguments()[1].GetGenericParameterConstraints());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-execute-binder-identity-order-failure")]
    public void ExecuteBinder_ForwardsExactIdentityInOrderAndPreservesBindingFailureIdentity()
    {
        var trace = new List<string>();
        var @event = new TriggerEvent("Observed");
        var activity = new RecordingActivity("execute", trace);
        var binder = new ExecuteActivityBinder<TestSaga>(@event, activity);
        var state = new RecordingState(trace);
        var builder = new RecordingBuilder(trace);

        binder.Bind(state);
        binder.Bind(builder);

        Assert.Equal(["state:bind", "builder:add"], trace);
        Assert.Same(@event, state.BoundEvent);
        Assert.Same(activity, state.BoundActivity);
        Assert.Same(activity, Assert.Single(builder.Activities));

        var stateFailure = new MarkerException("state bind");
        state.BindFailure = stateFailure;
        Assert.Same(stateFailure, Assert.Throws<MarkerException>(() => binder.Bind(state)));

        var builderFailure = new MarkerException("builder add");
        builder.AddFailure = builderFailure;
        Assert.Same(builderFailure, Assert.Throws<MarkerException>(() => binder.Bind(builder)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-ignore-binder-forwarding")]
    public void IgnoreBinders_ForwardExactEventAndFilterWithoutCreatingRuntimeActivities()
    {
        var trace = new List<string>();
        var untypedEvent = new TriggerEvent("Untyped");
        var typedEvent = new MessageEvent<Message>("Typed");
        StateMachineCondition<TestSaga, Message> filter = _ => true;
        var state = new RecordingState(trace);
        var builder = new RecordingBuilder(trace);

        var untyped = new IgnoreEventActivityBinder<TestSaga>(untypedEvent);
        untyped.Bind(state);
        untyped.Bind(builder);

        Assert.Same(untypedEvent, state.IgnoredEvent);
        Assert.Null(state.IgnoreFilter);
        Assert.Empty(builder.Activities);

        var typed = new IgnoreEventActivityBinder<TestSaga, Message>(typedEvent, filter);
        typed.Bind(state);
        typed.Bind(builder);

        Assert.Same(typedEvent, typed.Event);
        Assert.Same(typedEvent, state.IgnoredEvent);
        Assert.Same(filter, state.IgnoreFilter);
        Assert.Empty(builder.Activities);
        Assert.Equal(["state:ignore", "state:ignore-message"], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-catch-exception-successful-callback-runtime-composition")]
    public void CatchExceptionBinders_SuccessfulCallbacksBuildExactNestedRuntimeForUntypedAndTypedEvents()
    {
        IStateMachine<TestSaga> machine = CreateContext<IStateMachine<TestSaga>>();

        var untypedTrace = new List<string>();
        var untypedEvent = new TriggerEvent("Untyped");
        var untyped = new CatchExceptionActivityBinder<TestSaga, MarkerException>(machine, untypedEvent);
        var untypedCallbackCalls = 0;
        IExceptionActivityBinder<TestSaga, MarkerException> untypedResult = untyped.Catch<InvalidOperationException>(binder =>
        {
            untypedCallbackCalls++;
            Assert.Same(machine, binder.StateMachine);
            Assert.Same(untypedEvent, binder.Event);
            Assert.Empty(binder.GetStateActivityBinders());
            return binder.Add(new RecordingActivity("untyped-nested", untypedTrace));
        });

        Assert.Equal(1, untypedCallbackCalls);
        Assert.Same(machine, untypedResult.StateMachine);
        Assert.Same(untypedEvent, untypedResult.Event);
        AssertCatchBranchRuntime(untypedResult, untypedEvent, untypedTrace, "untyped-nested:accept");

        var typedTrace = new List<string>();
        var typedEvent = new MessageEvent<Message>("Typed");
        var typed = new CatchExceptionActivityBinder<TestSaga, Message, MarkerException>(machine, typedEvent);
        var typedCallbackCalls = 0;
        IExceptionActivityBinder<TestSaga, Message, MarkerException> typedResult = typed.Catch<InvalidOperationException>(binder =>
        {
            typedCallbackCalls++;
            Assert.Same(machine, binder.StateMachine);
            Assert.Same(typedEvent, binder.Event);
            Assert.Empty(binder.GetStateActivityBinders());
            return binder.Add(new RecordingActivity("typed-nested", typedTrace));
        });

        Assert.Equal(1, typedCallbackCalls);
        Assert.Same(machine, typedResult.StateMachine);
        Assert.Same(typedEvent, typedResult.Event);
        AssertCatchBranchRuntime(typedResult, typedEvent, typedTrace, "typed-nested:accept");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-ignore-composite-binding-failure-identity")]
    public void IgnoreCatchAndRetryBinders_PreserveStateAndBuilderFailureIdentity()
    {
        var untypedEvent = new TriggerEvent("Untyped");
        var typedEvent = new MessageEvent<Message>("Typed");
        StateMachineCondition<TestSaga, Message> filter = _ => true;
        IActivityBinder<TestSaga>[] ignoreBinders =
        [
            new IgnoreEventActivityBinder<TestSaga>(untypedEvent),
            new IgnoreEventActivityBinder<TestSaga, Message>(typedEvent, filter),
        ];

        foreach (IActivityBinder<TestSaga> binder in ignoreBinders)
        {
            var stateFailure = new MarkerException("ignore state");
            var state = new RecordingState([]) { IgnoreFailure = stateFailure };
            Assert.Same(stateFailure, Assert.Throws<MarkerException>(() => binder.Bind(state)));

            var builder = new RecordingBuilder { AddFailure = new MarkerException("must not be observed") };
            binder.Bind(builder);
            Assert.Empty(builder.Activities);
        }

        foreach (CompositeBinderKind kind in Enum.GetValues<CompositeBinderKind>())
        {
            IActivityBinder<TestSaga> binder = CreateCompositeBinder(kind, untypedEvent, new MutableEventActivities());
            var stateFailure = new MarkerException($"{kind} state");
            var state = new RecordingState([]) { BindFailure = stateFailure };
            Assert.Same(stateFailure, Assert.Throws<MarkerException>(() => binder.Bind(state)));

            var builderFailure = new MarkerException($"{kind} builder");
            var builder = new RecordingBuilder { AddFailure = builderFailure };
            Assert.Same(builderFailure, Assert.Throws<MarkerException>(() => binder.Bind(builder)));
        }
    }

    [Theory]
    [InlineData(CompositeBinderKind.Catch)]
    [InlineData(CompositeBinderKind.Retry)]
    [InlineData(CompositeBinderKind.TypedRetry)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-composite-state-bind-runtime-identity")]
    public void CompositeBinders_StateBindingPublishesExactEventAndStableRuntimeIdentity(CompositeBinderKind kind)
    {
        var @event = new TriggerEvent("Observed");
        var activities = new MutableEventActivities();
        IActivityBinder<TestSaga> binder = CreateCompositeBinder(kind, @event, activities);
        var first = new RecordingState([]);
        var second = new RecordingState([]);
        var builder = new RecordingBuilder();

        binder.Bind(first);
        binder.Bind(second);
        binder.Bind(builder);

        Assert.Same(@event, first.BoundEvent);
        Assert.Same(@event, second.BoundEvent);
        Assert.Same(first.BoundActivity, second.BoundActivity);
        Assert.Same(first.BoundActivity, Assert.Single(builder.Activities));
        Assert.Equal(1, activities.GetCalls);
        Assert.Equal(ExpectedCompositeRuntimeType(kind), first.BoundActivity!.GetType());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-composite-binder-snapshot-identity-concurrency")]
    public async Task CompositeBinder_SnapshotsOrderedActivitiesAndPublishesOneRuntimeIdentityConcurrentlyAsync(bool retry)
    {
        var trace = new List<string>();
        var first = new RecordingActivity("first", trace);
        var second = new RecordingActivity("second", trace);
        var late = new RecordingActivity("late", trace);
        var activities = new MutableEventActivities(
            new RecordingBinder("first", first, trace),
            new RecordingBinder("second", second, trace));
        var @event = new TriggerEvent("Observed");
        IActivityBinder<TestSaga> binder = retry
            ? new RetryActivityBinder<TestSaga>(@event, Retry.None, activities)
            : new CatchActivityBinder<TestSaga, MarkerException>(@event, activities);

        activities.Add(new RecordingBinder("late", late, trace));
        Assert.Equal(["first:bind", "second:bind"], trace);
        Assert.Equal(1, activities.GetCalls);

        RecordingBuilder[] builders = Enumerable.Range(0, 32).Select(_ => new RecordingBuilder()).ToArray();
        await Task.WhenAll(builders.Select(builder => Task.Run(() => binder.Bind(builder))));

        IStateMachineActivity<TestSaga>[] runtimeActivities = builders.Select(builder => Assert.Single(builder.Activities)).ToArray();
        IStateMachineActivity<TestSaga> runtimeActivity = runtimeActivities[0];
        Assert.All(runtimeActivities, candidate => Assert.Same(runtimeActivity, candidate));
        if (retry)
            Assert.IsType<RetryActivity<TestSaga>>(runtimeActivity);
        else
            Assert.IsType<CatchFaultActivity<TestSaga, MarkerException>>(runtimeActivity);

        trace.Clear();
        runtimeActivity.Accept(new ContinuingVisitor());
        Assert.Equal(["first:accept", "second:accept"], trace);

        trace.Clear();
        runtimeActivity.Probe(new PassiveProbeContext());
        Assert.Equal(["first:probe", "second:probe"], trace);
        Assert.DoesNotContain(trace, call => call.StartsWith("late:", StringComparison.Ordinal));

        var visitorFailure = new MarkerException("visitor");
        first.Failure = visitorFailure;
        Assert.Same(visitorFailure, Assert.Throws<MarkerException>(() => runtimeActivity.Accept(new ContinuingVisitor())));

        first.Failure = null;
        var probeFailure = new MarkerException("probe");
        second.Failure = probeFailure;
        Assert.Same(probeFailure, Assert.Throws<MarkerException>(() => runtimeActivity.Probe(new PassiveProbeContext())));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-catch-fault-routing")]
    public async Task CatchBinder_MatchingFaultCompensatesThenContinuesAndNonmatchingFaultForwardsAsync(bool message)
    {
        var compensation = new RecordingActivity("compensation", []);
        var activities = new MutableEventActivities(new RecordingBinder("compensation", compensation, []));
        var binder = new CatchActivityBinder<TestSaga, MarkerException>(new TriggerEvent("Observed"), activities);
        var builder = new RecordingBuilder();
        binder.Bind(builder);
        IStateMachineActivity<TestSaga> runtime = Assert.Single(builder.Activities);

        var matchingNext = new RecordingBehavior();
        object matchingContext;
        if (message)
        {
            IBehaviorExceptionContext<TestSaga, Message, MarkerException> context =
                CreateContext<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
            matchingContext = context;
            await runtime.FaultedAsync(context, matchingNext.AsTyped<Message>());
        }
        else
        {
            IBehaviorExceptionContext<TestSaga, MarkerException> context =
                CreateContext<IBehaviorExceptionContext<TestSaga, MarkerException>>();
            matchingContext = context;
            await runtime.FaultedAsync(context, matchingNext);
        }

        Assert.Equal(1, compensation.FaultCalls);
        Assert.Same(matchingContext, Assert.Single(compensation.FaultContexts));
        Assert.Equal(1, matchingNext.ExecuteCalls);
        Assert.Equal(0, matchingNext.FaultCalls);
        Assert.Same(matchingContext, Assert.Single(matchingNext.ExecuteContexts));

        compensation.ResetRuntimeCalls();
        var nonmatchingNext = new RecordingBehavior();
        object nonmatchingContext;
        if (message)
        {
            IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException> context =
                CreateContext<IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException>>();
            nonmatchingContext = context;
            await runtime.FaultedAsync(context, nonmatchingNext.AsTyped<Message>());
        }
        else
        {
            IBehaviorExceptionContext<TestSaga, InvalidOperationException> context =
                CreateContext<IBehaviorExceptionContext<TestSaga, InvalidOperationException>>();
            nonmatchingContext = context;
            await runtime.FaultedAsync(context, nonmatchingNext);
        }

        Assert.Equal(0, compensation.FaultCalls);
        Assert.Empty(compensation.FaultContexts);
        Assert.Equal(0, nonmatchingNext.ExecuteCalls);
        Assert.Equal(1, nonmatchingNext.FaultCalls);
        Assert.Same(nonmatchingContext, Assert.Single(nonmatchingNext.FaultContexts));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-typed-retry-runtime-gating-snapshot-concurrency")]
    public async Task TypedRetryBinder_PublishesExactSnapshotIdentityConcurrentlyAndGatesByMessageTypeAsync()
    {
        var trace = new List<string>();
        var selected = new RecordingActivity("selected", trace);
        var late = new RecordingActivity("late", trace);
        var activities = new MutableEventActivities(new RecordingBinder("selected", selected, trace));
        var binder = new RetryActivityBinder<TestSaga, Message>(
            new MessageEvent<Message>("Observed"), Retry.None, activities);

        activities.Add(new RecordingBinder("late", late, trace));
        Assert.Equal(["selected:bind"], trace);
        Assert.Equal(1, activities.GetCalls);

        RecordingBuilder[] builders = Enumerable.Range(0, 32).Select(_ => new RecordingBuilder()).ToArray();
        await Task.WhenAll(builders.Select(builder => Task.Run(() => binder.Bind(builder))));

        IStateMachineActivity<TestSaga>[] roots = builders.Select(builder => Assert.Single(builder.Activities)).ToArray();
        var root = Assert.IsType<RetryActivity<TestSaga, Message>>(roots[0]);
        Assert.All(roots, candidate => Assert.Same(root, candidate));

        trace.Clear();
        root.Accept(new ContinuingVisitor());
        root.Probe(new PassiveProbeContext());
        Assert.Equal(["selected:accept", "selected:probe"], trace);
        Assert.DoesNotContain(trace, call => call.StartsWith("late:", StringComparison.Ordinal));

        var untypedNext = new RecordingBehavior();
        await Assert.ThrowsAsync<SagaStateMachineException>(() =>
            root.ExecuteAsync(CreateContext<IBehaviorContext<TestSaga>>(), untypedNext));
        Assert.Equal(0, selected.ExecuteCalls);
        Assert.Equal(0, untypedNext.ExecuteCalls);

        IBehaviorContext<TestSaga, Message> matchingContext = CreateContext<IBehaviorContext<TestSaga, Message>>();
        var matchingNext = new RecordingBehavior();
        await root.ExecuteAsync(matchingContext, matchingNext.AsTyped<Message>());
        Assert.Equal(1, selected.ExecuteCalls);
        Assert.Same(matchingContext, Assert.Single(selected.ExecuteContexts));
        Assert.Equal(1, matchingNext.ExecuteCalls);
        Assert.Same(matchingContext, Assert.Single(matchingNext.ExecuteContexts));

        IBehaviorContext<TestSaga, OtherMessage> otherContext = CreateContext<IBehaviorContext<TestSaga, OtherMessage>>();
        var otherNext = new RecordingBehavior();
        await root.ExecuteAsync(otherContext, otherNext.AsTyped<OtherMessage>());
        Assert.Equal(1, selected.ExecuteCalls);
        Assert.Single(selected.ExecuteContexts);
        Assert.Equal(1, otherNext.ExecuteCalls);
        Assert.Same(otherContext, Assert.Single(otherNext.ExecuteContexts));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-retry-policy-attempts-continuation-failure-identity")]
    public async Task RetryBinder_UsesConfiguredAttemptsThenContinuesAndPreservesTerminalFailureIdentityAsync(bool typed)
    {
        var @event = new TriggerEvent("Observed");
        var recoverableFailure = new MarkerException("recoverable");
        var recoverable = new AttemptActivity(2, recoverableFailure);
        IActivityBinder<TestSaga> recoveringBinder = CreateRetryBinder(typed, @event, Retry.Immediate(2), recoverable);
        var recoveringBuilder = new RecordingBuilder();
        recoveringBinder.Bind(recoveringBuilder);
        IStateMachineActivity<TestSaga> recoveringRuntime = Assert.Single(recoveringBuilder.Activities);
        var next = new RecordingBehavior();

        if (typed)
            await recoveringRuntime.ExecuteAsync(CreateContext<IBehaviorContext<TestSaga, Message>>(), next.AsTyped<Message>());
        else
            await recoveringRuntime.ExecuteAsync(CreateContext<IBehaviorContext<TestSaga>>(), next);

        Assert.Equal(3, recoverable.Attempts);
        Assert.Equal(1, next.ExecuteCalls);

        var terminalFailure = new MarkerException("terminal");
        var terminal = new AttemptActivity(int.MaxValue, terminalFailure);
        IActivityBinder<TestSaga> terminalBinder = CreateRetryBinder(typed, @event, Retry.Immediate(1), terminal);
        var terminalBuilder = new RecordingBuilder();
        terminalBinder.Bind(terminalBuilder);
        IStateMachineActivity<TestSaga> terminalRuntime = Assert.Single(terminalBuilder.Activities);

        MarkerException actual = typed
            ? await Assert.ThrowsAsync<MarkerException>(() =>
                terminalRuntime.ExecuteAsync(CreateContext<IBehaviorContext<TestSaga, Message>>(), new RecordingBehavior().AsTyped<Message>()))
            : await Assert.ThrowsAsync<MarkerException>(() =>
                terminalRuntime.ExecuteAsync(CreateContext<IBehaviorContext<TestSaga>>(), new RecordingBehavior()));

        Assert.Same(terminalFailure, actual);
        Assert.Equal(2, terminal.Attempts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-composite-binder-construction-failure-identity")]
    public void CompositeBinder_PreservesNestedBindingFailureIdentity(bool retry)
    {
        var failure = new MarkerException("nested bind");
        var activities = new MutableEventActivities(new ThrowingBinder(failure));
        var @event = new TriggerEvent("Observed");

        MarkerException actual = retry
            ? Assert.Throws<MarkerException>(() => new RetryActivityBinder<TestSaga>(@event, Retry.None, activities))
            : Assert.Throws<MarkerException>(() => new CatchActivityBinder<TestSaga, MarkerException>(@event, activities));

        Assert.Same(failure, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-218-typed-retry-construction-failure-identity")]
    public void TypedRetryBinder_PreservesNestedBindingFailureIdentity()
    {
        var failure = new MarkerException("typed retry nested bind");
        var activities = new MutableEventActivities(new ThrowingBinder(failure));

        MarkerException actual = Assert.Throws<MarkerException>(() =>
            new RetryActivityBinder<TestSaga, Message>(new MessageEvent<Message>("Observed"), Retry.None, activities));

        Assert.Same(failure, actual);
        Assert.Equal(1, activities.GetCalls);
    }

    static IActivityBinder<TestSaga>[] CreateBinders(IEvent @event)
    {
        var activities = new MutableEventActivities();
        var binders = new List<IActivityBinder<TestSaga>>
        {
            new ExecuteActivityBinder<TestSaga>(@event, new RecordingActivity("activity", [])),
            new IgnoreEventActivityBinder<TestSaga>(@event),
            new CatchActivityBinder<TestSaga, MarkerException>(@event, activities),
            new RetryActivityBinder<TestSaga>(@event, Retry.None, activities),
            new RetryActivityBinder<TestSaga, Message>(@event, Retry.None, activities),
        };

        if (@event is IEvent<IState> typedEvent)
            binders.Add(new IgnoreEventActivityBinder<TestSaga, IState>(typedEvent, _ => true));

        return [.. binders];
    }

    static IActivityBinder<TestSaga> CreateCompositeBinder(
        CompositeBinderKind kind,
        IEvent @event,
        IEventActivities<TestSaga> activities) => kind switch
        {
            CompositeBinderKind.Catch => new CatchActivityBinder<TestSaga, MarkerException>(@event, activities),
            CompositeBinderKind.Retry => new RetryActivityBinder<TestSaga>(@event, Retry.None, activities),
            CompositeBinderKind.TypedRetry => new RetryActivityBinder<TestSaga, Message>(@event, Retry.None, activities),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    static Type ExpectedCompositeRuntimeType(CompositeBinderKind kind) => kind switch
    {
        CompositeBinderKind.Catch => typeof(CatchFaultActivity<TestSaga, MarkerException>),
        CompositeBinderKind.Retry => typeof(RetryActivity<TestSaga>),
        CompositeBinderKind.TypedRetry => typeof(RetryActivity<TestSaga, Message>),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    static IActivityBinder<TestSaga> CreateRetryBinder(
        bool typed,
        IEvent @event,
        IRetryPolicy retryPolicy,
        IStateMachineActivity<TestSaga> activity)
    {
        var activities = new MutableEventActivities(new RecordingBinder("attempt", activity, []));
        return typed
            ? new RetryActivityBinder<TestSaga, Message>(@event, retryPolicy, activities)
            : new RetryActivityBinder<TestSaga>(@event, retryPolicy, activities);
    }

    static T CreateContext<T>()
        where T : class => DispatchProxy.Create<T, ContextProxy>();

    static void AssertCatchBranchRuntime(
        IEventActivities<TestSaga> activities,
        IEvent expectedEvent,
        List<string> trace,
        string expectedAcceptCall)
    {
        IActivityBinder<TestSaga> catchBinder = Assert.Single(activities.GetStateActivityBinders());
        Assert.Same(expectedEvent, catchBinder.Event);

        var builder = new RecordingBuilder();
        catchBinder.Bind(builder);
        var runtime = Assert.IsType<CatchFaultActivity<TestSaga, InvalidOperationException>>(
            Assert.Single(builder.Activities));

        runtime.Accept(new ContinuingVisitor());
        Assert.Equal([expectedAcceptCall], trace);
    }

    static void AssertBinderInterfaceSurface()
    {
        Type type = typeof(IActivityBinder<TestSaga>);
        Assert.True(type.IsPublic);
        Assert.True(type.IsInterface);
        Assert.Empty(type.GetConstructors());
        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
        Assert.Empty(type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));

        PropertyInfo @event = Assert.Single(type.GetProperties());
        Assert.Equal(nameof(IActivityBinder<TestSaga>.Event), @event.Name);
        Assert.Equal(typeof(IEvent), @event.PropertyType);
        Assert.NotNull(@event.GetMethod);
        Assert.Null(@event.SetMethod);
        Assert.Equal(NullabilityState.NotNull, new NullabilityInfoContext().Create(@event).ReadState);

        AssertBinderMethods(type);
    }

    static void AssertConcreteBinderSurface(
        Type type,
        Type[] constructorParameterTypes,
        string[] constructorParameterNames)
    {
        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.False(type.IsAbstract);
        Assert.False(type.IsSealed);
        Assert.Contains(typeof(IActivityBinder<TestSaga>), type.GetInterfaces());
        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
        Assert.Empty(type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));

        ConstructorInfo constructor = Assert.Single(type.GetConstructors());
        ParameterInfo[] constructorParameters = constructor.GetParameters();
        Assert.Equal(constructorParameterTypes, constructorParameters.Select(parameter => parameter.ParameterType));
        Assert.Equal(constructorParameterNames, constructorParameters.Select(parameter => parameter.Name));
        Assert.All(constructorParameters, AssertRequiredNotNullParameter);

        PropertyInfo @event = Assert.Single(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Equal(nameof(IActivityBinder<TestSaga>.Event), @event.Name);
        Assert.Equal(typeof(IEvent), @event.PropertyType);
        Assert.NotNull(@event.GetMethod);
        Assert.Null(@event.SetMethod);
        Assert.Equal(NullabilityState.NotNull, new NullabilityInfoContext().Create(@event).ReadState);

        AssertBinderMethods(type);
    }

    static void AssertBinderMethods(Type type)
    {
        MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .OrderBy(method => method.Name)
            .ThenBy(method => method.GetParameters()[0].ParameterType.Name)
            .ToArray();
        Assert.Equal(3, methods.Length);
        Assert.All(methods, method =>
        {
            Assert.False(method.IsGenericMethod);
            Assert.Single(method.GetParameters());
            AssertRequiredNotNullParameter(method.GetParameters()[0]);
        });

        MethodInfo transition = Assert.Single(methods, method => method.Name == nameof(IActivityBinder<TestSaga>.IsStateTransitionEvent));
        Assert.Equal(typeof(bool), transition.ReturnType);
        ParameterInfo transitionParameter = Assert.Single(transition.GetParameters());
        Assert.Equal(typeof(IState), transitionParameter.ParameterType);
        Assert.Equal("state", transitionParameter.Name);

        MethodInfo[] binds = methods.Where(method => method.Name == nameof(IActivityBinder<TestSaga>.Bind)).ToArray();
        Assert.Equal(2, binds.Length);
        Assert.Equal(
            [typeof(IBehaviorBuilder<TestSaga>), typeof(IState<TestSaga>)],
            binds.Select(method => Assert.Single(method.GetParameters()).ParameterType).OrderBy(parameterType => parameterType.Name));
        Assert.All(binds, method => Assert.Equal(typeof(void), method.ReturnType));
        Assert.Equal(
            ["builder", "state"],
            binds.Select(method => Assert.Single(method.GetParameters()).Name).OrderBy(parameterName => parameterName));
    }

    static void AssertRequiredNotNullParameter(ParameterInfo parameter)
    {
        Assert.False(parameter.IsOptional);
        Assert.False(parameter.HasDefaultValue);
        Assert.Equal(NullabilityState.NotNull, new NullabilityInfoContext().Create(parameter).ReadState);
    }

    static void AssertSagaConstraint(Type openGenericType, int parameterIndex)
    {
        Type parameter = openGenericType.GetGenericArguments()[parameterIndex];
        Assert.True(parameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        Assert.Contains(typeof(ISagaStateMachineInstance), parameter.GetGenericParameterConstraints());
    }

    static void AssertReferenceConstraint(Type openGenericType, int parameterIndex)
    {
        Type parameter = openGenericType.GetGenericArguments()[parameterIndex];
        Assert.True(parameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        Assert.Empty(parameter.GetGenericParameterConstraints());
    }

    static void AssertParam(string parameterName, Action action) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);

    public sealed class TestSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed record Message;

    public sealed record OtherMessage;

    public enum CompositeBinderKind
    {
        Catch,
        Retry,
        TypedRetry,
    }

    public sealed class MarkerException(string message) : Exception(message);

    sealed class MutableEventActivities(params IActivityBinder<TestSaga>[] binders) : IEventActivities<TestSaga>
    {
        readonly List<IActivityBinder<TestSaga>> _binders = [.. binders];

        public int GetCalls { get; private set; }

        public void Add(IActivityBinder<TestSaga> binder) => _binders.Add(binder);

        public IEnumerable<IActivityBinder<TestSaga>> GetStateActivityBinders()
        {
            GetCalls++;
            return _binders.ToArray();
        }
    }

    sealed class RecordingBinder(string name, IStateMachineActivity<TestSaga> activity, List<string> trace) : IActivityBinder<TestSaga>
    {
        public IEvent Event { get; } = new TriggerEvent(name);

        public bool IsStateTransitionEvent(IState state) => false;

        public void Bind(IState<TestSaga> state) => state.Bind(Event, activity);

        public void Bind(IBehaviorBuilder<TestSaga> builder)
        {
            trace.Add($"{name}:bind");
            builder.Add(activity);
        }
    }

    sealed class ThrowingBinder(Exception failure) : IActivityBinder<TestSaga>
    {
        public IEvent Event { get; } = new TriggerEvent("Throwing");
        public bool IsStateTransitionEvent(IState state) => false;
        public void Bind(IState<TestSaga> state) => throw failure;
        public void Bind(IBehaviorBuilder<TestSaga> builder) => throw failure;
    }

    sealed class RecordingBuilder(List<string>? trace = null) : IBehaviorBuilder<TestSaga>
    {
        public List<IStateMachineActivity<TestSaga>> Activities { get; } = [];
        public Exception? AddFailure { get; set; }

        public void Add(IStateMachineActivity<TestSaga> activity)
        {
            trace?.Add("builder:add");
            if (AddFailure is not null)
                throw AddFailure;
            Activities.Add(activity);
        }
    }

    sealed class RecordingState(List<string> trace) : IState<TestSaga>
    {
        public string Name => "Recording";
        public IEvent Enter { get; } = new TriggerEvent("Recording.Enter");
        public IEvent Leave { get; } = new TriggerEvent("Recording.Leave");
        public IEvent<IState> BeforeEnter { get; } = new MessageEvent<IState>("Recording.BeforeEnter");
        public IEvent<IState> AfterLeave { get; } = new MessageEvent<IState>("Recording.AfterLeave");
        public IEnumerable<IEvent> Events => [];
        public IEnumerable<IEvent> DeclaredEvents => [];
        public IState<TestSaga>? SuperState => null;
        public IEvent? BoundEvent { get; private set; }
        public IStateMachineActivity<TestSaga>? BoundActivity { get; private set; }
        public Exception? BindFailure { get; set; }
        public Exception? IgnoreFailure { get; set; }
        public IEvent? IgnoredEvent { get; private set; }
        public object? IgnoreFilter { get; private set; }

        public void Bind(IEvent @event, IStateMachineActivity<TestSaga> activity)
        {
            trace.Add("state:bind");
            if (BindFailure is not null)
                throw BindFailure;
            BoundEvent = @event;
            BoundActivity = activity;
        }

        public void Ignore(IEvent @event)
        {
            trace.Add("state:ignore");
            if (IgnoreFailure is not null)
                throw IgnoreFailure;
            IgnoredEvent = @event;
            IgnoreFilter = null;
        }

        public void Ignore<TMessage>(IEvent<TMessage> @event, StateMachineCondition<TestSaga, TMessage> filter)
            where TMessage : class
        {
            trace.Add("state:ignore-message");
            if (IgnoreFailure is not null)
                throw IgnoreFailure;
            IgnoredEvent = @event;
            IgnoreFilter = filter;
        }

        public Task RaiseAsync(IBehaviorContext<TestSaga> context, CancellationToken cancellationToken = default) => throw Unexpected();
        public Task RaiseAsync<TMessage>(IBehaviorContext<TestSaga, TMessage> context, CancellationToken cancellationToken = default)
            where TMessage : class => throw Unexpected();
        public void AddSubstate(IState<TestSaga> subState) => throw Unexpected();
        public bool HasState(IState<TestSaga> state) => throw Unexpected();
        public bool IsStateOf(IState<TestSaga> state) => throw Unexpected();
        public int CompareTo(IState? other) => string.Compare(Name, other?.Name, StringComparison.Ordinal);
        public void Accept(IStateMachineVisitor visitor) => throw Unexpected();
        public void Probe(ProbeContext context) => throw Unexpected();
    }

    sealed class TransitionState : IState
    {
        public TransitionState(
            IEvent? enter = null,
            IEvent? leave = null,
            IEvent<IState>? beforeEnter = null,
            IEvent<IState>? afterLeave = null)
        {
            Enter = enter ?? new TriggerEvent("Transition.Enter");
            Leave = leave ?? new TriggerEvent("Transition.Leave");
            BeforeEnter = beforeEnter ?? new MessageEvent<IState>("Transition.BeforeEnter");
            AfterLeave = afterLeave ?? new MessageEvent<IState>("Transition.AfterLeave");
        }

        public string Name => "Transition";
        public IEvent Enter { get; }
        public IEvent Leave { get; }
        public IEvent<IState> BeforeEnter { get; }
        public IEvent<IState> AfterLeave { get; }
        public int CompareTo(IState? other) => string.Compare(Name, other?.Name, StringComparison.Ordinal);
        public void Accept(IStateMachineVisitor visitor) => throw Unexpected();
        public void Probe(ProbeContext context) => throw Unexpected();
    }

    sealed class RecordingActivity(string name, List<string> trace) : IStateMachineActivity<TestSaga>
    {
        public Exception? Failure { get; set; }
        public int ExecuteCalls { get; private set; }
        public int FaultCalls { get; private set; }
        public List<object> ExecuteContexts { get; } = [];
        public List<object> FaultContexts { get; } = [];

        public void Accept(IStateMachineVisitor visitor)
        {
            trace.Add($"{name}:accept");
            ThrowIfConfigured();
        }

        public void Probe(ProbeContext context)
        {
            trace.Add($"{name}:probe");
            ThrowIfConfigured();
        }

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context, IBehavior<TestSaga> next)
        {
            ExecuteCalls++;
            ExecuteContexts.Add(context);
            return next.ExecuteAsync(context);
        }

        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context, IBehavior<TestSaga, T> next) where T : class
        {
            ExecuteCalls++;
            ExecuteContexts.Add(context);
            return next.ExecuteAsync(context);
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context, IBehavior<TestSaga> next)
            where TException : Exception
        {
            FaultCalls++;
            FaultContexts.Add(context);
            return next.FaultedAsync(context);
        }

        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context, IBehavior<TestSaga, T> next)
            where T : class where TException : Exception
        {
            FaultCalls++;
            FaultContexts.Add(context);
            return next.FaultedAsync(context);
        }

        public void ResetRuntimeCalls()
        {
            ExecuteCalls = 0;
            FaultCalls = 0;
            ExecuteContexts.Clear();
            FaultContexts.Clear();
        }

        void ThrowIfConfigured()
        {
            if (Failure is not null)
                throw Failure;
        }
    }

    sealed class AttemptActivity(int failuresBeforeSuccess, Exception failure) : IStateMachineActivity<TestSaga>
    {
        public int Attempts { get; private set; }

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);
        public void Probe(ProbeContext context) => context.CreateScope("attempt");
        public Task ExecuteAsync(IBehaviorContext<TestSaga> context, IBehavior<TestSaga> next) => ExecuteCore();
        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context, IBehavior<TestSaga, T> next) where T : class => ExecuteCore();
        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context, IBehavior<TestSaga> next)
            where TException : Exception => next.FaultedAsync(context);
        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context, IBehavior<TestSaga, T> next)
            where T : class where TException : Exception => next.FaultedAsync(context);

        Task ExecuteCore()
        {
            Attempts++;
            return Attempts <= failuresBeforeSuccess ? Task.FromException(failure) : Task.CompletedTask;
        }
    }

    sealed class RecordingBehavior : IBehavior<TestSaga>
    {
        public int ExecuteCalls { get; private set; }
        public int FaultCalls { get; private set; }
        public List<object> ExecuteContexts { get; } = [];
        public List<object> FaultContexts { get; } = [];

        public IBehavior<TestSaga, T> AsTyped<T>() where T : class => new TypedBehavior<T>(this);
        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);
        public void Probe(ProbeContext context) => context.CreateScope("next");
        public Task ExecuteAsync(IBehaviorContext<TestSaga> context)
        {
            ExecuteCalls++;
            ExecuteContexts.Add(context);
            return Task.CompletedTask;
        }
        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context) where T : class
        {
            ExecuteCalls++;
            ExecuteContexts.Add(context);
            return Task.CompletedTask;
        }
        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context)
            where T : class where TException : Exception
        {
            FaultCalls++;
            FaultContexts.Add(context);
            return Task.CompletedTask;
        }
        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context)
            where TException : Exception
        {
            FaultCalls++;
            FaultContexts.Add(context);
            return Task.CompletedTask;
        }

        sealed class TypedBehavior<T>(RecordingBehavior owner) : IBehavior<TestSaga, T> where T : class
        {
            public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);
            public void Probe(ProbeContext context) => context.CreateScope("next");
            public Task ExecuteAsync(IBehaviorContext<TestSaga, T> context)
            {
                owner.ExecuteCalls++;
                owner.ExecuteContexts.Add(context);
                return Task.CompletedTask;
            }
            public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, T, TException> context)
                where TException : Exception
            {
                owner.FaultCalls++;
                owner.FaultContexts.Add(context);
                return Task.CompletedTask;
            }
        }
    }

    sealed class ContinuingVisitor : IStateMachineVisitor
    {
        public void Visit(IState state, Action<IState> next) => next(state);
        public void Visit(IEvent @event, Action<IEvent> next) => next(@event);
        public void Visit<TMessage>(IEvent<TMessage> @event, Action<IEvent<TMessage>> next) where TMessage : class => next(@event);
        public void Visit(IStateMachineActivity activity) { }
        public void Visit(IStateMachineExceptionActivity activity, Action<IStateMachineExceptionActivity> next) => next(activity);
        public void Visit<T>(IBehavior<T> behavior) where T : class, ISagaStateMachineInstance { }
        public void Visit<T>(IBehavior<T> behavior, Action<IBehavior<T>> next) where T : class, ISagaStateMachineInstance => next(behavior);
        public void Visit<T, TMessage>(IBehavior<T, TMessage> behavior)
            where T : class, ISagaStateMachineInstance where TMessage : class
        { }
        public void Visit<T, TMessage>(IBehavior<T, TMessage> behavior, Action<IBehavior<T, TMessage>> next)
            where T : class, ISagaStateMachineInstance where TMessage : class => next(behavior);
        public void Visit(IStateMachineActivity activity, Action<IStateMachineActivity> next) => next(activity);
    }

    sealed class PassiveProbeContext : ProbeContext
    {
        public CancellationToken CancellationToken => default;
        public void Add(string key, string? value) { }
        public void Add(string key, object? value) { }
        public void Set(object values) { }
        public void Set(IEnumerable<KeyValuePair<string, object?>> values) { }
        public ProbeContext CreateScope(string key) => this;
    }

    public class ContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_CancellationToken")
                return CancellationToken.None;
            throw Unexpected();
        }
    }

    static Exception Unexpected() => new Xunit.Sdk.XunitException("An unrelated member was called.");
}
