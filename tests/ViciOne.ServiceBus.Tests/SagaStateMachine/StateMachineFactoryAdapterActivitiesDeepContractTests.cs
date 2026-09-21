using System.Collections.Concurrent;
using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineFactoryAdapterActivitiesDeepContractTests
{
    public static IEnumerable<object[]> SynchronousFactoryCases =>
        from shape in Enum.GetValues<LifecycleShape>()
        from outcome in Enum.GetValues<CompletionOutcome>()
        select new object[] { shape, outcome };

    public static IEnumerable<object[]> AsyncFactoryCases =>
        from shape in Enum.GetValues<LifecycleShape>()
        from outcome in Enum.GetValues<AsyncOutcome>()
        select new object[] { shape, outcome };

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-219-factory-adapter-public-api-nullability")]
    public void PublicSurface_ExposesExactFactoryAdapterContractsConstraintsAndNullability()
    {
        AssertSagaConstraint(typeof(FactoryActivity<>), 0);
        AssertSagaConstraint(typeof(FactoryActivity<,>), 0);
        AssertReferenceConstraint(typeof(FactoryActivity<,>), 1);
        AssertSagaConstraint(typeof(AsyncFactoryActivity<>), 0);
        AssertSagaConstraint(typeof(AsyncFactoryActivity<,>), 0);
        AssertReferenceConstraint(typeof(AsyncFactoryActivity<,>), 1);
        AssertSagaConstraint(typeof(SlimActivity<,>), 0);
        AssertReferenceConstraint(typeof(SlimActivity<,>), 1);

        AssertTypeSurface(
            typeof(FactoryActivity<TestSaga>),
            typeof(IStateMachineActivity<TestSaga>),
            typeof(Func<IBehaviorContext<TestSaga>, IStateMachineActivity<TestSaga>>),
            "activityFactory",
            6);
        AssertTypeSurface(
            typeof(FactoryActivity<TestSaga, Message>),
            typeof(IStateMachineActivity<TestSaga, Message>),
            typeof(Func<IBehaviorContext<TestSaga, Message>, IStateMachineActivity<TestSaga, Message>>),
            "activityFactory",
            4);
        AssertTypeSurface(
            typeof(AsyncFactoryActivity<TestSaga>),
            typeof(IStateMachineActivity<TestSaga>),
            typeof(Func<IBehaviorContext<TestSaga>, Task<IStateMachineActivity<TestSaga>>>),
            "activityFactory",
            2);
        AssertTypeSurface(
            typeof(AsyncFactoryActivity<TestSaga, Message>),
            typeof(IStateMachineActivity<TestSaga, Message>),
            typeof(Func<IBehaviorContext<TestSaga, Message>, Task<IStateMachineActivity<TestSaga, Message>>>),
            "activityFactory",
            4);
        AssertTypeSurface(
            typeof(SlimActivity<TestSaga, Message>),
            typeof(IStateMachineActivity<TestSaga, Message>),
            typeof(IStateMachineActivity<TestSaga>),
            "activity",
            4);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-219-factory-adapter-required-input-boundaries")]
    public async Task ConstructorsAndLifecycleMembers_RejectNullInputsBeforeCollaboratorEffectsAsync()
    {
        AssertParam("activityFactory", () => _ = new FactoryActivity<TestSaga>(null!));
        AssertParam("activityFactory", () => _ = new FactoryActivity<TestSaga, Message>(null!));
        AssertParam("activityFactory", () => _ = new AsyncFactoryActivity<TestSaga>(null!));
        AssertParam("activityFactory", () => _ = new AsyncFactoryActivity<TestSaga, Message>(null!));
        AssertParam("activity", () => _ = new SlimActivity<TestSaga, Message>(null!));

        var factoryCalls = 0;
        var nested = new RecordingActivity(Task.CompletedTask);
        IStateMachineActivity<TestSaga>[] untyped =
        [
            new FactoryActivity<TestSaga>(_ =>
            {
                factoryCalls++;
                return nested;
            }),
            new AsyncFactoryActivity<TestSaga>(_ =>
            {
                factoryCalls++;
                return Task.FromResult<IStateMachineActivity<TestSaga>>(nested);
            }),
        ];
        IStateMachineActivity<TestSaga, Message>[] typed =
        [
            new FactoryActivity<TestSaga, Message>(_ =>
            {
                factoryCalls++;
                return nested;
            }),
            new AsyncFactoryActivity<TestSaga, Message>(_ =>
            {
                factoryCalls++;
                return Task.FromResult<IStateMachineActivity<TestSaga, Message>>(nested);
            }),
            new SlimActivity<TestSaga, Message>(nested),
        ];
        IBehaviorContext<TestSaga> context = CreateProxy<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, Message> messageContext = CreateProxy<IBehaviorContext<TestSaga, Message>>();
        IBehaviorExceptionContext<TestSaga, MarkerException> exceptionContext =
            CreateProxy<IBehaviorExceptionContext<TestSaga, MarkerException>>();
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> messageExceptionContext =
            CreateProxy<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
        var next = new RecordingBehavior();
        var messageNext = new RecordingMessageBehavior();

        foreach (IStateMachineActivity<TestSaga> activity in untyped)
        {
            await AssertParamAsync("context", () => activity.ExecuteAsync(null!, next));
            await AssertParamAsync("next", () => activity.ExecuteAsync(context, null!));
            await AssertParamAsync("context", () => activity.ExecuteAsync<Message>(null!, messageNext));
            await AssertParamAsync("next", () => activity.ExecuteAsync(messageContext, null!));
            await AssertParamAsync("context", () => activity.FaultedAsync<MarkerException>(null!, next));
            await AssertParamAsync("next", () => activity.FaultedAsync(exceptionContext, null!));
            await AssertParamAsync("context", () => activity.FaultedAsync<Message, MarkerException>(null!, messageNext));
            await AssertParamAsync("next", () => activity.FaultedAsync(messageExceptionContext, null!));
        }

        foreach (IStateMachineActivity<TestSaga, Message> activity in typed)
        {
            await AssertParamAsync("context", () => activity.ExecuteAsync(null!, messageNext));
            await AssertParamAsync("next", () => activity.ExecuteAsync(messageContext, null!));
            await AssertParamAsync("context", () => activity.FaultedAsync<MarkerException>(null!, messageNext));
            await AssertParamAsync("next", () => activity.FaultedAsync(messageExceptionContext, null!));
        }

        foreach (IStateMachineActivity activity in untyped.Cast<IStateMachineActivity>().Concat(typed))
        {
            AssertParam("visitor", () => activity.Accept(null!));
            AssertParam("context", () => activity.Probe(null!));
        }

        Assert.Equal(0, factoryCalls);
        Assert.Equal(0, nested.Calls);
        Assert.Equal(0, next.Calls);
        Assert.Equal(0, messageNext.Calls);
    }

    [Theory]
    [MemberData(nameof(SynchronousFactoryCases))]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-219-sync-factory-context-task-outcome-identity")]
    public async Task SynchronousFactories_PreserveFactoryContextActivityTaskAndOutcomeIdentityAsync(
        LifecycleShape shape,
        CompletionOutcome outcome)
    {
        Completion completion = CreateCompletion(outcome);
        var nested = new RecordingActivity(completion.Task);
        object? factoryContext = null;
        var factoryCalls = 0;

        Invocation invocation = InvokeSynchronousFactory(shape, context =>
        {
            factoryCalls++;
            factoryContext = context;
            return nested;
        });

        Assert.Same(completion.Task, invocation.Returned);
        Assert.Same(invocation.Context, factoryContext);
        Assert.Equal(1, nested.Calls);
        Assert.Equal(ExpectedOperation(shape), nested.Operation);
        Assert.Same(invocation.Context, nested.Context);
        Assert.Same(invocation.Next, nested.Next);
        Assert.Equal(0, Assert.IsAssignableFrom<ICallCounter>(invocation.Next).Calls);
        await AssertCompletionAsync(invocation.Returned, completion);
        Assert.Equal(1, factoryCalls);
    }

    [Theory]
    [InlineData(LifecycleShape.Execute)]
    [InlineData(LifecycleShape.ExecuteMessage)]
    [InlineData(LifecycleShape.Fault)]
    [InlineData(LifecycleShape.FaultMessage)]
    [InlineData(LifecycleShape.TypedExecute)]
    [InlineData(LifecycleShape.TypedFault)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-219-sync-factory-failure-identity")]
    public void SynchronousFactories_PreserveFactoryFailureIdentity(LifecycleShape shape)
    {
        var failure = new MarkerException("factory failed synchronously");
        var calls = 0;

        MarkerException actual = Assert.Throws<MarkerException>(() => InvokeSynchronousFactory(shape, _ =>
        {
            calls++;
            throw failure;
        }));

        Assert.Same(failure, actual);
        Assert.Equal(1, calls);

        var activityFailure = new MarkerException("activity failed synchronously");
        var nested = new RecordingActivity(Task.CompletedTask) { SynchronousFailure = activityFailure };
        var activityFactoryCalls = 0;

        MarkerException nestedActual = Assert.Throws<MarkerException>(() => InvokeSynchronousFactory(shape, _ =>
        {
            activityFactoryCalls++;
            return nested;
        }));

        Assert.Same(activityFailure, nestedActual);
        Assert.Equal(1, activityFactoryCalls);
        Assert.Equal(1, nested.Calls);
        Assert.Equal(ExpectedOperation(shape), nested.Operation);
        Assert.NotNull(nested.Context);
        Assert.Equal(0, Assert.IsAssignableFrom<ICallCounter>(nested.Next).Calls);
    }

    [Theory]
    [MemberData(nameof(AsyncFactoryCases))]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-219-async-factory-failure-cancellation-context")]
    public async Task AsyncFactories_PreserveContextAndFactoryOrActivityFailureAndCancellationIdentityAsync(
        LifecycleShape shape,
        AsyncOutcome outcome)
    {
        var factoryFailure = new MarkerException("factory failure");
        var activityFailure = new MarkerException("activity failure");
        using var factoryCancellation = new CancellationTokenSource();
        using var activityCancellation = new CancellationTokenSource();
        factoryCancellation.Cancel();
        activityCancellation.Cancel();
        Task activityTask = outcome switch
        {
            AsyncOutcome.ActivityFailure => Task.FromException(activityFailure),
            AsyncOutcome.ActivityCancellation => Task.FromCanceled(activityCancellation.Token),
            _ => Task.CompletedTask,
        };
        var nested = new RecordingActivity(activityTask);
        object? factoryContext = null;
        var factoryCalls = 0;
        object? Provider(object context)
        {
            factoryCalls++;
            factoryContext = context;
            return outcome switch
            {
                AsyncOutcome.FactoryThrows => throw factoryFailure,
                AsyncOutcome.FactoryTaskFailure => FailedFactoryTask(shape, factoryFailure),
                AsyncOutcome.FactoryCancellation => CanceledFactoryTask(shape, factoryCancellation.Token),
                _ => CompletedFactoryTask(shape, nested),
            };
        }

        Invocation invocation = InvokeAsyncFactory(shape, Provider);

        Assert.Equal(1, factoryCalls);
        Assert.Same(invocation.Context, factoryContext);
        switch (outcome)
        {
            case AsyncOutcome.FactoryThrows:
            case AsyncOutcome.FactoryTaskFailure:
                Assert.Same(factoryFailure, await Assert.ThrowsAsync<MarkerException>(() => invocation.Returned));
                Assert.Equal(0, nested.Calls);
                break;
            case AsyncOutcome.FactoryCancellation:
                {
                    OperationCanceledException canceled =
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => invocation.Returned);
                    Assert.Equal(factoryCancellation.Token, canceled.CancellationToken);
                    Assert.Equal(0, nested.Calls);
                    break;
                }
            case AsyncOutcome.ActivityFailure:
                Assert.Same(activityFailure, await Assert.ThrowsAsync<MarkerException>(() => invocation.Returned));
                AssertNestedInvocation(shape, invocation, nested);
                break;
            case AsyncOutcome.ActivityCancellation:
                {
                    OperationCanceledException canceled =
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => invocation.Returned);
                    Assert.Equal(activityCancellation.Token, canceled.CancellationToken);
                    AssertNestedInvocation(shape, invocation, nested);
                    break;
                }
            case AsyncOutcome.Success:
                await invocation.Returned;
                AssertNestedInvocation(shape, invocation, nested);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null);
        }

        Assert.Equal(0, Assert.IsAssignableFrom<ICallCounter>(invocation.Next).Calls);
        Assert.Equal(1, factoryCalls);
    }

    [Theory]
    [InlineData(LifecycleShape.Execute)]
    [InlineData(LifecycleShape.ExecuteMessage)]
    [InlineData(LifecycleShape.Fault)]
    [InlineData(LifecycleShape.FaultMessage)]
    [InlineData(LifecycleShape.TypedExecute)]
    [InlineData(LifecycleShape.TypedFault)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-219-factory-null-result-contract")]
    public async Task Factories_RejectNullFactoryTasksAndActivityResultsWithStableFailureAsync(LifecycleShape shape)
    {
        var syncFactoryCalls = 0;
        InvalidOperationException syncFailure = Assert.Throws<InvalidOperationException>(() =>
            InvokeSynchronousFactory(shape, _ =>
            {
                syncFactoryCalls++;
                return null;
            }));
        Assert.Equal("The activity factory returned null.", syncFailure.Message);
        Assert.Equal(1, syncFactoryCalls);

        var nullTaskFactoryCalls = 0;
        Invocation nullTask = InvokeAsyncFactory(shape, _ =>
        {
            nullTaskFactoryCalls++;
            return null;
        });
        InvalidOperationException nullTaskFailure =
            await Assert.ThrowsAsync<InvalidOperationException>(() => nullTask.Returned);
        Assert.Equal("The activity factory returned null.", nullTaskFailure.Message);
        Assert.Equal(1, nullTaskFactoryCalls);

        var nullResultFactoryCalls = 0;
        Invocation nullResult = InvokeAsyncFactory(shape, _ =>
        {
            nullResultFactoryCalls++;
            return NullFactoryResultTask(shape);
        });
        InvalidOperationException nullResultFailure =
            await Assert.ThrowsAsync<InvalidOperationException>(() => nullResult.Returned);
        Assert.Equal("The activity factory returned null.", nullResultFailure.Message);
        Assert.Equal(1, nullResultFactoryCalls);
        Assert.Equal(0, Assert.IsAssignableFrom<ICallCounter>(nullTask.Next).Calls);
        Assert.Equal(0, Assert.IsAssignableFrom<ICallCounter>(nullResult.Next).Calls);
    }

    [Theory]
    [InlineData(LifecycleShape.Execute)]
    [InlineData(LifecycleShape.ExecuteMessage)]
    [InlineData(LifecycleShape.Fault)]
    [InlineData(LifecycleShape.FaultMessage)]
    [InlineData(LifecycleShape.TypedExecute)]
    [InlineData(LifecycleShape.TypedFault)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-219-factory-continuation-and-async-sequencing")]
    public async Task Factories_InvokeExactContinuationAndAsyncFactoriesWaitForMaterializationAsync(LifecycleShape shape)
    {
        var syncNested = new RecordingActivity(Task.CompletedTask) { ContinuePipeline = true };
        var syncFactoryCalls = 0;
        Invocation sync = InvokeSynchronousFactory(shape, _ =>
        {
            syncFactoryCalls++;
            return syncNested;
        });
        await sync.Returned;
        AssertNestedInvocation(shape, sync, syncNested);
        Assert.Equal(1, syncFactoryCalls);
        Assert.Equal(1, Assert.IsAssignableFrom<ICallCounter>(sync.Next).Calls);

        var asyncNested = new RecordingActivity(Task.CompletedTask) { ContinuePipeline = true };
        PendingFactory pending = CreatePendingFactory(shape);
        var asyncFactoryCalls = 0;
        Invocation asyncInvocation = InvokeAsyncFactory(shape, _ =>
        {
            asyncFactoryCalls++;
            return pending.Task;
        });
        Assert.False(asyncInvocation.Returned.IsCompleted);
        Assert.Equal(1, asyncFactoryCalls);
        Assert.Equal(0, asyncNested.Calls);
        Assert.Equal(0, Assert.IsAssignableFrom<ICallCounter>(asyncInvocation.Next).Calls);

        pending.Complete(asyncNested);
        await asyncInvocation.Returned;

        AssertNestedInvocation(shape, asyncInvocation, asyncNested);
        Assert.Equal(1, asyncFactoryCalls);
        Assert.Equal(1, Assert.IsAssignableFrom<ICallCounter>(asyncInvocation.Next).Calls);

        var activityFailure = new MarkerException("activity failed synchronously after materialization");
        var throwingNested = new RecordingActivity(Task.CompletedTask) { SynchronousFailure = activityFailure };
        PendingFactory failingPending = CreatePendingFactory(shape);
        var failingFactoryCalls = 0;
        Invocation failingInvocation = InvokeAsyncFactory(shape, _ =>
        {
            failingFactoryCalls++;
            return failingPending.Task;
        });
        Assert.False(failingInvocation.Returned.IsCompleted);
        Assert.Equal(1, failingFactoryCalls);
        Assert.Equal(0, throwingNested.Calls);
        Assert.Equal(0, Assert.IsAssignableFrom<ICallCounter>(failingInvocation.Next).Calls);

        failingPending.Complete(throwingNested);

        Assert.Same(activityFailure,
            await Assert.ThrowsAsync<MarkerException>(() => failingInvocation.Returned));
        Assert.Equal(1, failingFactoryCalls);
        AssertNestedInvocation(shape, failingInvocation, throwingNested);
        Assert.Equal(0, Assert.IsAssignableFrom<ICallCounter>(failingInvocation.Next).Calls);
    }

    [Theory]
    [InlineData(false, CompletionOutcome.Success)]
    [InlineData(false, CompletionOutcome.Failure)]
    [InlineData(false, CompletionOutcome.Cancellation)]
    [InlineData(true, CompletionOutcome.Success)]
    [InlineData(true, CompletionOutcome.Failure)]
    [InlineData(true, CompletionOutcome.Cancellation)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-219-slim-adapter-identity-and-outcomes")]
    public async Task SlimActivity_PreservesExactContextNextTaskAndOutcomeIdentityAsync(
        bool faulted,
        CompletionOutcome outcome)
    {
        Completion completion = CreateCompletion(outcome);
        var nested = new RecordingActivity(completion.Task);
        var slim = new SlimActivity<TestSaga, Message>(nested);
        object context;
        object next;
        Task returned;
        if (faulted)
        {
            var typedContext = CreateProxy<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
            var typedNext = new RecordingMessageBehavior();
            context = typedContext;
            next = typedNext;
            returned = slim.FaultedAsync(typedContext, typedNext);
        }
        else
        {
            var typedContext = CreateProxy<IBehaviorContext<TestSaga, Message>>();
            var typedNext = new RecordingMessageBehavior();
            context = typedContext;
            next = typedNext;
            returned = slim.ExecuteAsync(typedContext, typedNext);
        }

        Assert.Same(completion.Task, returned);
        Assert.Equal(faulted ? "fault-message" : "execute-message", nested.Operation);
        Assert.Same(context, nested.Context);
        Assert.Same(next, nested.Next);
        Assert.Equal(0, ((ICallCounter)next).Calls);
        await AssertCompletionAsync(returned, completion);

        var activityFailure = new MarkerException("slim activity failed synchronously");
        var throwingNested = new RecordingActivity(Task.CompletedTask) { SynchronousFailure = activityFailure };
        var throwingSlim = new SlimActivity<TestSaga, Message>(throwingNested);
        var throwingNext = new RecordingMessageBehavior();
        object throwingContext;
        MarkerException throwingActual;
        if (faulted)
        {
            IBehaviorExceptionContext<TestSaga, Message, MarkerException> typedContext =
                CreateProxy<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
            throwingContext = typedContext;
            throwingActual = Assert.Throws<MarkerException>(() =>
            {
                _ = throwingSlim.FaultedAsync(typedContext, throwingNext);
            });
        }
        else
        {
            IBehaviorContext<TestSaga, Message> typedContext =
                CreateProxy<IBehaviorContext<TestSaga, Message>>();
            throwingContext = typedContext;
            throwingActual = Assert.Throws<MarkerException>(() =>
            {
                _ = throwingSlim.ExecuteAsync(typedContext, throwingNext);
            });
        }

        Assert.Same(activityFailure, throwingActual);
        Assert.Equal(1, throwingNested.Calls);
        Assert.Equal(faulted ? "fault-message" : "execute-message", throwingNested.Operation);
        Assert.Same(throwingContext, throwingNested.Context);
        Assert.Same(throwingNext, throwingNested.Next);
        Assert.Equal(0, throwingNext.Calls);

        if (outcome == CompletionOutcome.Success)
        {
            var continuingNested = new RecordingActivity(Task.CompletedTask) { ContinuePipeline = true };
            var continuingSlim = new SlimActivity<TestSaga, Message>(continuingNested);
            var continuingNext = new RecordingMessageBehavior();
            if (faulted)
            {
                IBehaviorExceptionContext<TestSaga, Message, MarkerException> continuingContext =
                    CreateProxy<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
                await continuingSlim.FaultedAsync(continuingContext, continuingNext);
                Assert.Same(continuingContext, continuingNested.Context);
            }
            else
            {
                IBehaviorContext<TestSaga, Message> continuingContext =
                    CreateProxy<IBehaviorContext<TestSaga, Message>>();
                await continuingSlim.ExecuteAsync(continuingContext, continuingNext);
                Assert.Same(continuingContext, continuingNested.Context);
            }

            Assert.Same(continuingNext, continuingNested.Next);
            Assert.Equal(1, continuingNext.Calls);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-219-factory-adapter-visitor-probe-contract")]
    public void FactoryWrappersInspectThemselvesWithoutMaterializationAndSlimInspectsTheWrappedActivity()
    {
        var factoryCalls = 0;
        var nested = new RecordingActivity(Task.CompletedTask);
        IStateMachineActivity[] factories =
        [
            new FactoryActivity<TestSaga>(_ =>
            {
                factoryCalls++;
                return nested;
            }),
            new FactoryActivity<TestSaga, Message>(_ =>
            {
                factoryCalls++;
                return nested;
            }),
            new AsyncFactoryActivity<TestSaga>(_ =>
            {
                factoryCalls++;
                return Task.FromResult<IStateMachineActivity<TestSaga>>(nested);
            }),
            new AsyncFactoryActivity<TestSaga, Message>(_ =>
            {
                factoryCalls++;
                return Task.FromResult<IStateMachineActivity<TestSaga, Message>>(nested);
            }),
        ];
        var visitor = new RecordingVisitor();
        var probe = new RecordingProbeContext();

        foreach (IStateMachineActivity factory in factories)
        {
            factory.Accept(visitor);
            factory.Probe(probe);
        }

        Assert.Equal(factories, visitor.Activities);
        Assert.Equal(["factory", "factory", "activityFactory", "activityFactory"], probe.Scopes);
        Assert.Equal(0, factoryCalls);
        Assert.Equal(0, nested.AcceptCalls);
        Assert.Equal(0, nested.ProbeCalls);

        var slim = new SlimActivity<TestSaga, Message>(nested);
        slim.Accept(visitor);
        slim.Probe(probe);
        Assert.Same(nested, visitor.Activities[^1]);
        Assert.Equal(1, nested.AcceptCalls);
        Assert.Equal(1, nested.ProbeCalls);
        Assert.Same(probe, nested.ProbeContext);
        Assert.Equal("recording", probe.Scopes[^1]);

        var inspectionFailure = new MarkerException("inspection failed");
        var failingVisitor = new RecordingVisitor { Failure = inspectionFailure };
        var failingProbe = new RecordingProbeContext { Failure = inspectionFailure };
        foreach (IStateMachineActivity factory in factories)
        {
            Assert.Same(inspectionFailure,
                Assert.Throws<MarkerException>(() => factory.Accept(failingVisitor)));
            Assert.Same(inspectionFailure,
                Assert.Throws<MarkerException>(() => factory.Probe(failingProbe)));
        }

        nested.AcceptFailure = inspectionFailure;
        nested.ProbeFailure = inspectionFailure;
        Assert.Same(inspectionFailure, Assert.Throws<MarkerException>(() => slim.Accept(new RecordingVisitor())));
        Assert.Same(inspectionFailure, Assert.Throws<MarkerException>(() => slim.Probe(new RecordingProbeContext())));
        Assert.Equal(0, factoryCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-219-factory-adapter-concurrent-context-pairing")]
    public async Task FactoryAndSlimActivities_KeepConcurrentContextAndContinuationPairsIndependentAsync()
    {
        const int count = 32;
        var syncPairs = new ConcurrentBag<(object Context, object Next)>();
        var asyncPairs = new ConcurrentBag<(object Context, object Next)>();
        var slimPairs = new ConcurrentBag<(object Context, object Next)>();
        var sync = new FactoryActivity<TestSaga>(context => new PairRecordingActivity(syncPairs, context));
        var asyncFactory = new AsyncFactoryActivity<TestSaga, Message>(async context =>
        {
            await Task.Yield();
            return new PairRecordingActivity(asyncPairs, context);
        });
        var slim = new SlimActivity<TestSaga, Message>(new PairRecordingActivity(slimPairs));

        (object Context, object Next)[] syncExpected = Enumerable.Range(0, count)
            .Select(_ => ((object)CreateProxy<IBehaviorContext<TestSaga>>(), (object)new RecordingBehavior()))
            .ToArray();
        (object Context, object Next)[] asyncExpected = Enumerable.Range(0, count)
            .Select(_ => ((object)CreateProxy<IBehaviorContext<TestSaga, Message>>(), (object)new RecordingMessageBehavior()))
            .ToArray();
        (object Context, object Next)[] slimExpected = Enumerable.Range(0, count)
            .Select(_ => ((object)CreateProxy<IBehaviorContext<TestSaga, Message>>(), (object)new RecordingMessageBehavior()))
            .ToArray();

        await Task.WhenAll(syncExpected.Select(pair => Task.Run(() =>
            sync.ExecuteAsync((IBehaviorContext<TestSaga>)pair.Context, (IBehavior<TestSaga>)pair.Next))));
        await Task.WhenAll(asyncExpected.Select(pair => Task.Run(() =>
            asyncFactory.ExecuteAsync(
                (IBehaviorContext<TestSaga, Message>)pair.Context,
                (IBehavior<TestSaga, Message>)pair.Next))));
        await Task.WhenAll(slimExpected.Select(pair => Task.Run(() =>
            slim.ExecuteAsync(
                (IBehaviorContext<TestSaga, Message>)pair.Context,
                (IBehavior<TestSaga, Message>)pair.Next))));

        AssertExactPairs(syncExpected, syncPairs);
        AssertExactPairs(asyncExpected, asyncPairs);
        AssertExactPairs(slimExpected, slimPairs);
        Assert.All(syncExpected.Concat(asyncExpected).Concat(slimExpected), pair =>
            Assert.Equal(1, Assert.IsAssignableFrom<ICallCounter>(pair.Next).Calls));
    }

    static Invocation InvokeSynchronousFactory(LifecycleShape shape, Func<object, object?> factory)
    {
        switch (shape)
        {
            case LifecycleShape.Execute:
                {
                    var context = CreateProxy<IBehaviorContext<TestSaga>>();
                    var next = new RecordingBehavior();
                    var wrapper = new FactoryActivity<TestSaga>(candidate =>
                        (IStateMachineActivity<TestSaga>)factory(candidate)!);
                    return new Invocation(wrapper.ExecuteAsync(context, next), context, next);
                }
            case LifecycleShape.ExecuteMessage:
                {
                    var context = CreateProxy<IBehaviorContext<TestSaga, Message>>();
                    var next = new RecordingMessageBehavior();
                    IStateMachineActivity<TestSaga> wrapper = new FactoryActivity<TestSaga>(candidate =>
                        (IStateMachineActivity<TestSaga>)factory(candidate)!);
                    return new Invocation(wrapper.ExecuteAsync(context, next), context, next);
                }
            case LifecycleShape.Fault:
                {
                    var context = CreateProxy<IBehaviorExceptionContext<TestSaga, MarkerException>>();
                    var next = new RecordingBehavior();
                    var wrapper = new FactoryActivity<TestSaga>(candidate =>
                        (IStateMachineActivity<TestSaga>)factory(candidate)!);
                    return new Invocation(wrapper.FaultedAsync(context, next), context, next);
                }
            case LifecycleShape.FaultMessage:
                {
                    var context = CreateProxy<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
                    var next = new RecordingMessageBehavior();
                    IStateMachineActivity<TestSaga> wrapper = new FactoryActivity<TestSaga>(candidate =>
                        (IStateMachineActivity<TestSaga>)factory(candidate)!);
                    return new Invocation(wrapper.FaultedAsync(context, next), context, next);
                }
            case LifecycleShape.TypedExecute:
                {
                    var context = CreateProxy<IBehaviorContext<TestSaga, Message>>();
                    var next = new RecordingMessageBehavior();
                    var wrapper = new FactoryActivity<TestSaga, Message>(candidate =>
                        (IStateMachineActivity<TestSaga, Message>)factory(candidate)!);
                    return new Invocation(wrapper.ExecuteAsync(context, next), context, next);
                }
            case LifecycleShape.TypedFault:
                {
                    var context = CreateProxy<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
                    var next = new RecordingMessageBehavior();
                    var wrapper = new FactoryActivity<TestSaga, Message>(candidate =>
                        (IStateMachineActivity<TestSaga, Message>)factory(candidate)!);
                    return new Invocation(wrapper.FaultedAsync(context, next), context, next);
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
        }
    }

    static Invocation InvokeAsyncFactory(LifecycleShape shape, Func<object, object?> factory)
    {
        switch (shape)
        {
            case LifecycleShape.Execute:
                {
                    var context = CreateProxy<IBehaviorContext<TestSaga>>();
                    var next = new RecordingBehavior();
                    IStateMachineActivity<TestSaga> wrapper = new AsyncFactoryActivity<TestSaga>(candidate =>
                        (Task<IStateMachineActivity<TestSaga>>)factory(candidate)!);
                    return new Invocation(wrapper.ExecuteAsync(context, next), context, next);
                }
            case LifecycleShape.ExecuteMessage:
                {
                    var context = CreateProxy<IBehaviorContext<TestSaga, Message>>();
                    var next = new RecordingMessageBehavior();
                    IStateMachineActivity<TestSaga> wrapper = new AsyncFactoryActivity<TestSaga>(candidate =>
                        (Task<IStateMachineActivity<TestSaga>>)factory(candidate)!);
                    return new Invocation(wrapper.ExecuteAsync(context, next), context, next);
                }
            case LifecycleShape.Fault:
                {
                    var context = CreateProxy<IBehaviorExceptionContext<TestSaga, MarkerException>>();
                    var next = new RecordingBehavior();
                    IStateMachineActivity<TestSaga> wrapper = new AsyncFactoryActivity<TestSaga>(candidate =>
                        (Task<IStateMachineActivity<TestSaga>>)factory(candidate)!);
                    return new Invocation(wrapper.FaultedAsync(context, next), context, next);
                }
            case LifecycleShape.FaultMessage:
                {
                    var context = CreateProxy<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
                    var next = new RecordingMessageBehavior();
                    IStateMachineActivity<TestSaga> wrapper = new AsyncFactoryActivity<TestSaga>(candidate =>
                        (Task<IStateMachineActivity<TestSaga>>)factory(candidate)!);
                    return new Invocation(wrapper.FaultedAsync(context, next), context, next);
                }
            case LifecycleShape.TypedExecute:
                {
                    var context = CreateProxy<IBehaviorContext<TestSaga, Message>>();
                    var next = new RecordingMessageBehavior();
                    var wrapper = new AsyncFactoryActivity<TestSaga, Message>(candidate =>
                        (Task<IStateMachineActivity<TestSaga, Message>>)factory(candidate)!);
                    return new Invocation(wrapper.ExecuteAsync(context, next), context, next);
                }
            case LifecycleShape.TypedFault:
                {
                    var context = CreateProxy<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
                    var next = new RecordingMessageBehavior();
                    var wrapper = new AsyncFactoryActivity<TestSaga, Message>(candidate =>
                        (Task<IStateMachineActivity<TestSaga, Message>>)factory(candidate)!);
                    return new Invocation(wrapper.FaultedAsync(context, next), context, next);
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
        }
    }

    static object CompletedFactoryTask(LifecycleShape shape, RecordingActivity activity) => IsTypedFactory(shape)
        ? Task.FromResult<IStateMachineActivity<TestSaga, Message>>(activity)
        : Task.FromResult<IStateMachineActivity<TestSaga>>(activity);

    static object FailedFactoryTask(LifecycleShape shape, Exception failure) => IsTypedFactory(shape)
        ? Task.FromException<IStateMachineActivity<TestSaga, Message>>(failure)
        : Task.FromException<IStateMachineActivity<TestSaga>>(failure);

    static object CanceledFactoryTask(LifecycleShape shape, CancellationToken cancellationToken) => IsTypedFactory(shape)
        ? Task.FromCanceled<IStateMachineActivity<TestSaga, Message>>(cancellationToken)
        : Task.FromCanceled<IStateMachineActivity<TestSaga>>(cancellationToken);

    static object NullFactoryResultTask(LifecycleShape shape) => IsTypedFactory(shape)
        ? Task.FromResult<IStateMachineActivity<TestSaga, Message>>(null!)
        : Task.FromResult<IStateMachineActivity<TestSaga>>(null!);

    static PendingFactory CreatePendingFactory(LifecycleShape shape)
    {
        if (IsTypedFactory(shape))
        {
            var source = new TaskCompletionSource<IStateMachineActivity<TestSaga, Message>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            return new PendingFactory(source.Task, activity => source.SetResult(activity));
        }

        var untypedSource = new TaskCompletionSource<IStateMachineActivity<TestSaga>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        return new PendingFactory(untypedSource.Task, activity => untypedSource.SetResult(activity));
    }

    static bool IsTypedFactory(LifecycleShape shape) =>
        shape is LifecycleShape.TypedExecute or LifecycleShape.TypedFault;

    static string ExpectedOperation(LifecycleShape shape) => shape switch
    {
        LifecycleShape.Execute => "execute",
        LifecycleShape.ExecuteMessage => "execute-message",
        LifecycleShape.Fault => "fault",
        LifecycleShape.FaultMessage => "fault-message",
        LifecycleShape.TypedExecute => "execute-message",
        LifecycleShape.TypedFault => "fault-message",
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
    };

    static void AssertNestedInvocation(LifecycleShape shape, Invocation invocation, RecordingActivity nested)
    {
        Assert.Equal(1, nested.Calls);
        Assert.Equal(ExpectedOperation(shape), nested.Operation);
        Assert.Same(invocation.Context, nested.Context);
        Assert.Same(invocation.Next, nested.Next);
    }

    static Completion CreateCompletion(CompletionOutcome outcome)
    {
        switch (outcome)
        {
            case CompletionOutcome.Success:
                {
                    var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    source.SetResult();
                    return new Completion(source.Task, null, null);
                }
            case CompletionOutcome.Failure:
                {
                    var failure = new MarkerException("activity failure");
                    return new Completion(Task.FromException(failure), failure, null);
                }
            case CompletionOutcome.Cancellation:
                {
                    using var source = new CancellationTokenSource();
                    source.Cancel();
                    return new Completion(Task.FromCanceled(source.Token), null, source.Token);
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null);
        }
    }

    static async Task AssertCompletionAsync(Task task, Completion completion)
    {
        if (completion.Failure is not null)
        {
            Assert.Same(completion.Failure, await Assert.ThrowsAsync<MarkerException>(() => task));
            return;
        }

        if (completion.CancellationToken is { } cancellationToken)
        {
            OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            Assert.Equal(cancellationToken, canceled.CancellationToken);
            return;
        }

        await task;
    }

    static void AssertExactPairs(
        IEnumerable<(object Context, object Next)> expected,
        IEnumerable<(object Context, object Next)> actual)
    {
        (object Context, object Next)[] expectedArray = expected.ToArray();
        (object Context, object Next)[] actualArray = actual.ToArray();
        Assert.Equal(expectedArray.Length, actualArray.Length);
        Assert.All(expectedArray, pair => Assert.Contains(actualArray,
            candidate => ReferenceEquals(pair.Context, candidate.Context) && ReferenceEquals(pair.Next, candidate.Next)));
    }

    static void AssertTypeSurface(
        Type type,
        Type contract,
        Type constructorParameterType,
        string constructorParameterName,
        int declaredPublicMethodCount)
    {
        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.False(type.IsAbstract);
        Assert.False(type.IsSealed);
        Assert.Contains(contract, type.GetInterfaces());
        ConstructorInfo constructor = Assert.Single(type.GetConstructors());
        ParameterInfo parameter = Assert.Single(constructor.GetParameters());
        Assert.Equal(constructorParameterType, parameter.ParameterType);
        Assert.Equal(constructorParameterName, parameter.Name);
        AssertRequired(parameter);
        AssertNotNullTree(new NullabilityInfoContext().Create(parameter));
        Assert.Equal(declaredPublicMethodCount,
            type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Count(method => !method.IsSpecialName));
        Assert.Empty(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
        Assert.Empty(type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));

        InterfaceMapping mapping = type.GetInterfaceMap(contract);
        Assert.NotEmpty(mapping.TargetMethods);
        foreach (MethodInfo method in mapping.TargetMethods)
        {
            if (method.ReturnType == typeof(Task))
                Assert.Equal(NullabilityState.NotNull, new NullabilityInfoContext().Create(method.ReturnParameter).ReadState);
            Assert.All(method.GetParameters().Where(candidate => !candidate.ParameterType.IsValueType), AssertRequired);
        }
    }

    static void AssertSagaConstraint(Type definition, int index)
    {
        Type parameter = definition.GetGenericArguments()[index];
        Assert.True(parameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        Assert.Equal([typeof(ISagaStateMachineInstance)], parameter.GetGenericParameterConstraints());
    }

    static void AssertReferenceConstraint(Type definition, int index)
    {
        Type parameter = definition.GetGenericArguments()[index];
        Assert.True(parameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        Assert.Empty(parameter.GetGenericParameterConstraints());
    }

    static void AssertRequired(ParameterInfo parameter)
    {
        Assert.False(parameter.IsOptional);
        Assert.False(parameter.HasDefaultValue);
        Assert.Equal(NullabilityState.NotNull, new NullabilityInfoContext().Create(parameter).ReadState);
    }

    static void AssertNotNullTree(NullabilityInfo info)
    {
        Assert.Equal(NullabilityState.NotNull, info.ReadState);
        if (info.ElementType is not null)
            AssertNotNullTree(info.ElementType);
        foreach (NullabilityInfo argument in info.GenericTypeArguments)
            AssertNotNullTree(argument);
    }

    static void AssertParam(string name, Action action) =>
        Assert.Equal(name, Assert.Throws<ArgumentNullException>(action).ParamName);

    static async Task AssertParamAsync(string name, Func<Task> action)
    {
        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.Equal(name, exception.ParamName);
    }

    static T CreateProxy<T>() where T : class => DispatchProxy.Create<T, StrictProxy>();

    public enum LifecycleShape
    {
        Execute,
        ExecuteMessage,
        Fault,
        FaultMessage,
        TypedExecute,
        TypedFault,
    }

    public enum CompletionOutcome
    {
        Success,
        Failure,
        Cancellation,
    }

    public enum AsyncOutcome
    {
        Success,
        FactoryThrows,
        FactoryTaskFailure,
        FactoryCancellation,
        ActivityFailure,
        ActivityCancellation,
    }

    public sealed class TestSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed record Message;

    public sealed class MarkerException(string message) : Exception(message);

    sealed record Invocation(Task Returned, object Context, object Next);

    sealed record Completion(Task Task, MarkerException? Failure, CancellationToken? CancellationToken);

    sealed record PendingFactory(object Task, Action<RecordingActivity> Complete);

    interface ICallCounter
    {
        int Calls { get; }
    }

    sealed class RecordingActivity(Task completion) :
        IStateMachineActivity<TestSaga>,
        IStateMachineActivity<TestSaga, Message>
    {
        int _calls;

        public int AcceptCalls { get; private set; }
        public MarkerException? AcceptFailure { get; set; }
        public int Calls => Volatile.Read(ref _calls);
        public bool ContinuePipeline { get; init; }
        public object? Context { get; private set; }
        public object? Next { get; private set; }
        public string? Operation { get; private set; }
        public int ProbeCalls { get; private set; }
        public MarkerException? ProbeFailure { get; set; }
        public ProbeContext? ProbeContext { get; private set; }
        public MarkerException? SynchronousFailure { get; init; }

        public void Accept(IStateMachineVisitor visitor)
        {
            AcceptCalls++;
            if (AcceptFailure is not null)
                throw AcceptFailure;
            visitor.Visit(this);
        }

        public void Probe(ProbeContext context)
        {
            ProbeCalls++;
            ProbeContext = context;
            if (ProbeFailure is not null)
                throw ProbeFailure;
            context.CreateScope("recording");
        }

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context, IBehavior<TestSaga> next) =>
            RecordAsync("execute", context, next, () => next.ExecuteAsync(context));

        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context, IBehavior<TestSaga, T> next)
            where T : class => RecordAsync("execute-message", context, next, () => next.ExecuteAsync(context));

        public Task FaultedAsync<TException>(
            IBehaviorExceptionContext<TestSaga, TException> context,
            IBehavior<TestSaga> next)
            where TException : Exception => RecordAsync("fault", context, next, () => next.FaultedAsync(context));

        public Task FaultedAsync<T, TException>(
            IBehaviorExceptionContext<TestSaga, T, TException> context,
            IBehavior<TestSaga, T> next)
            where T : class
            where TException : Exception => RecordAsync("fault-message", context, next, () => next.FaultedAsync(context));

        Task IStateMachineActivity<TestSaga, Message>.ExecuteAsync(
            IBehaviorContext<TestSaga, Message> context,
            IBehavior<TestSaga, Message> next) =>
            RecordAsync("execute-message", context, next, () => next.ExecuteAsync(context));

        Task IStateMachineActivity<TestSaga, Message>.FaultedAsync<TException>(
            IBehaviorExceptionContext<TestSaga, Message, TException> context,
            IBehavior<TestSaga, Message> next) =>
            RecordAsync("fault-message", context, next, () => next.FaultedAsync(context));

        Task RecordAsync(string operation, object context, object next, Func<Task> continuation)
        {
            Interlocked.Increment(ref _calls);
            Operation = operation;
            Context = context;
            Next = next;
            if (SynchronousFailure is not null)
                throw SynchronousFailure;
            return ContinuePipeline ? continuation() : completion;
        }
    }

    sealed class PairRecordingActivity(
        ConcurrentBag<(object Context, object Next)> pairs,
        object? factoryContext = null) :
        IStateMachineActivity<TestSaga>,
        IStateMachineActivity<TestSaga, Message>
    {
        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);
        public void Probe(ProbeContext context) => context.CreateScope("pair");

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context, IBehavior<TestSaga> next) => RecordAsync(context, next, () => next.ExecuteAsync(context));
        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context, IBehavior<TestSaga, T> next) where T : class =>
            RecordAsync(context, next, () => next.ExecuteAsync(context));
        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context, IBehavior<TestSaga> next)
            where TException : Exception => RecordAsync(context, next, () => next.FaultedAsync(context));
        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context, IBehavior<TestSaga, T> next)
            where T : class where TException : Exception => RecordAsync(context, next, () => next.FaultedAsync(context));
        Task IStateMachineActivity<TestSaga, Message>.ExecuteAsync(IBehaviorContext<TestSaga, Message> context, IBehavior<TestSaga, Message> next) =>
            RecordAsync(context, next, () => next.ExecuteAsync(context));
        Task IStateMachineActivity<TestSaga, Message>.FaultedAsync<TException>(
            IBehaviorExceptionContext<TestSaga, Message, TException> context,
            IBehavior<TestSaga, Message> next) => RecordAsync(context, next, () => next.FaultedAsync(context));

        Task RecordAsync(object context, object next, Func<Task> continuation)
        {
            if (factoryContext is not null)
                Assert.Same(factoryContext, context);
            pairs.Add((context, next));
            return continuation();
        }
    }

    sealed class RecordingBehavior : IBehavior<TestSaga>, ICallCounter
    {
        int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);
        public void Probe(ProbeContext context) => context.CreateScope("next");
        public Task ExecuteAsync(IBehaviorContext<TestSaga> context) { Interlocked.Increment(ref _calls); return Task.CompletedTask; }
        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context) where T : class { Interlocked.Increment(ref _calls); return Task.CompletedTask; }
        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context)
            where T : class where TException : Exception
        { Interlocked.Increment(ref _calls); return Task.CompletedTask; }
        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context)
            where TException : Exception
        { Interlocked.Increment(ref _calls); return Task.CompletedTask; }
    }

    sealed class RecordingMessageBehavior : IBehavior<TestSaga, Message>, ICallCounter
    {
        int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public void Accept(IStateMachineVisitor visitor) => visitor.Visit<TestSaga, Message>(this);
        public void Probe(ProbeContext context) => context.CreateScope("next");
        public Task ExecuteAsync(IBehaviorContext<TestSaga, Message> context) { Interlocked.Increment(ref _calls); return Task.CompletedTask; }
        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, Message, TException> context)
            where TException : Exception
        { Interlocked.Increment(ref _calls); return Task.CompletedTask; }
    }

    sealed class RecordingVisitor : IStateMachineVisitor
    {
        public List<IStateMachineActivity> Activities { get; } = [];
        public MarkerException? Failure { get; init; }
        public void Visit(IState state, Action<IState> next) => next(state);
        public void Visit(IEvent @event, Action<IEvent> next) => next(@event);
        public void Visit<T>(IEvent<T> @event, Action<IEvent<T>> next) where T : class => next(@event);
        public void Visit(IStateMachineActivity activity)
        {
            if (Failure is not null)
                throw Failure;
            Activities.Add(activity);
        }
        public void Visit(IStateMachineExceptionActivity activity, Action<IStateMachineExceptionActivity> next) => next(activity);
        public void Visit<T>(IBehavior<T> behavior) where T : class, ISagaStateMachineInstance { }
        public void Visit<T>(IBehavior<T> behavior, Action<IBehavior<T>> next) where T : class, ISagaStateMachineInstance => next(behavior);
        public void Visit<T, TData>(IBehavior<T, TData> behavior)
            where T : class, ISagaStateMachineInstance where TData : class
        { }
        public void Visit<T, TData>(IBehavior<T, TData> behavior, Action<IBehavior<T, TData>> next)
            where T : class, ISagaStateMachineInstance where TData : class => next(behavior);
        public void Visit(IStateMachineActivity activity, Action<IStateMachineActivity> next) { Activities.Add(activity); next(activity); }
    }

    sealed class RecordingProbeContext : ProbeContext
    {
        public CancellationToken CancellationToken => default;
        public MarkerException? Failure { get; init; }
        public List<string> Scopes { get; } = [];
        public void Add(string key, string? value) { }
        public void Add(string key, object? value) { }
        public void Set(object values) { }
        public void Set(IEnumerable<KeyValuePair<string, object?>> values) { }
        public ProbeContext CreateScope(string key)
        {
            if (Failure is not null)
                throw Failure;
            Scopes.Add(key);
            return this;
        }
    }

    public class StrictProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new Xunit.Sdk.XunitException($"Unexpected context member: {targetMethod?.Name}.");
    }
}
