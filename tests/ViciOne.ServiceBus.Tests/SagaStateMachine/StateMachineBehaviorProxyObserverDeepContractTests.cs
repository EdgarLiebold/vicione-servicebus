using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineBehaviorProxyObserverDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "behavior-proxies-preserve-saga-event-message-and-exception-identity")]
    public async Task BehaviorProxies_PreserveSagaEventMessageAndExceptionIdentityAsync()
    {
        using BehaviorFixture fixture = await BehaviorFixture.CreateAsync();
        var context = new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorContextProxy(
            fixture.Machine,
            fixture.SagaContext,
            fixture.Machine.Signal);

        Assert.Same(fixture.Machine, context.StateMachine);
        Assert.Same(fixture.Instance, context.Saga);
        Assert.Same(fixture.Instance, context.Instance);
        Assert.Same(fixture.Machine.Signal, ((IBehaviorContext<ProxySaga>)context).Event);
        Assert.Equal(fixture.Instance.CorrelationId, context.CorrelationId);

        var data = new ProxyData("payload");
        IBehaviorContext<ProxySaga, ProxyData> dataContext = context.CreateProxy(fixture.Machine.DataSignal, data);

        Assert.Same(fixture.Machine, dataContext.StateMachine);
        Assert.Same(fixture.Instance, dataContext.Saga);
        Assert.Same(fixture.Machine.DataSignal, dataContext.Event);
        Assert.Same(data, dataContext.Message);

        var failure = new InvalidOperationException("expected");
        var exceptionContext = new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorExceptionContextProxy<InvalidOperationException>(
            context,
            failure);
        IBehaviorExceptionContext<ProxySaga, ProxyData, InvalidOperationException> dataExceptionContext =
            exceptionContext.CreateProxy(fixture.Machine.DataSignal, data);

        Assert.Same(failure, exceptionContext.Exception);
        Assert.Same(fixture.Instance, exceptionContext.Saga);
        Assert.Same(fixture.Machine.Signal, ((IBehaviorContext<ProxySaga>)exceptionContext).Event);
        Assert.Same(failure, dataExceptionContext.Exception);
        Assert.Same(fixture.Instance, dataExceptionContext.Saga);
        Assert.Same(fixture.Machine.DataSignal, dataExceptionContext.Event);
        Assert.Same(data, dataExceptionContext.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "behavior-proxy-concrete-views-shared-completion-and-initialization")]
    public async Task BehaviorProxies_ExposeConcreteViewsSharedCompletionAndInitializationAsync()
    {
        using BehaviorFixture fixture = await BehaviorFixture.CreateAsync();
        var context = new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorContextProxy(
            fixture.Machine,
            fixture.SagaContext,
            fixture.Machine.Signal);
        var data = new ProxyData("source");
        var dataContext = Assert.IsType<ViciOneServiceBusStateMachine<ProxySaga>.BehaviorContextProxy<ProxyData>>(
            context.CreateProxy(fixture.Machine.DataSignal, data));

        Assert.Same(fixture.Instance, context.Instance);
        Assert.Same(fixture.Instance, dataContext.Instance);
        Assert.Same(data, dataContext.Data);
        Assert.Same(data, dataContext.Message);
        Assert.False(context.IsCompleted);
        Assert.False(dataContext.IsCompleted);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.SetCompletedAsync(cancellation.Token));

        Assert.Equal(cancellation.Token, canceled.CancellationToken);
        Assert.False(context.IsCompleted);
        Assert.False(dataContext.IsCompleted);

        Task completion = dataContext.SetCompletedAsync(TestContext.Current.CancellationToken);

        Assert.Same(Task.CompletedTask, completion);
        await completion;
        Assert.True(context.IsCompleted);
        Assert.True(dataContext.IsCompleted);

        var initializedFromUntyped =
            await ((IBehaviorContext<ProxySaga>)context).InitAsync<InitializedProxyMessage>(
                new { Value = "untyped" }, TestContext.Current.CancellationToken);
        var initializedFromTyped =
            await ((IBehaviorContext<ProxySaga, ProxyData>)dataContext).InitAsync<InitializedProxyMessage>(
                new { Value = "typed" }, TestContext.Current.CancellationToken);

        Assert.Equal("untyped", initializedFromUntyped.Message.Value);
        Assert.Equal("typed", initializedFromTyped.Message.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "behavior-proxy-untyped-and-typed-raise-from-both-proxy-shapes")]
    public async Task BehaviorProxies_RaiseUntypedAndTypedEventsFromBothProxyShapesAsync()
    {
        using BehaviorFixture fixture = await BehaviorFixture.CreateAsync();
        var context = new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorContextProxy(
            fixture.Machine,
            fixture.SagaContext,
            fixture.Machine.Signal);
        IBehaviorContext<ProxySaga, ProxyData> dataContext =
            context.CreateProxy(fixture.Machine.DataSignal, new ProxyData("source"));
        var first = new ProxyData("first");
        var second = new ProxyData("second");

        await context.RaiseAsync(fixture.Machine.Signal, TestContext.Current.CancellationToken);
        await context.RaiseAsync(fixture.Machine.DataSignal, first, TestContext.Current.CancellationToken);
        await dataContext.RaiseAsync(fixture.Machine.Signal, TestContext.Current.CancellationToken);
        await dataContext.RaiseAsync(fixture.Machine.DataSignal, second, TestContext.Current.CancellationToken);

        Assert.Equal(2, fixture.Machine.SignalExecutions);
        Assert.Equal([first, second], fixture.Machine.DataExecutions);

        AssertArgument("event", () => context.RaiseAsync(null!));
        AssertArgument("event", () => context.RaiseAsync<ProxyData>(null!, first));
        AssertArgument("data", () => context.RaiseAsync(fixture.Machine.DataSignal, null!));
        AssertArgument("event", () => dataContext.RaiseAsync(null!));
        AssertArgument("event", () => dataContext.RaiseAsync<ProxyData>(null!, second));
        AssertArgument("data", () => dataContext.RaiseAsync(fixture.Machine.DataSignal, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "typed-behavior-exception-proxy-create-proxy-chain")]
    public async Task TypedBehaviorExceptionProxy_CreateProxyPreservesExceptionSagaAndReplacementDataAsync()
    {
        using BehaviorFixture fixture = await BehaviorFixture.CreateAsync();
        var context = new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorContextProxy(
            fixture.Machine,
            fixture.SagaContext,
            fixture.Machine.Signal);
        IBehaviorContext<ProxySaga, ProxyData> dataContext =
            context.CreateProxy(fixture.Machine.DataSignal, new ProxyData("source"));
        var failure = new InvalidOperationException("expected");
        var exceptionContext =
            new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorExceptionContextProxy<ProxyData, InvalidOperationException>(
                dataContext,
                failure);
        var replacement = new ReplacementProxyData("replacement");

        IBehaviorExceptionContext<ProxySaga, ReplacementProxyData, InvalidOperationException> chained =
            exceptionContext.CreateProxy(fixture.Machine.ReplacementDataSignal, replacement);
        var concrete = Assert.IsType<
            ViciOneServiceBusStateMachine<ProxySaga>.BehaviorExceptionContextProxy<ReplacementProxyData, InvalidOperationException>>(chained);

        Assert.Same(failure, concrete.Exception);
        Assert.Same(fixture.Instance, concrete.Saga);
        Assert.Same(fixture.Instance, concrete.Instance);
        Assert.Same(
            fixture.Machine.ReplacementDataSignal,
            ((IBehaviorContext<ProxySaga, ReplacementProxyData>)concrete).Event);
        Assert.Same(replacement, concrete.Data);
        Assert.Same(replacement, concrete.Message);
        AssertArgument("event", () => exceptionContext.CreateProxy<ReplacementProxyData>(null!, replacement));
        AssertArgument("data", () => exceptionContext.CreateProxy(fixture.Machine.ReplacementDataSignal, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "behavior-proxy-required-input-boundary-matrix")]
    public async Task BehaviorProxies_RejectEveryMissingRequiredOwnerAndProxyInputAsync()
    {
        using BehaviorFixture fixture = await BehaviorFixture.CreateAsync();
        var context = new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorContextProxy(
            fixture.Machine,
            fixture.SagaContext,
            fixture.Machine.Signal);
        var data = new ProxyData();
        ConsumeContext<ProxyData> consumeContext = new MessageConsumeContext<ProxyData>(fixture.SagaContext, data);
        IBehaviorContext<ProxySaga, ProxyData> dataContext = context.CreateProxy(fixture.Machine.DataSignal, data);

        AssertArgument("machine", () => new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorContextProxy(
            null!, fixture.SagaContext, fixture.Machine.Signal));
        AssertArgument("context", () => new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorContextProxy(
            fixture.Machine, null!, fixture.Machine.Signal));
        AssertArgument("event", () => new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorContextProxy(
            fixture.Machine, fixture.SagaContext, null!));
        AssertArgument("machine", () => new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorContextProxy<ProxyData>(
            null!, fixture.SagaContext, consumeContext, fixture.Machine.DataSignal));
        AssertArgument("context", () => new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorContextProxy<ProxyData>(
            fixture.Machine, null!, consumeContext, fixture.Machine.DataSignal));
        AssertArgument("consumeContext", () => new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorContextProxy<ProxyData>(
            fixture.Machine, fixture.SagaContext, null!, fixture.Machine.DataSignal));
        AssertArgument("event", () => new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorContextProxy<ProxyData>(
            fixture.Machine, fixture.SagaContext, consumeContext, null!));
        AssertArgument("context", () => new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorExceptionContextProxy<InvalidOperationException>(
            null!, new InvalidOperationException()));
        AssertArgument("exception", () => new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorExceptionContextProxy<InvalidOperationException>(
            context, null!));
        AssertArgument("context", () => new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorExceptionContextProxy<ProxyData, InvalidOperationException>(
            null!, new InvalidOperationException()));
        AssertArgument("exception", () => new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorExceptionContextProxy<ProxyData, InvalidOperationException>(
            dataContext, null!));
        AssertArgument("event", () => context.CreateProxy(null!));
        AssertArgument("event", () => context.CreateProxy<ProxyData>(null!, data));
        AssertArgument("data", () => context.CreateProxy(fixture.Machine.DataSignal, null!));
        AssertArgument("event", () => dataContext.CreateProxy(null!));
        AssertArgument("event", () => dataContext.CreateProxy<ProxyData>(null!, data));
        AssertArgument("data", () => dataContext.CreateProxy(fixture.Machine.DataSignal, null!));
    }

    [Theory]
    [InlineData(ObserverCall.Pre, false)]
    [InlineData(ObserverCall.Pre, true)]
    [InlineData(ObserverCall.Post, false)]
    [InlineData(ObserverCall.Post, true)]
    [InlineData(ObserverCall.Fault, false)]
    [InlineData(ObserverCall.Fault, true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-OBSERVATION", "selected-observer-typed-untyped-forwarding-filter-and-task-identity")]
    public async Task SelectedObserver_ForwardsOnlyMatchingEventsAndPreservesObserverTasksAsync(ObserverCall call, bool typed)
    {
        using BehaviorFixture fixture = await BehaviorFixture.CreateAsync();
        IBehaviorContext<ProxySaga> context = new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorContextProxy(
            fixture.Machine,
            fixture.SagaContext,
            fixture.Machine.Signal);
        IBehaviorContext<ProxySaga, ProxyData> dataContext = context.CreateProxy(fixture.Machine.DataSignal, new ProxyData());
        var observer = new RecordingEventObserver();
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        observer.NextTask = completion.Task;
        IEvent selectedEvent = typed ? fixture.Machine.DataSignal : fixture.Machine.Signal;
        var selected = new ViciOneServiceBusStateMachine<ProxySaga>.SelectedEventObserver(selectedEvent, observer);
        var failure = new InvalidOperationException("fault");

        Task forwarded = Invoke(selected, call, typed, context, dataContext, failure);

        Assert.Same(completion.Task, forwarded);
        Assert.Equal([CallName(call, typed)], observer.Calls);
        Assert.Same(typed ? dataContext : context, observer.LastContext);
        if (call == ObserverCall.Fault)
            Assert.Same(failure, observer.LastException);
        completion.SetResult(true);
        await forwarded;

        observer.Calls.Clear();
        var mismatch = new ViciOneServiceBusStateMachine<ProxySaga>.SelectedEventObserver(fixture.Machine.Other, observer);
        Task skipped = Invoke(mismatch, call, typed, context, dataContext, failure);
        Assert.Same(Task.CompletedTask, skipped);
        Assert.Empty(observer.Calls);
    }

    [Theory]
    [InlineData(ObserverCall.Pre, false)]
    [InlineData(ObserverCall.Pre, true)]
    [InlineData(ObserverCall.Post, false)]
    [InlineData(ObserverCall.Post, true)]
    [InlineData(ObserverCall.Fault, false)]
    [InlineData(ObserverCall.Fault, true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-OBSERVATION", "selected-observer-null-task-and-argument-boundary-matrix")]
    public async Task SelectedObserver_RejectsNullTasksAndRequiredArguments(ObserverCall call, bool typed)
    {
        using BehaviorFixture fixture = await BehaviorFixture.CreateAsync();
        IBehaviorContext<ProxySaga> context = new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorContextProxy(
            fixture.Machine,
            fixture.SagaContext,
            fixture.Machine.Signal);
        IBehaviorContext<ProxySaga, ProxyData> dataContext = context.CreateProxy(fixture.Machine.DataSignal, new ProxyData());
        var observer = new RecordingEventObserver { NextTask = null };
        IEvent selectedEvent = typed ? fixture.Machine.DataSignal : fixture.Machine.Signal;
        var selected = new ViciOneServiceBusStateMachine<ProxySaga>.SelectedEventObserver(selectedEvent, observer);
        var failure = new InvalidOperationException("fault");

        InvalidOperationException nullTask = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = Invoke(selected, call, typed, context, dataContext, failure);
        });
        Assert.Contains("no notification task", nullTask.Message, StringComparison.Ordinal);

        AssertArgument("context", () => Invoke(selected, call, typed, null!, null!, failure));
        if (call == ObserverCall.Fault)
            AssertArgument("exception", () => Invoke(selected, call, typed, context, dataContext, null!));
        AssertArgument("event", () => new ViciOneServiceBusStateMachine<ProxySaga>.SelectedEventObserver(null!, observer));
        AssertArgument("observer", () => new ViciOneServiceBusStateMachine<ProxySaga>.SelectedEventObserver(selectedEvent, null!));
    }

    [Theory]
    [InlineData(ObserverCall.Pre, false)]
    [InlineData(ObserverCall.Pre, true)]
    [InlineData(ObserverCall.Post, false)]
    [InlineData(ObserverCall.Post, true)]
    [InlineData(ObserverCall.Fault, false)]
    [InlineData(ObserverCall.Fault, true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-OBSERVATION", "event-observable-fans-out-all-notification-shapes")]
    public async Task EventObservable_FansOutEveryNotificationShapeAsync(ObserverCall call, bool typed)
    {
        using BehaviorFixture fixture = await BehaviorFixture.CreateAsync();
        IBehaviorContext<ProxySaga> context = new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorContextProxy(
            fixture.Machine,
            fixture.SagaContext,
            fixture.Machine.Signal);
        IBehaviorContext<ProxySaga, ProxyData> dataContext = context.CreateProxy(fixture.Machine.DataSignal, new ProxyData());
        var first = new RecordingEventObserver();
        var second = new RecordingEventObserver();
        var observable = new ViciOneServiceBusStateMachine<ProxySaga>.EventObservable();
        using IDisposable firstHandle = observable.Connect(first);
        using IDisposable secondHandle = observable.Connect(second);
        var failure = new InvalidOperationException("fault");

        await Invoke(observable, call, typed, context, dataContext, failure);

        Assert.Equal([CallName(call, typed)], first.Calls);
        Assert.Equal([CallName(call, typed)], second.Calls);
        Assert.Same(typed ? dataContext : context, first.LastContext);
        Assert.Same(typed ? dataContext : context, second.LastContext);
        if (call == ObserverCall.Fault)
        {
            Assert.Same(failure, first.LastException);
            Assert.Same(failure, second.LastException);
        }
    }

    [Theory]
    [InlineData(ObserverCall.Pre, false, ObserverTaskOutcome.Incomplete)]
    [InlineData(ObserverCall.Pre, true, ObserverTaskOutcome.Incomplete)]
    [InlineData(ObserverCall.Post, false, ObserverTaskOutcome.Incomplete)]
    [InlineData(ObserverCall.Post, true, ObserverTaskOutcome.Incomplete)]
    [InlineData(ObserverCall.Fault, false, ObserverTaskOutcome.Incomplete)]
    [InlineData(ObserverCall.Fault, true, ObserverTaskOutcome.Incomplete)]
    [InlineData(ObserverCall.Pre, false, ObserverTaskOutcome.Faulted)]
    [InlineData(ObserverCall.Pre, true, ObserverTaskOutcome.Faulted)]
    [InlineData(ObserverCall.Post, false, ObserverTaskOutcome.Faulted)]
    [InlineData(ObserverCall.Post, true, ObserverTaskOutcome.Faulted)]
    [InlineData(ObserverCall.Fault, false, ObserverTaskOutcome.Faulted)]
    [InlineData(ObserverCall.Fault, true, ObserverTaskOutcome.Faulted)]
    [InlineData(ObserverCall.Pre, false, ObserverTaskOutcome.Canceled)]
    [InlineData(ObserverCall.Pre, true, ObserverTaskOutcome.Canceled)]
    [InlineData(ObserverCall.Post, false, ObserverTaskOutcome.Canceled)]
    [InlineData(ObserverCall.Post, true, ObserverTaskOutcome.Canceled)]
    [InlineData(ObserverCall.Fault, false, ObserverTaskOutcome.Canceled)]
    [InlineData(ObserverCall.Fault, true, ObserverTaskOutcome.Canceled)]
    [RequirementCoverage(
        "REQ-VSB-STATE-MACHINE-OBSERVATION",
        "event-observable-awaits-and-propagates-incomplete-faulted-and-canceled-observer-tasks")]
    public async Task EventObservable_AwaitsAndPropagatesEveryObserverTaskAsync(
        ObserverCall call,
        bool typed,
        ObserverTaskOutcome outcome)
    {
        using BehaviorFixture fixture = await BehaviorFixture.CreateAsync();
        IBehaviorContext<ProxySaga> context = new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorContextProxy(
            fixture.Machine,
            fixture.SagaContext,
            fixture.Machine.Signal);
        IBehaviorContext<ProxySaga, ProxyData> dataContext = context.CreateProxy(fixture.Machine.DataSignal, new ProxyData());
        var first = new RecordingEventObserver();
        var second = new RecordingEventObserver();
        var incomplete = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var expectedFailure = new InvalidOperationException("observer failure");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        second.NextTask = outcome switch
        {
            ObserverTaskOutcome.Incomplete => incomplete.Task,
            ObserverTaskOutcome.Faulted => Task.FromException(expectedFailure),
            _ => Task.FromCanceled(cancellation.Token),
        };
        var observable = new ViciOneServiceBusStateMachine<ProxySaga>.EventObservable();
        using IDisposable firstHandle = observable.Connect(first);
        using IDisposable secondHandle = observable.Connect(second);
        var notificationFailure = new InvalidOperationException("notification failure");

        Task notification = Invoke(observable, call, typed, context, dataContext, notificationFailure);

        Assert.Equal([CallName(call, typed)], first.Calls);
        Assert.Equal([CallName(call, typed)], second.Calls);
        switch (outcome)
        {
            case ObserverTaskOutcome.Incomplete:
                Assert.False(notification.IsCompleted);
                incomplete.SetResult(true);
                await notification;
                Assert.True(notification.IsCompletedSuccessfully);
                break;
            case ObserverTaskOutcome.Faulted:
                InvalidOperationException propagated = await Assert.ThrowsAsync<InvalidOperationException>(() => notification);
                Assert.Same(expectedFailure, propagated);
                Assert.True(notification.IsFaulted);
                break;
            default:
                OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => notification);
                Assert.Equal(cancellation.Token, canceled.CancellationToken);
                Assert.True(notification.IsCanceled);
                break;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-OBSERVATION", "state-observable-preserves-context-state-and-task-identity")]
    public async Task StateObservable_PreservesContextStatesAndSingleObserverTaskIdentityAsync()
    {
        using BehaviorFixture fixture = await BehaviorFixture.CreateAsync();
        IBehaviorContext<ProxySaga> context = new ViciOneServiceBusStateMachine<ProxySaga>.BehaviorContextProxy(
            fixture.Machine,
            fixture.SagaContext,
            fixture.Machine.Signal);
        var observer = new RecordingStateObserver();
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        observer.NextTask = completion.Task;
        var observable = new ViciOneServiceBusStateMachine<ProxySaga>.StateObservable();
        using IDisposable handle = observable.Connect(observer);

        Task returned = observable.StateChangedAsync(context, fixture.Machine.Final, fixture.Machine.Initial);

        Assert.Same(completion.Task, returned);
        Assert.Same(context, observer.Context);
        Assert.Same(fixture.Machine.Final, observer.Current);
        Assert.Same(fixture.Machine.Initial, observer.Previous);
        completion.SetResult(true);
        await returned;

        AssertArgument("context", () => observable.StateChangedAsync(null!, fixture.Machine.Final, null));
        AssertArgument("currentState", () => observable.StateChangedAsync(context, null!, null));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-OBSERVATION", "nontransition-observer-null-task-is-deterministic")]
    public async Task NonTransitionObserver_RejectsANullNotificationTaskDeterministicallyAsync()
    {
        var machine = new ProxyMachine();
        var observer = new RecordingEventObserver { NextTask = null };
        using IDisposable handle = machine.ConnectEventObserver(observer);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => StateMachineTestExecution.RaiseAsync(machine, new ProxySaga(), machine.Signal));

        Assert.Contains("no notification task", exception.Message, StringComparison.Ordinal);
        Assert.Equal(["pre", "fault"], observer.Calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-OBSERVATION", "public-event-observer-forwards-typed-nontransition-context")]
    public async Task ConnectEventObserver_ForwardsTypedNonTransitionContextThroughPublicApiAsync()
    {
        var machine = new ProxyMachine();
        var observer = new RecordingEventObserver();
        using IDisposable handle = machine.ConnectEventObserver(observer);
        var instance = new ProxySaga();
        var data = new ProxyData("observed");

        await StateMachineTestExecution.RaiseAsync(machine, instance, machine.DataSignal, data);

        Assert.Equal(["pre-typed", "post-typed"], observer.Calls);
        var observed = Assert.IsAssignableFrom<IBehaviorContext<ProxySaga, ProxyData>>(observer.LastContext);
        Assert.Same(machine, observed.StateMachine);
        Assert.Same(instance, observed.Saga);
        Assert.Same(machine.DataSignal, observed.Event);
        Assert.Same(data, observed.Message);
        Assert.Null(observer.LastException);
    }

    [Fact]
    [RequirementCoverage(
        "REQ-VSB-STATE-MACHINE-OBSERVATION",
        "public-nontransition-observer-skips-typed-transition-pre-post-and-fault-notifications")]
    public async Task ConnectEventObserver_SkipsTypedTransitionNotificationsThroughPublicApiAsync()
    {
        var successfulMachine = new TransitionObserverMachine();
        var successfulObserver = new RecordingEventObserver();
        using IDisposable successfulHandle = successfulMachine.ConnectEventObserver(successfulObserver);

        await StateMachineTestExecution.RaiseAsync(successfulMachine, new ProxySaga(), successfulMachine.Start);

        Assert.Equal(1, successfulMachine.BeforeEnterExecutions);
        Assert.Equal(["pre", "post"], successfulObserver.Calls);
        Assert.DoesNotContain(successfulObserver.Calls, call => call.EndsWith("-typed", StringComparison.Ordinal));

        var expectedFailure = new InvalidOperationException("typed transition failure");
        var faultedMachine = new TransitionObserverMachine(expectedFailure);
        var faultedObserver = new RecordingEventObserver();
        using IDisposable faultedHandle = faultedMachine.ConnectEventObserver(faultedObserver);

        InvalidOperationException propagated = await Assert.ThrowsAsync<InvalidOperationException>(
            () => StateMachineTestExecution.RaiseAsync(faultedMachine, new ProxySaga(), faultedMachine.Start));

        Assert.Same(expectedFailure, propagated);
        Assert.Equal(1, faultedMachine.BeforeEnterExecutions);
        Assert.Equal(["pre", "fault"], faultedObserver.Calls);
        Assert.DoesNotContain(faultedObserver.Calls, call => call.EndsWith("-typed", StringComparison.Ordinal));
        Assert.Same(expectedFailure, faultedObserver.LastException);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "unhandled-context-ignore-throw-and-cancellation-task-shapes")]
    public async Task UnhandledContext_ExposesStateAndReturnsExactTerminalTaskShapesAsync()
    {
        var machine = new ProxyMachine();
        IUnhandledEventContext<ProxySaga>? captured = null;
        machine.UseUnhandled(context =>
        {
            captured = context;
            return context.IgnoreAsync();
        });
        var instance = new ProxySaga();

        await StateMachineTestExecution.RaiseAsync(machine, instance, machine.Other);

        IUnhandledEventContext<ProxySaga> context = Assert.IsAssignableFrom<IUnhandledEventContext<ProxySaga>>(captured);
        Assert.Same(instance, context.Saga);
        Assert.Same(machine.Other, context.Event);
        Assert.Same(machine.Initial, context.CurrentState);
        Assert.Same(Task.CompletedTask, context.IgnoreAsync(TestContext.Current.CancellationToken));

        Task rejected = context.ThrowAsync(TestContext.Current.CancellationToken);
        Assert.True(rejected.IsFaulted);
        UnhandledEventException failure = await Assert.ThrowsAsync<UnhandledEventException>(() => rejected);
        string machineName = ((IStateMachine<ProxySaga>)machine).Name;
        Assert.Equal(
            $"The {machine.Other.Name} event is not handled during the {machine.Initial.Name} state for the {machineName} state machine",
            failure.Message);
        Assert.Equal(machineName, failure.MachineName);
        Assert.Equal(machine.Other.Name, failure.EventName);
        Assert.Equal(machine.Initial.Name, failure.StateName);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        OperationCanceledException ignoredCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.IgnoreAsync(cancellation.Token));
        OperationCanceledException rejectedCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.ThrowAsync(cancellation.Token));
        Assert.Equal(cancellation.Token, ignoredCancellation.CancellationToken);
        Assert.Equal(cancellation.Token, rejectedCancellation.CancellationToken);
    }

    private static Task Invoke(
        IEventObserver<ProxySaga> observer,
        ObserverCall call,
        bool typed,
        IBehaviorContext<ProxySaga> context,
        IBehaviorContext<ProxySaga, ProxyData> dataContext,
        Exception failure) =>
        (call, typed) switch
        {
            (ObserverCall.Pre, false) => observer.PreExecuteAsync(context),
            (ObserverCall.Pre, true) => observer.PreExecuteAsync(dataContext),
            (ObserverCall.Post, false) => observer.PostExecuteAsync(context),
            (ObserverCall.Post, true) => observer.PostExecuteAsync(dataContext),
            (ObserverCall.Fault, false) => observer.ExecuteFaultAsync(context, failure),
            _ => observer.ExecuteFaultAsync(dataContext, failure),
        };

    private static string CallName(ObserverCall call, bool typed) =>
        $"{call.ToString().ToLowerInvariant()}{(typed ? "-typed" : string.Empty)}";

    private static void AssertArgument(string parameterName, Action action)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    public enum ObserverCall
    {
        Pre,
        Post,
        Fault,
    }

    public enum ObserverTaskOutcome
    {
        Incomplete,
        Faulted,
        Canceled,
    }

    private sealed class BehaviorFixture : IDisposable
    {
        BehaviorFixture(
            ProxyMachine machine,
            ProxySaga instance,
            InMemorySagaConsumeContext<ProxySaga, StateMachineSignal> sagaContext)
        {
            Machine = machine;
            Instance = instance;
            SagaContext = sagaContext;
        }

        public ProxyMachine Machine { get; }
        public ProxySaga Instance { get; }
        public InMemorySagaConsumeContext<ProxySaga, StateMachineSignal> SagaContext { get; }

        public static async Task<BehaviorFixture> CreateAsync()
        {
            var machine = new ProxyMachine();
            var instance = new ProxySaga();
            ConsumeContext<StateMachineSignal> consumeContext = InMemoryOutboxTestContextFactory.Create(new StateMachineSignal());
            var sagaInstance = new SagaInstance<ProxySaga>(instance);
            await sagaInstance.MarkInUseAsync(consumeContext.CancellationToken);
            var sagaContext = new InMemorySagaConsumeContext<ProxySaga, StateMachineSignal>(consumeContext, sagaInstance);
            return new BehaviorFixture(machine, instance, sagaContext);
        }

        public void Dispose() => SagaContext.Dispose();
    }

    private sealed class RecordingEventObserver : IEventObserver<ProxySaga>
    {
        public List<string> Calls { get; } = [];
        public object? LastContext { get; private set; }
        public Exception? LastException { get; private set; }
        public Task? NextTask { get; set; } = Task.CompletedTask;

        public Task PreExecuteAsync(IBehaviorContext<ProxySaga> context) => Record("pre", context);
        public Task PreExecuteAsync<T>(IBehaviorContext<ProxySaga, T> context) where T : class => Record("pre-typed", context);
        public Task PostExecuteAsync(IBehaviorContext<ProxySaga> context) => Record("post", context);
        public Task PostExecuteAsync<T>(IBehaviorContext<ProxySaga, T> context) where T : class => Record("post-typed", context);
        public Task ExecuteFaultAsync(IBehaviorContext<ProxySaga> context, Exception exception) => Record("fault", context, exception);
        public Task ExecuteFaultAsync<T>(IBehaviorContext<ProxySaga, T> context, Exception exception) where T : class =>
            Record("fault-typed", context, exception);

        Task Record(string call, object context, Exception? exception = null)
        {
            Calls.Add(call);
            LastContext = context;
            LastException = exception;
            return NextTask!;
        }
    }

    private sealed class RecordingStateObserver : IStateObserver<ProxySaga>
    {
        public IBehaviorContext<ProxySaga>? Context { get; private set; }
        public IState? Current { get; private set; }
        public IState? Previous { get; private set; }
        public Task NextTask { get; set; } = Task.CompletedTask;

        public Task StateChangedAsync(IBehaviorContext<ProxySaga> context, IState currentState, IState? previousState)
        {
            Context = context;
            Current = currentState;
            Previous = previousState;
            return NextTask;
        }
    }

    private sealed class ProxyMachine : ViciOneServiceBusStateMachine<ProxySaga>
    {
        public ProxyMachine()
        {
            During(Initial, When(Signal).Then(_ => SignalExecutions++));
            During(Initial, When(DataSignal).Then(context => DataExecutions.Add(context.Message)));
        }

        public IEvent Signal { get; private set; } = null!;
        public IEvent Other { get; private set; } = null!;
        public IEvent<ProxyData> DataSignal { get; private set; } = null!;
        public IEvent<ReplacementProxyData> ReplacementDataSignal { get; private set; } = null!;
        public int SignalExecutions { get; private set; }
        public List<ProxyData> DataExecutions { get; } = [];

        public void UseUnhandled(UnhandledEventCallback<ProxySaga> callback) => OnUnhandledEvent(callback);
    }

    private sealed class TransitionObserverMachine : ViciOneServiceBusStateMachine<ProxySaga>
    {
        public TransitionObserverMachine(Exception? beforeEnterFailure = null)
        {
            During(Initial, When(Start).TransitionTo(Running));
            BeforeEnter(Running, behavior => behavior.Then(_ =>
            {
                BeforeEnterExecutions++;
                if (beforeEnterFailure != null)
                    throw beforeEnterFailure;
            }));
        }

        public IState Running { get; private set; } = null!;
        public IEvent Start { get; private set; } = null!;
        public int BeforeEnterExecutions { get; private set; }
    }

    private sealed class ProxySaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public IState? CurrentState { get; set; }
    }

    public sealed record ProxyData(string Value = "data");

    private sealed record ReplacementProxyData(string Value);

    public sealed record InitializedProxyMessage
    {
        public string Value { get; init; } = string.Empty;
    }
}
