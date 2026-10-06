using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaStateQueryRuntimeExtensionDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-STATE-QUERY-RUNTIME-EXTENSIONS", "public-overloads-reject-null-at-the-immediate-boundary")]
    public void PublicOverloads_RejectNullAtTheImmediateBoundaryBeforeCollaboratorAccess()
    {
        var runtimeMachine = new RuntimeMachine();
        var accessor = new RecordingAccessor
        {
            StateExpression = saga => saga.CurrentState == runtimeMachine.Running.Name,
        };
        (IStateMachine<RuntimeSaga> machine, MachineProxy machineControl) = CreateMachine(accessor);
        (IBehaviorContext<RuntimeSaga> context, BehaviorContextProxy contextControl) = CreateContext(machine);
        Expression<Func<RuntimeSaga, bool>> expression = saga => saga.Score > 0;

        Assert.Equal("machine", Assert.Throws<ArgumentNullException>(() =>
            SagaStateMachineExtensions.CreateSagaQuery<RuntimeSaga>(null!, expression, runtimeMachine.Running)).ParamName);
        Assert.Equal("expression", Assert.Throws<ArgumentNullException>(() =>
            machine.CreateSagaQuery(null!, runtimeMachine.Running)).ParamName);
        Assert.Equal("states", Assert.Throws<ArgumentNullException>(() =>
            machine.CreateSagaQuery(expression, (IState[])null!)).ParamName);
        Assert.Equal("states", Assert.Throws<ArgumentException>(() =>
            machine.CreateSagaQuery(expression, runtimeMachine.Running, null!)).ParamName);

        Assert.Equal("machine", Assert.Throws<ArgumentNullException>(() =>
            SagaStateMachineExtensions.CreateSagaFilter<RuntimeSaga>(null!, expression, runtimeMachine.Running)).ParamName);
        Assert.Equal("expression", Assert.Throws<ArgumentNullException>(() =>
            machine.CreateSagaFilter(null!, runtimeMachine.Running)).ParamName);
        Assert.Equal("states", Assert.Throws<ArgumentNullException>(() =>
            machine.CreateSagaFilter(expression, (IState[])null!)).ParamName);
        Assert.Equal("states", Assert.Throws<ArgumentException>(() =>
            machine.CreateSagaFilter(expression, runtimeMachine.Running, null!)).ParamName);

        Assert.Equal("accessor", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = StateAccessorExtensions.GetStateAsync<RuntimeSaga>(
                (IStateAccessor<RuntimeSaga>)null!, context, TestContext.Current.CancellationToken);
        }).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = accessor.GetStateAsync(null!, TestContext.Current.CancellationToken);
        }).ParamName);
        Assert.Equal("accessor", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = StateAccessorExtensions.GetStateAsync<RuntimeSaga>(
                (IStateMachine<RuntimeSaga>)null!, context, TestContext.Current.CancellationToken);
        }).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = machine.GetStateAsync(null!, TestContext.Current.CancellationToken);
        }).ParamName);

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = StateMachineExtensions.TransitionToStateAsync<RuntimeSaga>(
                null!, runtimeMachine.Running, TestContext.Current.CancellationToken);
        }).ParamName);
        Assert.Equal("state", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = context.TransitionToStateAsync(null!, TestContext.Current.CancellationToken);
        }).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = StateMachineIntrospectionExtensions.NextEventsAsync<RuntimeSaga>(
                null!, TestContext.Current.CancellationToken);
        }).ParamName);

        Assert.Equal(0, machineControl.AccessorGets);
        Assert.Empty(accessor.GetCalls);
        Assert.Empty(accessor.StateExpressionCalls);
        Assert.Equal(0, contextControl.InvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-STATE-QUERY-RUNTIME-EXTENSIONS", "query-and-filter-compose-predicate-with-the-same-state-expression")]
    public void QueryAndFilter_ComposePredicateAndStateExpressionWithEquivalentResultsAndExactStates()
    {
        var runtimeMachine = new RuntimeMachine();
        IState[] states = [runtimeMachine.Initial, runtimeMachine.Running];
        var accessor = new RecordingAccessor
        {
            StateExpression = saga => saga.CurrentState == runtimeMachine.Initial.Name
                || saga.CurrentState == runtimeMachine.Running.Name,
        };
        (IStateMachine<RuntimeSaga> machine, MachineProxy machineControl) = CreateMachine(accessor);
        Expression<Func<RuntimeSaga, bool>> predicate = saga => saga.Score >= 10;

        ISagaQuery<RuntimeSaga> query = machine.CreateSagaQuery(predicate, states);
        Func<RuntimeSaga, bool> filter = machine.CreateSagaFilter(predicate, states);
        Func<RuntimeSaga, bool> queryExpression = query.FilterExpression.Compile();
        Func<RuntimeSaga, bool> queryFilter = query.GetFilter();

        var cases = new (RuntimeSaga Saga, bool Expected)[]
        {
            (new RuntimeSaga { CurrentState = runtimeMachine.Initial.Name, Score = 10 }, true),
            (new RuntimeSaga { CurrentState = runtimeMachine.Running.Name, Score = 42 }, true),
            (new RuntimeSaga { CurrentState = runtimeMachine.Final.Name, Score = 42 }, false),
            (new RuntimeSaga { CurrentState = runtimeMachine.Running.Name, Score = 9 }, false),
        };
        foreach ((RuntimeSaga saga, bool expected) in cases)
        {
            Assert.Equal(expected, queryExpression(saga));
            Assert.Equal(expected, queryFilter(saga));
            Assert.Equal(expected, filter(saga));
        }

        Assert.Equal(2, machineControl.AccessorGets);
        Assert.Collection(
            accessor.StateExpressionCalls,
            actual => Assert.Same(states, actual),
            actual => Assert.Same(states, actual));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-STATE-QUERY-RUNTIME-EXTENSIONS", "accessor-and-machine-overloads-forward-context-token-result-and-task")]
    public async Task GetStateOverloads_ForwardExactContextTokenResultAndTaskAsync()
    {
        var runtimeMachine = new RuntimeMachine();
        Task<IState<RuntimeSaga>?> expectedTask =
            Task.FromResult<IState<RuntimeSaga>?>((IState<RuntimeSaga>)runtimeMachine.Running);
        var accessor = new RecordingAccessor { ResultTask = expectedTask };
        (IStateMachine<RuntimeSaga> machine, MachineProxy machineControl) = CreateMachine(accessor);
        (IBehaviorContext<RuntimeSaga> context, _) = CreateContext(machine);
        using var cancellation = new CancellationTokenSource();

        Task<IState<RuntimeSaga>?> accessorTask = accessor.GetStateAsync(context, cancellation.Token);
        Task<IState<RuntimeSaga>?> machineTask = machine.GetStateAsync(context, cancellation.Token);

        Assert.Same(expectedTask, accessorTask);
        Assert.Same(expectedTask, machineTask);
        Assert.Same(runtimeMachine.Running, await accessorTask);
        Assert.Same(runtimeMachine.Running, await machineTask);
        Assert.Equal(1, machineControl.AccessorGets);
        Assert.Collection(
            accessor.GetCalls,
            call =>
            {
                Assert.Same(context, call.Context);
                Assert.Equal(cancellation.Token, call.CancellationToken);
            },
            call =>
            {
                Assert.Same(context, call.Context);
                Assert.Equal(cancellation.Token, call.CancellationToken);
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-STATE-QUERY-RUNTIME-EXTENSIONS", "transition-pre-cancellation-short-circuits-with-the-original-token")]
    public async Task Transition_PreCanceledTokenReturnsCanceledTaskWithoutTouchingContextAsync()
    {
        var runtimeMachine = new RuntimeMachine();
        (IBehaviorContext<RuntimeSaga> context, BehaviorContextProxy contextControl) = CreateContext(null, throwOnInvocation: true);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Task transition = context.TransitionToStateAsync(runtimeMachine.Running, cancellation.Token);

        Assert.True(transition.IsCanceled);
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transition);
        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(0, contextControl.InvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-STATE-QUERY-RUNTIME-EXTENSIONS", "transition-resolves-machine-state-and-uses-its-enter-proxy")]
    public async Task Transition_ResolvesTheMachineOwnedStateAndRunsItsEnterEventOnTheSameSagaAsync()
    {
        var machine = new RuntimeMachine();
        var saga = new RuntimeSaga();
        var alias = new NameOnlyState(machine.Running.Name);

        await WithBehaviorContextAsync(
            machine,
            saga,
            context => context.TransitionToStateAsync(alias, TestContext.Current.CancellationToken));

        Assert.Equal(machine.Running.Name, saga.CurrentState);
        Assert.Same(machine.Running.Enter, saga.ObservedEnterEvent);
        Assert.NotNull(saga.ObservedEnterContext);
        Assert.Same(machine, saga.ObservedEnterContext.StateMachine);
        Assert.Same(saga, saga.ObservedEnterContext.Saga);
        Assert.Same(machine.Running.Enter, saga.ObservedEnterContext.Event);
        Assert.Equal(1, alias.NameReads);
        Assert.Equal(0, alias.LifecycleReads);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-STATE-QUERY-RUNTIME-EXTENSIONS", "next-events-forwards-context-token-state-and-event-enumerable")]
    public async Task NextEvents_ForwardsAccessorStateAndReturnsTheMachineEventSequenceAsync()
    {
        var runtimeMachine = new RuntimeMachine();
        IEvent[] expectedEvents = [new TriggerEvent("First"), new TriggerEvent("Second")];
        var accessor = new RecordingAccessor
        {
            ResultTask = Task.FromResult<IState<RuntimeSaga>?>((IState<RuntimeSaga>)runtimeMachine.Running),
        };
        (IStateMachine<RuntimeSaga> machine, MachineProxy machineControl) = CreateMachine(accessor);
        machineControl.NextEventsResult = expectedEvents;
        (IBehaviorContext<RuntimeSaga> context, BehaviorContextProxy contextControl) = CreateContext(machine);
        using var cancellation = new CancellationTokenSource();

        IEnumerable<IEvent> actual = await context.NextEventsAsync(cancellation.Token);

        Assert.Same(expectedEvents, actual);
        StateReadCall read = Assert.Single(accessor.GetCalls);
        Assert.Same(context, read.Context);
        Assert.Equal(cancellation.Token, read.CancellationToken);
        Assert.Same(runtimeMachine.Running, Assert.Single(machineControl.NextEventStates));
        Assert.Equal(1, machineControl.AccessorGets);
        Assert.Equal(1, contextControl.InvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-STATE-QUERY-RUNTIME-EXTENSIONS", "next-events-null-state-has-stable-diagnostic-and-no-machine-enumeration")]
    public async Task NextEvents_NullAccessorStateFailsWithTheStableDiagnosticBeforeMachineEnumerationAsync()
    {
        var accessor = new RecordingAccessor
        {
            ResultTask = Task.FromResult<IState<RuntimeSaga>?>(null),
        };
        (IStateMachine<RuntimeSaga> machine, MachineProxy machineControl) = CreateMachine(accessor);
        (IBehaviorContext<RuntimeSaga> context, _) = CreateContext(machine);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.NextEventsAsync(TestContext.Current.CancellationToken));

        Assert.Equal("The state machine accessor did not resolve a current state.", exception.Message);
        Assert.Single(accessor.GetCalls);
        Assert.Empty(machineControl.NextEventStates);
        Assert.Equal(1, machineControl.AccessorGets);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-SAGA-STATE-QUERY-RUNTIME-EXTENSIONS", "transition-live-caller-token-cooperates-before-state-mutation")]
    public async Task Transition_LiveCallerTokenReachesCooperativeAccessorBeforeMutationAsync(int mode)
    {
        var machine = new RuntimeMachine();
        var saga = new RuntimeSaga();
        using var caller = new CancellationTokenSource();
        CancellationToken callerToken = mode == 0 ? CancellationToken.None : caller.Token;
        CancellationToken contextToken = mode == 1 ? caller.Token : CancellationToken.None;
        var accessor = new HeldTransitionAccessor(((IStateMachine<RuntimeSaga>)machine).Accessor);
        IStateMachine<RuntimeSaga> selectedMachine =
            DispatchProxy.Create<IStateMachine<RuntimeSaga>, AccessorOverrideMachineProxy>();
        var selectedControl = (AccessorOverrideMachineProxy)(object)selectedMachine;
        selectedControl.Machine = machine;
        selectedControl.Accessor = accessor;
        ConsumeContext<RuntimeSignal> consumeContext =
            InMemoryOutboxTestContextFactory.Create(new RuntimeSignal(), cancellationToken: contextToken);
        var sagaInstance = new SagaInstance<RuntimeSaga>(saga);
        await sagaInstance.MarkInUseAsync(contextToken);
        using var sagaContext = new InMemorySagaConsumeContext<RuntimeSaga, RuntimeSignal>(consumeContext, sagaInstance);
        IBehaviorContext<RuntimeSaga> context =
            new ViciOneServiceBusStateMachine<RuntimeSaga>.BehaviorContextProxy(selectedMachine, sagaContext, machine.Initial.Enter);
        Task? operation = null;
        try
        {
            operation = context.TransitionToStateAsync(machine.Running, callerToken);
            await accessor.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

            Assert.Equal(1, accessor.ReadCalls);
            Assert.NotNull(accessor.ReadContext);
            Assert.Same(saga, accessor.ReadContext.Saga);
            Assert.Same(selectedMachine, accessor.ReadContext.StateMachine);
            Assert.Same(machine.Running.Enter, accessor.ReadContext.Event);
            Assert.Equal(contextToken, context.CancellationToken);
            Assert.Equal(string.Empty, saga.CurrentState);
            Assert.Equal(0, accessor.SetCalls);
            Assert.Null(saga.ObservedEnterContext);
            Assert.NotNull(accessor.ReadTask);
            Assert.False(accessor.ReadTask.IsCompleted);
            Assert.False(operation.IsCompleted);
            Assert.Equal(callerToken, accessor.ReadToken);

            if (mode == 2)
            {
                caller.Cancel();
                OperationCanceledException error = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => operation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
                Assert.Equal(caller.Token, error.CancellationToken);
                Assert.True(operation.IsCanceled);
                Assert.Equal(0, accessor.SetCalls);
                Assert.Equal(string.Empty, saga.CurrentState);
                Assert.Null(saga.ObservedEnterContext);
            }
            else
            {
                accessor.Release.TrySetResult();
                await operation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                Assert.Equal(1, accessor.SetCalls);
                Assert.Equal(callerToken, accessor.SetToken);
                Assert.Equal(machine.Running.Name, saga.CurrentState);
                Assert.Same(machine.Running.Enter, saga.ObservedEnterEvent);
                Assert.NotNull(saga.ObservedEnterContext);
                Assert.Same(saga, saga.ObservedEnterContext.Saga);
                Assert.Same(selectedMachine, saga.ObservedEnterContext.StateMachine);
                Assert.Equal(callerToken, saga.ObservedEnterContext.CancellationToken);
            }
        }
        finally
        {
            accessor.Release.TrySetResult();
            try
            {
                if (accessor.ReadTask is not null)
                    await ObserveTransitionTaskAsync(accessor.ReadTask);
            }
            finally
            {
                if (operation is not null)
                    await ObserveTransitionTaskAsync(operation);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-STATE-QUERY-RUNTIME-EXTENSIONS", "transition-selected-live-token-reaches-lifecycle-and-nested-contexts")]
    public async Task Transition_SelectedLiveTokenReachesBeforeEnterAndItsNestedContextsAsync(bool cancelBeforeEnter)
    {
        var machine = new LifecycleTokenMachine();
        var saga = new RuntimeSaga { CurrentState = machine.Initial.Name };
        using var caller = new CancellationTokenSource();
        using var consumed = new CancellationTokenSource();
        ConsumeContext<RuntimeSignal> consumeContext =
            InMemoryOutboxTestContextFactory.Create(new RuntimeSignal(), cancellationToken: consumed.Token);
        var sagaInstance = new SagaInstance<RuntimeSaga>(saga);
        await sagaInstance.MarkInUseAsync(TestContext.Current.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<RuntimeSaga, RuntimeSignal>(consumeContext, sagaInstance);
        IBehaviorContext<RuntimeSaga> context =
            new ViciOneServiceBusStateMachine<RuntimeSaga>.BehaviorContextProxy(machine, sagaContext, machine.Initial.Enter);
        Task? operation = null;
        try
        {
            Assert.NotEqual(caller.Token, consumed.Token);
            operation = context.TransitionToStateAsync(machine.Running, caller.Token);
            await machine.BeforeEnterEntered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

            Assert.Equal(1, machine.BeforeEnterCalls);
            Assert.NotNull(machine.BeforeEnterContext);
            Assert.Same(saga, machine.BeforeEnterContext.Saga);
            Assert.Same(machine, machine.BeforeEnterContext.StateMachine);
            Assert.Same(machine.Running.BeforeEnter, machine.BeforeEnterContext.Event);
            Assert.Same(machine.Running, machine.BeforeEnterContext.Message);
            Assert.Equal(machine.Initial.Name, saga.CurrentState);
            Assert.Null(saga.ObservedEnterContext);
            Assert.NotNull(machine.BeforeEnterTask);
            Assert.False(machine.BeforeEnterTask.IsCompleted);
            Assert.False(operation.IsCompleted);
            Assert.Equal(consumed.Token, context.CancellationToken);
            Assert.Equal(consumed.Token, sagaContext.CancellationToken);
            Assert.Equal(caller.Token, machine.BeforeEnterContext.CancellationToken);
            Assert.NotNull(machine.NestedUntypedContext);
            Assert.Same(saga, machine.NestedUntypedContext.Saga);
            Assert.Same(machine, machine.NestedUntypedContext.StateMachine);
            Assert.Same(machine.Running.Enter, machine.NestedUntypedContext.Event);
            Assert.Equal(caller.Token, machine.NestedUntypedContext.CancellationToken);
            Assert.NotNull(machine.NestedTypedContext);
            Assert.Same(saga, machine.NestedTypedContext.Saga);
            Assert.Same(machine.Running, machine.NestedTypedContext.Message);
            Assert.Same(machine.Running.BeforeEnter, machine.NestedTypedContext.Event);
            Assert.Equal(caller.Token, machine.NestedTypedContext.CancellationToken);

            if (cancelBeforeEnter)
            {
                caller.Cancel();
                OperationCanceledException error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    operation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
                Assert.Equal(caller.Token, error.CancellationToken);
                Assert.True(operation.IsCanceled);
                Assert.True(machine.BeforeEnterTask.IsCanceled);
                Assert.Equal(machine.Initial.Name, saga.CurrentState);
                Assert.Null(saga.ObservedEnterContext);
            }
            else
            {
                machine.BeforeEnterRelease.TrySetResult();
                await operation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                Assert.True(operation.IsCompletedSuccessfully);
                Assert.Equal(machine.Running.Name, saga.CurrentState);
                Assert.NotNull(saga.ObservedEnterContext);
                Assert.Same(saga, saga.ObservedEnterContext.Saga);
                Assert.Same(machine, saga.ObservedEnterContext.StateMachine);
                Assert.Same(machine.Running.Enter, saga.ObservedEnterContext.Event);
                Assert.Equal(caller.Token, saga.ObservedEnterContext.CancellationToken);
            }
            Assert.False(consumed.IsCancellationRequested);
            Assert.Equal(consumed.Token, context.CancellationToken);
            Assert.Equal(consumed.Token, sagaContext.CancellationToken);
        }
        finally
        {
            machine.BeforeEnterRelease.TrySetResult();
            try
            {
                if (machine.BeforeEnterTask is not null)
                    await ObserveTransitionTaskAsync(machine.BeforeEnterTask);
            }
            finally
            {
                if (operation is not null)
                    await ObserveTransitionTaskAsync(operation);
            }
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-STATE-QUERY-RUNTIME-EXTENSIONS", "transition-selected-token-preserves-delegated-createproxy-side-effects")]
    public async Task Transition_SelectedTokenPreservesDelegatedCreateProxySideEffectsAsync()
    {
        var machine = new LifecycleTokenMachine();
        var saga = new RuntimeSaga { CurrentState = machine.Initial.Name };
        using var caller = new CancellationTokenSource();
        using var consumed = new CancellationTokenSource();
        ConsumeContext<RuntimeSignal> consumeContext =
            InMemoryOutboxTestContextFactory.Create(new RuntimeSignal(), cancellationToken: consumed.Token);
        var instance = new SagaInstance<RuntimeSaga>(saga);
        await instance.MarkInUseAsync(TestContext.Current.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<RuntimeSaga, RuntimeSignal>(consumeContext, instance);
        IBehaviorContext<RuntimeSaga> actualSource =
            new ViciOneServiceBusStateMachine<RuntimeSaga>.BehaviorContextProxy(machine, sagaContext, machine.Initial.Enter);
        var calls = new List<DelegatedProxyCall>();
        IBehaviorContext<RuntimeSaga> context = DelegatingBehaviorContextProxy.Wrap(actualSource, calls);
        Task? operation = null;
        try
        {
            Assert.NotEqual(caller.Token, consumed.Token);
            operation = context.TransitionToStateAsync(machine.Running, caller.Token);
            await machine.BeforeEnterEntered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

            Assert.False(operation.IsCompleted);
            Assert.NotNull(machine.BeforeEnterTask);
            Assert.False(machine.BeforeEnterTask.IsCompleted);
            Assert.Equal(machine.Initial.Name, saga.CurrentState);
            Assert.Equal(1, machine.BeforeEnterCalls);
            Assert.NotNull(machine.BeforeEnterContext);
            Assert.Equal(caller.Token, machine.BeforeEnterContext.CancellationToken);
            Assert.Same(machine.Running, machine.BeforeEnterContext.Message);
            Assert.NotNull(machine.NestedUntypedContext);
            Assert.NotNull(machine.NestedTypedContext);
            Assert.Equal(caller.Token, machine.NestedUntypedContext.CancellationToken);
            Assert.Equal(caller.Token, machine.NestedTypedContext.CancellationToken);
            Assert.Same(machine.Running, machine.NestedTypedContext.Message);
            Assert.Equal(consumed.Token, context.CancellationToken);
            Assert.Equal(consumed.Token, actualSource.CancellationToken);
            Assert.Collection(calls.Where(call => ReferenceEquals(call.Event, machine.Running.Enter)
                    || ReferenceEquals(call.Event, machine.Running.BeforeEnter)),
                call => { Assert.Same(actualSource, call.Source); Assert.Same(machine.Running.Enter, call.Event); Assert.Null(call.Data); },
                call => { Assert.Same(machine.Running.BeforeEnter, call.Event); Assert.Same(machine.Running, call.Data); },
                call => { Assert.Same(machine.Running.Enter, call.Event); Assert.Null(call.Data); },
                call => { Assert.Same(machine.Running.BeforeEnter, call.Event); Assert.Same(machine.Running, call.Data); });

            machine.BeforeEnterRelease.TrySetResult();
            await operation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.True(operation.IsCompletedSuccessfully);
            Assert.Equal(machine.Running.Name, saga.CurrentState);
            Assert.NotNull(saga.ObservedEnterContext);
            Assert.Same(machine, saga.ObservedEnterContext.StateMachine);
            Assert.Same(saga, saga.ObservedEnterContext.Saga);
            Assert.Same(machine.Running.Enter, saga.ObservedEnterContext.Event);
            Assert.Equal(caller.Token, saga.ObservedEnterContext.CancellationToken);
            Assert.Equal(3, calls.Count(call => ReferenceEquals(call.Event, machine.Running.Enter)));
            Assert.Equal(2, calls.Count(call => ReferenceEquals(call.Event, machine.Running.BeforeEnter)));
            Assert.All(calls, call =>
            {
                Assert.Same(machine, call.Result.StateMachine);
                Assert.Same(saga, call.Result.Saga);
                Assert.Same(call.Event, call.Result.Event);
                Assert.Equal(consumed.Token, call.Result.CancellationToken);
                if (call.Data is not null)
                    Assert.Same(call.Data, Assert.IsAssignableFrom<IBehaviorContext<RuntimeSaga, IState>>(call.Result).Message);
            });
            Assert.False(caller.IsCancellationRequested);
            Assert.False(consumed.IsCancellationRequested);
        }
        finally
        {
            machine.BeforeEnterRelease.TrySetResult();
            try
            {
                if (machine.BeforeEnterTask is not null)
                    await ObserveTransitionTaskAsync(machine.BeforeEnterTask);
            }
            finally
            {
                if (operation is not null)
                    await ObserveTransitionTaskAsync(operation);
            }
        }
    }

    private sealed record DelegatedProxyCall(
        object Source, IEvent Event, object? Data, IBehaviorContext<RuntimeSaga> Result);

    private class DelegatingBehaviorContextProxy : DispatchProxy
    {
        public object Source { get; set; } = null!;
        public List<DelegatedProxyCall> Calls { get; set; } = null!;

        public static IBehaviorContext<RuntimeSaga> Wrap(IBehaviorContext<RuntimeSaga> source, List<DelegatedProxyCall> calls)
        {
            IBehaviorContext<RuntimeSaga> result = source is IBehaviorContext<RuntimeSaga, IState>
                ? DispatchProxy.Create<IBehaviorContext<RuntimeSaga, IState>, DelegatingBehaviorContextProxy>()
                : DispatchProxy.Create<IBehaviorContext<RuntimeSaga>, DelegatingBehaviorContextProxy>();
            var control = (DelegatingBehaviorContextProxy)(object)result;
            control.Source = source;
            control.Calls = calls;
            return result;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            object? result;
            try
            {
                result = targetMethod.Invoke(Source, args);
            }
            catch (TargetInvocationException error) when (error.InnerException is not null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
            if (targetMethod.Name == nameof(IBehaviorContext<RuntimeSaga>.CreateProxy))
            {
                var actual = (IBehaviorContext<RuntimeSaga>)result!;
                Calls.Add(new DelegatedProxyCall(Source, (IEvent)args![0]!, args.Length == 2 ? args[1] : null, actual));
                return Wrap(actual, Calls);
            }
            return result;
        }
    }

    private static async Task ObserveTransitionTaskAsync(Task task)
    {
        try
        {
            // Cleanup must join owned work even when the test's cancellation budget has ended.
            await task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        }
        catch (Exception failure) when (failure is not TimeoutException && (task.IsFaulted || task.IsCanceled))
        {
        }
    }

    private sealed class HeldTransitionAccessor(IStateAccessor<RuntimeSaga> inner) : IStateAccessor<RuntimeSaga>
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public IBehaviorContext<RuntimeSaga>? ReadContext { get; private set; }
        public CancellationToken ReadToken { get; private set; }
        public CancellationToken SetToken { get; private set; }
        public int ReadCalls { get; private set; }
        public int SetCalls { get; private set; }
        public Task<IState<RuntimeSaga>?>? ReadTask { get; private set; }

        public Task<IState<RuntimeSaga>?> GetAsync(IBehaviorContext<RuntimeSaga> context, CancellationToken cancellationToken = default)
        {
            ReadCalls++;
            ReadContext = context;
            ReadToken = cancellationToken;
            ReadTask = ReadHeldAsync(context, cancellationToken);
            Entered.TrySetResult();
            return ReadTask;
        }

        private async Task<IState<RuntimeSaga>?> ReadHeldAsync(IBehaviorContext<RuntimeSaga> context, CancellationToken cancellationToken)
        {
            await Release.Task.WaitAsync(cancellationToken);
            return await inner.GetAsync(context, cancellationToken: cancellationToken);
        }

        public Task SetAsync(IBehaviorContext<RuntimeSaga> context, IState<RuntimeSaga> state, CancellationToken cancellationToken = default)
        {
            SetCalls++;
            SetToken = cancellationToken;
            return inner.SetAsync(context, state, cancellationToken: cancellationToken);
        }

        public Expression<Func<RuntimeSaga, bool>> GetStateExpression(params IState[] states) => inner.GetStateExpression(states);
        public void Probe(ProbeContext context) => inner.Probe(context);
    }

    private class AccessorOverrideMachineProxy : DispatchProxy
    {
        public IStateMachine<RuntimeSaga> Machine { get; set; } = null!;
        public IStateAccessor<RuntimeSaga> Accessor { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == "get_Accessor")
                return Accessor;
            try
            {
                return targetMethod.Invoke(Machine, args);
            }
            catch (TargetInvocationException error) when (error.InnerException is not null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }
    }

    private static (IStateMachine<RuntimeSaga> Machine, MachineProxy Control) CreateMachine(IStateAccessor<RuntimeSaga> accessor)
    {
        IStateMachine<RuntimeSaga> machine = DispatchProxy.Create<IStateMachine<RuntimeSaga>, MachineProxy>();
        var control = (MachineProxy)(object)machine;
        control.AccessorValue = accessor;
        return (machine, control);
    }

    private static (IBehaviorContext<RuntimeSaga> Context, BehaviorContextProxy Control) CreateContext(
        IStateMachine<RuntimeSaga>? machine,
        bool throwOnInvocation = false)
    {
        IBehaviorContext<RuntimeSaga> context = DispatchProxy.Create<IBehaviorContext<RuntimeSaga>, BehaviorContextProxy>();
        var control = (BehaviorContextProxy)(object)context;
        control.Machine = machine;
        control.ThrowOnInvocation = throwOnInvocation;
        return (context, control);
    }

    private static async Task WithBehaviorContextAsync(
        RuntimeMachine machine,
        RuntimeSaga saga,
        Func<IBehaviorContext<RuntimeSaga>, Task> action)
    {
        ConsumeContext<RuntimeSignal> consumeContext = InMemoryOutboxTestContextFactory.Create(new RuntimeSignal());
        var sagaInstance = new SagaInstance<RuntimeSaga>(saga);
        await sagaInstance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<RuntimeSaga, RuntimeSignal>(consumeContext, sagaInstance);
        IBehaviorContext<RuntimeSaga> behaviorContext =
            new ViciOneServiceBusStateMachine<RuntimeSaga>.BehaviorContextProxy(machine, sagaContext, machine.Initial.Enter);

        await action(behaviorContext);
    }

    private sealed class RecordingAccessor : IStateAccessor<RuntimeSaga>
    {
        public Task<IState<RuntimeSaga>?> ResultTask { get; init; } = Task.FromResult<IState<RuntimeSaga>?>(null);

        public Expression<Func<RuntimeSaga, bool>> StateExpression { get; init; } = _ => true;

        public List<StateReadCall> GetCalls { get; } = [];

        public List<IState[]> StateExpressionCalls { get; } = [];

        public Task<IState<RuntimeSaga>?> GetAsync(IBehaviorContext<RuntimeSaga> context, CancellationToken cancellationToken = default)
        {
            GetCalls.Add(new StateReadCall(context, cancellationToken));
            return ResultTask;
        }

        public Task SetAsync(IBehaviorContext<RuntimeSaga> context, IState<RuntimeSaga> state, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("State writes are not used by these accessor forwarding contracts.");

        public Expression<Func<RuntimeSaga, bool>> GetStateExpression(params IState[] states)
        {
            StateExpressionCalls.Add(states);
            return StateExpression;
        }

        public void Probe(ProbeContext context) =>
            throw new NotSupportedException("Probing is not used by these accessor forwarding contracts.");
    }

    private sealed record StateReadCall(IBehaviorContext<RuntimeSaga> Context, CancellationToken CancellationToken);

    private class MachineProxy : DispatchProxy
    {
        public object AccessorValue { get; set; } = null!;

        public IEnumerable<IEvent> NextEventsResult { get; set; } = [];

        public int AccessorGets { get; private set; }

        public List<IState> NextEventStates { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == "get_Accessor")
            {
                AccessorGets++;
                return AccessorValue;
            }

            if (targetMethod.Name == nameof(IStateMachine.NextEvents))
            {
                NextEventStates.Add(Assert.IsAssignableFrom<IState>(Assert.Single(args!)));
                return NextEventsResult;
            }

            throw new NotSupportedException($"The machine member '{targetMethod.Name}' is outside this contract.");
        }
    }

    private class BehaviorContextProxy : DispatchProxy
    {
        public object? Machine { get; set; }

        public bool ThrowOnInvocation { get; set; }

        public int InvocationCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            InvocationCount++;
            if (ThrowOnInvocation)
                throw new NotSupportedException($"The context member '{targetMethod.Name}' must not be invoked.");

            if (targetMethod.Name == "get_StateMachine")
                return Machine;

            throw new NotSupportedException($"The context member '{targetMethod.Name}' is outside this contract.");
        }
    }

    private sealed class NameOnlyState(string name) : IState
    {
        public int NameReads { get; private set; }

        public int LifecycleReads { get; private set; }

        public string Name
        {
            get
            {
                NameReads++;
                return name;
            }
        }

        public IEvent Enter => ThrowLifecycle<IEvent>();

        public IEvent Leave => ThrowLifecycle<IEvent>();

        public IEvent<IState> BeforeEnter => ThrowLifecycle<IEvent<IState>>();

        public IEvent<IState> AfterLeave => ThrowLifecycle<IEvent<IState>>();

        public int CompareTo(IState? other) => string.Compare(Name, other?.Name, StringComparison.Ordinal);

        public void Accept(IStateMachineVisitor visitor) =>
            throw new NotSupportedException("Visiting is outside this state-name contract.");

        public void Probe(ProbeContext context) =>
            throw new NotSupportedException("Probing is outside this state-name contract.");

        private T ThrowLifecycle<T>()
        {
            LifecycleReads++;
            throw new NotSupportedException("Only the alias state name may be observed.");
        }
    }

    private sealed class LifecycleTokenMachine : ViciOneServiceBusStateMachine<RuntimeSaga>
    {
        public LifecycleTokenMachine()
        {
            InstanceState(saga => saga.CurrentState);
            BeforeEnter(Running, behavior => behavior.ThenAwaited(context =>
            {
                BeforeEnterCalls++;
                BeforeEnterContext = context;
                NestedUntypedContext = context.CreateProxy(Running.Enter);
                NestedTypedContext = context.CreateProxy(Running.BeforeEnter, Running);
                BeforeEnterTask = BeforeEnterRelease.Task.WaitAsync(context.CancellationToken);
                BeforeEnterEntered.TrySetResult();
                return BeforeEnterTask;
            }));
            WhenEnter(Running, behavior => behavior.Then(context =>
            {
                context.Saga.ObservedEnterContext = context;
                context.Saga.ObservedEnterEvent = context.Event;
            }));
        }

        public IState Running { get; private set; } = null!;
        public TaskCompletionSource BeforeEnterEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource BeforeEnterRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int BeforeEnterCalls { get; private set; }
        public IBehaviorContext<RuntimeSaga, IState>? BeforeEnterContext { get; private set; }
        public IBehaviorContext<RuntimeSaga>? NestedUntypedContext { get; private set; }
        public IBehaviorContext<RuntimeSaga, IState>? NestedTypedContext { get; private set; }
        public Task? BeforeEnterTask { get; private set; }
    }

    private sealed class RuntimeMachine : ViciOneServiceBusStateMachine<RuntimeSaga>
    {
        public RuntimeMachine()
        {
            InstanceState(saga => saga.CurrentState);
            WhenEnter(Running, behavior => behavior.Then(context =>
            {
                context.Saga.ObservedEnterContext = context;
                context.Saga.ObservedEnterEvent = context.Event;
            }));
        }

        public IState Running { get; private set; } = null!;
    }

    private sealed class RuntimeSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public string CurrentState { get; set; } = string.Empty;

        public int Score { get; set; }

        public IBehaviorContext<RuntimeSaga>? ObservedEnterContext { get; set; }

        public IEvent? ObservedEnterEvent { get; set; }
    }

    private sealed record RuntimeSignal;
}
