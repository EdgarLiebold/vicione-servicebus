using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineBehaviorFaultDispatchDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-FAULT", "iteration-216-post-precheck-cancellation-preserves-ordinary-fault")]
    public async Task ActivityBehavior_PostPrecheckCancellationPreservesAndDispatchesTheOrdinaryFaultForBothContextShapesAsync()
    {
        using var untypedCancellation = new CancellationTokenSource();
        var untypedFailure = new MarkerException("untyped activity failed");
        var untypedActivity = new RecordingActivity
        {
            BeforeExecute = untypedCancellation.Cancel,
            ExecuteFailure = untypedFailure,
        };
        var untypedNext = new RecordingBehavior { ReadFaultException = true };
        IBehavior<TestSaga> untypedBehavior = CreateActivityBehavior(untypedActivity, untypedNext);
        IBehaviorContext<TestSaga> untypedContext = CreateContext<IBehaviorContext<TestSaga>>(
            untypedCancellation.Token,
            new TriggerEvent("Trigger"));

        await untypedBehavior.ExecuteAsync(untypedContext);

        Assert.Equal(1, untypedActivity.UntypedExecuteCalls);
        Assert.Equal(1, untypedNext.UntypedFaultCalls);
        Assert.Equal(0, untypedNext.TypedFaultCalls);
        Assert.Same(untypedFailure, untypedNext.LastFaultException);
        var untypedFaultContext = Assert.IsAssignableFrom<IBehaviorExceptionContext<TestSaga, MarkerException>>(
            untypedNext.LastFaultContext);
        Assert.Same(untypedFailure, untypedFaultContext.Exception);
        Assert.Equal(untypedCancellation.Token, untypedFaultContext.CancellationToken);
        Assert.Same(untypedContext, GetWrappedContext(untypedFaultContext));

        using var typedCancellation = new CancellationTokenSource();
        var typedFailure = new MarkerException("typed activity failed");
        var typedActivity = new RecordingActivity
        {
            BeforeExecute = typedCancellation.Cancel,
            ExecuteFailure = typedFailure,
        };
        var typedNext = new RecordingBehavior { ReadFaultException = true };
        IBehavior<TestSaga> typedBehavior = CreateActivityBehavior(typedActivity, typedNext);
        IBehaviorContext<TestSaga, Message> typedContext = CreateContext<IBehaviorContext<TestSaga, Message>>(
            typedCancellation.Token,
            new MessageEvent<Message>("Message"),
            new Message("payload"));

        await typedBehavior.ExecuteAsync<Message>(typedContext);

        Assert.Equal(1, typedActivity.TypedExecuteCalls);
        Assert.Equal(0, typedNext.UntypedFaultCalls);
        Assert.Equal(1, typedNext.TypedFaultCalls);
        Assert.Same(typedFailure, typedNext.LastFaultException);
        var typedFaultContext = Assert.IsAssignableFrom<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>(
            typedNext.LastFaultContext);
        Assert.Same(typedFailure, typedFaultContext.Exception);
        Assert.Equal(typedCancellation.Token, typedFaultContext.CancellationToken);
        Assert.Same(typedContext, GetWrappedContext(typedFaultContext));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-FAULT", "iteration-216-activity-behavior-constructor-null-precedence")]
    public void ActivityBehavior_ConstructorRejectsActivityBeforeNextAndNeverTouchesCollaborators()
    {
        ConstructorInfo constructor = GetActivityBehaviorConstructor();
        var activity = new RecordingActivity();
        var next = new RecordingBehavior();

        AssertReflectionParameter("activity", () => { _ = constructor.Invoke([null, next]); });
        AssertReflectionParameter("activity", () => { _ = constructor.Invoke([null, null]); });
        AssertReflectionParameter("next", () => { _ = constructor.Invoke([activity, null]); });

        Assert.Equal(0, activity.UntypedExecuteCalls);
        Assert.Equal(0, activity.TypedExecuteCalls);
        Assert.Null(activity.LastVisitor);
        Assert.Null(activity.LastProbe);
        Assert.Equal(0, next.TotalCalls);
        Assert.Null(next.LastVisitor);
        Assert.Null(next.LastProbe);
        Assert.Null(next.LastFaultContext);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-FAULT", "iteration-216-activity-behavior-cancellation-classification")]
    public async Task ActivityBehavior_ClassifiesPreCanceledMatchingAndForeignCancellationForBothContextShapesAsync(
        bool typed,
        int scenario)
    {
        using var contextCancellation = new CancellationTokenSource();
        using var foreignCancellation = new CancellationTokenSource();
        foreignCancellation.Cancel();

        OperationCanceledException? sourceCancellation = scenario switch
        {
            1 => new OperationCanceledException("matching cancellation", null, contextCancellation.Token),
            2 => new OperationCanceledException("foreign cancellation", null, foreignCancellation.Token),
            _ => null,
        };
        var activity = new RecordingActivity
        {
            BeforeExecute = scenario == 1 ? contextCancellation.Cancel : null,
            ExecuteFailure = sourceCancellation,
        };
        var next = new RecordingBehavior { ReadFaultException = true };
        IBehavior<TestSaga> behavior = CreateActivityBehavior(activity, next);

        if (scenario == 0)
            contextCancellation.Cancel();

        Task execution = typed
            ? behavior.ExecuteAsync<Message>(CreateContext<IBehaviorContext<TestSaga, Message>>(
                contextCancellation.Token,
                new MessageEvent<Message>("Message"),
                new Message("payload")))
            : behavior.ExecuteAsync(CreateContext<IBehaviorContext<TestSaga>>(
                contextCancellation.Token,
                new TriggerEvent("Trigger")));

        if (scenario < 2)
        {
            OperationCanceledException propagated = await Assert.ThrowsAsync<OperationCanceledException>(
                () => execution);
            Assert.Equal(contextCancellation.Token, propagated.CancellationToken);
            if (scenario == 1)
                Assert.Same(sourceCancellation, propagated);
            Assert.Equal(scenario == 0 ? 0 : 1, typed ? activity.TypedExecuteCalls : activity.UntypedExecuteCalls);
            Assert.Equal(0, next.TotalCalls);
            return;
        }

        await execution;
        Assert.Equal(1, typed ? activity.TypedExecuteCalls : activity.UntypedExecuteCalls);
        Assert.Equal(typed ? 0 : 1, next.UntypedFaultCalls);
        Assert.Equal(typed ? 1 : 0, next.TypedFaultCalls);
        Assert.Same(sourceCancellation, next.LastFaultException);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-FAULT", "iteration-216-activity-behavior-asynchronous-fault-awaiting")]
    public async Task ActivityBehavior_AwaitsPendingActivityBeforeDispatchingItsExactAsynchronousFailureAsync(bool typed)
    {
        var activityCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var faultCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var faultObserved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceFailure = new MarkerException("asynchronous activity failure");
        var activity = new RecordingActivity { ExecuteTask = activityCompletion.Task };
        var next = new RecordingBehavior
        {
            ReadFaultException = true,
            UntypedFaultTask = faultCompletion.Task,
            TypedFaultTask = faultCompletion.Task,
            FaultSignal = faultObserved,
        };
        IBehavior<TestSaga> behavior = CreateActivityBehavior(activity, next);

        Task execution = typed
            ? behavior.ExecuteAsync<Message>(CreateContext<IBehaviorContext<TestSaga, Message>>(
                new MessageEvent<Message>("Message"),
                new Message("payload")))
            : behavior.ExecuteAsync(CreateContext<IBehaviorContext<TestSaga>>(new TriggerEvent("Trigger")));

        Assert.False(execution.IsCompleted);
        Assert.Equal(0, next.TotalCalls);

        activityCompletion.SetException(sourceFailure);
        await faultObserved.Task.WaitAsync(TestContext.Current.CancellationToken);

        Assert.False(execution.IsCompleted);
        Assert.Same(sourceFailure, next.LastFaultException);
        Assert.Equal(typed ? 0 : 1, next.UntypedFaultCalls);
        Assert.Equal(typed ? 1 : 0, next.TypedFaultCalls);

        faultCompletion.SetResult(true);
        await execution;
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-FAULT", "iteration-216-activity-behavior-direct-fault-forwarding")]
    public async Task ActivityBehavior_DirectFaultPathsRejectNullAndPreserveContextContinuationAndTaskIdentity()
    {
        var untypedCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var typedCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var activity = new RecordingActivity
        {
            UntypedFaultTask = untypedCompletion.Task,
            TypedFaultTask = typedCompletion.Task,
        };
        var next = new RecordingBehavior();
        IBehavior<TestSaga> behavior = CreateActivityBehavior(activity, next);

        ArgumentNullException untypedExecuteNull = await Assert.ThrowsAsync<ArgumentNullException>(
            () => behavior.ExecuteAsync(null!));
        ArgumentNullException typedExecuteNull = await Assert.ThrowsAsync<ArgumentNullException>(
            () => behavior.ExecuteAsync<Message>(null!));
        Assert.Equal("context", untypedExecuteNull.ParamName);
        Assert.Equal("context", typedExecuteNull.ParamName);
        Assert.Equal(0, activity.UntypedExecuteCalls);
        Assert.Equal(0, activity.TypedExecuteCalls);
        Assert.Equal(0, activity.UntypedFaultCalls);
        Assert.Equal(0, activity.TypedFaultCalls);
        Assert.Null(activity.LastFaultContext);
        Assert.Null(activity.LastFaultNext);
        Assert.Equal(0, next.TotalCalls);

        AssertParameter("context", () => { _ = behavior.FaultedAsync<MarkerException>(null!); });
        AssertParameter("context", () => { _ = behavior.FaultedAsync<Message, MarkerException>(null!); });

        IBehaviorExceptionContext<TestSaga, MarkerException> untypedContext =
            CreateStrictProxy<IBehaviorExceptionContext<TestSaga, MarkerException>>();
        Task untypedTask = behavior.FaultedAsync(untypedContext);

        Assert.Same(untypedCompletion.Task, untypedTask);
        Assert.Same(untypedContext, activity.LastFaultContext);
        Assert.Same(next, activity.LastFaultNext);
        Assert.Equal(1, activity.UntypedFaultCalls);

        IBehaviorExceptionContext<TestSaga, Message, MarkerException> typedContext =
            CreateStrictProxy<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
        Task typedTask = behavior.FaultedAsync(typedContext);

        Assert.Same(typedCompletion.Task, typedTask);
        Assert.Same(typedContext, activity.LastFaultContext);
        var continuation = Assert.IsType<DataBehavior<TestSaga, Message>>(activity.LastFaultNext);
        Assert.Same(next, GetWrappedBehavior(continuation));
        Assert.Equal(1, activity.TypedFaultCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-FAULT", "iteration-216-execute-on-faulted-owned-null-boundaries")]
    public void ExecuteOnFaultedBehaviors_RejectEveryMissingRequiredInputBeforeForwarding()
    {
        var untypedNext = new RecordingBehavior();
        IBehaviorExceptionContext<TestSaga, MarkerException> untypedStored =
            CreateStrictProxy<IBehaviorExceptionContext<TestSaga, MarkerException>>();

        AssertParameter("next", () => _ = new ExecuteOnFaultedBehavior<TestSaga, MarkerException>(null!, untypedStored));
        AssertParameter("context", () => _ = new ExecuteOnFaultedBehavior<TestSaga, MarkerException>(untypedNext, null!));
        AssertParameter("next", () => _ = new ExecuteOnFaultedBehavior<TestSaga, MarkerException>(null!, null!));

        IBehavior<TestSaga> untyped = new ExecuteOnFaultedBehavior<TestSaga, MarkerException>(untypedNext, untypedStored);
        AssertParameter("visitor", () => untyped.Accept(null!));
        AssertParameter("context", () => untyped.Probe(null!));
        AssertParameter("context", () => { _ = untyped.ExecuteAsync(null!); });
        AssertParameter("context", () => { _ = untyped.ExecuteAsync<Message>(null!); });
        AssertParameter("context", () => { _ = untyped.FaultedAsync<MarkerException>(null!); });
        AssertParameter("context", () => { _ = untyped.FaultedAsync<Message, MarkerException>(null!); });
        AssertInvariant(() => { _ = untyped.FaultedAsync<MarkerException>(untypedStored); });
        AssertInvariant(() =>
        {
            _ = untyped.FaultedAsync<Message, MarkerException>(
                CreateStrictProxy<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>());
        });
        Assert.Equal(0, untypedNext.TotalCalls);

        var typedNext = new RecordingTypedBehavior();
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> typedStored =
            CreateStrictProxy<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();

        AssertParameter("next", () => _ = new ExecuteOnFaultedBehavior<TestSaga, Message, MarkerException>(null!, typedStored));
        AssertParameter("context", () => _ = new ExecuteOnFaultedBehavior<TestSaga, Message, MarkerException>(typedNext, null!));
        AssertParameter("next", () => _ = new ExecuteOnFaultedBehavior<TestSaga, Message, MarkerException>(null!, null!));

        IBehavior<TestSaga> typed = new ExecuteOnFaultedBehavior<TestSaga, Message, MarkerException>(typedNext, typedStored);
        AssertParameter("visitor", () => typed.Accept(null!));
        AssertParameter("context", () => typed.Probe(null!));
        AssertParameter("context", () => { _ = typed.ExecuteAsync(null!); });
        AssertParameter("context", () => { _ = typed.ExecuteAsync<Message>(null!); });
        AssertParameter("context", () => { _ = typed.FaultedAsync<MarkerException>(null!); });
        AssertParameter("context", () => { _ = typed.FaultedAsync<Message, MarkerException>(null!); });
        AssertInvariant(() => { _ = typed.FaultedAsync<MarkerException>(untypedStored); });
        AssertInvariant(() => { _ = typed.FaultedAsync<Message, MarkerException>(typedStored); });
        Assert.Equal(0, typedNext.TotalCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-FAULT", "iteration-216-exception-type-cache-collectible-lifetime")]
    public void ExceptionTypeCache_DoesNotKeepACollectibleExceptionTypeOrAssemblyAlive()
    {
        CollectibleReferences references = PopulateExceptionTypeCacheWithCollectibleType();

        for (var attempt = 0; attempt < 20 && (references.Type.IsAlive || references.Assembly.IsAlive); attempt++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        }

        Assert.False(references.Type.IsAlive);
        Assert.False(references.Assembly.IsAlive);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-FAULT", "iteration-216-exception-type-cache-cancellation-and-null-precedence")]
    public async Task ExceptionTypeCache_CanceledTokenReturnsExactCanceledTasksAfterRequiredArgumentValidationAsync()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        CancellationToken token = cancellation.Token;
        var exception = new MarkerException("must not be dispatched");
        var untypedBehavior = new RecordingBehavior();
        IBehaviorContext<TestSaga> untypedContext = CreateStrictProxy<IBehaviorContext<TestSaga>>();
        MethodInfo untypedMethod = GetExceptionTypeCacheMethod(genericArity: 1).MakeGenericMethod(typeof(TestSaga));

        AssertReflectionParameter("behavior", () =>
        {
            _ = untypedMethod.Invoke(null, [null, null, null, token]);
        });
        AssertReflectionParameter("behavior", () =>
        {
            _ = untypedMethod.Invoke(null, [null, untypedContext, exception, token]);
        });
        AssertReflectionParameter("context", () =>
        {
            _ = untypedMethod.Invoke(null, [untypedBehavior, null, null, token]);
        });
        AssertReflectionParameter("context", () =>
        {
            _ = untypedMethod.Invoke(null, [untypedBehavior, null, exception, token]);
        });
        AssertReflectionParameter("exception", () =>
        {
            _ = untypedMethod.Invoke(null, [untypedBehavior, untypedContext, null, token]);
        });

        Task untypedTask = Assert.IsAssignableFrom<Task>(
            untypedMethod.Invoke(null, [untypedBehavior, untypedContext, exception, token]));
        OperationCanceledException untypedCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => untypedTask);
        Assert.Equal(token, untypedCanceled.CancellationToken);
        Assert.True(untypedTask.IsCanceled);
        Assert.Equal(0, untypedBehavior.TotalCalls);

        var typedBehavior = new RecordingTypedBehavior();
        IBehaviorContext<TestSaga, Message> typedContext = CreateStrictProxy<IBehaviorContext<TestSaga, Message>>();
        MethodInfo typedMethod = GetExceptionTypeCacheMethod(genericArity: 2)
            .MakeGenericMethod(typeof(TestSaga), typeof(Message));

        AssertReflectionParameter("behavior", () =>
        {
            _ = typedMethod.Invoke(null, [null, null, null, token]);
        });
        AssertReflectionParameter("behavior", () =>
        {
            _ = typedMethod.Invoke(null, [null, typedContext, exception, token]);
        });
        AssertReflectionParameter("context", () =>
        {
            _ = typedMethod.Invoke(null, [typedBehavior, null, null, token]);
        });
        AssertReflectionParameter("context", () =>
        {
            _ = typedMethod.Invoke(null, [typedBehavior, null, exception, token]);
        });
        AssertReflectionParameter("exception", () =>
        {
            _ = typedMethod.Invoke(null, [typedBehavior, typedContext, null, token]);
        });

        Task typedTask = Assert.IsAssignableFrom<Task>(
            typedMethod.Invoke(null, [typedBehavior, typedContext, exception, token]));
        OperationCanceledException typedCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => typedTask);
        Assert.Equal(token, typedCanceled.CancellationToken);
        Assert.True(typedTask.IsCanceled);
        Assert.Equal(0, typedBehavior.TotalCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-FAULT", "iteration-216-exception-type-cache-normal-forwarding-and-concurrency")]
    public async Task ExceptionTypeCache_NormalDispatchPreservesIdentityForBothShapesAndConcurrentSameTypeCallsAsync()
    {
        using var cancellation = new CancellationTokenSource();
        CancellationToken token = cancellation.Token;
        var exception = new MarkerException("direct dispatch");
        var untypedCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var untypedBehavior = new RecordingBehavior
        {
            ReadFaultException = true,
            UntypedFaultTask = untypedCompletion.Task,
        };
        IBehaviorContext<TestSaga> untypedContext =
            CreateContext<IBehaviorContext<TestSaga>>(new TriggerEvent("Trigger"));
        MethodInfo untypedMethod = GetExceptionTypeCacheMethod(genericArity: 1).MakeGenericMethod(typeof(TestSaga));

        Task untypedTask = Assert.IsAssignableFrom<Task>(
            untypedMethod.Invoke(null, [untypedBehavior, untypedContext, exception, token]));

        Assert.Same(untypedCompletion.Task, untypedTask);
        Assert.Same(exception, untypedBehavior.LastFaultException);
        Assert.Same(untypedContext, GetWrappedContext(untypedBehavior.LastFaultContext!));

        var typedCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var typedBehavior = new RecordingTypedBehavior { FaultTask = typedCompletion.Task };
        IBehaviorContext<TestSaga, Message> typedContext = CreateContext<IBehaviorContext<TestSaga, Message>>(
            new MessageEvent<Message>("Message"),
            new Message("payload"));
        MethodInfo typedMethod = GetExceptionTypeCacheMethod(genericArity: 2)
            .MakeGenericMethod(typeof(TestSaga), typeof(Message));

        Task typedTask = Assert.IsAssignableFrom<Task>(
            typedMethod.Invoke(null, [typedBehavior, typedContext, exception, token]));

        Assert.Same(typedCompletion.Task, typedTask);
        var typedFaultContext = Assert.IsAssignableFrom<
            IBehaviorExceptionContext<TestSaga, Message, MarkerException>>(typedBehavior.LastFaultContext);
        Assert.Same(exception, typedFaultContext.Exception);
        Assert.Same(typedContext, GetWrappedContext(typedFaultContext));

        const int participantCount = 8;
        using var barrier = new Barrier(participantCount + 1);
        var scenarios = Enumerable.Range(0, participantCount)
            .Select(index =>
            {
                var sourceException = new ConcurrentMarkerException($"concurrent-{index}");
                var sourceContext = CreateContext<IBehaviorContext<TestSaga>>(new TriggerEvent($"Event-{index}"));
                var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                completion.SetResult(true);
                var sourceBehavior = new RecordingBehavior
                {
                    ReadFaultException = true,
                    UntypedFaultTask = completion.Task,
                };
                return new ConcurrentScenario(sourceException, sourceContext, sourceBehavior, completion.Task);
            })
            .ToArray();
        Task<ConcurrentObservation>[] workers = scenarios
            .Select(scenario => Task.Run(() =>
            {
                barrier.SignalAndWait(TestContext.Current.CancellationToken);
                Task returned = Assert.IsAssignableFrom<Task>(untypedMethod.Invoke(
                    null,
                    [scenario.Behavior, scenario.Context, scenario.Exception, token]));
                return new ConcurrentObservation(scenario, returned);
            }, TestContext.Current.CancellationToken))
            .ToArray();

        barrier.SignalAndWait(TestContext.Current.CancellationToken);
        ConcurrentObservation[] observations = await Task.WhenAll(workers);

        Assert.All(observations, observation =>
        {
            Assert.Same(observation.Scenario.ExpectedTask, observation.ReturnedTask);
            Assert.Equal(1, observation.Scenario.Behavior.UntypedFaultCalls);
            Assert.Same(observation.Scenario.Exception, observation.Scenario.Behavior.LastFaultException);
            Assert.Same(
                observation.Scenario.Context,
                GetWrappedContext(observation.Scenario.Behavior.LastFaultContext!));
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-FAULT", "iteration-216-execute-on-faulted-exact-forwarding-identity")]
    public void ExecuteOnFaultedBehaviors_ForwardVisitorProbeStoredFaultContextAndTaskIdentityExactly()
    {
        var visitor = new RecordingVisitor();
        ProbeContext probe = CreateStrictProxy<ProbeContext>();
        IBehaviorContext<TestSaga> incoming = CreateStrictProxy<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, Message> typedIncoming = CreateStrictProxy<IBehaviorContext<TestSaga, Message>>();

        var untypedCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var untypedNext = new RecordingBehavior { UntypedFaultTask = untypedCompletion.Task };
        IBehaviorExceptionContext<TestSaga, MarkerException> untypedStored =
            CreateStrictProxy<IBehaviorExceptionContext<TestSaga, MarkerException>>();
        IBehavior<TestSaga> untyped = new ExecuteOnFaultedBehavior<TestSaga, MarkerException>(untypedNext, untypedStored);

        untyped.Accept(visitor);
        untyped.Probe(probe);
        Task untypedFromUntyped = untyped.ExecuteAsync(incoming);
        Task untypedFromTyped = untyped.ExecuteAsync(typedIncoming);

        Assert.Equal(1, untypedNext.AcceptCalls);
        Assert.Same(visitor, untypedNext.LastVisitor);
        Assert.Equal(1, untypedNext.ProbeCalls);
        Assert.Same(probe, untypedNext.LastProbe);
        Assert.Equal(2, untypedNext.UntypedFaultCalls);
        Assert.Same(untypedStored, untypedNext.LastFaultContext);
        Assert.Same(untypedCompletion.Task, untypedFromUntyped);
        Assert.Same(untypedCompletion.Task, untypedFromTyped);

        var typedCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var typedNext = new RecordingTypedBehavior { FaultTask = typedCompletion.Task };
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> typedStored =
            CreateStrictProxy<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
        IBehavior<TestSaga> typed = new ExecuteOnFaultedBehavior<TestSaga, Message, MarkerException>(typedNext, typedStored);

        typed.Accept(visitor);
        typed.Probe(probe);
        Task typedFromUntyped = typed.ExecuteAsync(incoming);
        Task typedFromTyped = typed.ExecuteAsync(typedIncoming);

        Assert.Equal(1, typedNext.AcceptCalls);
        Assert.Same(visitor, typedNext.LastVisitor);
        Assert.Equal(1, typedNext.ProbeCalls);
        Assert.Same(probe, typedNext.LastProbe);
        Assert.Equal(2, typedNext.FaultCalls);
        Assert.Same(typedStored, typedNext.LastFaultContext);
        Assert.Same(typedCompletion.Task, typedFromUntyped);
        Assert.Same(typedCompletion.Task, typedFromTyped);

        var forwardingFailure = new MarkerException("fault forwarding failed");
        untypedNext.Failure = forwardingFailure;
        Assert.Same(forwardingFailure, Assert.Throws<MarkerException>(() => { _ = untyped.ExecuteAsync(incoming); }));
        typedNext.Failure = forwardingFailure;
        Assert.Same(forwardingFailure, Assert.Throws<MarkerException>(() => { _ = typed.ExecuteAsync(typedIncoming); }));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-FAULT", "iteration-216-activity-behavior-visitor-probe-order-and-failure")]
    public void ActivityBehavior_VisitsAndProbesInExactOrderAndStopsAtTheFirstFailure()
    {
        var calls = new List<string>();
        var activity = new RecordingActivity(calls);
        var next = new RecordingBehavior(calls);
        IBehavior<TestSaga> behavior = CreateActivityBehavior(activity, next);
        var visitor = new RecordingVisitor(calls);
        ProbeContext probe = CreateStrictProxy<ProbeContext>();

        behavior.Accept(visitor);
        Assert.Equal(["behavior-visit", "activity-accept", "next-accept"], calls);
        Assert.Same(visitor, activity.LastVisitor);
        Assert.Same(visitor, next.LastVisitor);

        calls.Clear();
        behavior.Probe(probe);
        Assert.Equal(["activity-probe", "next-probe"], calls);
        Assert.Same(probe, activity.LastProbe);
        Assert.Same(probe, next.LastProbe);

        calls.Clear();
        var visitorFailure = new MarkerException("activity visitor failed");
        activity.AcceptFailure = visitorFailure;
        Assert.Same(visitorFailure, Assert.Throws<MarkerException>(() => behavior.Accept(visitor)));
        Assert.Equal(["behavior-visit", "activity-accept"], calls);
        Assert.Equal(1, next.AcceptCalls);

        calls.Clear();
        activity.AcceptFailure = null;
        var probeFailure = new MarkerException("activity probe failed");
        activity.ProbeFailure = probeFailure;
        Assert.Same(probeFailure, Assert.Throws<MarkerException>(() => behavior.Probe(probe)));
        Assert.Equal(["activity-probe"], calls);
        Assert.Equal(1, next.ProbeCalls);

        AssertParameter("visitor", () => behavior.Accept(null!));
        AssertParameter("context", () => behavior.Probe(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-FAULT", "iteration-216-activity-behavior-runtime-fault-dispatch")]
    public async Task ActivityBehavior_RoutesBothRuntimeFaultShapesWithExactExceptionContextAndAwaitedOutcomeAsync()
    {
        var untypedFailure = new MarkerException("untyped source failure");
        var untypedCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var untypedActivity = new RecordingActivity { ExecuteFailure = untypedFailure };
        var untypedNext = new RecordingBehavior
        {
            ReadFaultException = true,
            UntypedFaultTask = untypedCompletion.Task,
        };
        IBehavior<TestSaga> untypedBehavior = CreateActivityBehavior(untypedActivity, untypedNext);
        var untypedEvent = new TriggerEvent("Trigger");
        IBehaviorContext<TestSaga> untypedContext = CreateContext<IBehaviorContext<TestSaga>>(untypedEvent);

        Assert.False(untypedCompletion.Task.IsCompleted);
        Task untypedExecution = untypedBehavior.ExecuteAsync(untypedContext);

        Assert.Equal(1, untypedNext.UntypedFaultCalls);
        Assert.Same(untypedFailure, untypedNext.LastFaultException);
        Assert.False(untypedExecution.IsCompleted);
        var untypedFaultContext = Assert.IsAssignableFrom<IBehaviorExceptionContext<TestSaga, MarkerException>>(
            untypedNext.LastFaultContext);
        Assert.Same(untypedFailure, untypedFaultContext.Exception);
        Assert.Same(untypedEvent, untypedFaultContext.Event);
        untypedCompletion.SetResult(true);
        await untypedExecution;

        var typedFailure = new MarkerException("typed source failure");
        var typedCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var typedActivity = new RecordingActivity { ExecuteFailure = typedFailure };
        var typedNext = new RecordingBehavior
        {
            ReadFaultException = true,
            TypedFaultTask = typedCompletion.Task,
        };
        IBehavior<TestSaga> typedBehavior = CreateActivityBehavior(typedActivity, typedNext);
        var typedEvent = new MessageEvent<Message>("Message");
        var message = new Message("payload");
        IBehaviorContext<TestSaga, Message> typedContext = CreateContext<IBehaviorContext<TestSaga, Message>>(
            typedEvent,
            message);

        Assert.False(typedCompletion.Task.IsCompleted);
        Task typedExecution = typedBehavior.ExecuteAsync<Message>(typedContext);

        Assert.Equal(1, typedNext.TypedFaultCalls);
        Assert.Same(typedFailure, typedNext.LastFaultException);
        Assert.False(typedExecution.IsCompleted);
        var typedFaultContext = Assert.IsAssignableFrom<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>(
            typedNext.LastFaultContext);
        Assert.Same(typedFailure, typedFaultContext.Exception);
        Assert.Same(typedEvent, typedFaultContext.Event);
        Assert.Same(message, typedFaultContext.Message);
        typedCompletion.SetResult(true);
        await typedExecution;

        var handlerFailure = new MarkerException("fault handler failed");
        var failingActivity = new RecordingActivity { ExecuteFailure = untypedFailure };
        var failingNext = new RecordingBehavior
        {
            ReadFaultException = true,
            UntypedFaultTask = Task.FromException(handlerFailure),
        };
        IBehavior<TestSaga> failingBehavior = CreateActivityBehavior(failingActivity, failingNext);

        MarkerException propagated = await Assert.ThrowsAsync<MarkerException>(
            () => failingBehavior.ExecuteAsync(untypedContext));

        Assert.Same(handlerFailure, propagated);
        Assert.Same(untypedFailure, failingNext.LastFaultException);
        Assert.Equal(1, failingNext.UntypedFaultCalls);
    }

    private static IBehavior<TestSaga> CreateActivityBehavior(
        IStateMachineActivity<TestSaga> activity,
        IBehavior<TestSaga> next)
    {
        object instance = GetActivityBehaviorConstructor().Invoke([activity, next]);
        return Assert.IsAssignableFrom<IBehavior<TestSaga>>(instance);
    }

    private static ConstructorInfo GetActivityBehaviorConstructor()
    {
        Type definition = typeof(Behavior).Assembly.GetType(
            "ViciOne.ServiceBus.SagaStateMachine.ActivityBehavior`1",
            throwOnError: true)!;
        Type closed = definition.MakeGenericType(typeof(TestSaga));
        return Assert.Single(closed.GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
    }

    private static MethodInfo GetExceptionTypeCacheMethod(int genericArity)
    {
        Type cacheType = typeof(Behavior).Assembly.GetType(
            "ViciOne.ServiceBus.SagaStateMachine.ExceptionTypeCache",
            throwOnError: true)!;
        return Assert.Single(
            cacheType.GetMethods(BindingFlags.Public | BindingFlags.Static),
            candidate => candidate.Name == "FaultedAsync"
                && candidate.GetGenericArguments().Length == genericArity);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static CollectibleReferences PopulateExceptionTypeCacheWithCollectibleType()
    {
        var name = new AssemblyName($"ViciOne.CollectibleException.{Guid.NewGuid():N}");
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);
        ModuleBuilder module = assembly.DefineDynamicModule(name.Name!);
        TypeBuilder builder = module.DefineType(
            "CollectibleException",
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed,
            typeof(Exception));
        builder.DefineDefaultConstructor(MethodAttributes.Public);
        Type exceptionType = builder.CreateType()!;
        Exception exception = Assert.IsAssignableFrom<Exception>(Activator.CreateInstance(exceptionType));
        var behavior = new DiscardingBehavior();
        IBehaviorContext<TestSaga> context = CreateContext<IBehaviorContext<TestSaga>>(new TriggerEvent("Cache"));
        object? result = GetExceptionTypeCacheMethod(genericArity: 1).MakeGenericMethod(typeof(TestSaga)).Invoke(
            null,
            [behavior, context, exception, CancellationToken.None]);
        Assert.IsAssignableFrom<Task>(result).GetAwaiter().GetResult();

        return new CollectibleReferences(new WeakReference(exceptionType), new WeakReference(assembly));
    }

    private static object GetWrappedContext(object exceptionContext)
    {
        FieldInfo? field = exceptionContext.GetType().GetField(
            "_context",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(field);
        object? value = field.GetValue(exceptionContext);
        Assert.NotNull(value);
        return value;
    }

    private static object GetWrappedBehavior(object dataBehavior)
    {
        FieldInfo? field = dataBehavior.GetType().GetField(
            "_behavior",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(field);
        object? value = field.GetValue(dataBehavior);
        Assert.NotNull(value);
        return value;
    }

    private static T CreateContext<T>(CancellationToken cancellationToken, IEvent @event, object? message = null)
        where T : class
    {
        T context = DispatchProxy.Create<T, ContextProxy>();
        var proxy = (ContextProxy)(object)context;
        proxy.CancellationToken = cancellationToken;
        proxy.Event = @event;
        proxy.Message = message;
        proxy.StateMachine = CreateStrictProxy<IStateMachine<TestSaga>>();
        proxy.ReceiveContext = CreateReceiveContext();
        proxy.SerializerContext = CreateStrictProxy<SerializerContext>();
        return context;
    }

    private static T CreateContext<T>(IEvent @event, object? message = null)
        where T : class
    {
        T context = DispatchProxy.Create<T, ContextProxy>();
        var proxy = (ContextProxy)(object)context;
        proxy.Event = @event;
        proxy.Message = message;
        proxy.StateMachine = CreateStrictProxy<IStateMachine<TestSaga>>();
        proxy.ReceiveContext = CreateReceiveContext();
        proxy.SerializerContext = CreateStrictProxy<SerializerContext>();
        return context;
    }

    private static ReceiveContext CreateReceiveContext()
    {
        ReceiveContext context = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
        ((ReceiveContextProxy)(object)context).PublishEndpointProvider = CreateStrictProxy<IPublishEndpointProvider>();
        return context;
    }

    private static T CreateStrictProxy<T>()
        where T : class => DispatchProxy.Create<T, StrictProxy>();

    private static void AssertParameter(string expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<ArgumentNullException>(action).ParamName);

    private static void AssertReflectionParameter(string expected, Action action)
    {
        TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(action);
        ArgumentNullException argument = Assert.IsType<ArgumentNullException>(invocation.InnerException);
        Assert.Equal(expected, argument.ParamName);
    }

    private static void AssertInvariant(Action action) =>
        Assert.Equal(
            "This should not ever be called.",
            Assert.Throws<SagaStateMachineException>(action).Message);

    public class StrictProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new Xunit.Sdk.XunitException($"Unexpected proxy member: {targetMethod?.Name}");
    }

    public class ContextProxy : DispatchProxy
    {
        public CancellationToken CancellationToken { get; set; }
        public IEvent Event { get; set; } = null!;
        public object? Message { get; set; }
        public IStateMachine<TestSaga> StateMachine { get; set; } = null!;
        public ReceiveContext ReceiveContext { get; set; } = null!;
        public SerializerContext SerializerContext { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_CancellationToken" => CancellationToken,
            "get_Event" => Event,
            "get_Message" => Message,
            "get_Data" => Message,
            "get_StateMachine" => StateMachine,
            "get_ReceiveContext" => ReceiveContext,
            "get_SerializerContext" => SerializerContext,
            _ => throw new Xunit.Sdk.XunitException($"Unexpected context member: {targetMethod?.Name}"),
        };
    }

    public class ReceiveContextProxy : DispatchProxy
    {
        public IPublishEndpointProvider PublishEndpointProvider { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_PublishEndpointProvider" => PublishEndpointProvider,
            _ => throw new Xunit.Sdk.XunitException($"Unexpected receive-context member: {targetMethod?.Name}"),
        };
    }

    private sealed class RecordingActivity(List<string>? calls = null) : IStateMachineActivity<TestSaga>
    {
        public Action? BeforeExecute { get; init; }
        public Exception? ExecuteFailure { get; init; }
        public Task ExecuteTask { get; init; } = Task.CompletedTask;
        public Task UntypedFaultTask { get; init; } = Task.CompletedTask;
        public Task TypedFaultTask { get; init; } = Task.CompletedTask;
        public Exception? AcceptFailure { get; set; }
        public Exception? ProbeFailure { get; set; }
        public int UntypedExecuteCalls { get; private set; }
        public int TypedExecuteCalls { get; private set; }
        public int UntypedFaultCalls { get; private set; }
        public int TypedFaultCalls { get; private set; }
        public IStateMachineVisitor? LastVisitor { get; private set; }
        public ProbeContext? LastProbe { get; private set; }
        public object? LastFaultContext { get; private set; }
        public object? LastFaultNext { get; private set; }

        public void Accept(IStateMachineVisitor visitor)
        {
            calls?.Add("activity-accept");
            LastVisitor = visitor;
            if (AcceptFailure is not null)
                throw AcceptFailure;
        }

        public void Probe(ProbeContext context)
        {
            calls?.Add("activity-probe");
            LastProbe = context;
            if (ProbeFailure is not null)
                throw ProbeFailure;
        }

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context, IBehavior<TestSaga> next)
        {
            UntypedExecuteCalls++;
            return ExecuteCore();
        }

        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context, IBehavior<TestSaga, T> next)
            where T : class
        {
            TypedExecuteCalls++;
            return ExecuteCore();
        }

        public Task FaultedAsync<TException>(
            IBehaviorExceptionContext<TestSaga, TException> context,
            IBehavior<TestSaga> next)
            where TException : Exception
        {
            UntypedFaultCalls++;
            LastFaultContext = context;
            LastFaultNext = next;
            return UntypedFaultTask;
        }

        public Task FaultedAsync<T, TException>(
            IBehaviorExceptionContext<TestSaga, T, TException> context,
            IBehavior<TestSaga, T> next)
            where T : class
            where TException : Exception
        {
            TypedFaultCalls++;
            LastFaultContext = context;
            LastFaultNext = next;
            return TypedFaultTask;
        }

        private Task ExecuteCore()
        {
            BeforeExecute?.Invoke();
            if (ExecuteFailure is not null)
                throw ExecuteFailure;
            return ExecuteTask;
        }
    }

    private sealed class RecordingBehavior(List<string>? calls = null) : IBehavior<TestSaga>
    {
        public Task UntypedFaultTask { get; init; } = Task.CompletedTask;
        public Task TypedFaultTask { get; init; } = Task.CompletedTask;
        public bool ReadFaultException { get; init; }
        public TaskCompletionSource<bool>? FaultSignal { get; init; }
        public Exception? Failure { get; set; }
        public int AcceptCalls { get; private set; }
        public int ProbeCalls { get; private set; }
        public int UntypedFaultCalls { get; private set; }
        public int TypedFaultCalls { get; private set; }
        public int TotalCalls => AcceptCalls + ProbeCalls + UntypedFaultCalls + TypedFaultCalls;
        public IStateMachineVisitor? LastVisitor { get; private set; }
        public ProbeContext? LastProbe { get; private set; }
        public object? LastFaultContext { get; private set; }
        public Exception? LastFaultException { get; private set; }

        public void Accept(IStateMachineVisitor visitor)
        {
            calls?.Add("next-accept");
            AcceptCalls++;
            LastVisitor = visitor;
            ThrowIfConfigured();
        }

        public void Probe(ProbeContext context)
        {
            calls?.Add("next-probe");
            ProbeCalls++;
            LastProbe = context;
            ThrowIfConfigured();
        }

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context)
        {
            ThrowIfConfigured();
            return Task.CompletedTask;
        }

        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context)
            where T : class
        {
            ThrowIfConfigured();
            return Task.CompletedTask;
        }

        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context)
            where T : class
            where TException : Exception
        {
            TypedFaultCalls++;
            LastFaultContext = context;
            if (ReadFaultException)
                LastFaultException = context.Exception;
            FaultSignal?.TrySetResult(true);
            ThrowIfConfigured();
            return TypedFaultTask;
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context)
            where TException : Exception
        {
            UntypedFaultCalls++;
            LastFaultContext = context;
            if (ReadFaultException)
                LastFaultException = context.Exception;
            FaultSignal?.TrySetResult(true);
            ThrowIfConfigured();
            return UntypedFaultTask;
        }

        private void ThrowIfConfigured()
        {
            if (Failure is not null)
                throw Failure;
        }
    }

    private sealed class RecordingTypedBehavior : IBehavior<TestSaga, Message>
    {
        public Task FaultTask { get; init; } = Task.CompletedTask;
        public Exception? Failure { get; set; }
        public int AcceptCalls { get; private set; }
        public int ProbeCalls { get; private set; }
        public int FaultCalls { get; private set; }
        public int TotalCalls => AcceptCalls + ProbeCalls + FaultCalls;
        public IStateMachineVisitor? LastVisitor { get; private set; }
        public ProbeContext? LastProbe { get; private set; }
        public object? LastFaultContext { get; private set; }

        public void Accept(IStateMachineVisitor visitor)
        {
            AcceptCalls++;
            LastVisitor = visitor;
            ThrowIfConfigured();
        }

        public void Probe(ProbeContext context)
        {
            ProbeCalls++;
            LastProbe = context;
            ThrowIfConfigured();
        }

        public Task ExecuteAsync(IBehaviorContext<TestSaga, Message> context)
        {
            ThrowIfConfigured();
            return Task.CompletedTask;
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, Message, TException> context)
            where TException : Exception
        {
            FaultCalls++;
            LastFaultContext = context;
            ThrowIfConfigured();
            return FaultTask;
        }

        private void ThrowIfConfigured()
        {
            if (Failure is not null)
                throw Failure;
        }
    }

    private sealed class DiscardingBehavior : IBehavior<TestSaga>
    {
        public void Accept(IStateMachineVisitor visitor) => throw Unexpected();
        public void Probe(ProbeContext context) => throw Unexpected();
        public Task ExecuteAsync(IBehaviorContext<TestSaga> context) => throw Unexpected();
        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context) where T : class => throw Unexpected();

        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context)
            where T : class
            where TException : Exception => Task.CompletedTask;

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context)
            where TException : Exception => Task.CompletedTask;

        private static Exception Unexpected() =>
            new Xunit.Sdk.XunitException("Only fault dispatch was expected.");
    }

    private sealed class RecordingVisitor(List<string>? calls = null) : IStateMachineVisitor
    {
        public void Visit<T>(IBehavior<T> behavior, Action<IBehavior<T>> next)
            where T : class, ISagaStateMachineInstance
        {
            calls?.Add("behavior-visit");
            next(behavior);
        }

        public void Visit(IState state, Action<IState> next) => throw Unexpected();
        public void Visit(IEvent @event, Action<IEvent> next) => throw Unexpected();
        public void Visit<TMessage>(IEvent<TMessage> @event, Action<IEvent<TMessage>> next) where TMessage : class => throw Unexpected();
        public void Visit(IStateMachineActivity activity) => throw Unexpected();
        public void Visit(IStateMachineExceptionActivity activity, Action<IStateMachineExceptionActivity> next) => throw Unexpected();
        public void Visit<T>(IBehavior<T> behavior) where T : class, ISagaStateMachineInstance => throw Unexpected();
        public void Visit<T, TMessage>(IBehavior<T, TMessage> behavior)
            where T : class, ISagaStateMachineInstance where TMessage : class => throw Unexpected();
        public void Visit<T, TMessage>(IBehavior<T, TMessage> behavior, Action<IBehavior<T, TMessage>> next)
            where T : class, ISagaStateMachineInstance where TMessage : class => throw Unexpected();
        public void Visit(IStateMachineActivity activity, Action<IStateMachineActivity> next) => throw Unexpected();

        private static Exception Unexpected() =>
            new Xunit.Sdk.XunitException("An unrelated visitor overload was called.");
    }

    public sealed class TestSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public IState CurrentState { get; set; } = null!;
    }

    public sealed record Message(string Value);
    public sealed class MarkerException(string message) : Exception(message);
    private sealed class ConcurrentMarkerException(string message) : Exception(message);
    private sealed record ConcurrentScenario(
        ConcurrentMarkerException Exception,
        IBehaviorContext<TestSaga> Context,
        RecordingBehavior Behavior,
        Task ExpectedTask);
    private sealed record ConcurrentObservation(ConcurrentScenario Scenario, Task ReturnedTask);
    private sealed record CollectibleReferences(WeakReference Type, WeakReference Assembly);
}
