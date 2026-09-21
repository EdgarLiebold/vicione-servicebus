using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineEventExceptionActivityBindersDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "binder-constructor-null-boundaries")]
    public void Constructors_RejectNullOwnersEventsCollectionsAndNullCollectionEntries()
    {
        var machine = new BinderMachine();
        IStateMachine<BinderSaga> stateMachine = machine;
        IActivityBinder<BinderSaga>[] noActivities = [];

        AssertArgument("machine", () => new TriggerEventActivityBinder<BinderSaga>(null!, machine.Trigger));
        AssertArgument("event", () => new TriggerEventActivityBinder<BinderSaga>(stateMachine, null!));
        AssertArgument("activities", () => new TriggerEventActivityBinder<BinderSaga>(stateMachine, machine.Trigger,
            (IActivityBinder<BinderSaga>[])null!));
        AssertCollectionEntry(() => new TriggerEventActivityBinder<BinderSaga>(stateMachine, machine.Trigger, [null!]));
        AssertArgument("machine", () => new TriggerEventActivityBinder<BinderSaga>(null!, machine.Trigger,
            (StateMachineCondition<BinderSaga>?)null, noActivities));
        AssertArgument("event", () => new TriggerEventActivityBinder<BinderSaga>(stateMachine, null!,
            (StateMachineCondition<BinderSaga>?)null, noActivities));
        AssertArgument("activities", () => new TriggerEventActivityBinder<BinderSaga>(stateMachine, machine.Trigger,
            (StateMachineCondition<BinderSaga>?)null, (IActivityBinder<BinderSaga>[])null!));
        AssertCollectionEntry(() => new TriggerEventActivityBinder<BinderSaga>(stateMachine, machine.Trigger,
            (StateMachineCondition<BinderSaga>?)null, [null!]));

        AssertArgument("machine", () => new DataEventActivityBinder<BinderSaga, BinderData>(null!, machine.Data));
        AssertArgument("event", () => new DataEventActivityBinder<BinderSaga, BinderData>(stateMachine, null!));
        AssertArgument("activities", () => new DataEventActivityBinder<BinderSaga, BinderData>(stateMachine, machine.Data,
            (IActivityBinder<BinderSaga>[])null!));
        AssertCollectionEntry(() => new DataEventActivityBinder<BinderSaga, BinderData>(stateMachine, machine.Data, [null!]));
        AssertArgument("machine", () => new DataEventActivityBinder<BinderSaga, BinderData>(null!, machine.Data,
            (StateMachineCondition<BinderSaga, BinderData>?)null, noActivities));
        AssertArgument("event", () => new DataEventActivityBinder<BinderSaga, BinderData>(stateMachine, null!,
            (StateMachineCondition<BinderSaga, BinderData>?)null, noActivities));
        AssertArgument("activities", () => new DataEventActivityBinder<BinderSaga, BinderData>(stateMachine, machine.Data,
            (StateMachineCondition<BinderSaga, BinderData>?)null, (IActivityBinder<BinderSaga>[])null!));
        AssertCollectionEntry(() => new DataEventActivityBinder<BinderSaga, BinderData>(stateMachine, machine.Data,
            (StateMachineCondition<BinderSaga, BinderData>?)null, [null!]));

        AssertArgument("machine", () => new CatchExceptionActivityBinder<BinderSaga, MarkerException>(null!, machine.Trigger));
        AssertArgument("event", () => new CatchExceptionActivityBinder<BinderSaga, MarkerException>(stateMachine, null!));
        AssertArgument("machine", () =>
            new CatchExceptionActivityBinder<BinderSaga, BinderData, MarkerException>(null!, machine.Data));
        AssertArgument("event", () =>
            new CatchExceptionActivityBinder<BinderSaga, BinderData, MarkerException>(stateMachine, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "binder-public-nullability")]
    public void PublicConstructors_DeclareRequiredOwnersEventsCollectionsAndOptionalFilters()
    {
        AssertConstructorNullability(
            typeof(TriggerEventActivityBinder<BinderSaga>),
            [typeof(IStateMachine<BinderSaga>), typeof(IEvent), typeof(IActivityBinder<BinderSaga>[])],
            [NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.NotNull]);
        AssertConstructorNullability(
            typeof(TriggerEventActivityBinder<BinderSaga>),
            [
                typeof(IStateMachine<BinderSaga>),
                typeof(IEvent),
                typeof(StateMachineCondition<BinderSaga>),
                typeof(IActivityBinder<BinderSaga>[]),
            ],
            [NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.NotNull]);
        AssertConstructorNullability(
            typeof(DataEventActivityBinder<BinderSaga, BinderData>),
            [typeof(IStateMachine<BinderSaga>), typeof(IEvent<BinderData>), typeof(IActivityBinder<BinderSaga>[])],
            [NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.NotNull]);
        AssertConstructorNullability(
            typeof(DataEventActivityBinder<BinderSaga, BinderData>),
            [
                typeof(IStateMachine<BinderSaga>),
                typeof(IEvent<BinderData>),
                typeof(StateMachineCondition<BinderSaga, BinderData>),
                typeof(IActivityBinder<BinderSaga>[]),
            ],
            [NullabilityState.NotNull, NullabilityState.NotNull, NullabilityState.Nullable, NullabilityState.NotNull]);
        AssertConstructorNullability(
            typeof(CatchExceptionActivityBinder<BinderSaga, MarkerException>),
            [typeof(IStateMachine<BinderSaga>), typeof(IEvent)],
            [NullabilityState.NotNull, NullabilityState.NotNull]);
        AssertConstructorNullability(
            typeof(CatchExceptionActivityBinder<BinderSaga, BinderData, MarkerException>),
            [typeof(IStateMachine<BinderSaga>), typeof(IEvent<BinderData>)],
            [NullabilityState.NotNull, NullabilityState.NotNull]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "binder-method-null-boundaries")]
    public void Methods_RejectNullActivitiesDelegatesAndConditionsBeforeInvokingCallbacks()
    {
        var machine = new BinderMachine();
        IStateMachine<BinderSaga> stateMachine = machine;
        IEventActivityBinder<BinderSaga> trigger = new TriggerEventActivityBinder<BinderSaga>(stateMachine, machine.Trigger);
        IEventActivityBinder<BinderSaga, BinderData> data =
            new DataEventActivityBinder<BinderSaga, BinderData>(stateMachine, machine.Data);
        IExceptionActivityBinder<BinderSaga, MarkerException> caught =
            new CatchExceptionActivityBinder<BinderSaga, MarkerException>(stateMachine, machine.Trigger);
        IExceptionActivityBinder<BinderSaga, BinderData, MarkerException> caughtData =
            new CatchExceptionActivityBinder<BinderSaga, BinderData, MarkerException>(stateMachine, machine.Data);
        var callbackCalls = 0;

        AssertArgument("activity", () => trigger.Add(null!));
        AssertArgument("activityCallback", () => trigger.Catch<MarkerException>(null!));
        AssertArgument("configure", () => trigger.Retry(null!, binder => binder));
        AssertArgument("activityCallback", () => trigger.Retry(_ => callbackCalls++, null!));
        AssertArgument("condition", () => trigger.If(null!, binder => binder));
        AssertArgument("activityCallback", () => trigger.If(_ => true, null!));
        AssertArgument("condition", () => trigger.IfAwaited(null!, binder => binder));
        AssertArgument("activityCallback", () => trigger.IfAwaited(_ => Task.FromResult(true), null!));
        AssertArgument("thenActivityCallback", () => trigger.IfElse(_ => true, null!, binder => binder));
        AssertArgument("elseActivityCallback", () => trigger.IfElse(_ => true, binder =>
        {
            callbackCalls++;
            return binder;
        }, null!));
        AssertArgument("thenActivityCallback", () => trigger.IfElseAwaited(_ => Task.FromResult(true), null!, binder => binder));
        AssertArgument("elseActivityCallback", () => trigger.IfElseAwaited(_ => Task.FromResult(true), binder =>
        {
            callbackCalls++;
            return binder;
        }, null!));
        Assert.Equal(0, callbackCalls);

        AssertArgument("activity", () => data.Add((IStateMachineActivity<BinderSaga>)null!));
        AssertArgument("activity", () => data.Add((IStateMachineActivity<BinderSaga, BinderData>)null!));
        AssertArgument("activityCallback", () => data.Catch<MarkerException>(null!));
        AssertArgument("configure", () => data.Retry(null!, binder => binder));
        AssertArgument("activityCallback", () => data.Retry(_ => callbackCalls++, null!));
        AssertArgument("condition", () => data.If(null!, binder => binder));
        AssertArgument("activityCallback", () => data.If(_ => true, null!));
        AssertArgument("condition", () => data.IfAwaited(null!, binder => binder));
        AssertArgument("activityCallback", () => data.IfAwaited(_ => Task.FromResult(true), null!));
        AssertArgument("thenActivityCallback", () => data.IfElse(_ => true, null!, binder => binder));
        AssertArgument("elseActivityCallback", () => data.IfElse(_ => true, binder =>
        {
            callbackCalls++;
            return binder;
        }, null!));
        AssertArgument("thenActivityCallback", () => data.IfElseAwaited(_ => Task.FromResult(true), null!, binder => binder));
        AssertArgument("elseActivityCallback", () => data.IfElseAwaited(_ => Task.FromResult(true), binder =>
        {
            callbackCalls++;
            return binder;
        }, null!));
        Assert.Equal(0, callbackCalls);

        AssertArgument("activity", () => caught.Add(null!));
        AssertArgument("activityCallback", () => caught.Catch<MarkerException>(null!));
        AssertArgument("condition", () => caught.If(null!, binder => binder));
        AssertArgument("activityCallback", () => caught.If(_ => true, null!));
        AssertArgument("condition", () => caught.IfAwaited(null!, binder => binder));
        AssertArgument("activityCallback", () => caught.IfAwaited(_ => Task.FromResult(true), null!));
        AssertArgument("thenActivityCallback", () => caught.IfElse(_ => true, null!, binder => binder));
        AssertArgument("elseActivityCallback", () => caught.IfElse(_ => true, binder =>
        {
            callbackCalls++;
            return binder;
        }, null!));
        AssertArgument("thenActivityCallback", () => caught.IfElseAwaited(_ => Task.FromResult(true), null!, binder => binder));
        AssertArgument("elseActivityCallback", () => caught.IfElseAwaited(_ => Task.FromResult(true), binder =>
        {
            callbackCalls++;
            return binder;
        }, null!));
        Assert.Equal(0, callbackCalls);

        AssertArgument("activity", () => caughtData.Add((IStateMachineActivity<BinderSaga>)null!));
        AssertArgument("activity", () => caughtData.Add((IStateMachineActivity<BinderSaga, BinderData>)null!));
        AssertArgument("activityCallback", () => caughtData.Catch<MarkerException>(null!));
        AssertArgument("condition", () => caughtData.If(null!, binder => binder));
        AssertArgument("activityCallback", () => caughtData.If(_ => true, null!));
        AssertArgument("condition", () => caughtData.IfAwaited(null!, binder => binder));
        AssertArgument("activityCallback", () => caughtData.IfAwaited(_ => Task.FromResult(true), null!));
        AssertArgument("thenActivityCallback", () => caughtData.IfElse(_ => true, null!, binder => binder));
        AssertArgument("elseActivityCallback", () => caughtData.IfElse(_ => true, binder =>
        {
            callbackCalls++;
            return binder;
        }, null!));
        AssertArgument("thenActivityCallback", () => caughtData.IfElseAwaited(_ => Task.FromResult(true), null!, binder => binder));
        AssertArgument("elseActivityCallback", () => caughtData.IfElseAwaited(_ => Task.FromResult(true), binder =>
        {
            callbackCalls++;
            return binder;
        }, null!));
        Assert.Equal(0, callbackCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "binder-null-callback-results-and-failure-identity")]
    public void ConfigurationCallbacks_RejectNullResultsAndPreserveThrownFailureIdentity()
    {
        var machine = new BinderMachine();
        IStateMachine<BinderSaga> stateMachine = machine;
        IEventActivityBinder<BinderSaga> trigger = new TriggerEventActivityBinder<BinderSaga>(stateMachine, machine.Trigger);
        IEventActivityBinder<BinderSaga, BinderData> data =
            new DataEventActivityBinder<BinderSaga, BinderData>(stateMachine, machine.Data);
        IExceptionActivityBinder<BinderSaga, MarkerException> caught =
            new CatchExceptionActivityBinder<BinderSaga, MarkerException>(stateMachine, machine.Trigger);
        IExceptionActivityBinder<BinderSaga, BinderData, MarkerException> caughtData =
            new CatchExceptionActivityBinder<BinderSaga, BinderData, MarkerException>(stateMachine, machine.Data);

        AssertNullResult(() => trigger.Catch<MarkerException>(_ => null!), "exception");
        AssertNullResult(() => trigger.Retry(retry => retry.Immediate(1), _ => null!), "event");
        AssertNullResult(() => trigger.If(_ => true, _ => null!), "event");
        AssertNullResult(() => trigger.IfAwaited(_ => Task.FromResult(true), _ => null!), "event");
        AssertNullResult(() => trigger.IfElse(_ => true, binder => binder, _ => null!), "event");
        AssertNullResult(() => trigger.IfElseAwaited(_ => Task.FromResult(true), _ => null!, binder => binder), "event");
        AssertNullResult(() => trigger.IfElseAwaited(_ => Task.FromResult(true), binder => binder, _ => null!), "event");

        AssertNullResult(() => data.Catch<MarkerException>(_ => null!), "exception");
        AssertNullResult(() => data.Retry(retry => retry.Immediate(1), _ => null!), "event");
        AssertNullResult(() => data.If(_ => true, _ => null!), "event");
        AssertNullResult(() => data.IfAwaited(_ => Task.FromResult(true), _ => null!), "event");
        AssertNullResult(() => data.IfElse(_ => true, binder => binder, _ => null!), "event");
        AssertNullResult(() => data.IfElseAwaited(_ => Task.FromResult(true), _ => null!, binder => binder), "event");
        AssertNullResult(() => data.IfElseAwaited(_ => Task.FromResult(true), binder => binder, _ => null!), "event");

        AssertNullResult(() => caught.Catch<MarkerException>(_ => null!), "exception");
        AssertNullResult(() => caught.If(_ => true, _ => null!), "exception");
        AssertNullResult(() => caught.IfAwaited(_ => Task.FromResult(true), _ => null!), "exception");
        AssertNullResult(() => caught.IfElse(_ => true, binder => binder, _ => null!), "exception");
        AssertNullResult(() => caught.IfElseAwaited(_ => Task.FromResult(true), _ => null!, binder => binder), "exception");
        AssertNullResult(() => caught.IfElseAwaited(_ => Task.FromResult(true), binder => binder, _ => null!), "exception");

        AssertNullResult(() => caughtData.Catch<MarkerException>(_ => null!), "exception");
        AssertNullResult(() => caughtData.If(_ => true, _ => null!), "exception");
        AssertNullResult(() => caughtData.IfAwaited(_ => Task.FromResult(true), _ => null!), "exception");
        AssertNullResult(() => caughtData.IfElse(_ => true, binder => binder, _ => null!), "exception");
        AssertNullResult(() => caughtData.IfElseAwaited(_ => Task.FromResult(true), _ => null!, binder => binder), "exception");
        AssertNullResult(() => caughtData.IfElseAwaited(_ => Task.FromResult(true), binder => binder, _ => null!), "exception");

        var failure = new MarkerException("configuration callback failed");
        Assert.Same(failure, Assert.Throws<MarkerException>(() => trigger.If(_ => true, _ => throw failure)));
        Assert.Same(failure, Assert.Throws<MarkerException>(() => data.Catch<MarkerException>(_ => throw failure)));
        Assert.Same(failure, Assert.Throws<MarkerException>(() => caught.If(_ => true, _ => throw failure)));
        Assert.Same(failure, Assert.Throws<MarkerException>(() => caughtData.Catch<MarkerException>(_ => throw failure)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "binder-snapshot-and-concurrent-enumeration")]
    public async Task ActivityCollections_AreConstructionAndEnumerationSnapshotsDuringConcurrentReadsAsync()
    {
        var machine = new BinderMachine();
        var first = new RecordingActivityBinder(machine.Trigger, new SequenceActivity("first", []));
        var replacement = new RecordingActivityBinder(machine.Trigger, new SequenceActivity("replacement", []));
        IActivityBinder<BinderSaga>[] triggerInput = [first];
        var trigger = new TriggerEventActivityBinder<BinderSaga>(machine, machine.Trigger, triggerInput);
        triggerInput[0] = replacement;
        AssertSnapshot(trigger, first, replacement);

        var firstData = new RecordingActivityBinder(machine.Data, new SequenceActivity("data", []));
        var replacementData = new RecordingActivityBinder(machine.Data, new SequenceActivity("replacement-data", []));
        IActivityBinder<BinderSaga>[] dataInput = [firstData];
        var data = new DataEventActivityBinder<BinderSaga, BinderData>(machine, machine.Data, dataInput);
        dataInput[0] = replacementData;
        AssertSnapshot(data, firstData, replacementData);

        IExceptionActivityBinder<BinderSaga, MarkerException> caught =
            new CatchExceptionActivityBinder<BinderSaga, MarkerException>(machine, machine.Trigger)
                .Add(new SequenceActivity("catch", []));
        IActivityBinder<BinderSaga> caughtActivity = Assert.Single(caught.GetStateActivityBinders());
        AssertSnapshot(caught, caughtActivity, replacement);

        IExceptionActivityBinder<BinderSaga, BinderData, MarkerException> caughtData =
            new CatchExceptionActivityBinder<BinderSaga, BinderData, MarkerException>(machine, machine.Data)
                .Add(new TypedIdentityActivity(Task.CompletedTask, Task.CompletedTask));
        IActivityBinder<BinderSaga> caughtDataActivity = Assert.Single(caughtData.GetStateActivityBinders());
        AssertSnapshot(caughtData, caughtDataActivity, replacementData);

        await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(() =>
        {
            Assert.Same(first, Assert.Single(trigger.GetStateActivityBinders()));
            Assert.Same(firstData, Assert.Single(data.GetStateActivityBinders()));
            Assert.Same(caughtActivity, Assert.Single(caught.GetStateActivityBinders()));
            Assert.Same(caughtDataActivity, Assert.Single(caughtData.GetStateActivityBinders()));
        })));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "binder-event-classification-owner-identity-and-order")]
    public async Task Binders_PreserveOwnerEventClassificationAndExecutionOrderAsync()
    {
        var machine = new BinderMachine();
        var untypedOrder = new List<string>();
        IEventActivityBinder<BinderSaga> trigger = new TriggerEventActivityBinder<BinderSaga>(
            machine,
            machine.Trigger,
            new RecordingActivityBinder(machine.Trigger, new SequenceActivity("initial-1", untypedOrder)),
            new RecordingActivityBinder(machine.Trigger, new SequenceActivity("initial-2", untypedOrder)));
        trigger = trigger.Add(new SequenceActivity("added", untypedOrder));
        trigger = trigger.IfElse(
            _ => true,
            binder => binder.Add(new SequenceActivity("then", untypedOrder)),
            binder => binder.Add(new SequenceActivity("else", untypedOrder)));

        Assert.Same(machine, trigger.StateMachine);
        Assert.Same(machine.Trigger, trigger.Event);
        Assert.All(trigger.GetStateActivityBinders(), binder =>
        {
            Assert.Same(machine.Trigger, binder.Event);
            Assert.False(binder.IsStateTransitionEvent(machine.Initial));
        });
        await BuildBehavior(trigger).ExecuteAsync(CreateProxy<IBehaviorContext<BinderSaga>>());
        Assert.Equal(["initial-1", "initial-2", "added", "then"], untypedOrder);

        var typedOrder = new List<string>();
        IEventActivityBinder<BinderSaga, BinderData> data = new DataEventActivityBinder<BinderSaga, BinderData>(
            machine,
            machine.Data,
            new RecordingActivityBinder(machine.Data, new SequenceActivity("data-initial", typedOrder)));
        data = data.Add(new SequenceActivity("data-untyped", typedOrder));
        data = data.Add(new TypedSequenceActivity("data-typed", typedOrder));

        Assert.Same(machine, data.StateMachine);
        Assert.Same(machine.Data, data.Event);
        Assert.All(data.GetStateActivityBinders(), binder =>
        {
            Assert.Same(machine.Data, binder.Event);
            Assert.False(binder.IsStateTransitionEvent(machine.Initial));
        });
        await BuildBehavior(data).ExecuteAsync(CreateProxy<IBehaviorContext<BinderSaga, BinderData>>());
        Assert.Equal(["data-initial", "data-untyped", "data-typed"], typedOrder);

        IEventActivityBinder<BinderSaga> enter = new TriggerEventActivityBinder<BinderSaga>(machine, machine.Initial.Enter);
        IActivityBinder<BinderSaga> enterActivity = Assert.Single(enter.Add(new SequenceActivity("enter", [])).GetStateActivityBinders());
        Assert.True(enterActivity.IsStateTransitionEvent(machine.Initial));

        IEventActivityBinder<BinderSaga, IState> beforeEnter =
            new DataEventActivityBinder<BinderSaga, IState>(machine, machine.Initial.BeforeEnter);
        IActivityBinder<BinderSaga> beforeEnterActivity =
            Assert.Single(beforeEnter.Add(new TypedStateActivity()).GetStateActivityBinders());
        Assert.True(beforeEnterActivity.IsStateTransitionEvent(machine.Initial));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "data-binder-context-message-task-and-cancellation-identity")]
    public async Task DataBinderAdapters_PreserveContextMessageTaskAndCancellationIdentityAsync()
    {
        var machine = new BinderMachine();
        var message = new BinderData("payload");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Task canceledTask = Task.FromCanceled(cancellation.Token);
        var activity = new TypedIdentityActivity(canceledTask, Task.CompletedTask);
        IEventActivityBinder<BinderSaga, BinderData> binder =
            new DataEventActivityBinder<BinderSaga, BinderData>(machine, machine.Data);
        binder = binder.Add(activity);
        IStateMachineActivity<BinderSaga> adapter = CaptureActivity(Assert.Single(binder.GetStateActivityBinders()));
        IBehaviorContext<BinderSaga, BinderData> context = CreateContext(message, TestContext.Current.CancellationToken);
        IBehavior<BinderSaga, BinderData> next = CreateProxy<IBehavior<BinderSaga, BinderData>>();

        Task returned = adapter.ExecuteAsync(context, next);

        Assert.Same(canceledTask, returned);
        Assert.Same(context, activity.ExecuteContext);
        Assert.Same(message, activity.Message);
        Assert.Same(next, activity.ExecuteNext);
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => returned);
        Assert.Equal(cancellation.Token, canceled.CancellationToken);

        var untyped = new UntypedIdentityActivity(canceledTask, Task.CompletedTask);
        binder = new DataEventActivityBinder<BinderSaga, BinderData>(machine, machine.Data);
        binder = binder.Add(untyped);
        adapter = CaptureActivity(Assert.Single(binder.GetStateActivityBinders()));

        returned = adapter.ExecuteAsync(context, next);

        Assert.Same(canceledTask, returned);
        Assert.Same(context, untyped.ExecuteContext);
        Assert.Same(next, untyped.ExecuteNext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RECOVERY", "data-exception-binder-context-message-failure-task-identity")]
    public async Task DataExceptionBinderAdapter_PreservesContextMessageExceptionFailureAndTaskIdentityAsync()
    {
        var machine = new BinderMachine();
        var message = new BinderData("fault-payload");
        var failure = new MarkerException("original failure");
        var activityFailure = new MarkerException("activity failure");
        Task faultTask = Task.FromException(activityFailure);
        var activity = new TypedIdentityActivity(Task.CompletedTask, faultTask);
        IExceptionActivityBinder<BinderSaga, BinderData, MarkerException> binder =
            new CatchExceptionActivityBinder<BinderSaga, BinderData, MarkerException>(machine, machine.Data).Add(activity);
        IStateMachineActivity<BinderSaga> adapter = CaptureActivity(Assert.Single(binder.GetStateActivityBinders()));
        IBehaviorExceptionContext<BinderSaga, BinderData, MarkerException> context =
            CreateExceptionContext(message, failure, TestContext.Current.CancellationToken);
        IBehavior<BinderSaga, BinderData> next = CreateProxy<IBehavior<BinderSaga, BinderData>>();

        Task returned = adapter.FaultedAsync(context, next);

        Assert.Same(faultTask, returned);
        Assert.Same(context, activity.FaultContext);
        Assert.Same(message, activity.FaultMessage);
        Assert.Same(failure, activity.Exception);
        Assert.Same(next, activity.FaultNext);
        Assert.Same(activityFailure, await Assert.ThrowsAsync<MarkerException>(() => returned));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RECOVERY", "exception-condition-context-failure-and-cancellation-identity")]
    public async Task ExceptionConditions_PreserveContextFailureAndCancellationIdentityAsync()
    {
        var machine = new BinderMachine();
        var originalFailure = new MarkerException("original");
        var message = new BinderData("condition-payload");
        IBehaviorExceptionContext<BinderSaga, BinderData, MarkerException> context =
            CreateExceptionContext(message, originalFailure, TestContext.Current.CancellationToken);
        var conditionCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        IBehaviorExceptionContext<BinderSaga, BinderData, MarkerException>? conditionContext = null;
        var selected = new FaultRecordingActivity();
        IExceptionActivityBinder<BinderSaga, BinderData, MarkerException> binder =
            new CatchExceptionActivityBinder<BinderSaga, BinderData, MarkerException>(machine, machine.Data)
                .IfElseAwaited(
                    observed =>
                    {
                        conditionContext = observed;
                        return conditionCompletion.Task;
                    },
                    branch => branch.Add(selected),
                    branch => branch.Add(new FaultRecordingActivity()));
        IStateMachineActivity<BinderSaga> conditional = CaptureActivity(Assert.Single(binder.GetStateActivityBinders()));
        var next = new RecordingTypedBehavior();

        Task pending = conditional.FaultedAsync(context, next);

        Assert.Same(context, conditionContext);
        Assert.False(pending.IsCompleted);
        conditionCompletion.SetResult(true);
        await pending;
        Assert.Same(context, selected.Context);
        Assert.Same(originalFailure, selected.Exception);
        Assert.Same(context, next.FaultContext);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        binder = new CatchExceptionActivityBinder<BinderSaga, BinderData, MarkerException>(machine, machine.Data)
            .IfAwaited(_ => Task.FromCanceled<bool>(cancellation.Token), branch => branch.Add(new FaultRecordingActivity()));
        conditional = CaptureActivity(Assert.Single(binder.GetStateActivityBinders()));

        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            conditional.FaultedAsync(context, next));
        Assert.Equal(cancellation.Token, canceled.CancellationToken);

        var conditionFailure = new MarkerException("condition failed");
        binder = new CatchExceptionActivityBinder<BinderSaga, BinderData, MarkerException>(machine, machine.Data)
            .IfAwaited(_ => Task.FromException<bool>(conditionFailure), branch => branch.Add(new FaultRecordingActivity()));
        conditional = CaptureActivity(Assert.Single(binder.GetStateActivityBinders()));
        Assert.Same(conditionFailure, await Assert.ThrowsAsync<MarkerException>(() => conditional.FaultedAsync(context, next)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "retry-policy-required-before-activity-configuration")]
    public void Retry_WithoutPolicyThrowsConfigurationExceptionBeforeConfiguringActivities()
    {
        var machine = new BinderMachine();
        IEventActivityBinder<BinderSaga> trigger = new TriggerEventActivityBinder<BinderSaga>(machine, machine.Trigger);
        IEventActivityBinder<BinderSaga, BinderData> data =
            new DataEventActivityBinder<BinderSaga, BinderData>(machine, machine.Data);
        var configureCalls = 0;
        var activityCalls = 0;

        ConfigurationException triggerException = Assert.Throws<ConfigurationException>(() => trigger.Retry(
            _ => configureCalls++,
            binder =>
            {
                activityCalls++;
                return binder;
            }));
        ConfigurationException dataException = Assert.Throws<ConfigurationException>(() => data.Retry(
            _ => configureCalls++,
            binder =>
            {
                activityCalls++;
                return binder;
            }));

        Assert.Contains("A retry policy must be specified", triggerException.Message, StringComparison.Ordinal);
        Assert.Contains("A retry policy must be specified", dataException.Message, StringComparison.Ordinal);
        Assert.Equal(2, configureCalls);
        Assert.Equal(0, activityCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "trigger-retry-policy-attempt-order-context-failure-cancellation")]
    public async Task TriggerRetry_UsesConfiguredPolicyAndPreservesRuntimeIdentitiesAsync()
    {
        var machine = new BinderMachine();
        var order = new List<string>();
        var transientFailure = new MarkerException("transient");
        var attempt = new RetryAttemptActivity(2, transientFailure, order);
        IEventActivityBinder<BinderSaga> configured = new TriggerEventActivityBinder<BinderSaga>(machine, machine.Trigger)
            .Retry(retry => retry.Immediate(2), branch => branch
                .Add(attempt)
                .Add(new SequenceActivity("inside-after", order)));
        var retryBinder = Assert.IsType<RetryActivityBinder<BinderSaga>>(Assert.Single(configured.GetStateActivityBinders()));
        Assert.Same(machine.Trigger, retryBinder.Event);
        IStateMachineActivity<BinderSaga> runtime = CaptureActivity(retryBinder);
        Assert.IsType<RetryActivity<BinderSaga>>(runtime);
        IBehaviorContext<BinderSaga> context = CreateExecutionContext(TestContext.Current.CancellationToken);
        var next = new RecordingBehavior(order, "outer-next");

        await runtime.ExecuteAsync(context, next);

        Assert.Equal(3, attempt.Attempts);
        Assert.All(attempt.Contexts, candidate => Assert.Same(context, candidate));
        Assert.Equal(["attempt-1", "attempt-2", "attempt-3", "inside-after", "outer-next"], order);
        Assert.Equal(1, next.ExecuteCalls);
        Assert.Same(context, Assert.Single(next.ExecuteContexts));

        var terminalFailure = new MarkerException("terminal");
        var terminalAttempt = new RetryAttemptActivity(int.MaxValue, terminalFailure, []);
        configured = new TriggerEventActivityBinder<BinderSaga>(machine, machine.Trigger)
            .Retry(retry => retry.Immediate(1), branch => branch.Add(terminalAttempt));
        runtime = CaptureActivity(Assert.Single(configured.GetStateActivityBinders()));
        next = new RecordingBehavior();

        Assert.Same(terminalFailure,
            await Assert.ThrowsAsync<MarkerException>(() =>
                runtime.ExecuteAsync(CreateExecutionContext(TestContext.Current.CancellationToken), next)));
        Assert.Equal(2, terminalAttempt.Attempts);
        Assert.Equal(0, next.ExecuteCalls);
        Assert.Equal(0, next.FaultCalls);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var canceledAttempt = new RetryAttemptActivity(0, transientFailure, []);
        configured = new TriggerEventActivityBinder<BinderSaga>(machine, machine.Trigger)
            .Retry(retry => retry.Immediate(2), branch => branch.Add(canceledAttempt));
        runtime = CaptureActivity(Assert.Single(configured.GetStateActivityBinders()));
        next = new RecordingBehavior();

        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            runtime.ExecuteAsync(CreateExecutionContext(cancellation.Token), next));
        Assert.Equal(cancellation.Token, canceled.CancellationToken);
        Assert.Equal(0, canceledAttempt.Attempts);
        Assert.Equal(0, next.ExecuteCalls);
        Assert.Equal(0, next.FaultCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "data-retry-policy-attempt-order-context-failure-cancellation-exact-runtime")]
    public async Task DataRetry_UsesExactTypedRuntimeAndPreservesRuntimeIdentitiesAsync()
    {
        var machine = new BinderMachine();
        var message = new BinderData("retry-payload");
        var order = new List<string>();
        var transientFailure = new MarkerException("typed-transient");
        var attempt = new TypedRetryAttemptActivity(2, transientFailure, order);
        IEventActivityBinder<BinderSaga, BinderData> configured =
            new DataEventActivityBinder<BinderSaga, BinderData>(machine, machine.Data)
                .Retry(retry => retry.Immediate(2), branch => branch
                    .Add(attempt)
                    .Add(new TypedSequenceActivity("inside-after", order)));
        var retryBinder = Assert.IsType<RetryActivityBinder<BinderSaga, BinderData>>(
            Assert.Single(configured.GetStateActivityBinders()));
        Assert.Same(machine.Data, retryBinder.Event);
        IStateMachineActivity<BinderSaga> runtime = CaptureActivity(retryBinder);
        Assert.IsType<RetryActivity<BinderSaga, BinderData>>(runtime);
        IBehaviorContext<BinderSaga, BinderData> context = CreateContext(message, TestContext.Current.CancellationToken);
        var next = new RecordingTypedBehavior(order, "outer-next");

        await runtime.ExecuteAsync(context, next);

        Assert.Equal(3, attempt.Attempts);
        Assert.All(attempt.Contexts, candidate => Assert.Same(context, candidate));
        Assert.All(attempt.Messages, candidate => Assert.Same(message, candidate));
        Assert.Equal(["attempt-1", "attempt-2", "attempt-3", "inside-after", "outer-next"], order);
        Assert.Equal(1, next.ExecuteCalls);
        Assert.Same(context, Assert.Single(next.ExecuteContexts));

        var terminalFailure = new MarkerException("typed-terminal");
        var terminalAttempt = new TypedRetryAttemptActivity(int.MaxValue, terminalFailure, []);
        configured = new DataEventActivityBinder<BinderSaga, BinderData>(machine, machine.Data)
            .Retry(retry => retry.Immediate(1), branch => branch.Add(terminalAttempt));
        runtime = CaptureActivity(Assert.Single(configured.GetStateActivityBinders()));
        next = new RecordingTypedBehavior();

        Assert.Same(terminalFailure,
            await Assert.ThrowsAsync<MarkerException>(() =>
                runtime.ExecuteAsync(CreateContext(message, TestContext.Current.CancellationToken), next)));
        Assert.Equal(2, terminalAttempt.Attempts);
        Assert.Equal(0, next.ExecuteCalls);
        Assert.Equal(0, next.FaultCalls);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var canceledAttempt = new TypedRetryAttemptActivity(0, transientFailure, []);
        configured = new DataEventActivityBinder<BinderSaga, BinderData>(machine, machine.Data)
            .Retry(retry => retry.Immediate(2), branch => branch.Add(canceledAttempt));
        runtime = CaptureActivity(Assert.Single(configured.GetStateActivityBinders()));
        next = new RecordingTypedBehavior();

        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            runtime.ExecuteAsync(CreateContext(message, cancellation.Token), next));
        Assert.Equal(cancellation.Token, canceled.CancellationToken);
        Assert.Equal(0, canceledAttempt.Attempts);
        Assert.Equal(0, next.ExecuteCalls);
        Assert.Equal(0, next.FaultCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "filtered-constructor-runtime-snapshot-and-concurrency")]
    public async Task FilteredConstructors_SelectByExactContextAndKeepSnapshotAcrossConcurrentReadsAsync()
    {
        var machine = new BinderMachine();
        var triggerSelected = new CountingActivity();
        var triggerReplacement = new CountingActivity();
        IBehaviorContext<BinderSaga> triggerContext = CreateExecutionContext(TestContext.Current.CancellationToken);
        IBehaviorContext<BinderSaga>? observedTriggerContext = null;
        var triggerResult = true;
        IActivityBinder<BinderSaga>[] triggerInput =
            [new RecordingActivityBinder(machine.Trigger, triggerSelected)];
        var trigger = new TriggerEventActivityBinder<BinderSaga>(machine, machine.Trigger, context =>
        {
            observedTriggerContext = context;
            return triggerResult;
        }, triggerInput);
        triggerInput[0] = new RecordingActivityBinder(machine.Trigger, triggerReplacement);

        IStateMachineActivity<BinderSaga> triggerConditional = CaptureActivity(Assert.Single(trigger.GetStateActivityBinders()));
        var triggerNext = new RecordingBehavior();
        await triggerConditional.ExecuteAsync(triggerContext, triggerNext);
        Assert.Same(triggerContext, observedTriggerContext);
        Assert.Equal(1, triggerSelected.ExecuteCalls);
        Assert.Equal(0, triggerReplacement.ExecuteCalls);
        Assert.Equal(1, triggerNext.ExecuteCalls);

        triggerResult = false;
        await triggerConditional.ExecuteAsync(triggerContext, triggerNext);
        Assert.Equal(1, triggerSelected.ExecuteCalls);
        Assert.Equal(2, triggerNext.ExecuteCalls);

        var triggerFailure = new MarkerException("trigger filter failed");
        trigger = new TriggerEventActivityBinder<BinderSaga>(machine, machine.Trigger, _ => throw triggerFailure,
            new RecordingActivityBinder(machine.Trigger, triggerSelected));
        triggerConditional = CaptureActivity(Assert.Single(trigger.GetStateActivityBinders()));
        triggerNext = new RecordingBehavior();
        Assert.Same(triggerFailure,
            await Assert.ThrowsAsync<MarkerException>(() => triggerConditional.ExecuteAsync(triggerContext, triggerNext)));
        Assert.Equal(0, triggerNext.ExecuteCalls);

        var dataSelected = new CountingTypedActivity();
        var dataReplacement = new CountingTypedActivity();
        var message = new BinderData("filter-payload");
        IBehaviorContext<BinderSaga, BinderData> dataContext =
            CreateContext(message, TestContext.Current.CancellationToken);
        IBehaviorContext<BinderSaga, BinderData>? observedDataContext = null;
        var dataResult = true;
        IActivityBinder<BinderSaga>[] dataInput =
            [new RecordingActivityBinder(machine.Data, new DataConverterActivity<BinderSaga, BinderData>(dataSelected))];
        var data = new DataEventActivityBinder<BinderSaga, BinderData>(machine, machine.Data, context =>
        {
            observedDataContext = context;
            return dataResult;
        }, dataInput);
        dataInput[0] = new RecordingActivityBinder(
            machine.Data,
            new DataConverterActivity<BinderSaga, BinderData>(dataReplacement));

        IStateMachineActivity<BinderSaga> dataConditional = CaptureActivity(Assert.Single(data.GetStateActivityBinders()));
        var dataNext = new RecordingTypedBehavior();
        await dataConditional.ExecuteAsync(dataContext, dataNext);
        Assert.Same(dataContext, observedDataContext);
        Assert.Same(message, dataSelected.Message);
        Assert.Equal(1, dataSelected.ExecuteCalls);
        Assert.Equal(0, dataReplacement.ExecuteCalls);
        Assert.Equal(1, dataNext.ExecuteCalls);

        dataResult = false;
        await dataConditional.ExecuteAsync(dataContext, dataNext);
        Assert.Equal(1, dataSelected.ExecuteCalls);
        Assert.Equal(2, dataNext.ExecuteCalls);

        var dataFailure = new MarkerException("data filter failed");
        data = new DataEventActivityBinder<BinderSaga, BinderData>(machine, machine.Data, _ => throw dataFailure,
            new RecordingActivityBinder(machine.Data, new DataConverterActivity<BinderSaga, BinderData>(dataSelected)));
        dataConditional = CaptureActivity(Assert.Single(data.GetStateActivityBinders()));
        dataNext = new RecordingTypedBehavior();
        Assert.Same(dataFailure,
            await Assert.ThrowsAsync<MarkerException>(() => dataConditional.ExecuteAsync(dataContext, dataNext)));
        Assert.Equal(0, dataNext.ExecuteCalls);

        trigger = new TriggerEventActivityBinder<BinderSaga>(machine, machine.Trigger, _ => true,
            new RecordingActivityBinder(machine.Trigger, triggerSelected));
        data = new DataEventActivityBinder<BinderSaga, BinderData>(machine, machine.Data, _ => true,
            new RecordingActivityBinder(machine.Data, new DataConverterActivity<BinderSaga, BinderData>(dataSelected)));
        int triggerBeforeConcurrency = triggerSelected.ExecuteCalls;
        int dataBeforeConcurrency = dataSelected.ExecuteCalls;

        await Task.WhenAll(Enumerable.Range(0, 32).Select(async _ =>
        {
            await CaptureActivity(Assert.Single(trigger.GetStateActivityBinders()))
                .ExecuteAsync(triggerContext, new RecordingBehavior());
            await CaptureActivity(Assert.Single(data.GetStateActivityBinders()))
                .ExecuteAsync(dataContext, new RecordingTypedBehavior());
        }));

        Assert.Equal(triggerBeforeConcurrency + 32, triggerSelected.ExecuteCalls);
        Assert.Equal(dataBeforeConcurrency + 32, dataSelected.ExecuteCalls);
        Assert.Equal(0, triggerReplacement.ExecuteCalls);
        Assert.Equal(0, dataReplacement.ExecuteCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "event-and-exception-else-branch-runtime-selection")]
    public async Task IfElse_FalseSelectsOnlyElseForTriggerDataAndBothExceptionBindersAsync()
    {
        var machine = new BinderMachine();
        var order = new List<string>();
        IEventActivityBinder<BinderSaga> trigger = new TriggerEventActivityBinder<BinderSaga>(machine, machine.Trigger)
            .IfElse(_ => false,
                branch => branch.Add(new BranchRecordingActivity("trigger-then", order)),
                branch => branch.Add(new BranchRecordingActivity("trigger-else", order)));
        IStateMachineActivity<BinderSaga> conditional = CaptureActivity(Assert.Single(trigger.GetStateActivityBinders()));
        var untypedNext = new RecordingBehavior(order, "trigger-next");
        IBehaviorContext<BinderSaga> triggerContext = CreateExecutionContext(TestContext.Current.CancellationToken);
        await conditional.ExecuteAsync(triggerContext, untypedNext);
        Assert.Equal(["trigger-else", "trigger-next"], order);
        Assert.Same(triggerContext, Assert.Single(untypedNext.ExecuteContexts));

        order.Clear();
        var message = new BinderData("else-payload");
        IEventActivityBinder<BinderSaga, BinderData> data =
            new DataEventActivityBinder<BinderSaga, BinderData>(machine, machine.Data)
                .IfElse(_ => false,
                    branch => branch.Add(new TypedBranchRecordingActivity("data-then", order)),
                    branch => branch.Add(new TypedBranchRecordingActivity("data-else", order)));
        conditional = CaptureActivity(Assert.Single(data.GetStateActivityBinders()));
        var typedNext = new RecordingTypedBehavior(order, "data-next");
        IBehaviorContext<BinderSaga, BinderData> dataContext =
            CreateContext(message, TestContext.Current.CancellationToken);
        await conditional.ExecuteAsync(dataContext, typedNext);
        Assert.Equal(["data-else", "data-next"], order);
        Assert.Same(dataContext, Assert.Single(typedNext.ExecuteContexts));

        order.Clear();
        var failure = new MarkerException("else-failure");
        IExceptionActivityBinder<BinderSaga, MarkerException> caught =
            new CatchExceptionActivityBinder<BinderSaga, MarkerException>(machine, machine.Trigger)
                .IfElse(_ => false,
                    branch => branch.Add(new BranchRecordingActivity("catch-then", order)),
                    branch => branch.Add(new BranchRecordingActivity("catch-else", order)));
        conditional = CaptureActivity(Assert.Single(caught.GetStateActivityBinders()));
        untypedNext = new RecordingBehavior(order, "catch-next");
        IBehaviorExceptionContext<BinderSaga, MarkerException> exceptionContext =
            CreateExceptionContext(failure, TestContext.Current.CancellationToken);
        await conditional.FaultedAsync(exceptionContext, untypedNext);
        Assert.Equal(["catch-else", "catch-next"], order);
        Assert.Same(exceptionContext, Assert.Single(untypedNext.FaultContexts));

        order.Clear();
        IExceptionActivityBinder<BinderSaga, BinderData, MarkerException> caughtData =
            new CatchExceptionActivityBinder<BinderSaga, BinderData, MarkerException>(machine, machine.Data)
                .IfElse(_ => false,
                    branch => branch.Add(new TypedBranchRecordingActivity("catch-data-then", order)),
                    branch => branch.Add(new TypedBranchRecordingActivity("catch-data-else", order)));
        conditional = CaptureActivity(Assert.Single(caughtData.GetStateActivityBinders()));
        typedNext = new RecordingTypedBehavior(order, "catch-data-next");
        IBehaviorExceptionContext<BinderSaga, BinderData, MarkerException> dataExceptionContext =
            CreateExceptionContext(message, failure, TestContext.Current.CancellationToken);
        await conditional.FaultedAsync(dataExceptionContext, typedNext);
        Assert.Equal(["catch-data-else", "catch-data-next"], order);
        Assert.Same(dataExceptionContext, Assert.Single(typedNext.FaultContexts));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "condition-failure-cancellation-have-no-branch-or-next-effects")]
    public async Task AwaitedConditionFailureAndCancellation_PreserveIdentityWithoutBranchOrNextEffectsAsync()
    {
        var machine = new BinderMachine();
        var failure = new MarkerException("condition failure");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await AssertEventConditionStopsPipelineAsync(
            new TriggerEventActivityBinder<BinderSaga>(machine, machine.Trigger),
            _ => Task.FromException<bool>(failure),
            failure);
        await AssertEventConditionStopsPipelineAsync(
            new TriggerEventActivityBinder<BinderSaga>(machine, machine.Trigger),
            _ => Task.FromCanceled<bool>(cancellation.Token),
            cancellation.Token);
        await AssertDataConditionStopsPipelineAsync(
            new DataEventActivityBinder<BinderSaga, BinderData>(machine, machine.Data),
            _ => Task.FromException<bool>(failure),
            failure);
        await AssertDataConditionStopsPipelineAsync(
            new DataEventActivityBinder<BinderSaga, BinderData>(machine, machine.Data),
            _ => Task.FromCanceled<bool>(cancellation.Token),
            cancellation.Token);
        await AssertExceptionConditionStopsPipelineAsync(
            new CatchExceptionActivityBinder<BinderSaga, MarkerException>(machine, machine.Trigger),
            _ => Task.FromException<bool>(failure),
            failure);
        await AssertExceptionConditionStopsPipelineAsync(
            new CatchExceptionActivityBinder<BinderSaga, MarkerException>(machine, machine.Trigger),
            _ => Task.FromCanceled<bool>(cancellation.Token),
            cancellation.Token);
        await AssertDataExceptionConditionStopsPipelineAsync(
            new CatchExceptionActivityBinder<BinderSaga, BinderData, MarkerException>(machine, machine.Data),
            _ => Task.FromException<bool>(failure),
            failure);
        await AssertDataExceptionConditionStopsPipelineAsync(
            new CatchExceptionActivityBinder<BinderSaga, BinderData, MarkerException>(machine, machine.Data),
            _ => Task.FromCanceled<bool>(cancellation.Token),
            cancellation.Token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "concrete-binder-complete-public-api-surface-and-nullability")]
    public void ConcreteBinders_HaveExactPublicSurfaceConstraintsReturnsAndNullability()
    {
        Type triggerDefinition = typeof(TriggerEventActivityBinder<>);
        Type dataDefinition = typeof(DataEventActivityBinder<,>);
        Type caughtDefinition = typeof(CatchExceptionActivityBinder<,>);
        Type caughtDataDefinition = typeof(CatchExceptionActivityBinder<,,>);
        AssertSagaConstraint(triggerDefinition, 0);
        AssertSagaConstraint(dataDefinition, 0);
        AssertReferenceConstraint(dataDefinition, 1);
        AssertSagaConstraint(caughtDefinition, 0);
        AssertExceptionConstraint(caughtDefinition, 1);
        AssertSagaConstraint(caughtDataDefinition, 0);
        AssertReferenceConstraint(caughtDataDefinition, 1);
        AssertExceptionConstraint(caughtDataDefinition, 2);

        Type triggerType = typeof(TriggerEventActivityBinder<BinderSaga>);
        Type triggerBinder = typeof(IEventActivityBinder<BinderSaga>);
        Type triggerCallback = typeof(Func<IEventActivityBinder<BinderSaga>, IEventActivityBinder<BinderSaga>>);
        AssertConcreteTypeShape(triggerType, typeof(IEventActivityBinder<BinderSaga>), 2, 1, 4);
        AssertPublicProperty(triggerType, nameof(TriggerEventActivityBinder<BinderSaga>.Event), typeof(IEvent));
        AssertPublicMethod(triggerType, nameof(IEventActivities<BinderSaga>.GetStateActivityBinders),
            typeof(IEnumerable<IActivityBinder<BinderSaga>>));
        AssertPublicMethod(triggerType, nameof(IEventActivityBinder<BinderSaga>.Retry), triggerBinder,
            typeof(Action<IRetryConfigurator>), triggerCallback);
        AssertPublicMethod(triggerType, nameof(IEventActivityBinder<BinderSaga>.IfElse), triggerBinder,
            typeof(StateMachineCondition<BinderSaga>), triggerCallback, triggerCallback);
        AssertPublicMethod(triggerType, nameof(IEventActivityBinder<BinderSaga>.IfElseAwaited), triggerBinder,
            typeof(StateMachineAsyncCondition<BinderSaga>), triggerCallback, triggerCallback);
        AssertInterfaceImplementationNullability(triggerType, typeof(IEventActivityBinder<BinderSaga>));

        Type dataType = typeof(DataEventActivityBinder<BinderSaga, BinderData>);
        Type dataBinder = typeof(IEventActivityBinder<BinderSaga, BinderData>);
        Type dataCallback = typeof(Func<IEventActivityBinder<BinderSaga, BinderData>, IEventActivityBinder<BinderSaga, BinderData>>);
        AssertConcreteTypeShape(dataType, typeof(IEventActivityBinder<BinderSaga, BinderData>), 2, 0, 4);
        AssertPublicMethod(dataType, nameof(IEventActivities<BinderSaga>.GetStateActivityBinders),
            typeof(IEnumerable<IActivityBinder<BinderSaga>>));
        AssertPublicMethod(dataType, nameof(IEventActivityBinder<BinderSaga, BinderData>.Retry), dataBinder,
            typeof(Action<IRetryConfigurator>), dataCallback);
        AssertPublicMethod(dataType, nameof(IEventActivityBinder<BinderSaga, BinderData>.IfElse), dataBinder,
            typeof(StateMachineCondition<BinderSaga, BinderData>), dataCallback, dataCallback);
        AssertPublicMethod(dataType, nameof(IEventActivityBinder<BinderSaga, BinderData>.IfElseAwaited), dataBinder,
            typeof(StateMachineAsyncCondition<BinderSaga, BinderData>), dataCallback, dataCallback);
        AssertInterfaceImplementationNullability(dataType, typeof(IEventActivityBinder<BinderSaga, BinderData>));

        Type caughtType = typeof(CatchExceptionActivityBinder<BinderSaga, MarkerException>);
        Type caughtBinder = typeof(IExceptionActivityBinder<BinderSaga, MarkerException>);
        Type caughtCallback = typeof(Func<IExceptionActivityBinder<BinderSaga, MarkerException>,
            IExceptionActivityBinder<BinderSaga, MarkerException>>);
        AssertConcreteTypeShape(caughtType, caughtBinder, 1, 2, 7);
        AssertPublicProperty(caughtType, nameof(CatchExceptionActivityBinder<BinderSaga, MarkerException>.Event), typeof(IEvent));
        AssertPublicProperty(caughtType, nameof(CatchExceptionActivityBinder<BinderSaga, MarkerException>.StateMachine),
            typeof(IStateMachine<BinderSaga>));
        AssertPublicMethod(caughtType, nameof(IEventActivities<BinderSaga>.GetStateActivityBinders),
            typeof(IEnumerable<IActivityBinder<BinderSaga>>));
        AssertPublicMethod(caughtType, nameof(IExceptionActivityBinder<BinderSaga, MarkerException>.Add), caughtBinder,
            typeof(IStateMachineActivity<BinderSaga>));
        AssertPublicMethod(caughtType, nameof(IExceptionActivityBinder<BinderSaga, MarkerException>.If), caughtBinder,
            typeof(StateMachineExceptionCondition<BinderSaga, MarkerException>), caughtCallback);
        AssertPublicMethod(caughtType, nameof(IExceptionActivityBinder<BinderSaga, MarkerException>.IfAwaited), caughtBinder,
            typeof(StateMachineAsyncExceptionCondition<BinderSaga, MarkerException>), caughtCallback);
        AssertPublicMethod(caughtType, nameof(IExceptionActivityBinder<BinderSaga, MarkerException>.IfElse), caughtBinder,
            typeof(StateMachineExceptionCondition<BinderSaga, MarkerException>), caughtCallback, caughtCallback);
        AssertPublicMethod(caughtType, nameof(IExceptionActivityBinder<BinderSaga, MarkerException>.IfElseAwaited), caughtBinder,
            typeof(StateMachineAsyncExceptionCondition<BinderSaga, MarkerException>), caughtCallback, caughtCallback);
        AssertCatchMethod(caughtType, caughtBinder, typeof(IExceptionActivityBinder<,>));
        AssertInterfaceImplementationNullability(caughtType, caughtBinder);

        Type caughtDataType = typeof(CatchExceptionActivityBinder<BinderSaga, BinderData, MarkerException>);
        Type caughtDataBinder = typeof(IExceptionActivityBinder<BinderSaga, BinderData, MarkerException>);
        Type caughtDataCallback = typeof(Func<IExceptionActivityBinder<BinderSaga, BinderData, MarkerException>,
            IExceptionActivityBinder<BinderSaga, BinderData, MarkerException>>);
        AssertConcreteTypeShape(caughtDataType, caughtDataBinder, 1, 2, 8);
        AssertPublicProperty(caughtDataType,
            nameof(CatchExceptionActivityBinder<BinderSaga, BinderData, MarkerException>.Event), typeof(IEvent<BinderData>));
        AssertPublicProperty(caughtDataType,
            nameof(CatchExceptionActivityBinder<BinderSaga, BinderData, MarkerException>.StateMachine),
            typeof(IStateMachine<BinderSaga>));
        AssertPublicMethod(caughtDataType, nameof(IEventActivities<BinderSaga>.GetStateActivityBinders),
            typeof(IEnumerable<IActivityBinder<BinderSaga>>));
        AssertPublicMethod(caughtDataType, nameof(IExceptionActivityBinder<BinderSaga, BinderData, MarkerException>.Add),
            caughtDataBinder, typeof(IStateMachineActivity<BinderSaga>));
        AssertPublicMethod(caughtDataType, nameof(IExceptionActivityBinder<BinderSaga, BinderData, MarkerException>.Add),
            caughtDataBinder, typeof(IStateMachineActivity<BinderSaga, BinderData>));
        AssertPublicMethod(caughtDataType, nameof(IExceptionActivityBinder<BinderSaga, BinderData, MarkerException>.If),
            caughtDataBinder, typeof(StateMachineExceptionCondition<BinderSaga, BinderData, MarkerException>), caughtDataCallback);
        AssertPublicMethod(caughtDataType, nameof(IExceptionActivityBinder<BinderSaga, BinderData, MarkerException>.IfAwaited),
            caughtDataBinder, typeof(StateMachineAsyncExceptionCondition<BinderSaga, BinderData, MarkerException>), caughtDataCallback);
        AssertPublicMethod(caughtDataType, nameof(IExceptionActivityBinder<BinderSaga, BinderData, MarkerException>.IfElse),
            caughtDataBinder, typeof(StateMachineExceptionCondition<BinderSaga, BinderData, MarkerException>),
            caughtDataCallback, caughtDataCallback);
        AssertPublicMethod(caughtDataType, nameof(IExceptionActivityBinder<BinderSaga, BinderData, MarkerException>.IfElseAwaited),
            caughtDataBinder, typeof(StateMachineAsyncExceptionCondition<BinderSaga, BinderData, MarkerException>),
            caughtDataCallback, caughtDataCallback);
        AssertCatchMethod(caughtDataType, caughtDataBinder, typeof(IExceptionActivityBinder<,,>));
        AssertInterfaceImplementationNullability(caughtDataType, caughtDataBinder);

        AssertAllPublicMembersNotNull(triggerType, ["filter"]);
        AssertAllPublicMembersNotNull(dataType, ["filter"]);
        AssertAllPublicMembersNotNull(caughtType, []);
        AssertAllPublicMembersNotNull(caughtDataType, []);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "binder-visitor-and-probe-composition")]
    public void NestedEventAndExceptionActivities_RemainVisibleToVisitorAndProbe()
    {
        var machine = new BinderMachine();
        var triggerActivity = new InspectableActivity();
        var catchActivity = new InspectableActivity();
        var dataActivity = new InspectableTypedActivity();
        IEventActivityBinder<BinderSaga> trigger =
            new TriggerEventActivityBinder<BinderSaga>(machine, machine.Trigger);
        trigger = trigger
            .Add(triggerActivity)
            .Catch<MarkerException>(caught => caught.Add(catchActivity));
        IEventActivityBinder<BinderSaga, BinderData> data =
            new DataEventActivityBinder<BinderSaga, BinderData>(machine, machine.Data);
        data = data.IfElse(_ => true, branch => branch.Add(dataActivity), branch => branch);
        IBehavior<BinderSaga> triggerBehavior = BuildBehavior(trigger);
        IBehavior<BinderSaga> dataBehavior = BuildBehavior(data);
        var visitor = new ContinuingVisitor();
        var probe = new RecordingProbeContext();

        triggerBehavior.Accept(visitor);
        dataBehavior.Accept(visitor);
        triggerBehavior.Probe(probe);
        dataBehavior.Probe(probe);

        Assert.Same(visitor, triggerActivity.Visitor);
        Assert.Same(visitor, catchActivity.Visitor);
        Assert.Same(visitor, dataActivity.Visitor);
        Assert.Same(probe, triggerActivity.ProbeContext);
        Assert.Same(probe, catchActivity.ProbeContext);
        Assert.Same(probe, dataActivity.ProbeContext);
        Assert.Contains("catch", probe.Scopes);
        Assert.Contains("behavior", probe.Scopes);
        Assert.Contains("condition", probe.Scopes);
        Assert.Contains(visitor.Activities, activity => ReferenceEquals(activity, triggerActivity));
        Assert.Contains(visitor.Activities, activity => ReferenceEquals(activity, catchActivity));
        Assert.Contains(visitor.Activities, activity => ReferenceEquals(activity, dataActivity));
    }

    private static async Task AssertEventConditionStopsPipelineAsync(
        IEventActivityBinder<BinderSaga> binder,
        StateMachineAsyncCondition<BinderSaga> condition,
        object expectedFailure)
    {
        var effects = new List<string>();
        binder = binder.IfElseAwaited(
            condition,
            branch => branch.Add(new BranchRecordingActivity("then", effects)),
            branch => branch.Add(new BranchRecordingActivity("else", effects)));
        IStateMachineActivity<BinderSaga> runtime = CaptureActivity(Assert.Single(binder.GetStateActivityBinders()));
        var next = new RecordingBehavior();

        await AssertFailureIdentityAsync(
            () => runtime.ExecuteAsync(CreateExecutionContext(TestContext.Current.CancellationToken), next),
            expectedFailure);

        Assert.Empty(effects);
        Assert.Equal(0, next.ExecuteCalls);
        Assert.Equal(0, next.FaultCalls);
    }

    private static async Task AssertDataConditionStopsPipelineAsync(
        IEventActivityBinder<BinderSaga, BinderData> binder,
        StateMachineAsyncCondition<BinderSaga, BinderData> condition,
        object expectedFailure)
    {
        var effects = new List<string>();
        binder = binder.IfElseAwaited(
            condition,
            branch => branch.Add(new TypedBranchRecordingActivity("then", effects)),
            branch => branch.Add(new TypedBranchRecordingActivity("else", effects)));
        IStateMachineActivity<BinderSaga> runtime = CaptureActivity(Assert.Single(binder.GetStateActivityBinders()));
        var next = new RecordingTypedBehavior();

        await AssertFailureIdentityAsync(
            () => runtime.ExecuteAsync(CreateContext(new BinderData("condition"), TestContext.Current.CancellationToken), next),
            expectedFailure);

        Assert.Empty(effects);
        Assert.Equal(0, next.ExecuteCalls);
        Assert.Equal(0, next.FaultCalls);
    }

    private static async Task AssertExceptionConditionStopsPipelineAsync(
        IExceptionActivityBinder<BinderSaga, MarkerException> binder,
        StateMachineAsyncExceptionCondition<BinderSaga, MarkerException> condition,
        object expectedFailure)
    {
        var effects = new List<string>();
        binder = binder.IfElseAwaited(
            condition,
            branch => branch.Add(new BranchRecordingActivity("then", effects)),
            branch => branch.Add(new BranchRecordingActivity("else", effects)));
        IStateMachineActivity<BinderSaga> runtime = CaptureActivity(Assert.Single(binder.GetStateActivityBinders()));
        var next = new RecordingBehavior();
        IBehaviorExceptionContext<BinderSaga, MarkerException> context =
            CreateExceptionContext(new MarkerException("original"), TestContext.Current.CancellationToken);

        await AssertFailureIdentityAsync(() => runtime.FaultedAsync(context, next), expectedFailure);

        Assert.Empty(effects);
        Assert.Equal(0, next.ExecuteCalls);
        Assert.Equal(0, next.FaultCalls);
    }

    private static async Task AssertDataExceptionConditionStopsPipelineAsync(
        IExceptionActivityBinder<BinderSaga, BinderData, MarkerException> binder,
        StateMachineAsyncExceptionCondition<BinderSaga, BinderData, MarkerException> condition,
        object expectedFailure)
    {
        var effects = new List<string>();
        binder = binder.IfElseAwaited(
            condition,
            branch => branch.Add(new TypedBranchRecordingActivity("then", effects)),
            branch => branch.Add(new TypedBranchRecordingActivity("else", effects)));
        IStateMachineActivity<BinderSaga> runtime = CaptureActivity(Assert.Single(binder.GetStateActivityBinders()));
        var next = new RecordingTypedBehavior();
        IBehaviorExceptionContext<BinderSaga, BinderData, MarkerException> context =
            CreateExceptionContext(
                new BinderData("condition"),
                new MarkerException("original"),
                TestContext.Current.CancellationToken);

        await AssertFailureIdentityAsync(() => runtime.FaultedAsync(context, next), expectedFailure);

        Assert.Empty(effects);
        Assert.Equal(0, next.ExecuteCalls);
        Assert.Equal(0, next.FaultCalls);
    }

    private static async Task AssertFailureIdentityAsync(Func<Task> action, object expectedFailure)
    {
        if (expectedFailure is CancellationToken cancellationToken)
        {
            OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(action);
            Assert.Equal(cancellationToken, canceled.CancellationToken);
            return;
        }

        Assert.Same(expectedFailure, await Assert.ThrowsAsync<MarkerException>(action));
    }

    private static void AssertConcreteTypeShape(
        Type type,
        Type contract,
        int constructorCount,
        int propertyCount,
        int methodCount)
    {
        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.False(type.IsAbstract);
        Assert.False(type.IsSealed);
        Assert.Contains(contract, type.GetInterfaces());
        Assert.Equal(constructorCount, type.GetConstructors().Length);
        Assert.Equal(propertyCount,
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Length);
        Assert.Equal(methodCount, type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Count(method => !method.IsSpecialName));
        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
        Assert.Empty(type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
    }

    private static void AssertPublicProperty(Type type, string name, Type propertyType)
    {
        PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            ?? throw new Xunit.Sdk.XunitException($"Property {name} was not found on {type}.");
        Assert.Equal(propertyType, property.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.Null(property.SetMethod);
        Assert.Equal(NullabilityState.NotNull, new NullabilityInfoContext().Create(property).ReadState);
    }

    private static void AssertPublicMethod(Type type, string name, Type returnType, params Type[] parameterTypes)
    {
        MethodInfo method = type.GetMethod(
            name,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
            binder: null,
            parameterTypes,
            modifiers: null) ?? throw new Xunit.Sdk.XunitException($"Method {name} was not found on {type}.");
        Assert.False(method.IsGenericMethod);
        Assert.Equal(returnType, method.ReturnType);
        Assert.Equal(NullabilityState.NotNull, new NullabilityInfoContext().Create(method.ReturnParameter).ReadState);
        Assert.All(method.GetParameters(), AssertRequiredNotNullParameter);
    }

    private static void AssertCatchMethod(Type type, Type returnType, Type nestedBinderDefinition)
    {
        MethodInfo method = Assert.Single(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            candidate => candidate.Name == nameof(IExceptionActivityBinder<BinderSaga, MarkerException>.Catch));
        Assert.True(method.IsGenericMethodDefinition);
        Assert.Equal(returnType, method.ReturnType);
        Type nestedException = Assert.Single(method.GetGenericArguments());
        AssertExceptionConstraint(nestedException);
        Type[] ownerArguments = type.GetGenericArguments();
        Type nestedBinder = nestedBinderDefinition.MakeGenericType(
            [.. ownerArguments.Take(ownerArguments.Length - 1), nestedException]);
        ParameterInfo callback = Assert.Single(method.GetParameters());
        Assert.Equal(typeof(Func<,>).MakeGenericType(nestedBinder, nestedBinder), callback.ParameterType);
        AssertRequiredNotNullParameter(callback);
        Assert.Equal(NullabilityState.NotNull, new NullabilityInfoContext().Create(method.ReturnParameter).ReadState);
    }

    private static void AssertInterfaceImplementationNullability(Type type, Type contract)
    {
        InterfaceMapping mapping = type.GetInterfaceMap(contract);
        Assert.Equal(mapping.InterfaceMethods.Length, mapping.TargetMethods.Length);
        Assert.NotEmpty(mapping.TargetMethods);
        var nullability = new NullabilityInfoContext();

        foreach (MethodInfo method in mapping.TargetMethods)
        {
            Assert.False(method.IsAbstract);
            if (method.ReturnType != typeof(void) && !method.ReturnType.IsValueType)
                Assert.Equal(NullabilityState.NotNull, nullability.Create(method.ReturnParameter).ReadState);
            foreach (ParameterInfo parameter in method.GetParameters().Where(parameter => !parameter.ParameterType.IsValueType))
                AssertRequiredNotNullParameter(parameter);
        }
    }

    private static void AssertAllPublicMembersNotNull(Type type, string[] nullableParameterNames)
    {
        var nullability = new NullabilityInfoContext();
        foreach (ConstructorInfo constructor in type.GetConstructors())
        {
            foreach (ParameterInfo parameter in constructor.GetParameters())
            {
                NullabilityInfo info = nullability.Create(parameter);
                Assert.Equal(nullableParameterNames.Contains(parameter.Name)
                    ? NullabilityState.Nullable
                    : NullabilityState.NotNull, info.ReadState);
                if (parameter.ParameterType.IsArray)
                    Assert.Equal(NullabilityState.NotNull, info.ElementType?.ReadState);
            }
        }

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            Assert.Equal(NullabilityState.NotNull, nullability.Create(property).ReadState);

        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                     .Where(method => !method.IsSpecialName))
        {
            if (!method.ReturnType.IsValueType)
                Assert.Equal(NullabilityState.NotNull, nullability.Create(method.ReturnParameter).ReadState);
            Assert.All(method.GetParameters().Where(parameter => !parameter.ParameterType.IsValueType), AssertRequiredNotNullParameter);
        }
    }

    private static void AssertSagaConstraint(Type openType, int index)
    {
        Type parameter = openType.GetGenericArguments()[index];
        Assert.True(parameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        Assert.Equal([typeof(ISagaStateMachineInstance)], parameter.GetGenericParameterConstraints());
    }

    private static void AssertReferenceConstraint(Type openType, int index)
    {
        Type parameter = openType.GetGenericArguments()[index];
        Assert.True(parameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        Assert.Empty(parameter.GetGenericParameterConstraints());
    }

    private static void AssertExceptionConstraint(Type openType, int index) =>
        AssertExceptionConstraint(openType.GetGenericArguments()[index]);

    private static void AssertExceptionConstraint(Type parameter)
    {
        Assert.False(parameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        Assert.Equal([typeof(Exception)], parameter.GetGenericParameterConstraints());
    }

    private static void AssertRequiredNotNullParameter(ParameterInfo parameter)
    {
        Assert.False(parameter.IsOptional);
        Assert.False(parameter.HasDefaultValue);
        Assert.Equal(NullabilityState.NotNull, new NullabilityInfoContext().Create(parameter).ReadState);
    }

    private static void AssertConstructorNullability(Type type, Type[] signature, NullabilityState[] expected)
    {
        ConstructorInfo constructor = type.GetConstructor(signature)
            ?? throw new Xunit.Sdk.XunitException($"Constructor was not found on {type}.");
        ParameterInfo[] parameters = constructor.GetParameters();
        Assert.Equal(expected.Length, parameters.Length);
        var nullability = new NullabilityInfoContext();

        for (var i = 0; i < parameters.Length; i++)
        {
            NullabilityInfo info = nullability.Create(parameters[i]);
            Assert.Equal(expected[i], info.ReadState);
            if (parameters[i].ParameterType.IsArray)
                Assert.Equal(NullabilityState.NotNull, info.ElementType?.ReadState);
        }
    }

    private static void AssertArgument(string expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<ArgumentNullException>(action).ParamName);

    private static void AssertCollectionEntry(Action action)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("activities", exception.ParamName);
        Assert.Equal("The activity collection cannot contain null. (Parameter 'activities')", exception.Message);
    }

    private static void AssertNullResult(Action action, string kind)
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Equal($"The {kind} activity configuration callback returned null.", exception.Message);
    }

    private static void AssertSnapshot(
        IEventActivities<BinderSaga> activities,
        IActivityBinder<BinderSaga> expected,
        IActivityBinder<BinderSaga> replacement)
    {
        IActivityBinder<BinderSaga>[] firstRead = Assert.IsType<IActivityBinder<BinderSaga>[]>(activities.GetStateActivityBinders());
        Assert.Same(expected, Assert.Single(firstRead));
        firstRead[0] = replacement;
        Assert.Same(expected, Assert.Single(activities.GetStateActivityBinders()));
    }

    private static IBehavior<BinderSaga> BuildBehavior(IEventActivities<BinderSaga> activities)
    {
        var builder = new ActivityBehaviorBuilder<BinderSaga>();
        foreach (IActivityBinder<BinderSaga> activity in activities.GetStateActivityBinders())
            activity.Bind(builder);

        return builder.Behavior;
    }

    private static IStateMachineActivity<BinderSaga> CaptureActivity(IActivityBinder<BinderSaga> binder)
    {
        var builder = new CapturingBehaviorBuilder();
        binder.Bind(builder);
        return Assert.Single(builder.Activities);
    }

    private static IBehaviorContext<BinderSaga> CreateExecutionContext(CancellationToken cancellationToken) =>
        CreateProxy<IBehaviorContext<BinderSaga>>((method, _) => method.Name switch
        {
            "get_CancellationToken" => cancellationToken,
            _ => throw Unexpected(method),
        });

    private static IBehaviorContext<BinderSaga, BinderData> CreateContext(
        BinderData message,
        CancellationToken cancellationToken) =>
        CreateProxy<IBehaviorContext<BinderSaga, BinderData>>((method, _) => method.Name switch
        {
            "get_Message" => message,
            "get_CancellationToken" => cancellationToken,
            _ => throw Unexpected(method),
        });

    private static IBehaviorExceptionContext<BinderSaga, MarkerException> CreateExceptionContext(
        MarkerException exception,
        CancellationToken cancellationToken) =>
        CreateProxy<IBehaviorExceptionContext<BinderSaga, MarkerException>>((method, _) => method.Name switch
        {
            "get_Exception" => exception,
            "get_CancellationToken" => cancellationToken,
            _ => throw Unexpected(method),
        });

    private static IBehaviorExceptionContext<BinderSaga, BinderData, MarkerException> CreateExceptionContext(
        BinderData message,
        MarkerException exception,
        CancellationToken cancellationToken) =>
        CreateProxy<IBehaviorExceptionContext<BinderSaga, BinderData, MarkerException>>((method, _) => method.Name switch
        {
            "get_Message" => message,
            "get_Exception" => exception,
            "get_CancellationToken" => cancellationToken,
            _ => throw Unexpected(method),
        });

    private static T CreateProxy<T>(Func<MethodInfo, object?[]?, object?>? handler = null)
        where T : class
    {
        T proxy = DispatchProxy.Create<T, StrictProxy>();
        ((StrictProxy)(object)proxy).Handler = handler ?? ((method, _) => method.Name switch
        {
            "get_CancellationToken" => TestContext.Current.CancellationToken,
            _ => throw Unexpected(method),
        });
        return proxy;
    }

    private static Exception Unexpected(MethodInfo method) =>
        new Xunit.Sdk.XunitException($"Unexpected member invocation: {method.Name}.");

    private sealed class BinderMachine : ViciOneServiceBusStateMachine<BinderSaga>
    {
        public BinderMachine()
        {
            InstanceState(instance => instance.CurrentState!);
        }

        public IState Running { get; private set; } = null!;
        public IEvent Trigger { get; private set; } = null!;
        public IEvent<BinderData> Data { get; private set; } = null!;
    }

    public sealed class BinderSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public IState? CurrentState { get; set; }
    }

    public sealed record BinderData(string Value);

    public sealed class MarkerException(string message = "marker") : Exception(message);

    private sealed class CapturingBehaviorBuilder : IBehaviorBuilder<BinderSaga>
    {
        public List<IStateMachineActivity<BinderSaga>> Activities { get; } = [];

        public void Add(IStateMachineActivity<BinderSaga> activity) => Activities.Add(activity);
    }

    private sealed class RecordingActivityBinder(IEvent @event, IStateMachineActivity<BinderSaga> activity) : IActivityBinder<BinderSaga>
    {
        public IEvent Event { get; } = @event;

        public bool IsStateTransitionEvent(IState state) =>
            Equals(Event, state.Enter) || Equals(Event, state.BeforeEnter)
                || Equals(Event, state.AfterLeave) || Equals(Event, state.Leave);

        public void Bind(IState<BinderSaga> state) => state.Bind(Event, activity);

        public void Bind(IBehaviorBuilder<BinderSaga> builder) => builder.Add(activity);
    }

    private sealed class SequenceActivity(string marker, List<string> order) : IStateMachineActivity<BinderSaga>
    {
        public Task ExecuteAsync(IBehaviorContext<BinderSaga> context, IBehavior<BinderSaga> next)
        {
            order.Add(marker);
            return next.ExecuteAsync(context);
        }

        public Task ExecuteAsync<T>(IBehaviorContext<BinderSaga, T> context, IBehavior<BinderSaga, T> next)
            where T : class
        {
            order.Add(marker);
            return next.ExecuteAsync(context);
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<BinderSaga, TException> context, IBehavior<BinderSaga> next)
            where TException : Exception => next.FaultedAsync(context);

        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<BinderSaga, T, TException> context, IBehavior<BinderSaga, T> next)
            where T : class
            where TException : Exception => next.FaultedAsync(context);

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);

        public void Probe(ProbeContext context) => context.CreateScope(marker);
    }

    private sealed class TypedSequenceActivity(string marker, List<string> order) : IStateMachineActivity<BinderSaga, BinderData>
    {
        public Task ExecuteAsync(IBehaviorContext<BinderSaga, BinderData> context, IBehavior<BinderSaga, BinderData> next)
        {
            order.Add(marker);
            return next.ExecuteAsync(context);
        }

        public Task FaultedAsync<TException>(
            IBehaviorExceptionContext<BinderSaga, BinderData, TException> context,
            IBehavior<BinderSaga, BinderData> next)
            where TException : Exception => next.FaultedAsync(context);

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);

        public void Probe(ProbeContext context) => context.CreateScope(marker);
    }

    private sealed class RetryAttemptActivity(
        int failuresBeforeSuccess,
        MarkerException failure,
        List<string> order) : IStateMachineActivity<BinderSaga>
    {
        int _attempts;

        public int Attempts => Volatile.Read(ref _attempts);
        public List<object> Contexts { get; } = [];

        public Task ExecuteAsync(IBehaviorContext<BinderSaga> context, IBehavior<BinderSaga> next) =>
            AttemptAsync(context, () => next.ExecuteAsync(context));

        public Task ExecuteAsync<T>(IBehaviorContext<BinderSaga, T> context, IBehavior<BinderSaga, T> next)
            where T : class => AttemptAsync(context, () => next.ExecuteAsync(context));

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<BinderSaga, TException> context, IBehavior<BinderSaga> next)
            where TException : Exception => next.FaultedAsync(context);

        public Task FaultedAsync<T, TException>(
            IBehaviorExceptionContext<BinderSaga, T, TException> context,
            IBehavior<BinderSaga, T> next)
            where T : class
            where TException : Exception => next.FaultedAsync(context);

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);

        public void Probe(ProbeContext context)
        {
        }

        Task AttemptAsync(object context, Func<Task> next)
        {
            int attempt = Interlocked.Increment(ref _attempts);
            Contexts.Add(context);
            order.Add($"attempt-{attempt}");
            return attempt <= failuresBeforeSuccess ? Task.FromException(failure) : next();
        }
    }

    private sealed class TypedRetryAttemptActivity(
        int failuresBeforeSuccess,
        MarkerException failure,
        List<string> order) : IStateMachineActivity<BinderSaga, BinderData>
    {
        int _attempts;

        public int Attempts => Volatile.Read(ref _attempts);
        public List<IBehaviorContext<BinderSaga, BinderData>> Contexts { get; } = [];
        public List<BinderData> Messages { get; } = [];

        public Task ExecuteAsync(IBehaviorContext<BinderSaga, BinderData> context, IBehavior<BinderSaga, BinderData> next)
        {
            int attempt = Interlocked.Increment(ref _attempts);
            Contexts.Add(context);
            Messages.Add(context.Message);
            order.Add($"attempt-{attempt}");
            return attempt <= failuresBeforeSuccess ? Task.FromException(failure) : next.ExecuteAsync(context);
        }

        public Task FaultedAsync<TException>(
            IBehaviorExceptionContext<BinderSaga, BinderData, TException> context,
            IBehavior<BinderSaga, BinderData> next)
            where TException : Exception => next.FaultedAsync(context);

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class CountingActivity : IStateMachineActivity<BinderSaga>
    {
        int _executeCalls;

        public int ExecuteCalls => Volatile.Read(ref _executeCalls);

        public Task ExecuteAsync(IBehaviorContext<BinderSaga> context, IBehavior<BinderSaga> next)
        {
            Interlocked.Increment(ref _executeCalls);
            return next.ExecuteAsync(context);
        }

        public Task ExecuteAsync<T>(IBehaviorContext<BinderSaga, T> context, IBehavior<BinderSaga, T> next)
            where T : class
        {
            Interlocked.Increment(ref _executeCalls);
            return next.ExecuteAsync(context);
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<BinderSaga, TException> context, IBehavior<BinderSaga> next)
            where TException : Exception => next.FaultedAsync(context);

        public Task FaultedAsync<T, TException>(
            IBehaviorExceptionContext<BinderSaga, T, TException> context,
            IBehavior<BinderSaga, T> next)
            where T : class
            where TException : Exception => next.FaultedAsync(context);

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class CountingTypedActivity : IStateMachineActivity<BinderSaga, BinderData>
    {
        int _executeCalls;

        public int ExecuteCalls => Volatile.Read(ref _executeCalls);
        public BinderData? Message { get; private set; }

        public Task ExecuteAsync(IBehaviorContext<BinderSaga, BinderData> context, IBehavior<BinderSaga, BinderData> next)
        {
            Message = context.Message;
            Interlocked.Increment(ref _executeCalls);
            return next.ExecuteAsync(context);
        }

        public Task FaultedAsync<TException>(
            IBehaviorExceptionContext<BinderSaga, BinderData, TException> context,
            IBehavior<BinderSaga, BinderData> next)
            where TException : Exception => next.FaultedAsync(context);

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class BranchRecordingActivity(string marker, List<string> effects) : IStateMachineActivity<BinderSaga>
    {
        public Task ExecuteAsync(IBehaviorContext<BinderSaga> context, IBehavior<BinderSaga> next)
        {
            effects.Add(marker);
            return next.ExecuteAsync(context);
        }

        public Task ExecuteAsync<T>(IBehaviorContext<BinderSaga, T> context, IBehavior<BinderSaga, T> next)
            where T : class
        {
            effects.Add(marker);
            return next.ExecuteAsync(context);
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<BinderSaga, TException> context, IBehavior<BinderSaga> next)
            where TException : Exception
        {
            effects.Add(marker);
            return next.FaultedAsync(context);
        }

        public Task FaultedAsync<T, TException>(
            IBehaviorExceptionContext<BinderSaga, T, TException> context,
            IBehavior<BinderSaga, T> next)
            where T : class
            where TException : Exception
        {
            effects.Add(marker);
            return next.FaultedAsync(context);
        }

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class TypedBranchRecordingActivity(string marker, List<string> effects) :
        IStateMachineActivity<BinderSaga, BinderData>
    {
        public Task ExecuteAsync(IBehaviorContext<BinderSaga, BinderData> context, IBehavior<BinderSaga, BinderData> next)
        {
            effects.Add(marker);
            return next.ExecuteAsync(context);
        }

        public Task FaultedAsync<TException>(
            IBehaviorExceptionContext<BinderSaga, BinderData, TException> context,
            IBehavior<BinderSaga, BinderData> next)
            where TException : Exception
        {
            effects.Add(marker);
            return next.FaultedAsync(context);
        }

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class TypedStateActivity : IStateMachineActivity<BinderSaga, IState>
    {
        public Task ExecuteAsync(IBehaviorContext<BinderSaga, IState> context, IBehavior<BinderSaga, IState> next) =>
            next.ExecuteAsync(context);

        public Task FaultedAsync<TException>(
            IBehaviorExceptionContext<BinderSaga, IState, TException> context,
            IBehavior<BinderSaga, IState> next)
            where TException : Exception => next.FaultedAsync(context);

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class TypedIdentityActivity(Task executeTask, Task faultTask) : IStateMachineActivity<BinderSaga, BinderData>
    {
        public IBehaviorContext<BinderSaga, BinderData>? ExecuteContext { get; private set; }
        public BinderData? Message { get; private set; }
        public IBehavior<BinderSaga, BinderData>? ExecuteNext { get; private set; }
        public IBehaviorExceptionContext<BinderSaga, BinderData, Exception>? FaultContext { get; private set; }
        public BinderData? FaultMessage { get; private set; }
        public Exception? Exception { get; private set; }
        public IBehavior<BinderSaga, BinderData>? FaultNext { get; private set; }

        public Task ExecuteAsync(IBehaviorContext<BinderSaga, BinderData> context, IBehavior<BinderSaga, BinderData> next)
        {
            ExecuteContext = context;
            Message = context.Message;
            ExecuteNext = next;
            return executeTask;
        }

        public Task FaultedAsync<TException>(
            IBehaviorExceptionContext<BinderSaga, BinderData, TException> context,
            IBehavior<BinderSaga, BinderData> next)
            where TException : Exception
        {
            FaultContext = context;
            FaultMessage = context.Message;
            Exception = context.Exception;
            FaultNext = next;
            return faultTask;
        }

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class UntypedIdentityActivity(Task executeTask, Task faultTask) : IStateMachineActivity<BinderSaga>
    {
        public object? ExecuteContext { get; private set; }
        public object? ExecuteNext { get; private set; }

        public Task ExecuteAsync(IBehaviorContext<BinderSaga> context, IBehavior<BinderSaga> next)
        {
            ExecuteContext = context;
            ExecuteNext = next;
            return executeTask;
        }

        public Task ExecuteAsync<T>(IBehaviorContext<BinderSaga, T> context, IBehavior<BinderSaga, T> next)
            where T : class
        {
            ExecuteContext = context;
            ExecuteNext = next;
            return executeTask;
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<BinderSaga, TException> context, IBehavior<BinderSaga> next)
            where TException : Exception => faultTask;

        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<BinderSaga, T, TException> context, IBehavior<BinderSaga, T> next)
            where T : class
            where TException : Exception => faultTask;

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class FaultRecordingActivity : IStateMachineActivity<BinderSaga, BinderData>
    {
        public object? Context { get; private set; }
        public Exception? Exception { get; private set; }

        public Task ExecuteAsync(IBehaviorContext<BinderSaga, BinderData> context, IBehavior<BinderSaga, BinderData> next) =>
            next.ExecuteAsync(context);

        public Task FaultedAsync<TException>(
            IBehaviorExceptionContext<BinderSaga, BinderData, TException> context,
            IBehavior<BinderSaga, BinderData> next)
            where TException : Exception
        {
            Context = context;
            Exception = context.Exception;
            return next.FaultedAsync(context);
        }

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class RecordingBehavior(List<string>? order = null, string marker = "next") : IBehavior<BinderSaga>
    {
        public int ExecuteCalls { get; private set; }
        public int FaultCalls { get; private set; }
        public List<object> ExecuteContexts { get; } = [];
        public List<object> FaultContexts { get; } = [];

        public Task ExecuteAsync(IBehaviorContext<BinderSaga> context)
        {
            ExecuteCalls++;
            ExecuteContexts.Add(context);
            order?.Add(marker);
            return Task.CompletedTask;
        }

        public Task ExecuteAsync<T>(IBehaviorContext<BinderSaga, T> context)
            where T : class
        {
            ExecuteCalls++;
            ExecuteContexts.Add(context);
            order?.Add(marker);
            return Task.CompletedTask;
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<BinderSaga, TException> context)
            where TException : Exception
        {
            FaultCalls++;
            FaultContexts.Add(context);
            order?.Add(marker);
            return Task.CompletedTask;
        }

        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<BinderSaga, T, TException> context)
            where T : class
            where TException : Exception
        {
            FaultCalls++;
            FaultContexts.Add(context);
            order?.Add(marker);
            return Task.CompletedTask;
        }

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class RecordingTypedBehavior(List<string>? order = null, string marker = "next") :
        IBehavior<BinderSaga, BinderData>
    {
        public object? FaultContext { get; private set; }
        public int ExecuteCalls { get; private set; }
        public int FaultCalls { get; private set; }
        public List<object> ExecuteContexts { get; } = [];
        public List<object> FaultContexts { get; } = [];

        public Task ExecuteAsync(IBehaviorContext<BinderSaga, BinderData> context)
        {
            ExecuteCalls++;
            ExecuteContexts.Add(context);
            order?.Add(marker);
            return Task.CompletedTask;
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<BinderSaga, BinderData, TException> context)
            where TException : Exception
        {
            FaultContext = context;
            FaultCalls++;
            FaultContexts.Add(context);
            order?.Add(marker);
            return Task.CompletedTask;
        }

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit<BinderSaga, BinderData>(this);

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class InspectableActivity : IStateMachineActivity<BinderSaga>
    {
        public IStateMachineVisitor? Visitor { get; private set; }
        public ProbeContext? ProbeContext { get; private set; }

        public Task ExecuteAsync(IBehaviorContext<BinderSaga> context, IBehavior<BinderSaga> next) => next.ExecuteAsync(context);

        public Task ExecuteAsync<T>(IBehaviorContext<BinderSaga, T> context, IBehavior<BinderSaga, T> next)
            where T : class => next.ExecuteAsync(context);

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<BinderSaga, TException> context, IBehavior<BinderSaga> next)
            where TException : Exception => next.FaultedAsync(context);

        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<BinderSaga, T, TException> context, IBehavior<BinderSaga, T> next)
            where T : class
            where TException : Exception => next.FaultedAsync(context);

        public void Accept(IStateMachineVisitor visitor)
        {
            Visitor = visitor;
            visitor.Visit(this);
        }

        public void Probe(ProbeContext context)
        {
            ProbeContext = context;
            context.CreateScope("inspectable");
        }
    }

    private sealed class InspectableTypedActivity : IStateMachineActivity<BinderSaga, BinderData>
    {
        public IStateMachineVisitor? Visitor { get; private set; }
        public ProbeContext? ProbeContext { get; private set; }

        public Task ExecuteAsync(IBehaviorContext<BinderSaga, BinderData> context, IBehavior<BinderSaga, BinderData> next) =>
            next.ExecuteAsync(context);

        public Task FaultedAsync<TException>(
            IBehaviorExceptionContext<BinderSaga, BinderData, TException> context,
            IBehavior<BinderSaga, BinderData> next)
            where TException : Exception => next.FaultedAsync(context);

        public void Accept(IStateMachineVisitor visitor)
        {
            Visitor = visitor;
            visitor.Visit(this);
        }

        public void Probe(ProbeContext context)
        {
            ProbeContext = context;
            context.CreateScope("inspectableTyped");
        }
    }

    private sealed class ContinuingVisitor : IStateMachineVisitor
    {
        public List<IStateMachineActivity> Activities { get; } = [];

        public void Visit(IState state, Action<IState> next) => next(state);

        public void Visit(IEvent @event, Action<IEvent> next) => next(@event);

        public void Visit<TMessage>(IEvent<TMessage> @event, Action<IEvent<TMessage>> next)
            where TMessage : class => next(@event);

        public void Visit(IStateMachineActivity activity) => Activities.Add(activity);

        public void Visit(IStateMachineExceptionActivity activity, Action<IStateMachineExceptionActivity> next)
        {
            Activities.Add(activity);
            next(activity);
        }

        public void Visit<T>(IBehavior<T> behavior)
            where T : class, ISagaStateMachineInstance
        {
        }

        public void Visit<T>(IBehavior<T> behavior, Action<IBehavior<T>> next)
            where T : class, ISagaStateMachineInstance => next(behavior);

        public void Visit<T, TMessage>(IBehavior<T, TMessage> behavior)
            where T : class, ISagaStateMachineInstance
            where TMessage : class
        {
        }

        public void Visit<T, TMessage>(IBehavior<T, TMessage> behavior, Action<IBehavior<T, TMessage>> next)
            where T : class, ISagaStateMachineInstance
            where TMessage : class => next(behavior);

        public void Visit(IStateMachineActivity activity, Action<IStateMachineActivity> next)
        {
            Activities.Add(activity);
            next(activity);
        }
    }

    private sealed class RecordingProbeContext : ProbeContext
    {
        public CancellationToken CancellationToken => System.Threading.CancellationToken.None;

        public List<string> Scopes { get; } = [];

        public void Add(string key, string? value)
        {
        }

        public void Add(string key, object? value)
        {
        }

        public void Set(object values)
        {
        }

        public void Set(IEnumerable<KeyValuePair<string, object?>> values)
        {
        }

        public ProbeContext CreateScope(string key)
        {
            Scopes.Add(key);
            return this;
        }
    }

    public class StrictProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?>? Handler { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null)
                throw new Xunit.Sdk.XunitException("The proxy received a null method.");

            return Handler?.Invoke(targetMethod, args) ?? throw Unexpected(targetMethod);
        }
    }
}
