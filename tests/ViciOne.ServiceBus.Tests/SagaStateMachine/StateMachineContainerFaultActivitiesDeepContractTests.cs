using System.Collections.Concurrent;
using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineContainerFaultActivitiesDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-219-container-fault-activity-public-surface")]
    public void Activities_HaveExactPublicShapeConstraintsTaskNamingAndNullability()
    {
        AssertActivityType(
            typeof(ContainerFactoryActivity<,>),
            typeof(IStateMachineActivity<>),
            ["TSaga", "TActivity"],
            publicMethodCount: 6,
            constructorParameterCount: 0);
        AssertActivityType(
            typeof(ContainerFactoryActivity<,,>),
            typeof(IStateMachineActivity<,>),
            ["TSaga", "TMessage", "TActivity"],
            publicMethodCount: 3,
            constructorParameterCount: 0);
        AssertActivityType(
            typeof(FaultedContainerFactoryActivity<,,>),
            typeof(IStateMachineActivity<>),
            ["TSaga", "TException", "TActivity"],
            publicMethodCount: 6,
            constructorParameterCount: 0);
        AssertActivityType(
            typeof(FaultedContainerFactoryActivity<,,,>),
            typeof(IStateMachineActivity<,>),
            ["TSaga", "TMessage", "TException", "TActivity"],
            publicMethodCount: 4,
            constructorParameterCount: 0);
        AssertActivityType(
            typeof(ExecuteOnFaultedActivity<>),
            typeof(IStateMachineActivity<>),
            ["TSaga"],
            publicMethodCount: 6,
            constructorParameterCount: 1);

        AssertSagaParameter(typeof(ContainerFactoryActivity<,>).GetGenericArguments()[0]);
        AssertActivityParameter(
            typeof(ContainerFactoryActivity<,>).GetGenericArguments()[1],
            typeof(IStateMachineActivity<>));

        Type[] typedContainerArguments = typeof(ContainerFactoryActivity<,,>).GetGenericArguments();
        AssertSagaParameter(typedContainerArguments[0]);
        AssertReferenceParameter(typedContainerArguments[1], "TMessage");
        AssertActivityParameter(typedContainerArguments[2], typeof(IStateMachineActivity<,>));

        Type[] faultedArguments = typeof(FaultedContainerFactoryActivity<,,>).GetGenericArguments();
        AssertSagaParameter(faultedArguments[0]);
        AssertExceptionParameter(faultedArguments[1], "TException");
        AssertActivityParameter(faultedArguments[2], typeof(IStateMachineActivity<>));

        Type[] typedFaultedArguments = typeof(FaultedContainerFactoryActivity<,,,>).GetGenericArguments();
        AssertSagaParameter(typedFaultedArguments[0]);
        AssertReferenceParameter(typedFaultedArguments[1], "TMessage");
        AssertExceptionParameter(typedFaultedArguments[2], "TException");
        AssertActivityParameter(typedFaultedArguments[3], typeof(IStateMachineActivity<,>));
        AssertSagaParameter(typeof(ExecuteOnFaultedActivity<>).GetGenericArguments()[0]);

        ConstructorInfo executeOnFaultedConstructor = Assert.Single(
            typeof(ExecuteOnFaultedActivity<TestSaga>).GetConstructors(BindingFlags.Instance | BindingFlags.Public));
        ParameterInfo ownedActivity = Assert.Single(executeOnFaultedConstructor.GetParameters());
        Assert.Equal(typeof(IStateMachineActivity<TestSaga>), ownedActivity.ParameterType);
        Assert.Equal("activity", ownedActivity.Name);

        MethodInfo explicitProbe = Assert.Single(
            typeof(ContainerFactoryActivity<,,>).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
        Assert.EndsWith("IProbeSite.Probe", explicitProbe.Name, StringComparison.Ordinal);
        Assert.Equal(typeof(void), explicitProbe.ReturnType);
        Assert.Equal(typeof(ProbeContext), Assert.Single(explicitProbe.GetParameters()).ParameterType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-219-container-fault-activity-null-boundaries")]
    public void Activities_RejectEveryMissingRequiredOwnerBeforeResolutionOrDelegation()
    {
        var serviceActivity = new RecordingActivity();
        var provider = new CountingProvider(type => type == typeof(RecordingActivity) ? serviceActivity : null);
        IBehaviorContext<TestSaga> context = CreateContext<IBehaviorContext<TestSaga>>(provider);
        IBehaviorContext<TestSaga, Message> typedContext = CreateContext<IBehaviorContext<TestSaga, Message>>(provider);
        IBehaviorExceptionContext<TestSaga, MarkerException> faultContext =
            CreateContext<IBehaviorExceptionContext<TestSaga, MarkerException>>(provider);
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> typedFaultContext =
            CreateContext<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>(provider);
        var next = new RecordingBehavior();
        var typedNext = new RecordingTypedBehavior();

        var container = new ContainerFactoryActivity<TestSaga, RecordingActivity>();
        AssertArgument("visitor", () => container.Accept(null!));
        AssertArgument("context", () => container.Probe(null!));
        AssertArgument("context", () => container.ExecuteAsync(null!, next));
        AssertArgument("next", () => container.ExecuteAsync(context, null!));
        AssertArgument("context", () => container.ExecuteAsync<Message>(null!, typedNext));
        AssertArgument("next", () => container.ExecuteAsync(typedContext, null!));
        AssertArgument("context", () => container.FaultedAsync<MarkerException>(null!, next));
        AssertArgument("next", () => container.FaultedAsync(faultContext, null!));
        AssertArgument("context", () => container.FaultedAsync<Message, MarkerException>(null!, typedNext));
        AssertArgument("next", () => container.FaultedAsync(typedFaultContext, null!));

        var typedContainer = new ContainerFactoryActivity<TestSaga, Message, RecordingTypedActivity>();
        AssertArgument("visitor", () => typedContainer.Accept(null!));
        AssertArgument("context", () => ((IProbeSite)typedContainer).Probe(null!));
        AssertArgument("context", () => typedContainer.ExecuteAsync(null!, typedNext));
        AssertArgument("next", () => typedContainer.ExecuteAsync(typedContext, null!));
        AssertArgument("context", () => typedContainer.FaultedAsync<MarkerException>(null!, typedNext));
        AssertArgument("next", () => typedContainer.FaultedAsync(typedFaultContext, null!));

        var faulted = new FaultedContainerFactoryActivity<TestSaga, MarkerException, RecordingActivity>();
        AssertArgument("visitor", () => faulted.Accept(null!));
        AssertArgument("context", () => faulted.Probe(null!));
        AssertArgument("context", () => faulted.ExecuteAsync(null!, next));
        AssertArgument("next", () => faulted.ExecuteAsync(context, null!));
        AssertArgument("context", () => faulted.ExecuteAsync<Message>(null!, typedNext));
        AssertArgument("next", () => faulted.ExecuteAsync(typedContext, null!));
        AssertArgument("context", () => faulted.FaultedAsync<MarkerException>(null!, next));
        AssertArgument("next", () => faulted.FaultedAsync(faultContext, null!));
        AssertArgument("context", () => faulted.FaultedAsync<Message, MarkerException>(null!, typedNext));
        AssertArgument("next", () => faulted.FaultedAsync(typedFaultContext, null!));

        var typedFaulted =
            new FaultedContainerFactoryActivity<TestSaga, Message, MarkerException, RecordingTypedActivity>();
        AssertArgument("visitor", () => typedFaulted.Accept(null!));
        AssertArgument("context", () => typedFaulted.Probe(null!));
        AssertArgument("context", () => typedFaulted.ExecuteAsync(null!, typedNext));
        AssertArgument("next", () => typedFaulted.ExecuteAsync(typedContext, null!));
        AssertArgument("context", () => typedFaulted.FaultedAsync<MarkerException>(null!, typedNext));
        AssertArgument("next", () => typedFaulted.FaultedAsync(typedFaultContext, null!));

        AssertArgument("activity", () => _ = new ExecuteOnFaultedActivity<TestSaga>(null!));
        var executeOnFaulted = new ExecuteOnFaultedActivity<TestSaga>(new RecordingActivity());
        AssertArgument("visitor", () => executeOnFaulted.Accept(null!));
        AssertArgument("context", () => executeOnFaulted.Probe(null!));
        AssertArgument("context", () => executeOnFaulted.ExecuteAsync(null!, next));
        AssertArgument("next", () => executeOnFaulted.ExecuteAsync(context, null!));
        AssertArgument("context", () => executeOnFaulted.ExecuteAsync<Message>(null!, typedNext));
        AssertArgument("next", () => executeOnFaulted.ExecuteAsync(typedContext, null!));
        AssertArgument("context", () => executeOnFaulted.FaultedAsync<MarkerException>(null!, next));
        AssertArgument("next", () => executeOnFaulted.FaultedAsync(faultContext, null!));
        AssertArgument("context", () => executeOnFaulted.FaultedAsync<Message, MarkerException>(null!, typedNext));
        AssertArgument("next", () => executeOnFaulted.FaultedAsync(typedFaultContext, null!));

        Assert.Equal(0, provider.RequestCount);
        Assert.Equal(0, next.TotalCalls);
        Assert.Equal(0, typedNext.TotalCalls);
        Assert.Empty(serviceActivity.Calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-219-container-resolution-timing-routing-and-identity")]
    public void ContainerFactories_ResolveAtEachInvocationAndPreserveEveryRouteContextNextAndTask()
    {
        var activity = new RecordingActivity();
        var provider = new CountingProvider(type => type == typeof(RecordingActivity) ? activity : null);
        IBehaviorContext<TestSaga> context = CreateContext<IBehaviorContext<TestSaga>>(provider);
        IBehaviorContext<TestSaga, Message> typedContext = CreateContext<IBehaviorContext<TestSaga, Message>>(provider);
        IBehaviorExceptionContext<TestSaga, MarkerException> faultContext =
            CreateContext<IBehaviorExceptionContext<TestSaga, MarkerException>>(provider);
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> typedFaultContext =
            CreateContext<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>(provider);
        var next = new RecordingBehavior();
        var typedNext = new RecordingTypedBehavior();
        var wrapper = new ContainerFactoryActivity<TestSaga, RecordingActivity>();

        Assert.Equal(0, provider.RequestCount);
        Assert.Same(activity.UntypedExecuteTask, wrapper.ExecuteAsync(context, next));
        Assert.Same(activity.TypedExecuteTask, wrapper.ExecuteAsync(typedContext, typedNext));
        Assert.Same(activity.UntypedFaultTask, wrapper.FaultedAsync(faultContext, next));
        Assert.Same(activity.TypedFaultTask, wrapper.FaultedAsync(typedFaultContext, typedNext));

        Assert.Equal(4, provider.RequestCount);
        Assert.Collection(
            activity.Calls,
            call => AssertCall(call, "execute", context, next),
            call => AssertCall(call, "execute-message", typedContext, typedNext),
            call => AssertCall(call, "fault", faultContext, next),
            call => AssertCall(call, "fault-message", typedFaultContext, typedNext));

        var typedActivity = new RecordingTypedActivity();
        var typedProvider = new CountingProvider(type => type == typeof(RecordingTypedActivity) ? typedActivity : null);
        IBehaviorContext<TestSaga, Message> messageContext =
            CreateContext<IBehaviorContext<TestSaga, Message>>(typedProvider);
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> messageFaultContext =
            CreateContext<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>(typedProvider);
        var typedWrapper = new ContainerFactoryActivity<TestSaga, Message, RecordingTypedActivity>();

        Assert.Same(typedActivity.ExecuteTask, typedWrapper.ExecuteAsync(messageContext, typedNext));
        Assert.Same(typedActivity.FaultTask, typedWrapper.FaultedAsync(messageFaultContext, typedNext));

        Assert.Equal(2, typedProvider.RequestCount);
        Assert.Collection(
            typedActivity.Calls,
            call => AssertCall(call, "execute", messageContext, typedNext),
            call => AssertCall(call, "fault", messageFaultContext, typedNext));

        FallbackActivity.Reset();
        IBehaviorContext<TestSaga> fallbackContext = CreateContext<IBehaviorContext<TestSaga>>();
        var fallbackNext = new RecordingBehavior();
        var fallbackWrapper = new ContainerFactoryActivity<TestSaga, FallbackActivity>();
        Task firstFallbackTask = fallbackWrapper.ExecuteAsync(fallbackContext, fallbackNext);
        Task secondFallbackTask = fallbackWrapper.ExecuteAsync(fallbackContext, fallbackNext);
        FallbackActivity[] fallbackInstances = FallbackActivity.Instances;

        Assert.Equal(2, fallbackInstances.Length);
        Assert.NotSame(fallbackInstances[0], fallbackInstances[1]);
        Assert.Same(fallbackInstances[0].ExecuteTask, firstFallbackTask);
        Assert.Same(fallbackInstances[1].ExecuteTask, secondFallbackTask);
        AssertCall(Assert.Single(fallbackInstances[0].Calls), "execute", fallbackContext, fallbackNext);
        AssertCall(Assert.Single(fallbackInstances[1].Calls), "execute", fallbackContext, fallbackNext);

        FallbackTypedActivity.Reset();
        IBehaviorContext<TestSaga, Message> fallbackTypedContext =
            CreateContext<IBehaviorContext<TestSaga, Message>>();
        var fallbackTypedNext = new RecordingTypedBehavior();
        var fallbackTypedWrapper =
            new ContainerFactoryActivity<TestSaga, Message, FallbackTypedActivity>();
        Task firstTypedFallbackTask = fallbackTypedWrapper.ExecuteAsync(fallbackTypedContext, fallbackTypedNext);
        Task secondTypedFallbackTask = fallbackTypedWrapper.ExecuteAsync(fallbackTypedContext, fallbackTypedNext);
        FallbackTypedActivity[] typedFallbackInstances = FallbackTypedActivity.Instances;

        Assert.Equal(2, typedFallbackInstances.Length);
        Assert.NotSame(typedFallbackInstances[0], typedFallbackInstances[1]);
        Assert.Same(typedFallbackInstances[0].ExecuteTask, firstTypedFallbackTask);
        Assert.Same(typedFallbackInstances[1].ExecuteTask, secondTypedFallbackTask);
        AssertCall(Assert.Single(typedFallbackInstances[0].Calls), "execute", fallbackTypedContext, fallbackTypedNext);
        AssertCall(Assert.Single(typedFallbackInstances[1].Calls), "execute", fallbackTypedContext, fallbackTypedNext);

        FallbackActivity.Reset();
        IBehaviorExceptionContext<TestSaga, DerivedMarkerException> fallbackFaultContext =
            CreateContext<IBehaviorExceptionContext<TestSaga, DerivedMarkerException>>();
        var fallbackFaultNext = new RecordingBehavior();
        var fallbackFaultWrapper =
            new FaultedContainerFactoryActivity<TestSaga, MarkerException, FallbackActivity>();
        Task firstFaultFallbackTask = fallbackFaultWrapper.FaultedAsync(fallbackFaultContext, fallbackFaultNext);
        Task secondFaultFallbackTask = fallbackFaultWrapper.FaultedAsync(fallbackFaultContext, fallbackFaultNext);
        FallbackActivity[] faultFallbackInstances = FallbackActivity.Instances;

        Assert.Equal(2, faultFallbackInstances.Length);
        Assert.NotSame(faultFallbackInstances[0], faultFallbackInstances[1]);
        Assert.Same(faultFallbackInstances[0].FaultTask, firstFaultFallbackTask);
        Assert.Same(faultFallbackInstances[1].FaultTask, secondFaultFallbackTask);
        AssertCall(Assert.Single(faultFallbackInstances[0].Calls), "fault", fallbackFaultContext, fallbackFaultNext);
        AssertCall(Assert.Single(faultFallbackInstances[1].Calls), "fault", fallbackFaultContext, fallbackFaultNext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-219-container-concurrent-resolution-isolation")]
    public async Task ContainerFactory_SharedWrapperKeepsConcurrentContextResolutionAndTaskIdentityIsolated()
    {
        const int invocationCount = 24;
        var wrapper = new ContainerFactoryActivity<TestSaga, RecordingActivity>();
        ConcurrentScenario[] scenarios = Enumerable.Range(0, invocationCount)
            .Select(_ =>
            {
                var activity = new RecordingActivity();
                var provider = new CountingProvider(type => type == typeof(RecordingActivity) ? activity : null);
                IBehaviorContext<TestSaga> context = CreateContext<IBehaviorContext<TestSaga>>(provider);
                var next = new RecordingBehavior();
                return new ConcurrentScenario(activity, provider, context, next);
            })
            .ToArray();

        Task<ConcurrentObservation>[] workers = scenarios
            .Select(scenario => Task.Run(
                () => new ConcurrentObservation(scenario, wrapper.ExecuteAsync(scenario.Context, scenario.Next)),
                TestContext.Current.CancellationToken))
            .ToArray();
        ConcurrentObservation[] observations = await Task.WhenAll(workers);

        Assert.All(observations, observation =>
        {
            ConcurrentScenario scenario = observation.Scenario;
            Assert.Same(scenario.Activity.UntypedExecuteTask, observation.ReturnedTask);
            Assert.Equal(1, scenario.Provider.RequestCount);
            RecordedCall call = Assert.Single(scenario.Activity.Calls);
            AssertCall(call, "execute", scenario.Context, scenario.Next);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-FAULT", "iteration-219-faulted-container-match-nonmatch-routing")]
    public void FaultedContainerFactories_ResolveOnlyMatchingFaultsAndOtherwiseContinueWithoutSideEffects()
    {
        var activity = new RecordingActivity();
        var provider = new CountingProvider(type => type == typeof(RecordingActivity) ? activity : null);
        var next = new RecordingBehavior();
        var typedNext = new RecordingTypedBehavior();
        var wrapper = new FaultedContainerFactoryActivity<TestSaga, MarkerException, RecordingActivity>();
        IBehaviorContext<TestSaga> context = CreateContext<IBehaviorContext<TestSaga>>(provider);
        IBehaviorContext<TestSaga, Message> typedContext = CreateContext<IBehaviorContext<TestSaga, Message>>(provider);
        IBehaviorExceptionContext<TestSaga, DerivedMarkerException> matching =
            CreateContext<IBehaviorExceptionContext<TestSaga, DerivedMarkerException>>(provider);
        IBehaviorExceptionContext<TestSaga, ForeignException> nonmatching =
            CreateContext<IBehaviorExceptionContext<TestSaga, ForeignException>>(provider);
        IBehaviorExceptionContext<TestSaga, Message, DerivedMarkerException> typedMatching =
            CreateContext<IBehaviorExceptionContext<TestSaga, Message, DerivedMarkerException>>(provider);
        IBehaviorExceptionContext<TestSaga, Message, ForeignException> typedNonmatching =
            CreateContext<IBehaviorExceptionContext<TestSaga, Message, ForeignException>>(provider);

        Assert.Same(next.UntypedExecuteTask, wrapper.ExecuteAsync(context, next));
        Assert.Same(typedNext.ExecuteTask, wrapper.ExecuteAsync(typedContext, typedNext));
        Assert.Same(next.UntypedFaultTask, wrapper.FaultedAsync(nonmatching, next));
        Assert.Same(typedNext.FaultTask, wrapper.FaultedAsync(typedNonmatching, typedNext));
        Assert.Equal(0, provider.RequestCount);
        Assert.Empty(activity.Calls);
        Assert.Same(activity.UntypedFaultTask, wrapper.FaultedAsync(matching, next));
        Assert.Same(activity.TypedFaultTask, wrapper.FaultedAsync(typedMatching, typedNext));
        Assert.Equal(2, provider.RequestCount);
        Assert.Collection(
            activity.Calls,
            call => AssertCall(call, "fault", matching, next),
            call => AssertCall(call, "fault-message", typedMatching, typedNext));
        Assert.Same(nonmatching, next.LastContext);
        Assert.Same(typedNonmatching, typedNext.LastContext);

        var typedActivity = new RecordingTypedActivity();
        var typedProvider = new CountingProvider(type => type == typeof(RecordingTypedActivity) ? typedActivity : null);
        var typedWrapper =
            new FaultedContainerFactoryActivity<TestSaga, Message, MarkerException, RecordingTypedActivity>();
        IBehaviorContext<TestSaga, Message> messageContext =
            CreateContext<IBehaviorContext<TestSaga, Message>>(typedProvider);
        IBehaviorExceptionContext<TestSaga, Message, DerivedMarkerException> messageMatching =
            CreateContext<IBehaviorExceptionContext<TestSaga, Message, DerivedMarkerException>>(typedProvider);
        IBehaviorExceptionContext<TestSaga, Message, ForeignException> messageNonmatching =
            CreateContext<IBehaviorExceptionContext<TestSaga, Message, ForeignException>>(typedProvider);

        typedNext.Clear();
        Assert.Same(typedNext.ExecuteTask, typedWrapper.ExecuteAsync(messageContext, typedNext));
        Assert.Same(typedNext.FaultTask, typedWrapper.FaultedAsync(messageNonmatching, typedNext));
        Assert.Equal(0, typedProvider.RequestCount);
        Assert.Same(typedActivity.FaultTask, typedWrapper.FaultedAsync(messageMatching, typedNext));
        Assert.Equal(1, typedProvider.RequestCount);
        AssertCall(Assert.Single(typedActivity.Calls), "fault", messageMatching, typedNext);
        Assert.Same(messageNonmatching, typedNext.LastContext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-FAULT", "iteration-219-execute-on-faulted-adapter-continuation-identity")]
    public void ExecuteOnFaulted_UsesTheStoredFaultContextForExactUntypedAndTypedContinuations()
    {
        var compensation = new RecordingActivity { ContinueExecution = true };
        var wrapper = new ExecuteOnFaultedActivity<TestSaga>(compensation);
        var next = new RecordingBehavior();
        var typedNext = new RecordingTypedBehavior();
        IBehaviorContext<TestSaga> ordinary = CreateContext<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, Message> typedOrdinary = CreateContext<IBehaviorContext<TestSaga, Message>>();
        IBehaviorExceptionContext<TestSaga, MarkerException> fault =
            CreateContext<IBehaviorExceptionContext<TestSaga, MarkerException>>();
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> typedFault =
            CreateContext<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();

        Assert.Same(next.UntypedExecuteTask, wrapper.ExecuteAsync(ordinary, next));
        Assert.Same(typedNext.ExecuteTask, wrapper.ExecuteAsync(typedOrdinary, typedNext));
        Assert.Empty(compensation.Calls);

        Assert.Same(next.UntypedFaultTask, wrapper.FaultedAsync(fault, next));
        Assert.Same(typedNext.FaultTask, wrapper.FaultedAsync(typedFault, typedNext));

        IBehavior<TestSaga> untypedAdapter = Assert.IsAssignableFrom<IBehavior<TestSaga>>(compensation.Calls[0].Next);
        IBehavior<TestSaga> typedAdapter = Assert.IsAssignableFrom<IBehavior<TestSaga>>(compensation.Calls[1].Next);
        IStateMachineVisitor visitor = CreateVisitor(out _);
        ProbeContext probe = CreateProbe(out _);
        untypedAdapter.Accept(visitor);
        untypedAdapter.Probe(probe);
        typedAdapter.Accept(visitor);
        typedAdapter.Probe(probe);

        Assert.Collection(
            compensation.Calls,
            call =>
            {
                Assert.Equal("execute", call.Route);
                Assert.Same(fault, call.Context);
                Assert.NotSame(next, call.Next);
            },
            call =>
            {
                Assert.Equal("execute", call.Route);
                Assert.Same(typedFault, call.Context);
                Assert.NotSame(typedNext, call.Next);
            });
        Assert.Equal(0, compensation.TypedExecuteCalls);
        Assert.Equal(1, next.UntypedFaultCalls);
        Assert.Equal(0, next.TypedFaultCalls);
        Assert.Same(fault, next.LastContext);
        Assert.Same(visitor, next.LastVisitor);
        Assert.Same(probe, next.LastProbe);
        Assert.Equal(1, typedNext.FaultCalls);
        Assert.Same(typedFault, typedNext.LastContext);
        Assert.Same(visitor, typedNext.LastVisitor);
        Assert.Same(probe, typedNext.LastProbe);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-FAULT", "iteration-219-container-fault-failure-cancellation-identity")]
    public void Activities_PreserveSynchronousFailureAndCanceledTaskIdentityWithoutExtraContinuation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Task canceled = Task.FromCanceled(cancellation.Token);
        var activity = new RecordingActivity { UntypedExecuteTask = canceled };
        var provider = new CountingProvider(type => type == typeof(RecordingActivity) ? activity : null);
        IBehaviorContext<TestSaga> context = CreateContext<IBehaviorContext<TestSaga>>(provider);
        var next = new RecordingBehavior();
        var container = new ContainerFactoryActivity<TestSaga, RecordingActivity>();

        Assert.Same(canceled, container.ExecuteAsync(context, next));
        Assert.Equal(0, next.TotalCalls);

        var activityFailure = new MarkerException("activity failed");
        activity.Failure = activityFailure;
        Assert.Same(activityFailure, Assert.Throws<MarkerException>(() => { _ = container.ExecuteAsync(context, next); }));
        Assert.Equal(0, next.TotalCalls);

        IBehaviorExceptionContext<TestSaga, ForeignException> nonmatching =
            CreateContext<IBehaviorExceptionContext<TestSaga, ForeignException>>(provider);
        var faulted = new FaultedContainerFactoryActivity<TestSaga, MarkerException, RecordingActivity>();
        var nextFailure = new MarkerException("next failed");
        next.Failure = nextFailure;
        Assert.Same(nextFailure, Assert.Throws<MarkerException>(() => { _ = faulted.FaultedAsync(nonmatching, next); }));
        Assert.Equal(2, provider.RequestCount);

        var compensation = new RecordingActivity { ContinueExecution = true };
        var executeOnFaulted = new ExecuteOnFaultedActivity<TestSaga>(compensation);
        IBehaviorExceptionContext<TestSaga, MarkerException> matching =
            CreateContext<IBehaviorExceptionContext<TestSaga, MarkerException>>();
        Assert.Same(nextFailure, Assert.Throws<MarkerException>(() => { _ = executeOnFaulted.FaultedAsync(matching, next); }));
        Assert.Equal(1, compensation.UntypedExecuteCalls);
        Assert.Equal(0, compensation.FaultCalls);

        var compensationFailure = new MarkerException("compensation failed");
        var failingCompensation = new RecordingActivity { Failure = compensationFailure };
        var untouchedNext = new RecordingBehavior();
        var failingWrapper = new ExecuteOnFaultedActivity<TestSaga>(failingCompensation);
        Assert.Same(
            compensationFailure,
            Assert.Throws<MarkerException>(() => { _ = failingWrapper.FaultedAsync(matching, untouchedNext); }));
        Assert.Equal(0, untouchedNext.TotalCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-219-container-fault-visitor-probe-contract")]
    public void Activities_ExposeExactLeafOrOwnedVisitorAndProbeSemanticsWithFailureIdentity()
    {
        IStateMachineVisitor visitor = CreateVisitor(out VisitorProxy visitorRecorder);
        ProbeContext probe = CreateProbe(out ProbeProxy probeRecorder);
        var container = new ContainerFactoryActivity<TestSaga, RecordingActivity>();
        var typedContainer = new ContainerFactoryActivity<TestSaga, Message, RecordingTypedActivity>();
        var faulted = new FaultedContainerFactoryActivity<TestSaga, MarkerException, RecordingActivity>();
        var typedFaulted =
            new FaultedContainerFactoryActivity<TestSaga, Message, MarkerException, RecordingTypedActivity>();

        container.Accept(visitor);
        typedContainer.Accept(visitor);
        faulted.Accept(visitor);
        typedFaulted.Accept(visitor);
        container.Probe(probe);
        ((IProbeSite)typedContainer).Probe(probe);
        faulted.Probe(probe);
        typedFaulted.Probe(probe);

        Assert.Equal([container, typedContainer, faulted, typedFaulted], visitorRecorder.Visited);
        Assert.Equal(
            ["containerActivityFactory", "containerActivityFactory", "containerActivityFactory", "containerActivityFactory"],
            probeRecorder.ScopeKeys);

        var owned = new RecordingActivity();
        var executeOnFaulted = new ExecuteOnFaultedActivity<TestSaga>(owned);
        executeOnFaulted.Accept(visitor);
        executeOnFaulted.Probe(probe);
        Assert.Same(visitor, owned.LastVisitor);
        Assert.Same(probe, owned.LastProbe);
        Assert.Equal(4, visitorRecorder.Visited.Count);
        Assert.Equal(4, probeRecorder.ScopeKeys.Count);

        var visitorFailure = new MarkerException("visitor failed");
        visitorRecorder.Failure = visitorFailure;
        Assert.Same(visitorFailure, Assert.Throws<MarkerException>(() => container.Accept(visitor)));

        var probeFailure = new MarkerException("probe failed");
        probeRecorder.Failure = probeFailure;
        Assert.Same(probeFailure, Assert.Throws<MarkerException>(() => faulted.Probe(probe)));

        owned.AcceptFailure = visitorFailure;
        owned.ProbeFailure = probeFailure;
        Assert.Same(visitorFailure, Assert.Throws<MarkerException>(() => executeOnFaulted.Accept(visitor)));
        Assert.Same(probeFailure, Assert.Throws<MarkerException>(() => executeOnFaulted.Probe(probe)));
    }

    static void AssertActivityType(Type type, Type expectedInterfaceDefinition, string[] genericNames,
        int publicMethodCount, int constructorParameterCount)
    {
        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.False(type.IsAbstract);
        Assert.False(type.IsSealed);
        Assert.Equal(genericNames, type.GetGenericArguments().Select(argument => argument.Name));
        Assert.Contains(type.GetInterfaces(), candidate =>
            candidate.IsGenericType && candidate.GetGenericTypeDefinition() == expectedInterfaceDefinition);

        ConstructorInfo constructor = Assert.Single(type.GetConstructors(BindingFlags.Instance | BindingFlags.Public));
        Assert.Equal(constructorParameterCount, constructor.GetParameters().Length);
        Assert.Equal(publicMethodCount, type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Length);

        var nullability = new NullabilityInfoContext();
        foreach (ParameterInfo parameter in constructor.GetParameters())
            Assert.Equal(NullabilityState.NotNull, nullability.Create(parameter).ReadState);

        foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            Assert.All(method.GetParameters(), parameter =>
                Assert.Equal(NullabilityState.NotNull, nullability.Create(parameter).ReadState));
            if (method.ReturnType == typeof(Task))
            {
                Assert.EndsWith("Async", method.Name, StringComparison.Ordinal);
                Assert.Equal(NullabilityState.NotNull, nullability.Create(method.ReturnParameter).ReadState);
            }
        }
    }

    static void AssertSagaParameter(Type parameter)
    {
        AssertReferenceParameter(parameter, "TSaga");
        Assert.Contains(typeof(ISagaStateMachineInstance), parameter.GetGenericParameterConstraints());
    }

    static void AssertReferenceParameter(Type parameter, string name)
    {
        Assert.Equal(name, parameter.Name);
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
    }

    static void AssertExceptionParameter(Type parameter, string name)
    {
        Assert.Equal(name, parameter.Name);
        Assert.Equal(GenericParameterAttributes.None,
            parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Contains(typeof(Exception), parameter.GetGenericParameterConstraints());
    }

    static void AssertActivityParameter(Type parameter, Type activityInterfaceDefinition)
    {
        Assert.Equal("TActivity", parameter.Name);
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Contains(parameter.GetGenericParameterConstraints(), constraint =>
            constraint.IsGenericType && constraint.GetGenericTypeDefinition() == activityInterfaceDefinition);
    }

    static void AssertArgument(string parameterName, Action action) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);

    static void AssertCall(RecordedCall call, string route, object context, object next)
    {
        Assert.Equal(route, call.Route);
        Assert.Same(context, call.Context);
        Assert.Same(next, call.Next);
    }

    static TContext CreateContext<TContext>(IServiceProvider? provider = null)
        where TContext : class
    {
        TContext context = DispatchProxy.Create<TContext, ContextProxy>();
        ((ContextProxy)(object)context).Provider = provider;
        return context;
    }

    static IStateMachineVisitor CreateVisitor(out VisitorProxy recorder)
    {
        IStateMachineVisitor visitor = DispatchProxy.Create<IStateMachineVisitor, VisitorProxy>();
        recorder = (VisitorProxy)(object)visitor;
        return visitor;
    }

    static ProbeContext CreateProbe(out ProbeProxy recorder)
    {
        ProbeContext probe = DispatchProxy.Create<ProbeContext, ProbeProxy>();
        recorder = (ProbeProxy)(object)probe;
        recorder.Proxy = probe;
        return probe;
    }

    public sealed class TestSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed record Message(string Value = "message");

    public class MarkerException(string message = "marker") : Exception(message);

    public sealed class DerivedMarkerException() : MarkerException("derived");

    public sealed class ForeignException() : Exception("foreign");

    public sealed record RecordedCall(string Route, object Context, object Next);

    sealed record ConcurrentScenario(
        RecordingActivity Activity,
        CountingProvider Provider,
        IBehaviorContext<TestSaga> Context,
        RecordingBehavior Next);

    sealed record ConcurrentObservation(ConcurrentScenario Scenario, Task ReturnedTask);

    public sealed class RecordingActivity : IStateMachineActivity<TestSaga>
    {
        public List<RecordedCall> Calls { get; } = [];
        public Task UntypedExecuteTask { get; set; } = NewTask();
        public Task TypedExecuteTask { get; set; } = NewTask();
        public Task UntypedFaultTask { get; set; } = NewTask();
        public Task TypedFaultTask { get; set; } = NewTask();
        public Exception? Failure { get; set; }
        public Exception? AcceptFailure { get; set; }
        public Exception? ProbeFailure { get; set; }
        public bool ContinueExecution { get; set; }
        public int TypedExecuteCalls { get; private set; }
        public int UntypedExecuteCalls { get; private set; }
        public int FaultCalls { get; private set; }
        public IStateMachineVisitor? LastVisitor { get; private set; }
        public ProbeContext? LastProbe { get; private set; }

        public void Accept(IStateMachineVisitor visitor)
        {
            LastVisitor = visitor;
            if (AcceptFailure is not null)
                throw AcceptFailure;
        }

        public void Probe(ProbeContext context)
        {
            LastProbe = context;
            if (ProbeFailure is not null)
                throw ProbeFailure;
        }

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context, IBehavior<TestSaga> next)
        {
            UntypedExecuteCalls++;
            Calls.Add(new RecordedCall("execute", context, next));
            if (Failure is not null)
                throw Failure;
            return ContinueExecution ? next.ExecuteAsync(context) : UntypedExecuteTask;
        }

        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context, IBehavior<TestSaga, T> next)
            where T : class
        {
            TypedExecuteCalls++;
            Calls.Add(new RecordedCall("execute-message", context, next));
            if (Failure is not null)
                throw Failure;
            return ContinueExecution ? next.ExecuteAsync(context) : TypedExecuteTask;
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context,
            IBehavior<TestSaga> next)
            where TException : Exception
        {
            FaultCalls++;
            Calls.Add(new RecordedCall("fault", context, next));
            if (Failure is not null)
                throw Failure;
            return UntypedFaultTask;
        }

        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context,
            IBehavior<TestSaga, T> next)
            where T : class
            where TException : Exception
        {
            FaultCalls++;
            Calls.Add(new RecordedCall("fault-message", context, next));
            if (Failure is not null)
                throw Failure;
            return TypedFaultTask;
        }
    }

    public sealed class RecordingTypedActivity : IStateMachineActivity<TestSaga, Message>
    {
        public List<RecordedCall> Calls { get; } = [];
        public Task ExecuteTask { get; } = NewTask();
        public Task FaultTask { get; } = NewTask();

        public void Accept(IStateMachineVisitor visitor) => throw Unexpected();
        public void Probe(ProbeContext context) => throw Unexpected();

        public Task ExecuteAsync(IBehaviorContext<TestSaga, Message> context, IBehavior<TestSaga, Message> next)
        {
            Calls.Add(new RecordedCall("execute", context, next));
            return ExecuteTask;
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, Message, TException> context,
            IBehavior<TestSaga, Message> next)
            where TException : Exception
        {
            Calls.Add(new RecordedCall("fault", context, next));
            return FaultTask;
        }
    }

    public sealed class FallbackActivity : IStateMachineActivity<TestSaga>
    {
        static readonly ConcurrentQueue<FallbackActivity> Created = new();

        public FallbackActivity()
        {
            Created.Enqueue(this);
        }

        public static FallbackActivity[] Instances => Created.ToArray();
        public List<RecordedCall> Calls { get; } = [];
        public Task ExecuteTask { get; } = NewTask();
        public Task FaultTask { get; } = NewTask();

        public static void Reset() => Created.Clear();
        public void Accept(IStateMachineVisitor visitor) => throw Unexpected();
        public void Probe(ProbeContext context) => throw Unexpected();

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context, IBehavior<TestSaga> next)
        {
            Calls.Add(new RecordedCall("execute", context, next));
            return ExecuteTask;
        }

        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context, IBehavior<TestSaga, T> next)
            where T : class => throw Unexpected();

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context,
            IBehavior<TestSaga> next)
            where TException : Exception
        {
            Calls.Add(new RecordedCall("fault", context, next));
            return FaultTask;
        }

        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context,
            IBehavior<TestSaga, T> next)
            where T : class
            where TException : Exception => throw Unexpected();
    }

    public sealed class FallbackTypedActivity : IStateMachineActivity<TestSaga, Message>
    {
        static readonly ConcurrentQueue<FallbackTypedActivity> Created = new();

        public FallbackTypedActivity()
        {
            Created.Enqueue(this);
        }

        public static FallbackTypedActivity[] Instances => Created.ToArray();
        public List<RecordedCall> Calls { get; } = [];
        public Task ExecuteTask { get; } = NewTask();

        public static void Reset() => Created.Clear();
        public void Accept(IStateMachineVisitor visitor) => throw Unexpected();
        public void Probe(ProbeContext context) => throw Unexpected();

        public Task ExecuteAsync(IBehaviorContext<TestSaga, Message> context, IBehavior<TestSaga, Message> next)
        {
            Calls.Add(new RecordedCall("execute", context, next));
            return ExecuteTask;
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, Message, TException> context,
            IBehavior<TestSaga, Message> next)
            where TException : Exception => throw Unexpected();
    }

    sealed class RecordingBehavior : IBehavior<TestSaga>
    {
        public Task UntypedExecuteTask { get; } = NewTask();
        public Task TypedExecuteTask { get; } = NewTask();
        public Task UntypedFaultTask { get; } = NewTask();
        public Task TypedFaultTask { get; } = NewTask();
        public Exception? Failure { get; set; }
        public object? LastContext { get; private set; }
        public int UntypedExecuteCalls { get; private set; }
        public int TypedExecuteCalls { get; private set; }
        public int UntypedFaultCalls { get; private set; }
        public int TypedFaultCalls { get; private set; }
        public int TotalCalls => UntypedExecuteCalls + TypedExecuteCalls + UntypedFaultCalls + TypedFaultCalls;
        public IStateMachineVisitor? LastVisitor { get; private set; }
        public ProbeContext? LastProbe { get; private set; }

        public void Accept(IStateMachineVisitor visitor) => LastVisitor = visitor;
        public void Probe(ProbeContext context) => LastProbe = context;

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context)
        {
            UntypedExecuteCalls++;
            LastContext = context;
            ThrowIfConfigured();
            return UntypedExecuteTask;
        }

        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context)
            where T : class
        {
            TypedExecuteCalls++;
            LastContext = context;
            ThrowIfConfigured();
            return TypedExecuteTask;
        }

        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context)
            where T : class
            where TException : Exception
        {
            TypedFaultCalls++;
            LastContext = context;
            ThrowIfConfigured();
            return TypedFaultTask;
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context)
            where TException : Exception
        {
            UntypedFaultCalls++;
            LastContext = context;
            ThrowIfConfigured();
            return UntypedFaultTask;
        }

        void ThrowIfConfigured()
        {
            if (Failure is not null)
                throw Failure;
        }
    }

    sealed class RecordingTypedBehavior : IBehavior<TestSaga, Message>
    {
        public Task ExecuteTask { get; } = NewTask();
        public Task FaultTask { get; } = NewTask();
        public Exception? Failure { get; set; }
        public object? LastContext { get; private set; }
        public int ExecuteCalls { get; private set; }
        public int FaultCalls { get; private set; }
        public int TotalCalls => ExecuteCalls + FaultCalls;
        public IStateMachineVisitor? LastVisitor { get; private set; }
        public ProbeContext? LastProbe { get; private set; }

        public void Accept(IStateMachineVisitor visitor) => LastVisitor = visitor;
        public void Probe(ProbeContext context) => LastProbe = context;

        public Task ExecuteAsync(IBehaviorContext<TestSaga, Message> context)
        {
            ExecuteCalls++;
            LastContext = context;
            ThrowIfConfigured();
            return ExecuteTask;
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, Message, TException> context)
            where TException : Exception
        {
            FaultCalls++;
            LastContext = context;
            ThrowIfConfigured();
            return FaultTask;
        }

        public void Clear()
        {
            LastContext = null;
            ExecuteCalls = 0;
            FaultCalls = 0;
        }

        void ThrowIfConfigured()
        {
            if (Failure is not null)
                throw Failure;
        }
    }

    sealed class CountingProvider(Func<Type, object?> resolve) : IServiceProvider
    {
        int _requestCount;

        public int RequestCount => Volatile.Read(ref _requestCount);

        public object? GetService(Type serviceType)
        {
            Interlocked.Increment(ref _requestCount);
            return resolve(serviceType);
        }
    }

    class ContextProxy : DispatchProxy
    {
        public IServiceProvider? Provider { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == nameof(PipeContext.TryGetPayload) && targetMethod.IsGenericMethod)
            {
                Type payloadType = targetMethod.GetGenericArguments()[0];
                if (payloadType == typeof(IServiceProvider) && Provider is not null)
                {
                    args![0] = Provider;
                    return true;
                }

                args![0] = null;
                return false;
            }

            throw Unexpected(targetMethod.Name);
        }
    }

    public class VisitorProxy : DispatchProxy
    {
        public List<object> Visited { get; } = [];
        public Exception? Failure { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            object visited = args![0]!;
            Visited.Add(visited);
            if (Failure is not null)
                throw Failure;
            return null;
        }
    }

    public class ProbeProxy : DispatchProxy
    {
        public ProbeContext Proxy { get; set; } = null!;
        public List<string> ScopeKeys { get; } = [];
        public Exception? Failure { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == nameof(ProbeContext.CreateScope))
            {
                ScopeKeys.Add((string)args![0]!);
                if (Failure is not null)
                    throw Failure;
                return Proxy;
            }

            throw Unexpected(targetMethod.Name);
        }
    }

    static Task NewTask() => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously).Task;

    static InvalidOperationException Unexpected(string? member = null) =>
        new($"Unexpected collaborator invocation{(member is null ? string.Empty : $" {member}")}.");
}
