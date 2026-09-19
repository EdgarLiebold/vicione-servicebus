using System.Collections.Concurrent;
using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineActionActivitiesDeepContractTests
{
    static readonly TimeSpan ObservationTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-219-action-activities-exact-public-contract")]
    public void PublicSurface_HasExactGenericNamesConstraintsMembersAndNullability()
    {
        AssertActivityType(typeof(ActionActivity<>), ["TSaga"], 6, "action", typeof(Action<>));
        AssertActivityType(typeof(ActionActivity<,>), ["TSaga", "TMessage"], 4, "action", typeof(Action<>));
        AssertActivityType(typeof(AsyncActivity<>), ["TSaga"], 6, "asyncAction", typeof(Func<,>));
        AssertActivityType(typeof(AsyncActivity<,>), ["TSaga", "TMessage"], 4, "asyncAction", typeof(Func<,>));
        AssertActivityType(typeof(FaultedActionActivity<,>), ["TSaga", "TException"], 6, "action", typeof(Action<>));
        AssertActivityType(typeof(FaultedActionActivity<,,>), ["TSaga", "TMessage", "TException"], 4, "action", typeof(Action<>));
        AssertActivityType(typeof(AsyncFaultedActionActivity<,>), ["TSaga", "TException"], 6, "asyncAction", typeof(Func<,>));
        AssertActivityType(typeof(AsyncFaultedActionActivity<,,>), ["TSaga", "TMessage", "TException"], 4, "asyncAction", typeof(Func<,>));

        AssertClosedConstructor<ActionActivity<TestSaga>>(typeof(Action<IBehaviorContext<TestSaga>>), "action");
        AssertClosedConstructor<ActionActivity<TestSaga, Message>>(typeof(Action<IBehaviorContext<TestSaga, Message>>), "action");
        AssertClosedConstructor<AsyncActivity<TestSaga>>(typeof(Func<IBehaviorContext<TestSaga>, Task>), "asyncAction");
        AssertClosedConstructor<AsyncActivity<TestSaga, Message>>(
            typeof(Func<IBehaviorContext<TestSaga, Message>, Task>), "asyncAction");
        AssertClosedConstructor<FaultedActionActivity<TestSaga, BaseMarkerException>>(
            typeof(Action<IBehaviorExceptionContext<TestSaga, BaseMarkerException>>), "action");
        AssertClosedConstructor<FaultedActionActivity<TestSaga, Message, BaseMarkerException>>(
            typeof(Action<IBehaviorExceptionContext<TestSaga, Message, BaseMarkerException>>), "action");
        AssertClosedConstructor<AsyncFaultedActionActivity<TestSaga, BaseMarkerException>>(
            typeof(Func<IBehaviorExceptionContext<TestSaga, BaseMarkerException>, Task>), "asyncAction");
        AssertClosedConstructor<AsyncFaultedActionActivity<TestSaga, Message, BaseMarkerException>>(
            typeof(Func<IBehaviorExceptionContext<TestSaga, Message, BaseMarkerException>, Task>), "asyncAction");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-219-action-activities-all-null-boundaries")]
    public async Task ConstructorsVisitorsProbesAndRuntimeMethods_RejectEveryNullBeforeDelegateEffectsAsync()
    {
        AssertArgument("action", () => new ActionActivity<TestSaga>(null!));
        AssertArgument("action", () => new ActionActivity<TestSaga, Message>(null!));
        AssertArgument("asyncAction", () => new AsyncActivity<TestSaga>(null!));
        AssertArgument("asyncAction", () => new AsyncActivity<TestSaga, Message>(null!));
        AssertArgument("action", () => new FaultedActionActivity<TestSaga, InvalidOperationException>(null!));
        AssertArgument("action", () => new FaultedActionActivity<TestSaga, Message, InvalidOperationException>(null!));
        AssertArgument("asyncAction", () => new AsyncFaultedActionActivity<TestSaga, InvalidOperationException>(null!));
        AssertArgument("asyncAction", () => new AsyncFaultedActionActivity<TestSaga, Message, InvalidOperationException>(null!));

        var effects = 0;
        IStateMachineActivity<TestSaga>[] untyped =
        [
            new ActionActivity<TestSaga>(_ => effects++),
            new AsyncActivity<TestSaga>(_ =>
            {
                effects++;
                return Task.CompletedTask;
            }),
            new FaultedActionActivity<TestSaga, InvalidOperationException>(_ => effects++),
            new AsyncFaultedActionActivity<TestSaga, InvalidOperationException>(_ =>
            {
                effects++;
                return Task.CompletedTask;
            }),
        ];
        IStateMachineActivity<TestSaga, Message>[] typed =
        [
            new ActionActivity<TestSaga, Message>(_ => effects++),
            new AsyncActivity<TestSaga, Message>(_ =>
            {
                effects++;
                return Task.CompletedTask;
            }),
            new FaultedActionActivity<TestSaga, Message, InvalidOperationException>(_ => effects++),
            new AsyncFaultedActionActivity<TestSaga, Message, InvalidOperationException>(_ =>
            {
                effects++;
                return Task.CompletedTask;
            }),
        ];

        IBehaviorContext<TestSaga> context = CreateContext<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, Message> typedContext = CreateContext<IBehaviorContext<TestSaga, Message>>();
        IBehaviorExceptionContext<TestSaga, InvalidOperationException> fault =
            CreateContext<IBehaviorExceptionContext<TestSaga, InvalidOperationException>>();
        IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException> typedFault =
            CreateContext<IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException>>();
        var next = new RecordingBehavior();
        var typedNext = new RecordingTypedBehavior();

        foreach (IStateMachineActivity activity in untyped.Cast<IStateMachineActivity>().Concat(typed))
        {
            AssertArgument("visitor", () => activity.Accept(null!));
            AssertArgument("context", () => activity.Probe(null!));
        }

        foreach (IStateMachineActivity<TestSaga> activity in untyped)
        {
            await AssertArgumentAsync("context", () => activity.ExecuteAsync(null!, next));
            await AssertArgumentAsync("next", () => activity.ExecuteAsync(context, null!));
            await AssertArgumentAsync("context", () => activity.ExecuteAsync<Message>(null!, typedNext));
            await AssertArgumentAsync("next", () => activity.ExecuteAsync(typedContext, null!));
            await AssertArgumentAsync("context", () => activity.FaultedAsync<InvalidOperationException>(null!, next));
            await AssertArgumentAsync("next", () => activity.FaultedAsync(fault, null!));
            await AssertArgumentAsync("context", () => activity.FaultedAsync<Message, InvalidOperationException>(null!, typedNext));
            await AssertArgumentAsync("next", () => activity.FaultedAsync(typedFault, null!));
        }

        foreach (IStateMachineActivity<TestSaga, Message> activity in typed)
        {
            await AssertArgumentAsync("context", () => activity.ExecuteAsync(null!, typedNext));
            await AssertArgumentAsync("next", () => activity.ExecuteAsync(typedContext, null!));
            await AssertArgumentAsync("context", () => activity.FaultedAsync<InvalidOperationException>(null!, typedNext));
            await AssertArgumentAsync("next", () => activity.FaultedAsync(typedFault, null!));
        }

        Assert.Equal(0, effects);
        Assert.Empty(next.ExecuteContexts);
        Assert.Empty(next.FaultContexts);
        Assert.Empty(typedNext.ExecuteContexts);
        Assert.Empty(typedNext.FaultContexts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-219-action-async-order-context-outcome-identity")]
    public async Task ActionAndAsyncActivities_PreserveOrderContextContinuationTasksFailuresCancellationAndNullTaskContractAsync()
    {
        IBehaviorContext<TestSaga> context = CreateContext<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, Message> typedContext = CreateContext<IBehaviorContext<TestSaga, Message>>();
        var order = new List<string>();

        var actionCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var actionNext = new RecordingBehavior(execute: observed =>
        {
            order.Add("action-next");
            Assert.Same(context, observed);
            return actionCompletion.Task;
        });
        var action = new ActionActivity<TestSaga>(observed =>
        {
            order.Add("action");
            Assert.Same(context, observed);
        });
        Task actionTask = action.ExecuteAsync(context, actionNext);
        Assert.Same(actionCompletion.Task, actionTask);
        Assert.Equal(["action", "action-next"], order);
        actionCompletion.SetResult(true);
        await actionTask;

        order.Clear();
        var typedCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var typedNext = new RecordingTypedBehavior(execute: observed =>
        {
            order.Add("typed-action-next");
            Assert.Same(typedContext, observed);
            return typedCompletion.Task;
        });
        var typedAction = new ActionActivity<TestSaga, Message>(observed =>
        {
            order.Add("typed-action");
            Assert.Same(typedContext, observed);
        });
        Task typedTask = typedAction.ExecuteAsync(typedContext, typedNext);
        Assert.Same(typedCompletion.Task, typedTask);
        Assert.Equal(["typed-action", "typed-action-next"], order);
        typedCompletion.SetResult(true);
        await typedTask;

        order.Clear();
        var genericAction = new ActionActivity<TestSaga>(observed =>
        {
            order.Add("generic-action");
            Assert.Same(typedContext, observed);
        });
        Task genericActionTask = genericAction.ExecuteAsync(typedContext, typedNext);
        Assert.Same(typedNext.LastExecuteTask, genericActionTask);
        Assert.Equal(["generic-action", "typed-action-next"], order);

        order.Clear();
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var asyncNext = new RecordingBehavior(execute: observed =>
        {
            order.Add("async-next");
            Assert.Same(context, observed);
            return Task.CompletedTask;
        });
        var asyncActivity = new AsyncActivity<TestSaga>(async observed =>
        {
            order.Add("async-start");
            Assert.Same(context, observed);
            await gate.Task.ConfigureAwait(false);
            order.Add("async-end");
        });
        Task pending = asyncActivity.ExecuteAsync(context, asyncNext);
        Assert.Equal(["async-start"], order);
        Assert.Empty(asyncNext.ExecuteContexts);
        gate.SetResult(true);
        await pending;
        Assert.Equal(["async-start", "async-end", "async-next"], order);

        var genericAsyncCalls = 0;
        var genericAsync = new AsyncActivity<TestSaga>(observed =>
        {
            Assert.Same(typedContext, observed);
            genericAsyncCalls++;
            return Task.CompletedTask;
        });
        await genericAsync.ExecuteAsync(typedContext, typedNext);
        Assert.Equal(1, genericAsyncCalls);

        var typedAsyncCalls = 0;
        var typedAsync = new AsyncActivity<TestSaga, Message>(observed =>
        {
            Assert.Same(typedContext, observed);
            typedAsyncCalls++;
            return Task.CompletedTask;
        });
        await typedAsync.ExecuteAsync(typedContext, typedNext);
        Assert.Equal(1, typedAsyncCalls);

        var actionFailure = new MarkerException("action");
        var never = new RecordingBehavior();
        var throwingAction = new ActionActivity<TestSaga>(_ => throw actionFailure);
        Assert.Same(actionFailure,
            Assert.Throws<MarkerException>((Action)(() => _ = throwingAction.ExecuteAsync(context, never))));
        Assert.Empty(never.ExecuteContexts);

        var asyncFailure = new MarkerException("async");
        var failingAsync = new AsyncActivity<TestSaga>(_ => Task.FromException(asyncFailure));
        Assert.Same(asyncFailure, await Assert.ThrowsAsync<MarkerException>(() => failingAsync.ExecuteAsync(context, never)));
        Assert.Empty(never.ExecuteContexts);

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var canceledAsync = new AsyncActivity<TestSaga>(_ => Task.FromCanceled(cancellationSource.Token));
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => canceledAsync.ExecuteAsync(context, never));
        Assert.Equal(cancellationSource.Token, canceled.CancellationToken);
        Assert.Empty(never.ExecuteContexts);

        var nullTaskAsync = new AsyncActivity<TestSaga>(_ => null!);
        InvalidOperationException nullTaskFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => nullTaskAsync.ExecuteAsync(context, never));
        Assert.Equal("The async action returned null.", nullTaskFailure.Message);
        Assert.Empty(never.ExecuteContexts);

        var nullGenericAsync = new AsyncActivity<TestSaga>(_ => null!);
        var nullGenericNext = new RecordingTypedBehavior();
        InvalidOperationException nullGenericFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => nullGenericAsync.ExecuteAsync(typedContext, nullGenericNext));
        Assert.Equal("The async action returned null.", nullGenericFailure.Message);
        Assert.Empty(nullGenericNext.ExecuteContexts);

        var nullTypedAsync = new AsyncActivity<TestSaga, Message>(_ => null!);
        var nullTypedNext = new RecordingTypedBehavior();
        InvalidOperationException nullTypedFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => nullTypedAsync.ExecuteAsync(typedContext, nullTypedNext));
        Assert.Equal("The async action returned null.", nullTypedFailure.Message);
        Assert.Empty(nullTypedNext.ExecuteContexts);

        await AssertAsyncActivitySynchronousThrowsReturnFaultedTasksAsync(context, typedContext);
        await AssertExecuteRouteOutcomeMatrixAsync();

        var canceledContinuation = new RecordingBehavior(execute: _ => Task.FromCanceled(cancellationSource.Token));
        Task exactCanceledTask = new ActionActivity<TestSaga>(_ => { }).ExecuteAsync(context, canceledContinuation);
        Assert.Same(canceledContinuation.LastExecuteTask, exactCanceledTask);

        var forwardFailure = new MarkerException("fault-forward");
        var faultContext = CreateContext<IBehaviorExceptionContext<TestSaga, MarkerException>>();
        var faultNext = new RecordingBehavior(fault: observed =>
        {
            Assert.Same(faultContext, observed);
            return Task.FromException(forwardFailure);
        });
        Task forwarded = asyncActivity.FaultedAsync(faultContext, faultNext);
        Assert.Same(faultNext.LastFaultTask, forwarded);
        Assert.Same(forwardFailure, await Assert.ThrowsAsync<MarkerException>(() => forwarded));

        var actionFaultNext = new RecordingBehavior();
        Task actionForwarded = action.FaultedAsync(faultContext, actionFaultNext);
        Assert.Same(actionFaultNext.LastFaultTask, actionForwarded);
        await actionForwarded;

        var typedFaultContext = CreateContext<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
        Task genericForwarded = action.FaultedAsync(typedFaultContext, typedNext);
        Assert.Same(typedNext.LastFaultTask, genericForwarded);
        await genericForwarded;
        Task asyncGenericForwarded = asyncActivity.FaultedAsync(typedFaultContext, typedNext);
        Assert.Same(typedNext.LastFaultTask, asyncGenericForwarded);
        await asyncGenericForwarded;
        Task typedActionForwarded = typedAction.FaultedAsync(typedFaultContext, typedNext);
        Assert.Same(typedNext.LastFaultTask, typedActionForwarded);
        await typedActionForwarded;
        Task typedAsyncForwarded = typedAsync.FaultedAsync(typedFaultContext, typedNext);
        Assert.Same(typedNext.LastFaultTask, typedAsyncForwarded);
        await typedAsyncForwarded;
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RECOVERY", "iteration-219-faulted-action-routing-outcome-matrix")]
    public async Task FaultedActionFamilies_RouteMatchingAndNonmatchingUntypedAndTypedFaultsWithExactOutcomesAsync()
    {
        IBehaviorContext<TestSaga> executeContext = CreateContext<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, Message> typedExecuteContext = CreateContext<IBehaviorContext<TestSaga, Message>>();
        IBehaviorExceptionContext<TestSaga, InvalidOperationException> matching =
            CreateContext<IBehaviorExceptionContext<TestSaga, InvalidOperationException>>();
        IBehaviorExceptionContext<TestSaga, MarkerException> nonmatching =
            CreateContext<IBehaviorExceptionContext<TestSaga, MarkerException>>();
        IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException> typedMatching =
            CreateContext<IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException>>();
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> typedNonmatching =
            CreateContext<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();

        var order = new List<string>();
        var next = new RecordingBehavior(
            execute: observed =>
            {
                order.Add("execute-next");
                Assert.Same(executeContext, observed);
                return Task.CompletedTask;
            },
            fault: observed =>
            {
                order.Add("fault-next");
                return Task.CompletedTask;
            });
        var sync = new FaultedActionActivity<TestSaga, InvalidOperationException>(observed =>
        {
            order.Add("sync-action");
            Assert.Same(matching, observed);
        });
        Task executeTask = sync.ExecuteAsync(executeContext, next);
        Assert.Same(next.LastExecuteTask, executeTask);
        await executeTask;
        Task matchingTask = sync.FaultedAsync(matching, next);
        Assert.Same(next.LastFaultTask, matchingTask);
        await matchingTask;
        await sync.FaultedAsync(nonmatching, next);
        Assert.Equal(["execute-next", "sync-action", "fault-next", "fault-next"], order);

        order.Clear();
        var typedNext = new RecordingTypedBehavior(
            execute: observed =>
            {
                order.Add("typed-execute-next");
                Assert.Same(typedExecuteContext, observed);
                return Task.CompletedTask;
            },
            fault: observed =>
            {
                order.Add("typed-fault-next");
                return Task.CompletedTask;
            });
        var typedSync = new FaultedActionActivity<TestSaga, Message, InvalidOperationException>(observed =>
        {
            order.Add("typed-sync-action");
            Assert.Same(typedMatching, observed);
        });
        await typedSync.ExecuteAsync(typedExecuteContext, typedNext);
        await typedSync.FaultedAsync(typedMatching, typedNext);
        await typedSync.FaultedAsync(typedNonmatching, typedNext);
        Assert.Equal(["typed-execute-next", "typed-sync-action", "typed-fault-next", "typed-fault-next"], order);

        order.Clear();
        Task genericExecuteTask = sync.ExecuteAsync(typedExecuteContext, typedNext);
        Assert.Same(typedNext.LastExecuteTask, genericExecuteTask);
        await genericExecuteTask;
        var genericSyncCalls = 0;
        var genericSync = new FaultedActionActivity<TestSaga, InvalidOperationException>(observed =>
        {
            genericSyncCalls++;
            Assert.Same(typedMatching, observed);
        });
        await genericSync.FaultedAsync(typedMatching, typedNext);
        await genericSync.FaultedAsync(typedNonmatching, typedNext);
        Assert.Equal(1, genericSyncCalls);

        order.Clear();
        var asyncGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var asyncNext = new RecordingBehavior(fault: observed =>
        {
            order.Add("async-fault-next");
            Assert.Same(matching, observed);
            return Task.CompletedTask;
        });
        var asyncFaulted = new AsyncFaultedActionActivity<TestSaga, InvalidOperationException>(async observed =>
        {
            order.Add("async-action-start");
            Assert.Same(matching, observed);
            await asyncGate.Task.ConfigureAwait(false);
            order.Add("async-action-end");
        });
        Task pending = asyncFaulted.FaultedAsync(matching, asyncNext);
        Assert.Equal(["async-action-start"], order);
        Assert.Empty(asyncNext.FaultContexts);
        asyncGate.SetResult(true);
        await pending;
        Assert.Equal(["async-action-start", "async-action-end", "async-fault-next"], order);

        await asyncFaulted.ExecuteAsync(executeContext, next);
        await asyncFaulted.ExecuteAsync(typedExecuteContext, typedNext);
        var genericAsyncCalls = 0;
        var genericAsync = new AsyncFaultedActionActivity<TestSaga, InvalidOperationException>(observed =>
        {
            genericAsyncCalls++;
            Assert.Same(typedMatching, observed);
            return Task.CompletedTask;
        });
        await genericAsync.FaultedAsync(typedMatching, typedNext);
        await genericAsync.FaultedAsync(typedNonmatching, typedNext);
        Assert.Equal(1, genericAsyncCalls);

        var typedAsyncCalls = 0;
        var typedAsync = new AsyncFaultedActionActivity<TestSaga, Message, InvalidOperationException>(observed =>
        {
            Interlocked.Increment(ref typedAsyncCalls);
            Assert.Same(typedMatching, observed);
            return Task.CompletedTask;
        });
        await typedAsync.FaultedAsync(typedMatching, typedNext);
        await typedAsync.FaultedAsync(typedNonmatching, typedNext);
        await typedAsync.ExecuteAsync(typedExecuteContext, typedNext);
        Assert.Equal(1, typedAsyncCalls);

        await AssertBaseHandlerDerivedExceptionRoutesAsync();
        await AssertFaultedRouteOutcomeMatrixAsync();
        await AssertAsyncFaultedSynchronousThrowsReturnFaultedTasksAsync(matching, typedMatching);
        AssertDirectTaskIdentityMatrix();

        var failure = new MarkerException("sync fault action");
        var noContinuation = new RecordingBehavior();
        var failingSync = new FaultedActionActivity<TestSaga, InvalidOperationException>(_ => throw failure);
        Assert.Same(failure,
            Assert.Throws<MarkerException>((Action)(() => _ = failingSync.FaultedAsync(matching, noContinuation))));
        Assert.Empty(noContinuation.FaultContexts);

        var asyncFailure = new MarkerException("async fault action");
        var failingAsync = new AsyncFaultedActionActivity<TestSaga, InvalidOperationException>(
            _ => Task.FromException(asyncFailure));
        Assert.Same(asyncFailure, await Assert.ThrowsAsync<MarkerException>(() => failingAsync.FaultedAsync(matching, noContinuation)));
        Assert.Empty(noContinuation.FaultContexts);

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var canceledAsync = new AsyncFaultedActionActivity<TestSaga, InvalidOperationException>(
            _ => Task.FromCanceled(cancellationSource.Token));
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => canceledAsync.FaultedAsync(matching, noContinuation));
        Assert.Equal(cancellationSource.Token, canceled.CancellationToken);
        Assert.Empty(noContinuation.FaultContexts);

        var nullTaskAsync = new AsyncFaultedActionActivity<TestSaga, InvalidOperationException>(_ => null!);
        InvalidOperationException nullTaskFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => nullTaskAsync.FaultedAsync(matching, noContinuation));
        Assert.Equal("The async action returned null.", nullTaskFailure.Message);
        Assert.Empty(noContinuation.FaultContexts);

        var nullGenericTaskAsync = new AsyncFaultedActionActivity<TestSaga, InvalidOperationException>(_ => null!);
        var nullGenericFaultNext = new RecordingTypedBehavior();
        InvalidOperationException nullGenericTaskFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => nullGenericTaskAsync.FaultedAsync(typedMatching, nullGenericFaultNext));
        Assert.Equal("The async action returned null.", nullGenericTaskFailure.Message);
        Assert.Empty(nullGenericFaultNext.FaultContexts);

        var nullTypedTaskAsync = new AsyncFaultedActionActivity<TestSaga, Message, InvalidOperationException>(_ => null!);
        var nullTypedFaultNext = new RecordingTypedBehavior();
        InvalidOperationException nullTypedTaskFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => nullTypedTaskAsync.FaultedAsync(typedMatching, nullTypedFaultNext));
        Assert.Equal("The async action returned null.", nullTypedTaskFailure.Message);
        Assert.Empty(nullTypedFaultNext.FaultContexts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-219-action-activities-concurrency-inspection-and-context-free-await")]
    public async Task Activities_AreConcurrentRemainInspectableAndAsyncFaultsDoNotCaptureSynchronizationContextAsync()
    {
        var visitor = new RecordingVisitor();
        var probe = new RecordingProbeContext();
        IStateMachineActivity[] activities =
        [
            new ActionActivity<TestSaga>(_ => { }),
            new ActionActivity<TestSaga, Message>(_ => { }),
            new AsyncActivity<TestSaga>(_ => Task.CompletedTask),
            new AsyncActivity<TestSaga, Message>(_ => Task.CompletedTask),
            new FaultedActionActivity<TestSaga, InvalidOperationException>(_ => { }),
            new FaultedActionActivity<TestSaga, Message, InvalidOperationException>(_ => { }),
            new AsyncFaultedActionActivity<TestSaga, InvalidOperationException>(_ => Task.CompletedTask),
            new AsyncFaultedActionActivity<TestSaga, Message, InvalidOperationException>(_ => Task.CompletedTask),
        ];
        foreach (IStateMachineActivity activity in activities)
        {
            activity.Accept(visitor);
            activity.Probe(probe);
        }

        Assert.Equal(activities, visitor.Activities);
        Assert.Equal(
            ["then", "then", "thenAsync", "thenAsync", "then-faulted", "then-faulted", "then-async-faulted", "then-async-faulted"],
            probe.ScopeKeys);

        var effects = 0;
        var continuations = 0;
        var syncAction = new ActionActivity<TestSaga>(_ => Interlocked.Increment(ref effects));
        var typedSyncAction = new ActionActivity<TestSaga, Message>(_ => Interlocked.Increment(ref effects));
        var asyncAction = new AsyncActivity<TestSaga>(_ =>
        {
            Interlocked.Increment(ref effects);
            return Task.CompletedTask;
        });
        var typedAsyncAction = new AsyncActivity<TestSaga, Message>(_ =>
        {
            Interlocked.Increment(ref effects);
            return Task.CompletedTask;
        });
        var syncFault = new FaultedActionActivity<TestSaga, InvalidOperationException>(
            _ => Interlocked.Increment(ref effects));
        var typedSyncFault = new FaultedActionActivity<TestSaga, Message, InvalidOperationException>(
            _ => Interlocked.Increment(ref effects));
        var asyncFault = new AsyncFaultedActionActivity<TestSaga, InvalidOperationException>(_ =>
        {
            Interlocked.Increment(ref effects);
            return Task.CompletedTask;
        });
        var typedAsyncFault = new AsyncFaultedActionActivity<TestSaga, Message, InvalidOperationException>(_ =>
        {
            Interlocked.Increment(ref effects);
            return Task.CompletedTask;
        });
        IBehaviorContext<TestSaga>[] contexts = Enumerable.Range(0, 64)
            .Select(_ => CreateContext<IBehaviorContext<TestSaga>>())
            .ToArray();
        IBehaviorContext<TestSaga, Message>[] typedContexts = Enumerable.Range(0, 64)
            .Select(_ => CreateContext<IBehaviorContext<TestSaga, Message>>())
            .ToArray();
        IBehaviorExceptionContext<TestSaga, InvalidOperationException>[] faults = Enumerable.Range(0, 64)
            .Select(_ => CreateContext<IBehaviorExceptionContext<TestSaga, InvalidOperationException>>())
            .ToArray();
        IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException>[] typedFaults = Enumerable.Range(0, 64)
            .Select(_ => CreateContext<IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException>>())
            .ToArray();
        await Task.WhenAll(Enumerable.Range(0, 64).SelectMany(index => new Func<Task>[]
        {
            () => syncAction.ExecuteAsync(contexts[index], CreateConcurrentNext(contexts[index], IncrementContinuation)),
            () => typedSyncAction.ExecuteAsync(typedContexts[index], CreateConcurrentNext(typedContexts[index], IncrementContinuation)),
            () => asyncAction.ExecuteAsync(contexts[index], CreateConcurrentNext(contexts[index], IncrementContinuation)),
            () => typedAsyncAction.ExecuteAsync(typedContexts[index], CreateConcurrentNext(typedContexts[index], IncrementContinuation)),
            () => syncFault.FaultedAsync(faults[index], CreateConcurrentFaultNext(faults[index], IncrementContinuation)),
            () => typedSyncFault.FaultedAsync(typedFaults[index], CreateConcurrentFaultNext(typedFaults[index], IncrementContinuation)),
            () => asyncFault.FaultedAsync(faults[index], CreateConcurrentFaultNext(faults[index], IncrementContinuation)),
            () => typedAsyncFault.FaultedAsync(typedFaults[index], CreateConcurrentFaultNext(typedFaults[index], IncrementContinuation)),
        }).Select(callback => Task.Run(callback)));
        Assert.Equal(512, effects);
        Assert.Equal(512, continuations);

        void IncrementContinuation() => Interlocked.Increment(ref continuations);

        var synchronizationContext = new RecordingSynchronizationContext();
        var actionGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var nextGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = CreateContext<IBehaviorExceptionContext<TestSaga, InvalidOperationException>>();
        var noCapture = new AsyncFaultedActionActivity<TestSaga, InvalidOperationException>(_ => actionGate.Task);
        var gatedNext = new RecordingBehavior(fault: _ => nextGate.Task);
        SynchronizationContext? previous = SynchronizationContext.Current;
        Task noCaptureTask;
        try
        {
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            noCaptureTask = noCapture.FaultedAsync(context, gatedNext);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        actionGate.SetResult(true);
        await gatedNext.FaultObserved.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
        nextGate.SetResult(true);
        await noCaptureTask.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
        Assert.Equal(0, synchronizationContext.PostCalls);

        var faultNextGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var faultNextOnly = new AsyncFaultedActionActivity<TestSaga, InvalidOperationException>(_ => Task.CompletedTask);
        Task faultNextTask;
        try
        {
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            faultNextTask = faultNextOnly.FaultedAsync(context, new RecordingBehavior(fault: _ => faultNextGate.Task));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
        faultNextGate.SetResult(true);
        await faultNextTask.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);

        var executeContext = CreateContext<IBehaviorContext<TestSaga>>();
        var executeActionGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var executeNextGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var executeNoCapture = new AsyncActivity<TestSaga>(_ => executeActionGate.Task);
        var executeNext = new RecordingBehavior(execute: _ => executeNextGate.Task);
        Task executeNoCaptureTask;
        try
        {
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            executeNoCaptureTask = executeNoCapture.ExecuteAsync(executeContext, executeNext);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
        executeActionGate.SetResult(true);
        await executeNext.ExecuteObserved.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
        executeNextGate.SetResult(true);
        await executeNoCaptureTask.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);

        var executeNextOnlyGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var executeNextOnly = new AsyncActivity<TestSaga>(_ => Task.CompletedTask);
        Task executeNextOnlyTask;
        try
        {
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            executeNextOnlyTask = executeNextOnly.ExecuteAsync(
                executeContext,
                new RecordingBehavior(execute: _ => executeNextOnlyGate.Task));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
        executeNextOnlyGate.SetResult(true);
        await executeNextOnlyTask.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
        Assert.Equal(0, synchronizationContext.PostCalls);

        await AssertAdditionalNoCaptureRoutesAsync(synchronizationContext);

        var visitorFailure = new MarkerException("visitor");
        var probeFailure = new MarkerException("probe");
        foreach (IStateMachineActivity activity in activities)
        {
            Assert.Same(visitorFailure,
                Assert.Throws<MarkerException>(() => activity.Accept(new RecordingVisitor(visitorFailure))));
            Assert.Same(probeFailure,
                Assert.Throws<MarkerException>(() => activity.Probe(new RecordingProbeContext(probeFailure))));
        }
    }

    static async Task AssertAsyncActivitySynchronousThrowsReturnFaultedTasksAsync(
        IBehaviorContext<TestSaga> context,
        IBehaviorContext<TestSaga, Message> typedContext)
    {
        var untypedFailure = new MarkerException("untyped execute sync throw");
        var untypedNext = new RecordingBehavior();
        await AssertDelegateSynchronousThrowIsCapturedAsync(
            () => new AsyncActivity<TestSaga>(_ => throw untypedFailure).ExecuteAsync(context, untypedNext),
            untypedFailure);
        Assert.Empty(untypedNext.ExecuteContexts);

        var genericFailure = new MarkerException("generic execute sync throw");
        var genericNext = new RecordingTypedBehavior();
        await AssertDelegateSynchronousThrowIsCapturedAsync(
            () => new AsyncActivity<TestSaga>(_ => throw genericFailure).ExecuteAsync(typedContext, genericNext),
            genericFailure);
        Assert.Empty(genericNext.ExecuteContexts);

        var typedFailure = new MarkerException("typed execute sync throw");
        var typedNext = new RecordingTypedBehavior();
        await AssertDelegateSynchronousThrowIsCapturedAsync(
            () => new AsyncActivity<TestSaga, Message>(_ => throw typedFailure).ExecuteAsync(typedContext, typedNext),
            typedFailure);
        Assert.Empty(typedNext.ExecuteContexts);
    }

    static async Task AssertAsyncFaultedSynchronousThrowsReturnFaultedTasksAsync(
        IBehaviorExceptionContext<TestSaga, InvalidOperationException> matching,
        IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException> typedMatching)
    {
        var untypedFailure = new MarkerException("untyped fault sync throw");
        var untypedNext = new RecordingBehavior();
        await AssertDelegateSynchronousThrowIsCapturedAsync(
            () => new AsyncFaultedActionActivity<TestSaga, InvalidOperationException>(_ => throw untypedFailure)
                .FaultedAsync(matching, untypedNext),
            untypedFailure);
        Assert.Empty(untypedNext.FaultContexts);

        var genericFailure = new MarkerException("generic fault sync throw");
        var genericNext = new RecordingTypedBehavior();
        await AssertDelegateSynchronousThrowIsCapturedAsync(
            () => new AsyncFaultedActionActivity<TestSaga, InvalidOperationException>(_ => throw genericFailure)
                .FaultedAsync(typedMatching, genericNext),
            genericFailure);
        Assert.Empty(genericNext.FaultContexts);

        var typedFailure = new MarkerException("typed fault sync throw");
        var typedNext = new RecordingTypedBehavior();
        await AssertDelegateSynchronousThrowIsCapturedAsync(
            () => new AsyncFaultedActionActivity<TestSaga, Message, InvalidOperationException>(_ => throw typedFailure)
                .FaultedAsync(typedMatching, typedNext),
            typedFailure);
        Assert.Empty(typedNext.FaultContexts);
    }

    static async Task AssertDelegateSynchronousThrowIsCapturedAsync(Func<Task> invoke, MarkerException expected)
    {
        Task? returnedTask = null;
        Exception? synchronousFailure = Record.Exception((Action)(() => returnedTask = invoke()));
        Assert.Null(synchronousFailure);
        Assert.NotNull(returnedTask);
        Assert.Same(expected, await Assert.ThrowsAsync<MarkerException>(() => returnedTask));
    }

    static async Task AssertBaseHandlerDerivedExceptionRoutesAsync()
    {
        IBehaviorExceptionContext<TestSaga, DerivedMarkerException> derived =
            CreateContext<IBehaviorExceptionContext<TestSaga, DerivedMarkerException>>();
        IBehaviorExceptionContext<TestSaga, Message, DerivedMarkerException> typedDerived =
            CreateContext<IBehaviorExceptionContext<TestSaga, Message, DerivedMarkerException>>();
        var untypedNext = new RecordingBehavior();
        var genericNext = new RecordingTypedBehavior();
        var typedNext = new RecordingTypedBehavior();
        var syncUntypedCalls = 0;
        var syncGenericCalls = 0;
        var syncTypedCalls = 0;
        var asyncUntypedCalls = 0;
        var asyncGenericCalls = 0;
        var asyncTypedCalls = 0;

        var syncUntyped = new FaultedActionActivity<TestSaga, BaseMarkerException>(observed =>
        {
            Assert.Same(derived, observed);
            syncUntypedCalls++;
        });
        Task syncUntypedTask = syncUntyped.FaultedAsync(derived, untypedNext);
        Assert.Same(untypedNext.LastFaultTask, syncUntypedTask);
        await syncUntypedTask;

        var syncGeneric = new FaultedActionActivity<TestSaga, BaseMarkerException>(observed =>
        {
            Assert.Same(typedDerived, observed);
            syncGenericCalls++;
        });
        Task syncGenericTask = syncGeneric.FaultedAsync(typedDerived, genericNext);
        Assert.Same(genericNext.LastFaultTask, syncGenericTask);
        await syncGenericTask;

        var syncTyped = new FaultedActionActivity<TestSaga, Message, BaseMarkerException>(observed =>
        {
            Assert.Same(typedDerived, observed);
            syncTypedCalls++;
        });
        Task syncTypedTask = syncTyped.FaultedAsync(typedDerived, typedNext);
        Assert.Same(typedNext.LastFaultTask, syncTypedTask);
        await syncTypedTask;

        var asyncUntyped = new AsyncFaultedActionActivity<TestSaga, BaseMarkerException>(observed =>
        {
            Assert.Same(derived, observed);
            asyncUntypedCalls++;
            return Task.CompletedTask;
        });
        await asyncUntyped.FaultedAsync(derived, untypedNext);

        var asyncGeneric = new AsyncFaultedActionActivity<TestSaga, BaseMarkerException>(observed =>
        {
            Assert.Same(typedDerived, observed);
            asyncGenericCalls++;
            return Task.CompletedTask;
        });
        await asyncGeneric.FaultedAsync(typedDerived, genericNext);

        var asyncTyped = new AsyncFaultedActionActivity<TestSaga, Message, BaseMarkerException>(observed =>
        {
            Assert.Same(typedDerived, observed);
            asyncTypedCalls++;
            return Task.CompletedTask;
        });
        await asyncTyped.FaultedAsync(typedDerived, typedNext);

        Assert.Equal(1, syncUntypedCalls);
        Assert.Equal(1, syncGenericCalls);
        Assert.Equal(1, syncTypedCalls);
        Assert.Equal(1, asyncUntypedCalls);
        Assert.Equal(1, asyncGenericCalls);
        Assert.Equal(1, asyncTypedCalls);
        Assert.Equal(2, untypedNext.FaultContexts.Count);
        Assert.Equal(2, genericNext.FaultContexts.Count);
        Assert.Equal(2, typedNext.FaultContexts.Count);
    }

    static async Task AssertExecuteRouteOutcomeMatrixAsync()
    {
        IBehaviorContext<TestSaga> context = CreateContext<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, Message> typedContext = CreateContext<IBehaviorContext<TestSaga, Message>>();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await AssertSyncExecuteRouteOutcomesAsync(
            action => new ActionActivity<TestSaga>(_ => action()),
            (activity, next) => activity.ExecuteAsync(context, next),
            continuation => new RecordingBehavior(execute: continuation),
            cancellationSource.Token);
        await AssertSyncExecuteRouteOutcomesAsync(
            action => new ActionActivity<TestSaga>(_ => action()),
            (activity, next) => activity.ExecuteAsync(typedContext, next),
            continuation => new RecordingTypedBehavior(execute: continuation),
            cancellationSource.Token);
        await AssertSyncExecuteRouteOutcomesAsync(
            action => new ActionActivity<TestSaga, Message>(_ => action()),
            (activity, next) => activity.ExecuteAsync(typedContext, next),
            continuation => new RecordingTypedBehavior(execute: continuation),
            cancellationSource.Token);

        await AssertAsyncExecuteRouteOutcomesAsync(
            action => new AsyncActivity<TestSaga>(_ => action()),
            (activity, next) => activity.ExecuteAsync(context, next),
            continuation => new RecordingBehavior(execute: continuation),
            cancellationSource.Token);
        await AssertAsyncExecuteRouteOutcomesAsync(
            action => new AsyncActivity<TestSaga>(_ => action()),
            (activity, next) => activity.ExecuteAsync(typedContext, next),
            continuation => new RecordingTypedBehavior(execute: continuation),
            cancellationSource.Token);
        await AssertAsyncExecuteRouteOutcomesAsync(
            action => new AsyncActivity<TestSaga, Message>(_ => action()),
            (activity, next) => activity.ExecuteAsync(typedContext, next),
            continuation => new RecordingTypedBehavior(execute: continuation),
            cancellationSource.Token);
    }

    static async Task AssertSyncExecuteRouteOutcomesAsync<TActivity, TNext>(
        Func<Action, TActivity> createActivity,
        Func<TActivity, TNext, Task> execute,
        Func<Func<object, Task>, TNext> createNext,
        CancellationToken cancellationToken)
    {
        var actionFailure = new MarkerException("sync execute action failure");
        var continuationCalls = 0;
        TNext noContinuation = createNext(_ =>
        {
            continuationCalls++;
            return Task.CompletedTask;
        });
        Assert.Same(actionFailure, Assert.Throws<MarkerException>((Action)(() =>
            _ = execute(createActivity(() => throw actionFailure), noContinuation))));
        OperationCanceledException actionCanceled = Assert.ThrowsAny<OperationCanceledException>((Action)(() =>
            _ = execute(createActivity(() => throw new OperationCanceledException(cancellationToken)), noContinuation)));
        Assert.Equal(cancellationToken, actionCanceled.CancellationToken);
        Assert.Equal(0, continuationCalls);

        var continuationFailure = new MarkerException("sync execute continuation failure");
        Task failedTask = Task.FromException(continuationFailure);
        Task returnedFailedTask = execute(createActivity(() => { }), createNext(_ => failedTask));
        Assert.Same(failedTask, returnedFailedTask);
        Assert.Same(continuationFailure, await Assert.ThrowsAsync<MarkerException>(() => returnedFailedTask));

        Task canceledTask = Task.FromCanceled(cancellationToken);
        Task returnedCanceledTask = execute(createActivity(() => { }), createNext(_ => canceledTask));
        Assert.Same(canceledTask, returnedCanceledTask);
        OperationCanceledException continuationCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => returnedCanceledTask);
        Assert.Equal(cancellationToken, continuationCanceled.CancellationToken);
    }

    static async Task AssertAsyncExecuteRouteOutcomesAsync<TActivity, TNext>(
        Func<Func<Task>, TActivity> createActivity,
        Func<TActivity, TNext, Task> execute,
        Func<Func<object, Task>, TNext> createNext,
        CancellationToken cancellationToken)
    {
        var actionFailure = new MarkerException("async execute action failure");
        var continuationCalls = 0;
        TNext noContinuation = createNext(_ =>
        {
            continuationCalls++;
            return Task.CompletedTask;
        });
        Assert.Same(actionFailure, await Assert.ThrowsAsync<MarkerException>(() =>
            execute(createActivity(() => Task.FromException(actionFailure)), noContinuation)));
        OperationCanceledException actionCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            execute(createActivity(() => Task.FromCanceled(cancellationToken)), noContinuation));
        Assert.Equal(cancellationToken, actionCanceled.CancellationToken);
        Assert.Equal(0, continuationCalls);

        var continuationFailure = new MarkerException("async execute continuation failure");
        Task failedTask = Task.FromException(continuationFailure);
        Assert.Same(continuationFailure, await Assert.ThrowsAsync<MarkerException>(() =>
            execute(createActivity(() => Task.CompletedTask), createNext(_ => failedTask))));

        Task canceledTask = Task.FromCanceled(cancellationToken);
        OperationCanceledException continuationCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            execute(createActivity(() => Task.CompletedTask), createNext(_ => canceledTask)));
        Assert.Equal(cancellationToken, continuationCanceled.CancellationToken);
    }

    static async Task AssertFaultedRouteOutcomeMatrixAsync()
    {
        IBehaviorExceptionContext<TestSaga, InvalidOperationException> matching =
            CreateContext<IBehaviorExceptionContext<TestSaga, InvalidOperationException>>();
        IBehaviorExceptionContext<TestSaga, MarkerException> nonmatching =
            CreateContext<IBehaviorExceptionContext<TestSaga, MarkerException>>();
        IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException> typedMatching =
            CreateContext<IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException>>();
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> typedNonmatching =
            CreateContext<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await AssertSyncFaultedRouteOutcomesAsync(
            action => new FaultedActionActivity<TestSaga, InvalidOperationException>(_ => action()),
            (activity, next) => activity.FaultedAsync(matching, next),
            (activity, next) => activity.FaultedAsync(nonmatching, next),
            continuation => new RecordingBehavior(fault: continuation),
            cancellationSource.Token);
        await AssertSyncFaultedRouteOutcomesAsync(
            action => new FaultedActionActivity<TestSaga, InvalidOperationException>(_ => action()),
            (activity, next) => activity.FaultedAsync(typedMatching, next),
            (activity, next) => activity.FaultedAsync(typedNonmatching, next),
            continuation => new RecordingTypedBehavior(fault: continuation),
            cancellationSource.Token);
        await AssertSyncFaultedRouteOutcomesAsync(
            action => new FaultedActionActivity<TestSaga, Message, InvalidOperationException>(_ => action()),
            (activity, next) => activity.FaultedAsync(typedMatching, next),
            (activity, next) => activity.FaultedAsync(typedNonmatching, next),
            continuation => new RecordingTypedBehavior(fault: continuation),
            cancellationSource.Token);

        await AssertAsyncFaultedRouteOutcomesAsync(
            action => new AsyncFaultedActionActivity<TestSaga, InvalidOperationException>(_ => action()),
            (activity, next) => activity.FaultedAsync(matching, next),
            (activity, next) => activity.FaultedAsync(nonmatching, next),
            continuation => new RecordingBehavior(fault: continuation),
            cancellationSource.Token);
        await AssertAsyncFaultedRouteOutcomesAsync(
            action => new AsyncFaultedActionActivity<TestSaga, InvalidOperationException>(_ => action()),
            (activity, next) => activity.FaultedAsync(typedMatching, next),
            (activity, next) => activity.FaultedAsync(typedNonmatching, next),
            continuation => new RecordingTypedBehavior(fault: continuation),
            cancellationSource.Token);
        await AssertAsyncFaultedRouteOutcomesAsync(
            action => new AsyncFaultedActionActivity<TestSaga, Message, InvalidOperationException>(_ => action()),
            (activity, next) => activity.FaultedAsync(typedMatching, next),
            (activity, next) => activity.FaultedAsync(typedNonmatching, next),
            continuation => new RecordingTypedBehavior(fault: continuation),
            cancellationSource.Token);
    }

    static async Task AssertSyncFaultedRouteOutcomesAsync<TActivity, TNext>(
        Func<Action, TActivity> createActivity,
        Func<TActivity, TNext, Task> invokeMatching,
        Func<TActivity, TNext, Task> invokeNonmatching,
        Func<Func<object, Task>, TNext> createNext,
        CancellationToken cancellationToken)
    {
        var actionFailure = new MarkerException("sync action failure");
        var actionFailureContinuationCalls = 0;
        TNext actionFailureNext = createNext(_ =>
        {
            actionFailureContinuationCalls++;
            return Task.CompletedTask;
        });
        var actionFailureActivity = createActivity(() => throw actionFailure);
        Assert.Same(actionFailure,
            Assert.Throws<MarkerException>((Action)(() =>
                _ = invokeMatching(actionFailureActivity, actionFailureNext))));
        Assert.Equal(0, actionFailureContinuationCalls);

        var actionCanceledContinuationCalls = 0;
        TNext actionCanceledNext = createNext(_ =>
        {
            actionCanceledContinuationCalls++;
            return Task.CompletedTask;
        });
        var actionCanceledActivity = createActivity(() => throw new OperationCanceledException(cancellationToken));
        OperationCanceledException actionCanceled = Assert.ThrowsAny<OperationCanceledException>((Action)(() =>
            _ = invokeMatching(actionCanceledActivity, actionCanceledNext)));
        Assert.Equal(cancellationToken, actionCanceled.CancellationToken);
        Assert.Equal(0, actionCanceledContinuationCalls);

        var continuationFailure = new MarkerException("sync continuation failure");
        Task continuationFailureTask = Task.FromException(continuationFailure);
        var matchingActionCalls = 0;
        TNext continuationFailureNext = createNext(_ => continuationFailureTask);
        Task returnedFailureTask = invokeMatching(createActivity(() => matchingActionCalls++), continuationFailureNext);
        Assert.Same(continuationFailureTask, returnedFailureTask);
        Assert.Same(continuationFailure,
            await Assert.ThrowsAsync<MarkerException>(() => returnedFailureTask));
        Assert.Equal(1, matchingActionCalls);

        Task continuationCanceledTask = Task.FromCanceled(cancellationToken);
        var nonmatchingActionCalls = 0;
        TNext continuationCanceledNext = createNext(_ => continuationCanceledTask);
        Task returnedCanceledTask = invokeNonmatching(createActivity(() => nonmatchingActionCalls++), continuationCanceledNext);
        Assert.Same(continuationCanceledTask, returnedCanceledTask);
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => returnedCanceledTask);
        Assert.Equal(cancellationToken, canceled.CancellationToken);
        Assert.Equal(0, nonmatchingActionCalls);
    }

    static async Task AssertAsyncFaultedRouteOutcomesAsync<TActivity, TNext>(
        Func<Func<Task>, TActivity> createActivity,
        Func<TActivity, TNext, Task> invokeMatching,
        Func<TActivity, TNext, Task> invokeNonmatching,
        Func<Func<object, Task>, TNext> createNext,
        CancellationToken cancellationToken)
    {
        var actionFailure = new MarkerException("async action failure");
        var actionFailureContinuationCalls = 0;
        TNext actionFailureNext = createNext(_ =>
        {
            actionFailureContinuationCalls++;
            return Task.CompletedTask;
        });
        var actionFailureActivity = createActivity(() => Task.FromException(actionFailure));
        Assert.Same(actionFailure,
            await Assert.ThrowsAsync<MarkerException>(() => invokeMatching(actionFailureActivity, actionFailureNext)));
        Assert.Equal(0, actionFailureContinuationCalls);

        var actionCanceledContinuationCalls = 0;
        TNext actionCanceledNext = createNext(_ =>
        {
            actionCanceledContinuationCalls++;
            return Task.CompletedTask;
        });
        var actionCanceledActivity = createActivity(() => Task.FromCanceled(cancellationToken));
        OperationCanceledException actionCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => invokeMatching(actionCanceledActivity, actionCanceledNext));
        Assert.Equal(cancellationToken, actionCanceled.CancellationToken);
        Assert.Equal(0, actionCanceledContinuationCalls);

        var continuationFailure = new MarkerException("async continuation failure");
        Task continuationFailureTask = Task.FromException(continuationFailure);
        var matchingActionCalls = 0;
        TNext continuationFailureNext = createNext(_ => continuationFailureTask);
        Assert.Same(continuationFailure,
            await Assert.ThrowsAsync<MarkerException>(() =>
                invokeMatching(createActivity(() =>
                {
                    matchingActionCalls++;
                    return Task.CompletedTask;
                }), continuationFailureNext)));
        Assert.Equal(1, matchingActionCalls);

        Task continuationCanceledTask = Task.FromCanceled(cancellationToken);
        var nonmatchingActionCalls = 0;
        TNext continuationCanceledNext = createNext(_ => continuationCanceledTask);
        OperationCanceledException continuationCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => invokeNonmatching(createActivity(() =>
            {
                nonmatchingActionCalls++;
                return Task.CompletedTask;
            }), continuationCanceledNext));
        Assert.Equal(cancellationToken, continuationCanceled.CancellationToken);
        Assert.Equal(0, nonmatchingActionCalls);
    }

    static void AssertDirectTaskIdentityMatrix()
    {
        IBehaviorContext<TestSaga> context = CreateContext<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, Message> typedContext = CreateContext<IBehaviorContext<TestSaga, Message>>();
        IBehaviorExceptionContext<TestSaga, MarkerException> fault =
            CreateContext<IBehaviorExceptionContext<TestSaga, MarkerException>>();
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> typedFault =
            CreateContext<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
        Task executeTask = Task.FromResult(new object());
        Task faultTask = Task.FromResult(new object());
        var next = new RecordingBehavior(execute: _ => executeTask, fault: _ => faultTask);
        var typedNext = new RecordingTypedBehavior(execute: _ => executeTask, fault: _ => faultTask);

        var action = new ActionActivity<TestSaga>(_ => { });
        Assert.Same(executeTask, action.ExecuteAsync(context, next));
        Assert.Same(executeTask, action.ExecuteAsync(typedContext, typedNext));
        Assert.Same(faultTask, action.FaultedAsync(fault, next));
        Assert.Same(faultTask, action.FaultedAsync(typedFault, typedNext));
        var typedAction = new ActionActivity<TestSaga, Message>(_ => { });
        Assert.Same(executeTask, typedAction.ExecuteAsync(typedContext, typedNext));
        Assert.Same(faultTask, typedAction.FaultedAsync(typedFault, typedNext));

        var asyncActivity = new AsyncActivity<TestSaga>(_ => Task.CompletedTask);
        Assert.Same(faultTask, asyncActivity.FaultedAsync(fault, next));
        Assert.Same(faultTask, asyncActivity.FaultedAsync(typedFault, typedNext));
        var typedAsyncActivity = new AsyncActivity<TestSaga, Message>(_ => Task.CompletedTask);
        Assert.Same(faultTask, typedAsyncActivity.FaultedAsync(typedFault, typedNext));

        var faulted = new FaultedActionActivity<TestSaga, InvalidOperationException>(_ => { });
        Assert.Same(executeTask, faulted.ExecuteAsync(context, next));
        Assert.Same(executeTask, faulted.ExecuteAsync(typedContext, typedNext));
        Assert.Same(faultTask, faulted.FaultedAsync(fault, next));
        Assert.Same(faultTask, faulted.FaultedAsync(typedFault, typedNext));
        var typedFaulted = new FaultedActionActivity<TestSaga, Message, InvalidOperationException>(_ => { });
        Assert.Same(executeTask, typedFaulted.ExecuteAsync(typedContext, typedNext));
        Assert.Same(faultTask, typedFaulted.FaultedAsync(typedFault, typedNext));

        var asyncFaulted = new AsyncFaultedActionActivity<TestSaga, InvalidOperationException>(_ => Task.CompletedTask);
        Assert.Same(executeTask, asyncFaulted.ExecuteAsync(context, next));
        Assert.Same(executeTask, asyncFaulted.ExecuteAsync(typedContext, typedNext));
        var typedAsyncFaulted =
            new AsyncFaultedActionActivity<TestSaga, Message, InvalidOperationException>(_ => Task.CompletedTask);
        Assert.Same(executeTask, typedAsyncFaulted.ExecuteAsync(typedContext, typedNext));
    }

    static async Task AssertAdditionalNoCaptureRoutesAsync(RecordingSynchronizationContext synchronizationContext)
    {
        IBehaviorContext<TestSaga, Message> executeContext =
            CreateContext<IBehaviorContext<TestSaga, Message>>();
        IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException> faultContext =
            CreateContext<IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException>>();

        await AssertBothAwaitSitesDoNotCaptureAsync(synchronizationContext, (actionTask, nextTask) =>
        {
            var next = new RecordingTypedBehavior(execute: _ => nextTask);
            var activity = new AsyncActivity<TestSaga, Message>(_ => actionTask);
            return (activity.ExecuteAsync(executeContext, next), next.ExecuteObserved);
        });
        await AssertBothAwaitSitesDoNotCaptureAsync(synchronizationContext, (actionTask, nextTask) =>
        {
            var next = new RecordingTypedBehavior(fault: _ => nextTask);
            var activity = new AsyncFaultedActionActivity<TestSaga, InvalidOperationException>(_ => actionTask);
            return (activity.FaultedAsync(faultContext, next), next.FaultObserved);
        });
        await AssertBothAwaitSitesDoNotCaptureAsync(synchronizationContext, (actionTask, nextTask) =>
        {
            var next = new RecordingTypedBehavior(fault: _ => nextTask);
            var activity =
                new AsyncFaultedActionActivity<TestSaga, Message, InvalidOperationException>(_ => actionTask);
            return (activity.FaultedAsync(faultContext, next), next.FaultObserved);
        });
    }

    static async Task AssertBothAwaitSitesDoNotCaptureAsync(
        RecordingSynchronizationContext synchronizationContext,
        Func<Task, Task, (Task Execution, Task NextInvoked)> start)
    {
        await AssertAwaitSiteDoesNotCaptureAsync(synchronizationContext, start, actionStartsIncomplete: true);
        await AssertAwaitSiteDoesNotCaptureAsync(synchronizationContext, start, actionStartsIncomplete: false);
    }

    static async Task AssertAwaitSiteDoesNotCaptureAsync(
        RecordingSynchronizationContext synchronizationContext,
        Func<Task, Task, (Task Execution, Task NextInvoked)> start,
        bool actionStartsIncomplete)
    {
        int initialPostCalls = synchronizationContext.PostCalls;
        var actionGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var nextGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task actionTask = actionStartsIncomplete ? actionGate.Task : Task.CompletedTask;
        SynchronizationContext? previous = SynchronizationContext.Current;
        (Task Execution, Task NextInvoked) invocation;
        try
        {
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            invocation = start(actionTask, nextGate.Task);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        if (actionStartsIncomplete)
            actionGate.SetResult(true);
        await invocation.NextInvoked.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
        nextGate.SetResult(true);
        await invocation.Execution.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
        Assert.Equal(initialPostCalls, synchronizationContext.PostCalls);
    }

    static RecordingBehavior CreateConcurrentNext(IBehaviorContext<TestSaga> expected, Action continuation) =>
        new(execute: observed =>
        {
            Assert.Same(expected, observed);
            continuation();
            return Task.CompletedTask;
        });

    static RecordingTypedBehavior CreateConcurrentNext(IBehaviorContext<TestSaga, Message> expected, Action continuation) =>
        new(execute: observed =>
        {
            Assert.Same(expected, observed);
            continuation();
            return Task.CompletedTask;
        });

    static RecordingBehavior CreateConcurrentFaultNext(
        IBehaviorExceptionContext<TestSaga, InvalidOperationException> expected,
        Action continuation) =>
        new(fault: observed =>
        {
            Assert.Same(expected, observed);
            continuation();
            return Task.CompletedTask;
        });

    static RecordingTypedBehavior CreateConcurrentFaultNext(
        IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException> expected,
        Action continuation) =>
        new(fault: observed =>
        {
            Assert.Same(expected, observed);
            continuation();
            return Task.CompletedTask;
        });

    static void AssertActivityType(Type type, string[] genericNames, int declaredMethodCount, string constructorParameterName,
        Type delegateDefinition)
    {
        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.False(type.IsAbstract);
        Type[] arguments = type.GetGenericArguments();
        Assert.Equal(genericNames, arguments.Select(argument => argument.Name));
        AssertSagaParameter(arguments[0]);
        foreach (Type argument in arguments.Skip(1))
        {
            if (argument.Name == "TException")
                Assert.Contains(typeof(Exception), argument.GetGenericParameterConstraints());
            else
                Assert.True(argument.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        }

        ConstructorInfo constructor = Assert.Single(type.GetConstructors());
        ParameterInfo parameter = Assert.Single(constructor.GetParameters());
        Assert.Equal(constructorParameterName, parameter.Name);
        Assert.Equal(delegateDefinition, parameter.ParameterType.GetGenericTypeDefinition());
        Assert.Equal(NullabilityState.NotNull, new NullabilityInfoContext().Create(parameter).ReadState);

        MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.Equal(declaredMethodCount, methods.Length);
        Assert.Equal(1, methods.Count(method => method.Name == nameof(IVisitable.Accept)));
        Assert.Equal(1, methods.Count(method => method.Name == nameof(IProbeSite.Probe)));
        Assert.Equal(declaredMethodCount == 6 ? 2 : 1, methods.Count(method => method.Name == "ExecuteAsync"));
        Assert.Equal(declaredMethodCount == 6 ? 2 : 1, methods.Count(method => method.Name == "FaultedAsync"));
        bool isFaultedAction = arguments[^1].Name == "TException";
        string[] expectedGenericMethods = (declaredMethodCount, isFaultedAction) switch
        {
            (6, false) => ["ExecuteAsync<TData>", "FaultedAsync<T,TException>", "FaultedAsync<TException>"],
            (6, true) => ["ExecuteAsync<TData>", "FaultedAsync<T>", "FaultedAsync<TData,T>"],
            (4, false) => ["FaultedAsync<TException>"],
            (4, true) => ["FaultedAsync<T>"],
            _ => throw new InvalidOperationException("Unexpected activity surface."),
        };
        Assert.Equal(expectedGenericMethods, methods
            .Where(method => method.IsGenericMethodDefinition)
            .Select(method => $"{method.Name}<{string.Join(',', method.GetGenericArguments().Select(argument => argument.Name))}>")
            .OrderBy(signature => signature));
        foreach (Type methodArgument in methods.SelectMany(method => method.GetGenericArguments()))
        {
            if (methodArgument.Name is "TException" or "T")
            {
                if (methodArgument.Name == "T" && methods
                        .Single(method => method.GetGenericArguments().Contains(methodArgument))
                        .GetGenericArguments().Length == 2
                    && !isFaultedAction)
                    Assert.True(methodArgument.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
                else
                    Assert.Contains(typeof(Exception), methodArgument.GetGenericParameterConstraints());
            }
            else
                Assert.True(methodArgument.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        }
        var nullability = new NullabilityInfoContext();
        foreach (MethodInfo method in methods)
        {
            foreach (ParameterInfo methodParameter in method.GetParameters())
                Assert.Equal(NullabilityState.NotNull, nullability.Create(methodParameter).ReadState);
            if (method.ReturnType == typeof(Task))
                Assert.Equal(NullabilityState.NotNull, nullability.Create(method.ReturnParameter).ReadState);
        }
    }

    static void AssertClosedConstructor<TActivity>(Type expectedDelegateType, string expectedParameterName)
    {
        ParameterInfo parameter = Assert.Single(Assert.Single(typeof(TActivity).GetConstructors()).GetParameters());
        Assert.Equal(expectedParameterName, parameter.Name);
        Assert.Equal(expectedDelegateType, parameter.ParameterType);
        Assert.Equal(NullabilityState.NotNull, new NullabilityInfoContext().Create(parameter).ReadState);
    }

    static void AssertSagaParameter(Type parameter)
    {
        Assert.True(parameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        Assert.Contains(typeof(ISagaStateMachineInstance), parameter.GetGenericParameterConstraints());
    }

    static void AssertArgument(string parameterName, Action action) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);

    static async Task AssertArgumentAsync(string parameterName, Func<Task> action)
    {
        try
        {
            Task task = action();
            ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(() => task);
            Assert.Equal(parameterName, exception.ParamName);
        }
        catch (ArgumentNullException exception)
        {
            Assert.Equal(parameterName, exception.ParamName);
        }
    }

    static T CreateContext<T>()
        where T : class => DispatchProxy.Create<T, ContextProxy>();

    public sealed class TestSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed record Message;

    public sealed class MarkerException(string message = "marker") : Exception(message);

    public class BaseMarkerException(string message = "base marker") : Exception(message);

    public sealed class DerivedMarkerException(string message = "derived marker") : BaseMarkerException(message);

    public class ContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_CancellationToken"
                ? CancellationToken.None
                : throw new NotSupportedException(targetMethod?.Name);
    }

    sealed class RecordingBehavior(
        Func<object, Task>? execute = null,
        Func<object, Task>? fault = null) : IBehavior<TestSaga>
    {
        readonly Func<object, Task> _execute = execute ?? (_ => Task.CompletedTask);
        readonly Func<object, Task> _fault = fault ?? (_ => Task.CompletedTask);
        readonly TaskCompletionSource<bool> _executeObserved =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<bool> _faultObserved =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ConcurrentQueue<object> ExecuteContexts { get; } = new();
        public ConcurrentQueue<object> FaultContexts { get; } = new();
        public Task ExecuteObserved => _executeObserved.Task;
        public Task FaultObserved => _faultObserved.Task;
        public Task? LastExecuteTask { get; private set; }
        public Task? LastFaultTask { get; private set; }

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context) => RecordExecute(context);
        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context) where T : class => RecordExecute(context);
        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context)
            where T : class where TException : Exception => RecordFault(context);
        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context)
            where TException : Exception => RecordFault(context);
        public void Accept(IStateMachineVisitor visitor) { }
        public void Probe(ProbeContext context) { }

        Task RecordExecute(object context)
        {
            ExecuteContexts.Enqueue(context);
            Task task = _execute(context);
            LastExecuteTask = task;
            _executeObserved.TrySetResult(true);
            return task;
        }

        Task RecordFault(object context)
        {
            FaultContexts.Enqueue(context);
            Task task = _fault(context);
            LastFaultTask = task;
            _faultObserved.TrySetResult(true);
            return task;
        }
    }

    sealed class RecordingTypedBehavior(
        Func<object, Task>? execute = null,
        Func<object, Task>? fault = null) : IBehavior<TestSaga, Message>
    {
        readonly Func<object, Task> _execute = execute ?? (_ => Task.CompletedTask);
        readonly Func<object, Task> _fault = fault ?? (_ => Task.CompletedTask);
        readonly TaskCompletionSource<bool> _executeObserved =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<bool> _faultObserved =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ConcurrentQueue<object> ExecuteContexts { get; } = new();
        public ConcurrentQueue<object> FaultContexts { get; } = new();
        public Task ExecuteObserved => _executeObserved.Task;
        public Task FaultObserved => _faultObserved.Task;
        public Task? LastExecuteTask { get; private set; }
        public Task? LastFaultTask { get; private set; }

        public Task ExecuteAsync(IBehaviorContext<TestSaga, Message> context)
        {
            ExecuteContexts.Enqueue(context);
            Task task = _execute(context);
            LastExecuteTask = task;
            _executeObserved.TrySetResult(true);
            return task;
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, Message, TException> context)
            where TException : Exception
        {
            FaultContexts.Enqueue(context);
            Task task = _fault(context);
            LastFaultTask = task;
            _faultObserved.TrySetResult(true);
            return task;
        }

        public void Accept(IStateMachineVisitor visitor) { }
        public void Probe(ProbeContext context) { }
    }

    sealed class RecordingVisitor(Exception? failure = null) : IStateMachineVisitor
    {
        public List<IStateMachineActivity> Activities { get; } = [];

        public void Visit(IState state, Action<IState> next) => throw Unexpected();
        public void Visit(IEvent @event, Action<IEvent> next) => throw Unexpected();
        public void Visit<TMessage>(IEvent<TMessage> @event, Action<IEvent<TMessage>> next) where TMessage : class => throw Unexpected();
        public void Visit(IStateMachineActivity activity)
        {
            if (failure != null)
                throw failure;
            Activities.Add(activity);
        }
        public void Visit(IStateMachineExceptionActivity activity, Action<IStateMachineExceptionActivity> next) => throw Unexpected();
        public void Visit<T>(IBehavior<T> behavior) where T : class, ISagaStateMachineInstance => throw Unexpected();
        public void Visit<T>(IBehavior<T> behavior, Action<IBehavior<T>> next) where T : class, ISagaStateMachineInstance => throw Unexpected();
        public void Visit<T, TMessage>(IBehavior<T, TMessage> behavior)
            where T : class, ISagaStateMachineInstance where TMessage : class => throw Unexpected();
        public void Visit<T, TMessage>(IBehavior<T, TMessage> behavior, Action<IBehavior<T, TMessage>> next)
            where T : class, ISagaStateMachineInstance where TMessage : class => throw Unexpected();
        public void Visit(IStateMachineActivity activity, Action<IStateMachineActivity> next) => throw Unexpected();

        static Exception Unexpected() => new InvalidOperationException("Unexpected visitor overload.");
    }

    sealed class RecordingProbeContext(Exception? failure = null) : ProbeContext
    {
        public CancellationToken CancellationToken => default;
        public List<string> ScopeKeys { get; } = [];
        public void Add(string key, string? value) => throw Unexpected();
        public void Add(string key, object? value) => throw Unexpected();
        public void Set(object values) => throw Unexpected();
        public void Set(IEnumerable<KeyValuePair<string, object?>> values) => throw Unexpected();
        public ProbeContext CreateScope(string key)
        {
            if (failure != null)
                throw failure;
            ScopeKeys.Add(key);
            return this;
        }
        static Exception Unexpected() => new InvalidOperationException("Unexpected probe operation.");
    }

    sealed class RecordingSynchronizationContext : SynchronizationContext
    {
        int _postCalls;
        public int PostCalls => Volatile.Read(ref _postCalls);
        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _postCalls);
            ThreadPool.QueueUserWorkItem(_ => d(state));
        }
    }
}
