using System.Collections.Concurrent;
using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineConditionFaultActivitiesDeepContractTests
{
    static readonly TimeSpan ObservationTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-220-condition-fault-exact-public-contract")]
    public void PublicSurface_HasExactTypesDelegatesConstraintsInterfacesAndNullability()
    {
        AssertActivityType(typeof(CatchFaultActivity<,>), ["TSaga", "TException"]);
        AssertActivityType(typeof(ConditionActivity<>), ["TSaga"]);
        AssertActivityType(typeof(ConditionActivity<,>), ["TSaga", "TMessage"]);
        AssertActivityType(typeof(ConditionExceptionActivity<,>), ["TSaga", "TConditionException"]);
        AssertActivityType(typeof(ConditionExceptionActivity<,,>), ["TSaga", "TMessage", "TConditionException"]);

        string[] conditionMethods = ["Accept/0", "ExecuteAsync/0", "ExecuteAsync/1", "FaultedAsync/1", "FaultedAsync/2"];
        AssertDeclaredSurface(typeof(CatchFaultActivity<,>),
            ["Accept/0", "ExecuteAsync/0", "ExecuteAsync/1", "FaultedAsync/1", "FaultedAsync/2", "Probe/0"],
            ["ExceptionType"]);
        AssertDeclaredSurface(typeof(ConditionActivity<>), conditionMethods, []);
        AssertDeclaredSurface(typeof(ConditionActivity<,>), conditionMethods, []);
        AssertDeclaredSurface(typeof(ConditionExceptionActivity<,>), conditionMethods, []);
        AssertDeclaredSurface(typeof(ConditionExceptionActivity<,,>), conditionMethods, []);

        AssertClosedConstructor<CatchFaultActivity<TestSaga, MarkerException>>(
            (typeof(IBehavior<TestSaga>), "behavior"));
        AssertClosedConstructor<ConditionActivity<TestSaga>>(
            (typeof(StateMachineAsyncCondition<TestSaga>), "condition"),
            (typeof(IBehavior<TestSaga>), "thenBehavior"),
            (typeof(IBehavior<TestSaga>), "elseBehavior"));
        AssertClosedConstructor<ConditionActivity<TestSaga, Message>>(
            (typeof(StateMachineAsyncCondition<TestSaga, Message>), "condition"),
            (typeof(IBehavior<TestSaga>), "thenBehavior"),
            (typeof(IBehavior<TestSaga>), "elseBehavior"));
        AssertClosedConstructor<ConditionExceptionActivity<TestSaga, MarkerException>>(
            (typeof(StateMachineAsyncExceptionCondition<TestSaga, MarkerException>), "condition"),
            (typeof(IBehavior<TestSaga>), "thenBehavior"),
            (typeof(IBehavior<TestSaga>), "elseBehavior"));
        AssertClosedConstructor<ConditionExceptionActivity<TestSaga, Message, MarkerException>>(
            (typeof(StateMachineAsyncExceptionCondition<TestSaga, Message, MarkerException>), "condition"),
            (typeof(IBehavior<TestSaga>), "thenBehavior"),
            (typeof(IBehavior<TestSaga>), "elseBehavior"));

        Assert.True(typeof(IStateMachineExceptionActivity)
            .IsAssignableFrom(typeof(CatchFaultActivity<TestSaga, MarkerException>)));
        var nullability = new NullabilityInfoContext();
        PropertyInfo exceptionType = Assert.Single(
            typeof(CatchFaultActivity<TestSaga, MarkerException>).GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Equal("ExceptionType", exceptionType.Name);
        Assert.Equal(typeof(Type), exceptionType.PropertyType);
        Assert.True(exceptionType.CanRead);
        Assert.False(exceptionType.CanWrite);
        MethodInfo getter = Assert.IsAssignableFrom<MethodInfo>(exceptionType.GetMethod);
        Assert.True(getter.IsPublic);
        Assert.Null(exceptionType.SetMethod);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(exceptionType).ReadState);
        Assert.Equal(typeof(MarkerException),
            new CatchFaultActivity<TestSaga, MarkerException>(new RecordingBehavior()).ExceptionType);
        Assert.False(typeof(IStateMachineActivity<TestSaga, Message>)
            .IsAssignableFrom(typeof(ConditionActivity<TestSaga, Message>)));
        Assert.False(typeof(IStateMachineActivity<TestSaga, Message>)
            .IsAssignableFrom(typeof(ConditionExceptionActivity<TestSaga, Message, MarkerException>)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-220-condition-fault-null-boundaries")]
    public async Task ConstructorsAndInterfaceMethods_RejectEveryNullBeforeAnyCollaboratorEffectAsync()
    {
        var behavior = new RecordingBehavior();
        StateMachineAsyncCondition<TestSaga> condition = _ => Task.FromResult(true);
        StateMachineAsyncCondition<TestSaga, Message> typedCondition = _ => Task.FromResult(true);
        StateMachineAsyncExceptionCondition<TestSaga, MarkerException> exceptionCondition = _ => Task.FromResult(true);
        StateMachineAsyncExceptionCondition<TestSaga, Message, MarkerException> typedExceptionCondition =
            _ => Task.FromResult(true);

        AssertArgument("behavior", () => new CatchFaultActivity<TestSaga, MarkerException>(null!));
        AssertConditionConstructorNulls(condition, behavior);
        AssertTypedConditionConstructorNulls(typedCondition, behavior);
        AssertExceptionConditionConstructorNulls(exceptionCondition, behavior);
        AssertTypedExceptionConditionConstructorNulls(typedExceptionCondition, behavior);

        var effects = 0;
        IStateMachineActivity<TestSaga>[] activities =
        [
            new CatchFaultActivity<TestSaga, MarkerException>(new RecordingBehavior(effect: () => effects++)),
            new ConditionActivity<TestSaga>(_ =>
            {
                effects++;
                return Task.FromResult(true);
            }, new RecordingBehavior(effect: () => effects++), new RecordingBehavior(effect: () => effects++)),
            new ConditionActivity<TestSaga, Message>(_ =>
            {
                effects++;
                return Task.FromResult(true);
            }, new RecordingBehavior(effect: () => effects++), new RecordingBehavior(effect: () => effects++)),
            new ConditionExceptionActivity<TestSaga, MarkerException>(_ =>
            {
                effects++;
                return Task.FromResult(true);
            }, new RecordingBehavior(effect: () => effects++), new RecordingBehavior(effect: () => effects++)),
            new ConditionExceptionActivity<TestSaga, Message, MarkerException>(_ =>
            {
                effects++;
                return Task.FromResult(true);
            }, new RecordingBehavior(effect: () => effects++), new RecordingBehavior(effect: () => effects++)),
        ];
        IBehaviorContext<TestSaga> context = CreateContext<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, Message> typedContext = CreateContext<IBehaviorContext<TestSaga, Message>>();
        IBehaviorExceptionContext<TestSaga, MarkerException> fault =
            CreateContext<IBehaviorExceptionContext<TestSaga, MarkerException>>();
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> typedFault =
            CreateContext<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
        var next = new RecordingBehavior(effect: () => effects++);
        var typedNext = new RecordingTypedBehavior<Message>(() => effects++);

        foreach (IStateMachineActivity<TestSaga> activity in activities)
        {
            AssertArgument("visitor", () => activity.Accept(null!));
            AssertArgument("context", () => ((IProbeSite)activity).Probe(null!));
            await AssertArgumentAsync("context", () => activity.ExecuteAsync(null!, next));
            await AssertArgumentAsync("next", () => activity.ExecuteAsync(context, null!));
            await AssertArgumentAsync("context", () => activity.ExecuteAsync<Message>(null!, typedNext));
            await AssertArgumentAsync("next", () => activity.ExecuteAsync(typedContext, null!));
            await AssertArgumentAsync("context", () => activity.FaultedAsync<MarkerException>(null!, next));
            await AssertArgumentAsync("next", () => activity.FaultedAsync(fault, null!));
            await AssertArgumentAsync("context", () => activity.FaultedAsync<Message, MarkerException>(null!, typedNext));
            await AssertArgumentAsync("next", () => activity.FaultedAsync(typedFault, null!));
        }

        Assert.Equal(0, effects);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-220-condition-routing-outcome-matrix")]
    public async Task ConditionActivities_GateTrueFalseTypedAndNonmatchingRoutesWithExactOutcomesAsync()
    {
        IBehaviorContext<TestSaga> context = CreateContext<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, Message> typedContext = CreateContext<IBehaviorContext<TestSaga, Message>>();
        IBehaviorContext<TestSaga, DerivedMessage> derivedContext = CreateContext<IBehaviorContext<TestSaga, DerivedMessage>>();
        IBehaviorContext<TestSaga, OtherMessage> otherContext = CreateContext<IBehaviorContext<TestSaga, OtherMessage>>();
        var order = new List<string>();
        var conditionGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var then = new RecordingBehavior(execute: observed =>
        {
            order.Add("then");
            Assert.Same(context, observed);
            return Task.CompletedTask;
        });
        var @else = new RecordingBehavior(execute: _ =>
        {
            order.Add("else");
            return Task.CompletedTask;
        });
        var next = new RecordingBehavior(execute: observed =>
        {
            order.Add("next");
            Assert.Same(context, observed);
            return Task.CompletedTask;
        });
        var activity = new ConditionActivity<TestSaga>(async observed =>
        {
            order.Add("condition");
            Assert.Same(context, observed);
            return await conditionGate.Task;
        }, then, @else);

        Task pending = activity.ExecuteAsync(context, next);
        Assert.Equal(["condition"], order);
        conditionGate.SetResult(true);
        await pending;
        Assert.Equal(["condition", "then", "next"], order);

        order.Clear();
        var falseActivity = new ConditionActivity<TestSaga>(_ => Task.FromResult(false), then, @else);
        await falseActivity.ExecuteAsync(context, next);
        Assert.Equal(["else", "next"], order);

        var genericThenCalls = 0;
        var genericElseCalls = 0;
        var genericNext = new RecordingTypedBehavior<Message>();
        await new ConditionActivity<TestSaga>(_ => Task.FromResult(true),
                new RecordingBehavior(execute: observed =>
                {
                    Assert.Same(typedContext, observed);
                    genericThenCalls++;
                    return Task.CompletedTask;
                }),
                new RecordingBehavior(execute: _ =>
                {
                    genericElseCalls++;
                    return Task.CompletedTask;
                }))
            .ExecuteAsync(typedContext, genericNext);
        Assert.Equal(1, genericThenCalls);
        Assert.Equal(0, genericElseCalls);
        Assert.Single(genericNext.ExecuteContexts);

        await new ConditionActivity<TestSaga>(_ => Task.FromResult(false),
                new RecordingBehavior(execute: _ => throw new InvalidOperationException("Unexpected then branch.")),
                new RecordingBehavior(execute: observed =>
                {
                    Assert.Same(typedContext, observed);
                    genericElseCalls++;
                    return Task.CompletedTask;
                }))
            .ExecuteAsync(typedContext, genericNext);
        Assert.Equal(1, genericThenCalls);
        Assert.Equal(1, genericElseCalls);
        Assert.Equal(2, genericNext.ExecuteContexts.Count);

        var typedConditionCalls = 0;
        var typedThenCalls = 0;
        var typedElseCalls = 0;
        var typedActivity = new ConditionActivity<TestSaga, BaseMessage>(observed =>
        {
            Assert.Same(derivedContext, observed);
            typedConditionCalls++;
            return Task.FromResult(true);
        },
        new RecordingBehavior(execute: observed =>
        {
            Assert.Same(derivedContext, observed);
            typedThenCalls++;
            return Task.CompletedTask;
        }),
        new RecordingBehavior(execute: _ =>
        {
            typedElseCalls++;
            return Task.CompletedTask;
        }));
        var derivedNext = new RecordingTypedBehavior<DerivedMessage>();
        await typedActivity.ExecuteAsync(derivedContext, derivedNext);
        Assert.Equal((1, 1, 0), (typedConditionCalls, typedThenCalls, typedElseCalls));
        Assert.Single(derivedNext.ExecuteContexts);

        var typedFalseActivity = new ConditionActivity<TestSaga, BaseMessage>(_ => Task.FromResult(false),
            new RecordingBehavior(execute: _ => throw new InvalidOperationException("Unexpected then branch.")),
            new RecordingBehavior(execute: observed =>
            {
                Assert.Same(derivedContext, observed);
                typedElseCalls++;
                return Task.CompletedTask;
            }));
        await typedFalseActivity.ExecuteAsync(derivedContext, derivedNext);
        Assert.Equal((1, 1, 1), (typedConditionCalls, typedThenCalls, typedElseCalls));
        Assert.Equal(2, derivedNext.ExecuteContexts.Count);

        var otherNext = new RecordingTypedBehavior<OtherMessage>();
        await typedActivity.ExecuteAsync(otherContext, otherNext);
        Assert.Equal((1, 1, 1), (typedConditionCalls, typedThenCalls, typedElseCalls));
        Assert.Single(otherNext.ExecuteContexts);

        SagaStateMachineException bodyRequired = Assert.Throws<SagaStateMachineException>((Action)(() =>
            _ = typedActivity.ExecuteAsync(context, next)));
        Assert.Equal("This activity requires a body with the event, but no body was specified.", bodyRequired.Message);

        await AssertConditionFailureCancellationAndNullTaskOutcomesAsync(context, typedContext);
        AssertConditionDirectTaskIdentity(context, typedContext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RECOVERY", "iteration-220-fault-covariance-compensation-matrix")]
    public async Task ExceptionConditionsAndCatch_RouteCovariantMatchingAndNonmatchingFaultsWithExactOutcomesAsync()
    {
        IBehaviorExceptionContext<TestSaga, BaseMarkerException> exact =
            CreateContext<IBehaviorExceptionContext<TestSaga, BaseMarkerException>>();
        IBehaviorExceptionContext<TestSaga, DerivedMarkerException> derived =
            CreateContext<IBehaviorExceptionContext<TestSaga, DerivedMarkerException>>();
        IBehaviorExceptionContext<TestSaga, MarkerException> nonmatching =
            CreateContext<IBehaviorExceptionContext<TestSaga, MarkerException>>();
        IBehaviorExceptionContext<TestSaga, DerivedMessage, DerivedMarkerException> typedDerived =
            CreateContext<IBehaviorExceptionContext<TestSaga, DerivedMessage, DerivedMarkerException>>();
        IBehaviorExceptionContext<TestSaga, DerivedMessage, MarkerException> typedNonmatchingException =
            CreateContext<IBehaviorExceptionContext<TestSaga, DerivedMessage, MarkerException>>();
        IBehaviorExceptionContext<TestSaga, OtherMessage, DerivedMarkerException> otherMessage =
            CreateContext<IBehaviorExceptionContext<TestSaga, OtherMessage, DerivedMarkerException>>();

        var conditionCalls = 0;
        var thenCalls = 0;
        var elseCalls = 0;
        var exceptionActivity = new ConditionExceptionActivity<TestSaga, BaseMarkerException>(observed =>
        {
            Assert.True(ReferenceEquals(exact, observed) || ReferenceEquals(derived, observed));
            conditionCalls++;
            return Task.FromResult(true);
        },
        new RecordingBehavior(fault: observed =>
        {
            Assert.True(ReferenceEquals(exact, observed) || ReferenceEquals(derived, observed));
            thenCalls++;
            return Task.CompletedTask;
        }),
        new RecordingBehavior(fault: _ =>
        {
            elseCalls++;
            return Task.CompletedTask;
        }));
        var next = new RecordingBehavior();
        await exceptionActivity.FaultedAsync(exact, next);
        await exceptionActivity.FaultedAsync(derived, next);
        await exceptionActivity.FaultedAsync(nonmatching, next);
        Assert.Equal((2, 2, 0), (conditionCalls, thenCalls, elseCalls));
        Assert.Equal(3, next.FaultContexts.Count);

        var falseUntypedCalls = 0;
        await new ConditionExceptionActivity<TestSaga, BaseMarkerException>(_ => Task.FromResult(false),
                new RecordingBehavior(fault: _ => throw new InvalidOperationException("Unexpected then branch.")),
                new RecordingBehavior(fault: observed =>
                {
                    Assert.Same(exact, observed);
                    falseUntypedCalls++;
                    return Task.CompletedTask;
                }))
            .FaultedAsync(exact, next);
        Assert.Equal(1, falseUntypedCalls);
        Assert.Equal(4, next.FaultContexts.Count);

        var falseGenericCalls = 0;
        var genericActivity = new ConditionExceptionActivity<TestSaga, BaseMarkerException>(_ => Task.FromResult(false),
            new RecordingBehavior(),
            new RecordingBehavior(fault: observed =>
            {
                Assert.Same(typedDerived, observed);
                falseGenericCalls++;
                return Task.CompletedTask;
            }));
        var genericNext = new RecordingTypedBehavior<DerivedMessage>();
        await genericActivity.FaultedAsync(typedDerived, genericNext);
        Assert.Equal(1, falseGenericCalls);
        Assert.Single(genericNext.FaultContexts);

        var typedConditionCalls = 0;
        var typedThenCalls = 0;
        var typedActivity = new ConditionExceptionActivity<TestSaga, BaseMessage, BaseMarkerException>(observed =>
        {
            Assert.Same(typedDerived, observed);
            typedConditionCalls++;
            return Task.FromResult(true);
        },
        new RecordingBehavior(fault: observed =>
        {
            Assert.Same(typedDerived, observed);
            typedThenCalls++;
            return Task.CompletedTask;
        }), new RecordingBehavior());
        var typedNext = new RecordingTypedBehavior<DerivedMessage>();
        await typedActivity.FaultedAsync(typedDerived, typedNext);
        var typedFalseCalls = 0;
        await new ConditionExceptionActivity<TestSaga, BaseMessage, BaseMarkerException>(_ => Task.FromResult(false),
                new RecordingBehavior(fault: _ => throw new InvalidOperationException("Unexpected then branch.")),
                new RecordingBehavior(fault: observed =>
                {
                    Assert.Same(typedDerived, observed);
                    typedFalseCalls++;
                    return Task.CompletedTask;
                }))
            .FaultedAsync(typedDerived, typedNext);
        var otherNext = new RecordingTypedBehavior<OtherMessage>();
        await typedActivity.FaultedAsync(otherMessage, otherNext);
        var typedNonmatchingNext = new RecordingTypedBehavior<DerivedMessage>();
        await typedActivity.FaultedAsync(typedNonmatchingException, typedNonmatchingNext);
        Assert.Equal((1, 1), (typedConditionCalls, typedThenCalls));
        Assert.Equal(1, typedFalseCalls);
        Assert.Equal(2, typedNext.FaultContexts.Count);
        Assert.Single(otherNext.FaultContexts);
        Assert.Single(typedNonmatchingNext.FaultContexts);

        var compensationCalls = 0;
        var catchActivity = new CatchFaultActivity<TestSaga, BaseMarkerException>(new RecordingBehavior(fault: observed =>
        {
            compensationCalls++;
            Assert.True(ReferenceEquals(exact, observed) || ReferenceEquals(derived, observed)
                || ReferenceEquals(typedDerived, observed));
            return Task.CompletedTask;
        }));
        var catchNext = new RecordingBehavior();
        await catchActivity.FaultedAsync(exact, catchNext);
        await catchActivity.FaultedAsync(derived, catchNext);
        Assert.Equal(2, catchNext.ExecuteContexts.Count);
        Assert.Empty(catchNext.FaultContexts);
        var catchTypedNext = new RecordingTypedBehavior<DerivedMessage>();
        await catchActivity.FaultedAsync(typedDerived, catchTypedNext);
        Assert.Single(catchTypedNext.ExecuteContexts);
        Assert.Empty(catchTypedNext.FaultContexts);
        await catchActivity.FaultedAsync(nonmatching, catchNext);
        Assert.Single(catchNext.FaultContexts);
        var catchTypedNonmatchingNext = new RecordingTypedBehavior<DerivedMessage>();
        await catchActivity.FaultedAsync(typedNonmatchingException, catchTypedNonmatchingNext);
        Assert.Empty(catchTypedNonmatchingNext.ExecuteContexts);
        Assert.Single(catchTypedNonmatchingNext.FaultContexts);
        Assert.Equal(3, compensationCalls);

        await AssertFaultFailureCancellationAndNullTaskOutcomesAsync(derived, typedDerived);
        AssertFaultDirectTaskIdentity();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-220-condition-fault-inspection-concurrency-context-free-await")]
    public async Task Activities_PreserveInspectionOrderConcurrencyAndContextFreeAwaitingAsync()
    {
        AssertConditionalInspection((then, @else) =>
            new ConditionActivity<TestSaga>(_ => Task.FromResult(true), then, @else));
        AssertConditionalInspection((then, @else) =>
            new ConditionActivity<TestSaga, Message>(_ => Task.FromResult(true), then, @else));
        AssertConditionalInspection((then, @else) =>
            new ConditionExceptionActivity<TestSaga, MarkerException>(_ => Task.FromResult(true), then, @else));
        AssertConditionalInspection((then, @else) =>
            new ConditionExceptionActivity<TestSaga, Message, MarkerException>(_ => Task.FromResult(true), then, @else));

        var visitOrder = new List<string>();
        var visitor = new RecordingVisitor(visitOrder);
        visitOrder.Clear();
        var catchBehavior = new RecordingBehavior(accept: () => visitOrder.Add("catch-behavior"));
        var catchActivity = new CatchFaultActivity<TestSaga, MarkerException>(catchBehavior);
        catchActivity.Accept(visitor);
        Assert.Equal(["exception-activity", "catch-behavior"], visitOrder);
        Assert.Same(catchActivity, visitor.LastExceptionActivity);

        var probeLog = new List<string>();
        var probe = new RecordingProbeContext(probeLog);
        new CatchFaultActivity<TestSaga, MarkerException>(
            new RecordingBehavior(probe: context => probeLog.Add($"behavior:{context.Path}")))
            .Probe(probe);
        Assert.Equal("scope:root/catch", probeLog[0]);
        Assert.StartsWith("add:root/catch:exceptionType:", probeLog[1], StringComparison.Ordinal);
        Assert.EndsWith("MarkerException", probeLog[1], StringComparison.Ordinal);
        Assert.Equal(["scope:root/catch/behavior", "behavior:root/catch/behavior"], probeLog.Skip(2));

        await AssertConcurrentExecutionAsync();
        await AssertConfigureAwaitMatrixAsync();
    }

    static void AssertConditionalInspection(
        Func<RecordingBehavior, RecordingBehavior, IStateMachineActivity<TestSaga>> createActivity)
    {
        var visitOrder = new List<string>();
        var visitor = new RecordingVisitor(visitOrder);
        var probeLog = new List<string>();
        IStateMachineActivity<TestSaga> activity = createActivity(
            new RecordingBehavior(accept: () => visitOrder.Add("then"),
                probe: context => probeLog.Add($"then:{context.Path}")),
            new RecordingBehavior(accept: () => visitOrder.Add("else"),
                probe: context => probeLog.Add($"else:{context.Path}")));

        activity.Accept(visitor);
        Assert.Equal(["activity", "then", "activity", "else"], visitOrder);
        ((IProbeSite)activity).Probe(new RecordingProbeContext(probeLog));
        Assert.Equal(["scope:root/condition", "then:root/condition", "else:root/condition"], probeLog);
    }

    static void AssertConditionConstructorNulls(StateMachineAsyncCondition<TestSaga> condition, IBehavior<TestSaga> behavior)
    {
        AssertArgument("condition", () => new ConditionActivity<TestSaga>(null!, behavior, behavior));
        AssertArgument("thenBehavior", () => new ConditionActivity<TestSaga>(condition, null!, behavior));
        AssertArgument("elseBehavior", () => new ConditionActivity<TestSaga>(condition, behavior, null!));
    }

    static void AssertTypedConditionConstructorNulls(StateMachineAsyncCondition<TestSaga, Message> condition,
        IBehavior<TestSaga> behavior)
    {
        AssertArgument("condition", () => new ConditionActivity<TestSaga, Message>(null!, behavior, behavior));
        AssertArgument("thenBehavior", () => new ConditionActivity<TestSaga, Message>(condition, null!, behavior));
        AssertArgument("elseBehavior", () => new ConditionActivity<TestSaga, Message>(condition, behavior, null!));
    }

    static void AssertExceptionConditionConstructorNulls(
        StateMachineAsyncExceptionCondition<TestSaga, MarkerException> condition, IBehavior<TestSaga> behavior)
    {
        AssertArgument("condition", () => new ConditionExceptionActivity<TestSaga, MarkerException>(null!, behavior, behavior));
        AssertArgument("thenBehavior", () => new ConditionExceptionActivity<TestSaga, MarkerException>(condition, null!, behavior));
        AssertArgument("elseBehavior", () => new ConditionExceptionActivity<TestSaga, MarkerException>(condition, behavior, null!));
    }

    static void AssertTypedExceptionConditionConstructorNulls(
        StateMachineAsyncExceptionCondition<TestSaga, Message, MarkerException> condition, IBehavior<TestSaga> behavior)
    {
        AssertArgument("condition", () => new ConditionExceptionActivity<TestSaga, Message, MarkerException>(null!, behavior, behavior));
        AssertArgument("thenBehavior", () => new ConditionExceptionActivity<TestSaga, Message, MarkerException>(condition, null!, behavior));
        AssertArgument("elseBehavior", () => new ConditionExceptionActivity<TestSaga, Message, MarkerException>(condition, behavior, null!));
    }

    static async Task AssertConditionFailureCancellationAndNullTaskOutcomesAsync(
        IBehaviorContext<TestSaga> context, IBehaviorContext<TestSaga, Message> typedContext)
    {
        var failure = new MarkerException("condition");
        var untouched = new RecordingBehavior();
        Assert.Same(failure, await Assert.ThrowsAsync<MarkerException>(() =>
            new ConditionActivity<TestSaga>(_ => Task.FromException<bool>(failure), untouched, untouched)
                .ExecuteAsync(context, untouched)));
        Assert.Empty(untouched.ExecuteContexts);

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ConditionActivity<TestSaga>(_ => Task.FromCanceled<bool>(cancellationSource.Token), untouched, untouched)
                .ExecuteAsync(context, untouched));
        Assert.Equal(cancellationSource.Token, canceled.CancellationToken);

        var branchFailure = new MarkerException("branch");
        var branchNext = new RecordingBehavior();
        Assert.Same(branchFailure, await Assert.ThrowsAsync<MarkerException>(() =>
            new ConditionActivity<TestSaga>(_ => Task.FromResult(true),
                    new RecordingBehavior(execute: _ => Task.FromException(branchFailure)), untouched)
                .ExecuteAsync(context, branchNext)));
        Assert.Empty(branchNext.ExecuteContexts);

        var canceledBranchNext = new RecordingBehavior();
        OperationCanceledException branchCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ConditionActivity<TestSaga>(_ => Task.FromResult(true),
                    new RecordingBehavior(execute: _ => Task.FromCanceled(cancellationSource.Token)), untouched)
                .ExecuteAsync(context, canceledBranchNext));
        Assert.Equal(cancellationSource.Token, branchCanceled.CancellationToken);
        Assert.Empty(canceledBranchNext.ExecuteContexts);

        var continuationFailure = new MarkerException("next");
        Assert.Same(continuationFailure, await Assert.ThrowsAsync<MarkerException>(() =>
            new ConditionActivity<TestSaga>(_ => Task.FromResult(false), untouched, new RecordingBehavior())
                .ExecuteAsync(context, new RecordingBehavior(execute: _ => Task.FromException(continuationFailure)))));
        OperationCanceledException continuationCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ConditionActivity<TestSaga>(_ => Task.FromResult(false), untouched, new RecordingBehavior())
                .ExecuteAsync(context,
                    new RecordingBehavior(execute: _ => Task.FromCanceled(cancellationSource.Token))));
        Assert.Equal(cancellationSource.Token, continuationCanceled.CancellationToken);

        await AssertNullConditionTaskAsync(
            () => new ConditionActivity<TestSaga>(_ => null!, untouched, untouched).ExecuteAsync(context, untouched),
            untouched);
        var genericUntouched = new RecordingTypedBehavior<Message>();
        await AssertNullConditionTaskAsync(
            () => new ConditionActivity<TestSaga>(_ => null!, untouched, untouched)
                .ExecuteAsync(typedContext, genericUntouched), genericUntouched);
        await AssertNullConditionTaskAsync(
            () => new ConditionActivity<TestSaga, Message>(_ => null!, untouched, untouched)
                .ExecuteAsync(typedContext, genericUntouched), genericUntouched);

        await AssertSynchronousThrowReturnsFaultedTaskAsync(
            () => new ConditionActivity<TestSaga>(_ => throw failure, untouched, untouched).ExecuteAsync(context, untouched),
            failure);
        await AssertSynchronousThrowReturnsFaultedTaskAsync(
            () => new ConditionActivity<TestSaga>(_ => throw failure, untouched, untouched)
                .ExecuteAsync(typedContext, genericUntouched), failure);
        await AssertSynchronousThrowReturnsFaultedTaskAsync(
            () => new ConditionActivity<TestSaga, Message>(_ => throw failure, untouched, untouched)
                .ExecuteAsync(typedContext, genericUntouched), failure);
    }

    static async Task AssertNullConditionTaskAsync(Func<Task> action, IRecordedBehavior continuation)
    {
        int executeCount = continuation.ExecuteCount;
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Equal("The async condition returned null.", exception.Message);
        Assert.Equal(executeCount, continuation.ExecuteCount);
    }

    static async Task AssertSynchronousThrowReturnsFaultedTaskAsync(Func<Task> action, MarkerException expected)
    {
        Task? task = null;
        Exception? synchronous = Record.Exception((Action)(() => task = action()));
        Assert.Null(synchronous);
        Assert.NotNull(task);
        Assert.Same(expected, await Assert.ThrowsAsync<MarkerException>(() => task));
    }

    static void AssertConditionDirectTaskIdentity(
        IBehaviorContext<TestSaga> context, IBehaviorContext<TestSaga, Message> typedContext)
    {
        IBehaviorExceptionContext<TestSaga, MarkerException> fault =
            CreateContext<IBehaviorExceptionContext<TestSaga, MarkerException>>();
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> typedFault =
            CreateContext<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
        Task faultTask = Task.FromResult(new object());
        var next = new RecordingBehavior(fault: _ => faultTask);
        var typedNext = new RecordingTypedBehavior<Message>(fault: _ => faultTask);
        var activity = new ConditionActivity<TestSaga>(_ => Task.FromResult(true), new RecordingBehavior(), new RecordingBehavior());
        Assert.Same(faultTask, activity.FaultedAsync(fault, next));
        Assert.Same(faultTask, activity.FaultedAsync(typedFault, typedNext));
        var typedActivity = new ConditionActivity<TestSaga, Message>(_ => Task.FromResult(true),
            new RecordingBehavior(), new RecordingBehavior());
        Assert.Same(faultTask, typedActivity.FaultedAsync(fault, next));
        Assert.Same(faultTask, typedActivity.FaultedAsync(typedFault, typedNext));
    }

    static async Task AssertFaultFailureCancellationAndNullTaskOutcomesAsync(
        IBehaviorExceptionContext<TestSaga, DerivedMarkerException> derived,
        IBehaviorExceptionContext<TestSaga, DerivedMessage, DerivedMarkerException> typedDerived)
    {
        var failure = new MarkerException("fault condition");
        var noNext = new RecordingBehavior();
        Assert.Same(failure, await Assert.ThrowsAsync<MarkerException>(() =>
            new ConditionExceptionActivity<TestSaga, BaseMarkerException>(_ => Task.FromException<bool>(failure),
                new RecordingBehavior(), new RecordingBehavior()).FaultedAsync(derived, noNext)));
        Assert.Empty(noNext.FaultContexts);

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ConditionExceptionActivity<TestSaga, BaseMarkerException>(
                _ => Task.FromCanceled<bool>(cancellationSource.Token), new RecordingBehavior(), new RecordingBehavior())
                .FaultedAsync(derived, noNext));
        Assert.Equal(cancellationSource.Token, canceled.CancellationToken);

        var branchFailure = new MarkerException("fault branch");
        Assert.Same(branchFailure, await Assert.ThrowsAsync<MarkerException>(() =>
            new ConditionExceptionActivity<TestSaga, BaseMarkerException>(_ => Task.FromResult(true),
                new RecordingBehavior(fault: _ => Task.FromException(branchFailure)), new RecordingBehavior())
                .FaultedAsync(derived, noNext)));
        Assert.Empty(noNext.FaultContexts);

        var canceledBranchNext = new RecordingBehavior();
        OperationCanceledException branchCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ConditionExceptionActivity<TestSaga, BaseMarkerException>(_ => Task.FromResult(true),
                new RecordingBehavior(fault: _ => Task.FromCanceled(cancellationSource.Token)), new RecordingBehavior())
                .FaultedAsync(derived, canceledBranchNext));
        Assert.Equal(cancellationSource.Token, branchCanceled.CancellationToken);
        Assert.Empty(canceledBranchNext.FaultContexts);

        var exceptionContinuationFailure = new MarkerException("fault continuation");
        Assert.Same(exceptionContinuationFailure, await Assert.ThrowsAsync<MarkerException>(() =>
            new ConditionExceptionActivity<TestSaga, BaseMarkerException>(_ => Task.FromResult(false),
                    new RecordingBehavior(), new RecordingBehavior())
                .FaultedAsync(derived,
                    new RecordingBehavior(fault: _ => Task.FromException(exceptionContinuationFailure)))));
        OperationCanceledException continuationCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ConditionExceptionActivity<TestSaga, BaseMarkerException>(_ => Task.FromResult(false),
                    new RecordingBehavior(), new RecordingBehavior())
                .FaultedAsync(derived,
                    new RecordingBehavior(fault: _ => Task.FromCanceled(cancellationSource.Token))));
        Assert.Equal(cancellationSource.Token, continuationCanceled.CancellationToken);

        var nullUntypedNext = new RecordingBehavior();
        InvalidOperationException nullUntypedFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ConditionExceptionActivity<TestSaga, BaseMarkerException>(_ => null!,
                new RecordingBehavior(), new RecordingBehavior()).FaultedAsync(derived, nullUntypedNext));
        Assert.Equal("The async condition returned null.", nullUntypedFailure.Message);
        Assert.Empty(nullUntypedNext.FaultContexts);
        var nullNext = new RecordingTypedBehavior<DerivedMessage>();
        InvalidOperationException nullFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ConditionExceptionActivity<TestSaga, BaseMarkerException>(_ => null!,
                new RecordingBehavior(), new RecordingBehavior()).FaultedAsync(typedDerived, nullNext));
        Assert.Equal("The async condition returned null.", nullFailure.Message);
        Assert.Empty(nullNext.FaultContexts);
        var nullTypedNext = new RecordingTypedBehavior<DerivedMessage>();
        InvalidOperationException nullTypedFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ConditionExceptionActivity<TestSaga, BaseMessage, BaseMarkerException>(_ => null!,
                new RecordingBehavior(), new RecordingBehavior()).FaultedAsync(typedDerived, nullTypedNext));
        Assert.Equal("The async condition returned null.", nullTypedFailure.Message);
        Assert.Empty(nullTypedNext.FaultContexts);

        await AssertSynchronousThrowReturnsFaultedTaskAsync(
            () => new ConditionExceptionActivity<TestSaga, BaseMarkerException>(_ => throw failure,
                new RecordingBehavior(), new RecordingBehavior()).FaultedAsync(derived, noNext), failure);
        await AssertSynchronousThrowReturnsFaultedTaskAsync(
            () => new ConditionExceptionActivity<TestSaga, BaseMarkerException>(_ => throw failure,
                new RecordingBehavior(), new RecordingBehavior()).FaultedAsync(typedDerived, nullNext), failure);
        await AssertSynchronousThrowReturnsFaultedTaskAsync(
            () => new ConditionExceptionActivity<TestSaga, BaseMessage, BaseMarkerException>(_ => throw failure,
                new RecordingBehavior(), new RecordingBehavior()).FaultedAsync(typedDerived, nullNext), failure);

        var compensationFailure = new MarkerException("compensation");
        var catchNoNext = new RecordingBehavior();
        Assert.Same(compensationFailure, await Assert.ThrowsAsync<MarkerException>(() =>
            new CatchFaultActivity<TestSaga, BaseMarkerException>(
                new RecordingBehavior(fault: _ => Task.FromException(compensationFailure)))
                .FaultedAsync(derived, catchNoNext)));
        Assert.Empty(catchNoNext.ExecuteContexts);
        OperationCanceledException compensationCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new CatchFaultActivity<TestSaga, BaseMarkerException>(
                new RecordingBehavior(fault: _ => Task.FromCanceled(cancellationSource.Token)))
                .FaultedAsync(derived, catchNoNext));
        Assert.Equal(cancellationSource.Token, compensationCanceled.CancellationToken);
        Assert.Empty(catchNoNext.ExecuteContexts);

        var continuationFailure = new MarkerException("catch continuation");
        Assert.Same(continuationFailure, await Assert.ThrowsAsync<MarkerException>(() =>
            new CatchFaultActivity<TestSaga, BaseMarkerException>(new RecordingBehavior())
                .FaultedAsync(derived, new RecordingBehavior(execute: _ => Task.FromException(continuationFailure)))));
        OperationCanceledException catchContinuationCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new CatchFaultActivity<TestSaga, BaseMarkerException>(new RecordingBehavior())
                .FaultedAsync(derived,
                    new RecordingBehavior(execute: _ => Task.FromCanceled(cancellationSource.Token))));
        Assert.Equal(cancellationSource.Token, catchContinuationCanceled.CancellationToken);
    }

    static void AssertFaultDirectTaskIdentity()
    {
        IBehaviorContext<TestSaga> context = CreateContext<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, Message> typedContext = CreateContext<IBehaviorContext<TestSaga, Message>>();
        Task executeTask = Task.FromResult(new object());
        var next = new RecordingBehavior(execute: _ => executeTask);
        var typedNext = new RecordingTypedBehavior<Message>(execute: _ => executeTask);
        var catchActivity = new CatchFaultActivity<TestSaga, MarkerException>(new RecordingBehavior());
        Assert.Same(executeTask, catchActivity.ExecuteAsync(context, next));
        Assert.Same(executeTask, catchActivity.ExecuteAsync(typedContext, typedNext));
        var exceptionActivity = new ConditionExceptionActivity<TestSaga, MarkerException>(_ => Task.FromResult(true),
            new RecordingBehavior(), new RecordingBehavior());
        Assert.Same(executeTask, exceptionActivity.ExecuteAsync(context, next));
        Assert.Same(executeTask, exceptionActivity.ExecuteAsync(typedContext, typedNext));
        var typedExceptionActivity = new ConditionExceptionActivity<TestSaga, Message, MarkerException>(
            _ => Task.FromResult(true), new RecordingBehavior(), new RecordingBehavior());
        Assert.Same(executeTask, typedExceptionActivity.ExecuteAsync(typedContext, typedNext));
        Assert.Throws<SagaStateMachineException>((Action)(() =>
            _ = typedExceptionActivity.ExecuteAsync(context, next)));
        IBehaviorExceptionContext<TestSaga, MarkerException> fault =
            CreateContext<IBehaviorExceptionContext<TestSaga, MarkerException>>();
        Assert.Throws<SagaStateMachineException>((Action)(() =>
            _ = typedExceptionActivity.FaultedAsync(fault, next)));
    }

    static async Task AssertConcurrentExecutionAsync()
    {
        const int count = 64;
        var effects = 0;
        var continuations = 0;
        var condition = new ConditionActivity<TestSaga>(_ =>
        {
            Interlocked.Increment(ref effects);
            return Task.FromResult(true);
        }, new RecordingBehavior(execute: _ =>
        {
            Interlocked.Increment(ref effects);
            return Task.CompletedTask;
        }), new RecordingBehavior());
        var typedCondition = new ConditionActivity<TestSaga, BaseMessage>(_ =>
        {
            Interlocked.Increment(ref effects);
            return Task.FromResult(true);
        }, new RecordingBehavior(execute: _ =>
        {
            Interlocked.Increment(ref effects);
            return Task.CompletedTask;
        }), new RecordingBehavior());
        var exception = new ConditionExceptionActivity<TestSaga, BaseMarkerException>(_ =>
        {
            Interlocked.Increment(ref effects);
            return Task.FromResult(true);
        }, new RecordingBehavior(fault: _ =>
        {
            Interlocked.Increment(ref effects);
            return Task.CompletedTask;
        }), new RecordingBehavior());
        var typedException = new ConditionExceptionActivity<TestSaga, BaseMessage, BaseMarkerException>(_ =>
        {
            Interlocked.Increment(ref effects);
            return Task.FromResult(true);
        }, new RecordingBehavior(fault: _ =>
        {
            Interlocked.Increment(ref effects);
            return Task.CompletedTask;
        }), new RecordingBehavior());
        var catchActivity = new CatchFaultActivity<TestSaga, BaseMarkerException>(new RecordingBehavior(fault: _ =>
        {
            Interlocked.Increment(ref effects);
            return Task.CompletedTask;
        }));
        Task[] tasks = Enumerable.Range(0, count).SelectMany(_ => new Func<Task>[]
        {
            () => condition.ExecuteAsync(CreateContext<IBehaviorContext<TestSaga>>(),
                new RecordingBehavior(execute: _ =>
                {
                    Interlocked.Increment(ref continuations);
                    return Task.CompletedTask;
                })),
            () => typedCondition.ExecuteAsync(CreateContext<IBehaviorContext<TestSaga, DerivedMessage>>(),
                new RecordingTypedBehavior<DerivedMessage>(execute: _ =>
                {
                    Interlocked.Increment(ref continuations);
                    return Task.CompletedTask;
                })),
            () => exception.FaultedAsync(CreateContext<IBehaviorExceptionContext<TestSaga, DerivedMarkerException>>(),
                new RecordingBehavior(fault: _ =>
                {
                    Interlocked.Increment(ref continuations);
                    return Task.CompletedTask;
                })),
            () => typedException.FaultedAsync(
                CreateContext<IBehaviorExceptionContext<TestSaga, DerivedMessage, DerivedMarkerException>>(),
                new RecordingTypedBehavior<DerivedMessage>(fault: _ =>
                {
                    Interlocked.Increment(ref continuations);
                    return Task.CompletedTask;
                })),
            () => catchActivity.FaultedAsync(CreateContext<IBehaviorExceptionContext<TestSaga, DerivedMarkerException>>(),
                new RecordingBehavior(execute: _ =>
                {
                    Interlocked.Increment(ref continuations);
                    return Task.CompletedTask;
                })),
            () => catchActivity.FaultedAsync(
                CreateContext<IBehaviorExceptionContext<TestSaga, DerivedMessage, DerivedMarkerException>>(),
                new RecordingTypedBehavior<DerivedMessage>(execute: _ =>
                {
                    Interlocked.Increment(ref continuations);
                    return Task.CompletedTask;
                })),
        }).Select(Task.Run).ToArray();
        await Task.WhenAll(tasks);
        Assert.Equal(count * 10, effects);
        Assert.Equal(count * 6, continuations);
    }

    static async Task AssertConfigureAwaitMatrixAsync()
    {
        var synchronizationContext = new RecordingSynchronizationContext();
        IBehaviorContext<TestSaga, DerivedMessage> context = CreateContext<IBehaviorContext<TestSaga, DerivedMessage>>();
        IBehaviorContext<TestSaga> untypedContext = CreateContext<IBehaviorContext<TestSaga>>();
        IBehaviorExceptionContext<TestSaga, DerivedMessage, DerivedMarkerException> fault =
            CreateContext<IBehaviorExceptionContext<TestSaga, DerivedMessage, DerivedMarkerException>>();
        IBehaviorExceptionContext<TestSaga, DerivedMarkerException> untypedFault =
            CreateContext<IBehaviorExceptionContext<TestSaga, DerivedMarkerException>>();

        await AssertThreeAwaitsDoNotCaptureAsync(synchronizationContext, (conditionTask, branchTask, nextTask) =>
            new ConditionActivity<TestSaga>(_ => conditionTask,
                    new RecordingBehavior(execute: _ => branchTask), new RecordingBehavior())
                .ExecuteAsync(untypedContext, new RecordingBehavior(execute: _ => nextTask)));
        await AssertThreeAwaitsDoNotCaptureAsync(synchronizationContext, (conditionTask, branchTask, nextTask) =>
            new ConditionActivity<TestSaga>(_ => conditionTask,
                    new RecordingBehavior(execute: _ => branchTask), new RecordingBehavior())
                .ExecuteAsync(context, new RecordingTypedBehavior<DerivedMessage>(execute: _ => nextTask)));
        await AssertThreeAwaitsDoNotCaptureAsync(synchronizationContext, (conditionTask, branchTask, nextTask) =>
            new ConditionActivity<TestSaga, BaseMessage>(_ => conditionTask,
                    new RecordingBehavior(execute: _ => branchTask), new RecordingBehavior())
                .ExecuteAsync(context, new RecordingTypedBehavior<DerivedMessage>(execute: _ => nextTask)));
        await AssertThreeAwaitsDoNotCaptureAsync(synchronizationContext, (conditionTask, branchTask, nextTask) =>
            new ConditionExceptionActivity<TestSaga, BaseMarkerException>(_ => conditionTask,
                    new RecordingBehavior(fault: _ => branchTask), new RecordingBehavior())
                .FaultedAsync(untypedFault, new RecordingBehavior(fault: _ => nextTask)));
        await AssertThreeAwaitsDoNotCaptureAsync(synchronizationContext, (conditionTask, branchTask, nextTask) =>
            new ConditionExceptionActivity<TestSaga, BaseMarkerException>(_ => conditionTask,
                    new RecordingBehavior(fault: _ => branchTask), new RecordingBehavior())
                .FaultedAsync(fault, new RecordingTypedBehavior<DerivedMessage>(fault: _ => nextTask)));
        await AssertThreeAwaitsDoNotCaptureAsync(synchronizationContext, (conditionTask, branchTask, nextTask) =>
            new ConditionExceptionActivity<TestSaga, BaseMessage, BaseMarkerException>(_ => conditionTask,
                    new RecordingBehavior(fault: _ => branchTask), new RecordingBehavior())
                .FaultedAsync(fault, new RecordingTypedBehavior<DerivedMessage>(fault: _ => nextTask)));
        await AssertTwoAwaitsDoNotCaptureAsync(synchronizationContext, (behaviorTask, nextTask) =>
            new CatchFaultActivity<TestSaga, BaseMarkerException>(new RecordingBehavior(fault: _ => behaviorTask))
                .FaultedAsync(untypedFault, new RecordingBehavior(execute: _ => nextTask)));
        await AssertTwoAwaitsDoNotCaptureAsync(synchronizationContext, (behaviorTask, nextTask) =>
            new CatchFaultActivity<TestSaga, BaseMarkerException>(new RecordingBehavior(fault: _ => behaviorTask))
                .FaultedAsync(fault, new RecordingTypedBehavior<DerivedMessage>(execute: _ => nextTask)));
    }

    static async Task AssertThreeAwaitsDoNotCaptureAsync(RecordingSynchronizationContext synchronizationContext,
        Func<Task<bool>, Task, Task, Task> start)
    {
        for (var pendingStage = 0; pendingStage < 3; pendingStage++)
        {
            var conditionGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var branchGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var nextGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<bool> conditionTask = pendingStage == 0 ? conditionGate.Task : Task.FromResult(true);
            Task branchTask = pendingStage == 1 ? branchGate.Task : Task.CompletedTask;
            Task nextTask = pendingStage == 2 ? nextGate.Task : Task.CompletedTask;
            int initialPosts = synchronizationContext.PostCalls;
            Task execution = StartUnderSynchronizationContextAsync(synchronizationContext,
                () => start(conditionTask, branchTask, nextTask));
            if (pendingStage == 0)
                conditionGate.SetResult(true);
            else if (pendingStage == 1)
                branchGate.SetResult(true);
            else
                nextGate.SetResult(true);
            await execution.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
            Assert.Equal(initialPosts, synchronizationContext.PostCalls);
        }
    }

    static async Task AssertTwoAwaitsDoNotCaptureAsync(RecordingSynchronizationContext synchronizationContext,
        Func<Task, Task, Task> start)
    {
        for (var pendingStage = 0; pendingStage < 2; pendingStage++)
        {
            var behaviorGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var nextGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task behaviorTask = pendingStage == 0 ? behaviorGate.Task : Task.CompletedTask;
            Task nextTask = pendingStage == 1 ? nextGate.Task : Task.CompletedTask;
            int initialPosts = synchronizationContext.PostCalls;
            Task execution = StartUnderSynchronizationContextAsync(synchronizationContext,
                () => start(behaviorTask, nextTask));
            if (pendingStage == 0)
                behaviorGate.SetResult(true);
            else
                nextGate.SetResult(true);
            await execution.WaitAsync(ObservationTimeout, TestContext.Current.CancellationToken);
            Assert.Equal(initialPosts, synchronizationContext.PostCalls);
        }
    }

    static Task StartUnderSynchronizationContextAsync(RecordingSynchronizationContext synchronizationContext, Func<Task> start)
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            return start();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    static void AssertActivityType(Type type, string[] genericNames)
    {
        Assert.True(type.IsPublic);
        Assert.False(type.IsAbstract);
        Type[] arguments = type.GetGenericArguments();
        Assert.Equal(genericNames, arguments.Select(argument => argument.Name));
        Assert.True(arguments[0].GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        Assert.Contains(typeof(ISagaStateMachineInstance), arguments[0].GetGenericParameterConstraints());
        foreach (Type argument in arguments.Skip(1))
        {
            if (argument.Name.Contains("Exception", StringComparison.Ordinal))
                Assert.Contains(typeof(Exception), argument.GetGenericParameterConstraints());
            else
                Assert.True(argument.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        }
        Assert.Contains(typeof(IStateMachineActivity<>), type.GetInterfaces().Select(@interface =>
            @interface.IsGenericType ? @interface.GetGenericTypeDefinition() : @interface));
        MethodInfo[] genericMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.IsGenericMethodDefinition)
            .ToArray();
        string[] expectedGenericMethods = type == typeof(CatchFaultActivity<,>)
            ? ["ExecuteAsync<T>", "FaultedAsync<T>", "FaultedAsync<TMessage,T>"]
            : ["ExecuteAsync<T>", "FaultedAsync<TException>", "FaultedAsync<T,TException>"];
        Assert.Equal(expectedGenericMethods.Order(), genericMethods.Select(FormatGenericMethod).Order());
        foreach (MethodInfo method in genericMethods)
        {
            Type[] methodArguments = method.GetGenericArguments();
            for (var index = 0; index < methodArguments.Length; index++)
            {
                Type argument = methodArguments[index];
                bool isException = method.Name == nameof(IStateMachineActivity<TestSaga>.FaultedAsync)
                    && index == methodArguments.Length - 1;
                if (isException)
                    Assert.Contains(typeof(Exception), argument.GetGenericParameterConstraints());
                else
                    Assert.True(argument.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
            }
        }
        var nullability = new NullabilityInfoContext();
        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            foreach (ParameterInfo parameter in method.GetParameters())
                Assert.Equal(NullabilityState.NotNull, nullability.Create(parameter).ReadState);
            if (method.ReturnType == typeof(Task))
            {
                Assert.EndsWith("Async", method.Name, StringComparison.Ordinal);
                Assert.Equal(NullabilityState.NotNull, nullability.Create(method.ReturnParameter).ReadState);
            }
        }
    }

    static string FormatGenericMethod(MethodInfo method) =>
        $"{method.Name}<{string.Join(',', method.GetGenericArguments().Select(argument => argument.Name))}>";

    static void AssertDeclaredSurface(Type type, string[] expectedMethods, string[] expectedProperties)
    {
        string[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => $"{method.Name}/{method.GetGenericArguments().Length}")
            .Order()
            .ToArray();
        string[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .Order()
            .ToArray();

        Assert.Equal(expectedMethods.Order(), methods);
        Assert.Equal(expectedProperties.Order(), properties);
    }

    static void AssertClosedConstructor<TActivity>(params (Type Type, string Name)[] expectedParameters)
    {
        ParameterInfo[] parameters = Assert.Single(typeof(TActivity).GetConstructors()).GetParameters();
        Assert.Equal(expectedParameters.Select(parameter => parameter.Type),
            parameters.Select(parameter => parameter.ParameterType));
        Assert.Equal(expectedParameters.Select(parameter => parameter.Name),
            parameters.Select(parameter => parameter.Name));
        var nullability = new NullabilityInfoContext();
        Assert.All(parameters, parameter => Assert.Equal(NullabilityState.NotNull, nullability.Create(parameter).ReadState));
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

    static T CreateContext<T>() where T : class => DispatchProxy.Create<T, ContextProxy>();

    public sealed class TestSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    public record BaseMessage;
    public sealed record Message : BaseMessage;
    public sealed record DerivedMessage : BaseMessage;
    public sealed record OtherMessage;
    public sealed class MarkerException(string message = "marker") : Exception(message);
    public class BaseMarkerException(string message = "base") : Exception(message);
    public sealed class DerivedMarkerException(string message = "derived") : BaseMarkerException(message);

    public class ContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_CancellationToken"
                ? CancellationToken.None
                : throw new NotSupportedException(targetMethod?.Name);
    }

    interface IRecordedBehavior
    {
        int ExecuteCount { get; }
    }

    sealed class RecordingBehavior(
        Func<object, Task>? execute = null,
        Func<object, Task>? fault = null,
        Action? effect = null,
        Action? accept = null,
        Action<RecordingProbeContext>? probe = null) : IBehavior<TestSaga>, IRecordedBehavior
    {
        readonly Func<object, Task> _execute = execute ?? (_ => Task.CompletedTask);
        readonly Func<object, Task> _fault = fault ?? (_ => Task.CompletedTask);
        public ConcurrentQueue<object> ExecuteContexts { get; } = new();
        public ConcurrentQueue<object> FaultContexts { get; } = new();
        public int ExecuteCount => ExecuteContexts.Count;

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context) => RecordExecuteAsync(context);
        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context) where T : class => RecordExecuteAsync(context);
        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context)
            where TException : Exception => RecordFaultAsync(context);
        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context)
            where T : class where TException : Exception => RecordFaultAsync(context);
        public void Accept(IStateMachineVisitor visitor) => accept?.Invoke();
        public void Probe(ProbeContext context) => probe?.Invoke(Assert.IsType<RecordingProbeContext>(context));

        Task RecordExecuteAsync(object context)
        {
            effect?.Invoke();
            ExecuteContexts.Enqueue(context);
            return _execute(context);
        }

        Task RecordFaultAsync(object context)
        {
            effect?.Invoke();
            FaultContexts.Enqueue(context);
            return _fault(context);
        }
    }

    sealed class RecordingTypedBehavior<TMessage>(
        Action? effect = null,
        Func<object, Task>? execute = null,
        Func<object, Task>? fault = null) : IBehavior<TestSaga, TMessage>, IRecordedBehavior
        where TMessage : class
    {
        readonly Func<object, Task> _execute = execute ?? (_ => Task.CompletedTask);
        readonly Func<object, Task> _fault = fault ?? (_ => Task.CompletedTask);
        public ConcurrentQueue<object> ExecuteContexts { get; } = new();
        public ConcurrentQueue<object> FaultContexts { get; } = new();
        public int ExecuteCount => ExecuteContexts.Count;
        public Task ExecuteAsync(IBehaviorContext<TestSaga, TMessage> context)
        {
            effect?.Invoke();
            ExecuteContexts.Enqueue(context);
            return _execute(context);
        }
        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TMessage, TException> context)
            where TException : Exception
        {
            effect?.Invoke();
            FaultContexts.Enqueue(context);
            return _fault(context);
        }
        public void Accept(IStateMachineVisitor visitor) { }
        public void Probe(ProbeContext context) { }
    }

    sealed class RecordingVisitor(List<string> order) : IStateMachineVisitor
    {
        public IStateMachineExceptionActivity? LastExceptionActivity { get; private set; }

        public void Visit(IStateMachineActivity activity, Action<IStateMachineActivity> next)
        {
            order.Add("activity");
            next(activity);
        }
        public void Visit(IStateMachineActivity activity) => order.Add("activity");
        public void Visit(IState state, Action<IState> next) => throw Unexpected();
        public void Visit(IEvent @event, Action<IEvent> next) => throw Unexpected();
        public void Visit<TMessage>(IEvent<TMessage> @event, Action<IEvent<TMessage>> next) where TMessage : class => throw Unexpected();
        public void Visit(IStateMachineExceptionActivity activity, Action<IStateMachineExceptionActivity> next)
        {
            order.Add("exception-activity");
            LastExceptionActivity = activity;
            next(activity);
        }
        public void Visit<T>(IBehavior<T> behavior) where T : class, ISagaStateMachineInstance => throw Unexpected();
        public void Visit<T>(IBehavior<T> behavior, Action<IBehavior<T>> next)
            where T : class, ISagaStateMachineInstance => throw Unexpected();
        public void Visit<T, TMessage>(IBehavior<T, TMessage> behavior)
            where T : class, ISagaStateMachineInstance where TMessage : class => throw Unexpected();
        public void Visit<T, TMessage>(IBehavior<T, TMessage> behavior, Action<IBehavior<T, TMessage>> next)
            where T : class, ISagaStateMachineInstance where TMessage : class => throw Unexpected();
        static Exception Unexpected() => new InvalidOperationException("Unexpected visitor overload.");
    }

    sealed class RecordingProbeContext(List<string> log, string path = "root") : ProbeContext
    {
        public string Path { get; } = path;
        public CancellationToken CancellationToken => default;
        public void Add(string key, string? value) => log.Add($"add:{Path}:{key}:{value}");
        public void Add(string key, object? value) => log.Add($"add:{Path}:{key}:{value}");
        public void Set(object values) => throw Unexpected();
        public void Set(IEnumerable<KeyValuePair<string, object?>> values) => throw Unexpected();
        public ProbeContext CreateScope(string key)
        {
            string child = $"{Path}/{key}";
            log.Add($"scope:{child}");
            return new RecordingProbeContext(log, child);
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
