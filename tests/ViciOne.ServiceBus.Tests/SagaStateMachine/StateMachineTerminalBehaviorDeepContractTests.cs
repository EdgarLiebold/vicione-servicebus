using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineTerminalBehaviorDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-216-behavior-cache-identity-partitioning")]
    public async Task BehaviorFactories_CacheExactInstancesPerClosedGenericPartitionAsync()
    {
        IBehavior<TestSaga> empty = Behavior.Empty<TestSaga>();
        IBehavior<TestSaga> faulted = Behavior.Faulted<TestSaga>();
        IBehavior<TestSaga, Message> typedEmpty = Behavior.Empty<TestSaga, Message>();
        IBehavior<TestSaga, Message> typedFaulted = Behavior.Faulted<TestSaga, Message>();

        Assert.IsType<EmptyBehavior<TestSaga>>(empty);
        Assert.IsType<FaultedBehavior<TestSaga>>(faulted);
        Assert.IsType<EmptyBehavior<TestSaga, Message>>(typedEmpty);
        Assert.IsType<FaultedBehavior<TestSaga, Message>>(typedFaulted);
        Assert.Same(empty, Behavior.Empty<TestSaga>());
        Assert.Same(faulted, Behavior.Faulted<TestSaga>());
        Assert.Same(typedEmpty, Behavior.Empty<TestSaga, Message>());
        Assert.Same(typedFaulted, Behavior.Faulted<TestSaga, Message>());
        Assert.NotSame(empty, faulted);
        Assert.NotSame(typedEmpty, typedFaulted);
        Assert.NotSame(empty, Behavior.Empty<OtherSaga>());
        Assert.NotSame(faulted, Behavior.Faulted<OtherSaga>());
        Assert.NotSame(typedEmpty, Behavior.Empty<TestSaga, OtherMessage>());
        Assert.NotSame(typedFaulted, Behavior.Faulted<TestSaga, OtherMessage>());
        Assert.NotSame(typedEmpty, Behavior.Empty<OtherSaga, Message>());
        Assert.NotSame(typedFaulted, Behavior.Faulted<OtherSaga, Message>());

        Task<IBehavior<TestSaga>>[] concurrentEmpty = Enumerable.Range(0, 32)
            .Select(_ => Task.Run(Behavior.Empty<TestSaga>))
            .ToArray();
        Task<IBehavior<TestSaga, Message>>[] concurrentTypedFaulted = Enumerable.Range(0, 32)
            .Select(_ => Task.Run(Behavior.Faulted<TestSaga, Message>))
            .ToArray();
        IBehavior<TestSaga>[] observedEmpty = await Task.WhenAll(concurrentEmpty);
        IBehavior<TestSaga, Message>[] observedTypedFaulted = await Task.WhenAll(concurrentTypedFaulted);

        Assert.All(observedEmpty, observed => Assert.Same(empty, observed));
        Assert.All(observedTypedFaulted, observed => Assert.Same(typedFaulted, observed));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-216-behavior-cold-first-publication-concurrency")]
    public async Task BehaviorFactories_ColdFirstPublicationCreatesOneIdentityPerClosedGenericPartitionAsync()
    {
        IBehavior<ColdEmptySaga>[] empty = await PublishConcurrentlyAsync(Behavior.Empty<ColdEmptySaga>);
        IBehavior<ColdFaultedSaga>[] faulted = await PublishConcurrentlyAsync(Behavior.Faulted<ColdFaultedSaga>);
        IBehavior<ColdTypedEmptySaga, ColdEmptyMessage>[] typedEmpty =
            await PublishConcurrentlyAsync(Behavior.Empty<ColdTypedEmptySaga, ColdEmptyMessage>);
        IBehavior<ColdTypedFaultedSaga, ColdFaultedMessage>[] typedFaulted =
            await PublishConcurrentlyAsync(Behavior.Faulted<ColdTypedFaultedSaga, ColdFaultedMessage>);

        var emptyIdentity = Assert.IsType<EmptyBehavior<ColdEmptySaga>>(empty[0]);
        var faultedIdentity = Assert.IsType<FaultedBehavior<ColdFaultedSaga>>(faulted[0]);
        var typedEmptyIdentity = Assert.IsType<EmptyBehavior<ColdTypedEmptySaga, ColdEmptyMessage>>(typedEmpty[0]);
        var typedFaultedIdentity = Assert.IsType<FaultedBehavior<ColdTypedFaultedSaga, ColdFaultedMessage>>(typedFaulted[0]);
        Assert.All(empty, observed => Assert.Same(emptyIdentity, observed));
        Assert.All(faulted, observed => Assert.Same(faultedIdentity, observed));
        Assert.All(typedEmpty, observed => Assert.Same(typedEmptyIdentity, observed));
        Assert.All(typedFaulted, observed => Assert.Same(typedFaultedIdentity, observed));
        Assert.Equal(
            4,
            new object[] { emptyIdentity, faultedIdentity, typedEmptyIdentity, typedFaultedIdentity }
                .Distinct(ReferenceEqualityComparer.Instance)
                .Count());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-216-terminal-behavior-exact-public-surface-nullability")]
    public void TerminalBehaviorTypes_ExposeExactSurfaceConstraintsNullabilityAndInterfaceMapping()
    {
        var nullability = new NullabilityInfoContext();

        AssertBehaviorFactorySurface(nullability);
        AssertTerminalBehaviorSurface(typeof(EmptyBehavior<>), typed: false, nullability);
        AssertTerminalBehaviorSurface(typeof(EmptyBehavior<,>), typed: true, nullability);
        AssertTerminalBehaviorSurface(typeof(FaultedBehavior<>), typed: false, nullability);
        AssertTerminalBehaviorSurface(typeof(FaultedBehavior<,>), typed: true, nullability);
        AssertDataBehaviorSurface(nullability);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-216-empty-behavior-terminal-semantics")]
    public void EmptyBehaviors_AreExactLeafVisitorsNoopProbesAndCanonicalCompletedTasks()
    {
        var untyped = new EmptyBehavior<TestSaga>();
        var typed = new EmptyBehavior<TestSaga, Message>();
        AssertLeafVisit(untyped, "untyped");
        AssertLeafVisit(typed, "typed");

        ProbeContext untypedProbe = CreateProbe(out RecordingProbeProxy untypedProbeRecorder);
        ProbeContext typedProbe = CreateProbe(out RecordingProbeProxy typedProbeRecorder);
        untyped.Probe(untypedProbe);
        typed.Probe(typedProbe);
        Assert.Equal(0, untypedProbeRecorder.Calls);
        Assert.Equal(0, typedProbeRecorder.Calls);

        IBehaviorContext<TestSaga> context = CreateStrictProxy<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, Message> typedContext = CreateStrictProxy<IBehaviorContext<TestSaga, Message>>();
        IBehaviorExceptionContext<TestSaga, MarkerException> exceptionContext =
            CreateStrictProxy<IBehaviorExceptionContext<TestSaga, MarkerException>>();
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> typedExceptionContext =
            CreateStrictProxy<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();

        Assert.Same(Task.CompletedTask, untyped.ExecuteAsync(context));
        Assert.Same(Task.CompletedTask, untyped.ExecuteAsync(typedContext));
        Assert.Same(Task.CompletedTask, untyped.FaultedAsync<Message, MarkerException>(typedExceptionContext));
        Assert.Same(Task.CompletedTask, untyped.FaultedAsync<MarkerException>(exceptionContext));
        Assert.Same(Task.CompletedTask, typed.ExecuteAsync(typedContext));
        Assert.Same(Task.CompletedTask, typed.FaultedAsync<MarkerException>(typedExceptionContext));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-216-empty-behavior-owned-null-boundaries")]
    public void EmptyBehaviors_RejectEveryNullBoundaryWithCanonicalParameters()
    {
        var untyped = new EmptyBehavior<TestSaga>();
        var typed = new EmptyBehavior<TestSaga, Message>();

        AssertParameter("visitor", () => untyped.Accept(null!));
        AssertParameter("context", () => untyped.Probe(null!));
        AssertParameter("context", () => { _ = untyped.ExecuteAsync(null!); });
        AssertParameter("context", () => { _ = untyped.ExecuteAsync<Message>(null!); });
        AssertParameter("context", () => { _ = untyped.FaultedAsync<Message, MarkerException>(null!); });
        AssertParameter("context", () => { _ = untyped.FaultedAsync<MarkerException>(null!); });
        AssertParameter("visitor", () => typed.Accept(null!));
        AssertParameter("context", () => typed.Probe(null!));
        AssertParameter("context", () => { _ = typed.ExecuteAsync(null!); });
        AssertParameter("context", () => { _ = typed.FaultedAsync<MarkerException>(null!); });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-216-faulted-behavior-terminal-semantics")]
    public void FaultedBehaviors_AreExactLeafVisitorsExceptionProbesAndFailureTerminals()
    {
        var untyped = new FaultedBehavior<TestSaga>();
        var typed = new FaultedBehavior<TestSaga, Message>();
        AssertLeafVisit(untyped, "untyped");
        AssertLeafVisit(typed, "typed");

        ProbeContext untypedProbe = CreateProbe(out RecordingProbeProxy untypedProbeRecorder);
        ProbeContext typedProbe = CreateProbe(out RecordingProbeProxy typedProbeRecorder);
        untyped.Probe(untypedProbe);
        typed.Probe(typedProbe);
        Assert.Equal(["exception"], untypedProbeRecorder.ScopeNames);
        Assert.Equal(["exception"], typedProbeRecorder.ScopeNames);

        var untypedProbeFailure = new MarkerException("untyped probe failed");
        ProbeContext failingUntypedProbe = CreateProbe(out RecordingProbeProxy failingUntypedProbeRecorder);
        failingUntypedProbeRecorder.Failure = untypedProbeFailure;
        Assert.Same(untypedProbeFailure, Assert.Throws<MarkerException>(() => untyped.Probe(failingUntypedProbe)));
        Assert.Equal(1, failingUntypedProbeRecorder.Calls);

        var typedProbeFailure = new MarkerException("typed probe failed");
        ProbeContext failingTypedProbe = CreateProbe(out RecordingProbeProxy failingTypedProbeRecorder);
        failingTypedProbeRecorder.Failure = typedProbeFailure;
        Assert.Same(typedProbeFailure, Assert.Throws<MarkerException>(() => typed.Probe(failingTypedProbe)));
        Assert.Equal(1, failingTypedProbeRecorder.Calls);

        IBehaviorContext<TestSaga> context = CreateStrictProxy<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, Message> typedContext = CreateStrictProxy<IBehaviorContext<TestSaga, Message>>();
        Assert.Same(Task.CompletedTask, untyped.ExecuteAsync(context));
        Assert.Same(Task.CompletedTask, untyped.ExecuteAsync(typedContext));
        Assert.Same(Task.CompletedTask, typed.ExecuteAsync(typedContext));

        var untypedEvent = new TriggerEvent("Wake");
        var typedEvent = new MessageEvent<Message>("Deliver");
        var untypedFailure = new MarkerException("untyped failed");
        var typedFailure = new MarkerException("typed failed");
        IBehaviorExceptionContext<TestSaga, MarkerException> exceptionContext =
            CreateExceptionContext<IBehaviorExceptionContext<TestSaga, MarkerException>>(untypedEvent, untypedFailure);
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> typedExceptionContext =
            CreateExceptionContext<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>(typedEvent, typedFailure);

        EventExecutionException untypedException = Assert.Throws<EventExecutionException>(() =>
        {
            _ = untyped.FaultedAsync<MarkerException>(exceptionContext);
        });
        Assert.Equal($"The {untypedEvent} execution faulted", untypedException.Message);
        Assert.Same(untypedFailure, untypedException.InnerException);

        EventExecutionException untypedDataException = Assert.Throws<EventExecutionException>(() =>
        {
            _ = untyped.FaultedAsync<Message, MarkerException>(typedExceptionContext);
        });
        Assert.Equal($"The {typedEvent} execution faulted", untypedDataException.Message);
        Assert.Same(typedFailure, untypedDataException.InnerException);

        EventExecutionException typedDataException = Assert.Throws<EventExecutionException>(() =>
        {
            _ = typed.FaultedAsync<MarkerException>(typedExceptionContext);
        });
        Assert.Equal($"The {typedEvent} execution faulted", typedDataException.Message);
        Assert.Same(typedFailure, typedDataException.InnerException);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-216-faulted-behavior-owned-null-boundaries")]
    public void FaultedBehaviors_RejectEveryNullBoundaryWithCanonicalParameters()
    {
        var untyped = new FaultedBehavior<TestSaga>();
        var typed = new FaultedBehavior<TestSaga, Message>();

        AssertParameter("visitor", () => untyped.Accept(null!));
        AssertParameter("context", () => untyped.Probe(null!));
        AssertParameter("context", () => { _ = untyped.ExecuteAsync(null!); });
        AssertParameter("context", () => { _ = untyped.ExecuteAsync<Message>(null!); });
        AssertParameter("context", () => { _ = untyped.FaultedAsync<Message, MarkerException>(null!); });
        AssertParameter("context", () => { _ = untyped.FaultedAsync<MarkerException>(null!); });
        AssertParameter("visitor", () => typed.Accept(null!));
        AssertParameter("context", () => typed.Probe(null!));
        AssertParameter("context", () => { _ = typed.ExecuteAsync(null!); });
        AssertParameter("context", () => { _ = typed.FaultedAsync<MarkerException>(null!); });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-216-data-behavior-exact-forwarding-identity")]
    public void DataBehavior_ForwardsExactCollaboratorsTasksAndFailureIdentityOnce()
    {
        var executionFailure = new MarkerException("execution task failed");
        var faultCancellation = new CancellationToken(canceled: true);
        Task executionTask = Task.FromException(executionFailure);
        Task faultTask = Task.FromCanceled(faultCancellation);
        var inner = new RecordingBehavior
        {
            TypedExecuteTask = executionTask,
            TypedFaultTask = faultTask,
        };
        IBehavior<TestSaga, Message> behavior = new DataBehavior<TestSaga, Message>(inner);
        var visitor = new RecordingVisitor();
        ProbeContext probe = CreateProbe(out _);
        IBehaviorContext<TestSaga, Message> context = CreateStrictProxy<IBehaviorContext<TestSaga, Message>>();
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> exceptionContext =
            CreateStrictProxy<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();

        behavior.Accept(visitor);
        behavior.Probe(probe);
        Task observedExecution = behavior.ExecuteAsync(context);
        Task observedFault = behavior.FaultedAsync<MarkerException>(exceptionContext);

        Assert.Equal(1, inner.AcceptCalls);
        Assert.Same(visitor, inner.LastVisitor);
        Assert.Equal(1, inner.ProbeCalls);
        Assert.Same(probe, inner.LastProbe);
        Assert.Equal(1, inner.TypedExecuteCalls);
        Assert.Same(context, inner.LastTypedExecuteContext);
        Assert.Equal(1, inner.TypedFaultCalls);
        Assert.Same(exceptionContext, inner.LastTypedFaultContext);
        Assert.Equal(0, inner.UntypedExecuteCalls);
        Assert.Equal(0, inner.UntypedFaultCalls);
        Assert.Same(executionTask, observedExecution);
        Assert.Same(executionFailure, observedExecution.Exception!.InnerException);
        Assert.Same(faultTask, observedFault);
        Assert.True(observedFault.IsCanceled);

        var forwardingFailure = new MarkerException("forwarding failed");
        inner.Failure = forwardingFailure;
        Assert.Same(forwardingFailure, Assert.Throws<MarkerException>(() => behavior.Accept(visitor)));
        Assert.Same(forwardingFailure, Assert.Throws<MarkerException>(() => behavior.Probe(probe)));
        Assert.Same(forwardingFailure, Assert.Throws<MarkerException>(() => { _ = behavior.ExecuteAsync(context); }));
        Assert.Same(forwardingFailure, Assert.Throws<MarkerException>(() =>
        {
            _ = behavior.FaultedAsync<MarkerException>(exceptionContext);
        }));
        Assert.Equal(2, inner.AcceptCalls);
        Assert.Equal(2, inner.ProbeCalls);
        Assert.Equal(2, inner.TypedExecuteCalls);
        Assert.Equal(2, inner.TypedFaultCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-216-data-behavior-owned-null-boundaries")]
    public void DataBehavior_RejectsNullCompositionAndInvocationBoundariesWithoutForwarding()
    {
        AssertParameter("behavior", () => _ = new DataBehavior<TestSaga, Message>(null!));

        var inner = new RecordingBehavior();
        IBehavior<TestSaga, Message> behavior = new DataBehavior<TestSaga, Message>(inner);
        AssertParameter("visitor", () => behavior.Accept(null!));
        AssertParameter("context", () => behavior.Probe(null!));
        AssertParameter("context", () => { _ = behavior.ExecuteAsync(null!); });
        AssertParameter("context", () => { _ = behavior.FaultedAsync<MarkerException>(null!); });
        Assert.Equal(0, inner.TotalCalls);
    }

    static void AssertLeafVisit(IVisitable behavior, string expectedKind)
    {
        var visitor = new RecordingVisitor();
        behavior.Accept(visitor);
        BehaviorVisit call = Assert.Single(visitor.Calls);
        Assert.Equal(expectedKind, call.Kind);
        Assert.Same(behavior, call.Behavior);

        var failure = new MarkerException("visitor failed");
        var failingVisitor = new RecordingVisitor(failure);
        Assert.Same(failure, Assert.Throws<MarkerException>(() => behavior.Accept(failingVisitor)));
        BehaviorVisit failingCall = Assert.Single(failingVisitor.Calls);
        Assert.Equal(expectedKind, failingCall.Kind);
        Assert.Same(behavior, failingCall.Behavior);
    }

    static void AssertParameter(string expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<ArgumentNullException>(action).ParamName);

    static async Task<T[]> PublishConcurrentlyAsync<T>(Func<T> factory)
        where T : class
    {
        const int concurrency = 32;
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var remaining = concurrency;
        Task<T>[] publications = Enumerable.Range(0, concurrency)
            .Select(_ => Task.Run(async () =>
            {
                if (Interlocked.Decrement(ref remaining) == 0)
                    allReady.SetResult();

                await start.Task;
                return factory();
            }))
            .ToArray();

        await allReady.Task;
        start.SetResult();
        return await Task.WhenAll(publications);
    }

    static void AssertBehaviorFactorySurface(NullabilityInfoContext nullability)
    {
        Type type = typeof(Behavior);
        Assert.True(type.IsPublic);
        Assert.True(type.IsAbstract);
        Assert.True(type.IsSealed);
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        AssertNoDeclaredPublicDataMembers(type);

        MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        Assert.Equal(4, methods.Length);
        MethodInfo empty = Assert.Single(methods, method => method.Name == nameof(Behavior.Empty)
            && method.GetGenericArguments().Length == 1);
        MethodInfo typedEmpty = Assert.Single(methods, method => method.Name == nameof(Behavior.Empty)
            && method.GetGenericArguments().Length == 2);
        MethodInfo faulted = Assert.Single(methods, method => method.Name == nameof(Behavior.Faulted)
            && method.GetGenericArguments().Length == 1);
        MethodInfo typedFaulted = Assert.Single(methods, method => method.Name == nameof(Behavior.Faulted)
            && method.GetGenericArguments().Length == 2);

        AssertFactoryMethod(empty, typeof(IBehavior<>), nullability);
        AssertFactoryMethod(faulted, typeof(IBehavior<>), nullability);
        AssertFactoryMethod(typedEmpty, typeof(IBehavior<,>), nullability);
        AssertFactoryMethod(typedFaulted, typeof(IBehavior<,>), nullability);
    }

    static void AssertFactoryMethod(MethodInfo method, Type returnTypeDefinition, NullabilityInfoContext nullability)
    {
        Assert.True(method.IsPublic);
        Assert.True(method.IsStatic);
        Assert.False(method.IsAbstract);
        Assert.Empty(method.GetParameters());
        Type[] arguments = method.GetGenericArguments();
        Assert.Equal(returnTypeDefinition.MakeGenericType(arguments), method.ReturnType);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(method.ReturnParameter).ReadState);
        AssertSagaConstraint(arguments[0]);
        if (arguments.Length == 2)
            AssertReferenceTypeConstraint(arguments[1]);
    }

    static void AssertTerminalBehaviorSurface(Type type, bool typed, NullabilityInfoContext nullability)
    {
        AssertPublicInstantiableGenericClass(type, typed ? 2 : 1);
        Type[] typeArguments = type.GetGenericArguments();
        AssertSagaConstraint(typeArguments[0]);
        if (typed)
            AssertReferenceTypeConstraint(typeArguments[1]);

        AssertOnlyPublicParameterlessConstructor(type);
        Type closedType = typed
            ? type.MakeGenericType(typeof(TestSaga), typeof(Message))
            : type.MakeGenericType(typeof(TestSaga));
        AssertOnlyPublicParameterlessConstructor(closedType);
        AssertNoDeclaredPublicDataMembers(type);
        MethodInfo[] methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        Assert.Equal(typed ? 4 : 6, methods.Length);

        AssertSimpleMethod(AssertMethod(methods, nameof(IVisitable.Accept), 0), typeof(void), typeof(IStateMachineVisitor), "visitor", nullability);
        AssertSimpleMethod(AssertMethod(methods, nameof(IProbeSite.Probe), 0), typeof(void), typeof(ProbeContext), "context", nullability);

        MethodInfo execute = AssertMethod(methods, nameof(IBehavior<TestSaga>.ExecuteAsync), 0);
        Type executeContext = typed
            ? typeof(IBehaviorContext<,>).MakeGenericType(typeArguments)
            : typeof(IBehaviorContext<>).MakeGenericType(typeArguments);
        AssertSimpleMethod(execute, typeof(Task), executeContext, "context", nullability);

        if (typed)
        {
            MethodInfo fault = AssertMethod(methods, nameof(IBehavior<TestSaga>.FaultedAsync), 1);
            Type exceptionType = fault.GetGenericArguments()[0];
            AssertExceptionConstraint(exceptionType);
            Type context = typeof(IBehaviorExceptionContext<,,>)
                .MakeGenericType(typeArguments[0], typeArguments[1], exceptionType);
            AssertSimpleMethod(fault, typeof(Task), context, "context", nullability);
        }
        else
        {
            MethodInfo typedExecute = AssertMethod(methods, nameof(IBehavior<TestSaga>.ExecuteAsync), 1);
            Type messageType = typedExecute.GetGenericArguments()[0];
            AssertReferenceTypeConstraint(messageType);
            Type typedContext = typeof(IBehaviorContext<,>).MakeGenericType(typeArguments[0], messageType);
            AssertSimpleMethod(typedExecute, typeof(Task), typedContext, "context", nullability);

            MethodInfo typedFault = AssertMethod(methods, nameof(IBehavior<TestSaga>.FaultedAsync), 2);
            Type[] typedFaultArguments = typedFault.GetGenericArguments();
            AssertReferenceTypeConstraint(typedFaultArguments[0]);
            AssertExceptionConstraint(typedFaultArguments[1]);
            Type typedFaultContext = typeof(IBehaviorExceptionContext<,,>)
                .MakeGenericType(typeArguments[0], typedFaultArguments[0], typedFaultArguments[1]);
            AssertSimpleMethod(typedFault, typeof(Task), typedFaultContext, "context", nullability);

            MethodInfo fault = AssertMethod(methods, nameof(IBehavior<TestSaga>.FaultedAsync), 1);
            Type exceptionType = fault.GetGenericArguments()[0];
            AssertExceptionConstraint(exceptionType);
            Type context = typeof(IBehaviorExceptionContext<,>).MakeGenericType(typeArguments[0], exceptionType);
            AssertSimpleMethod(fault, typeof(Task), context, "context", nullability);
        }

        Type behaviorInterface = (typed ? typeof(IBehavior<,>) : typeof(IBehavior<>)).MakeGenericType(typeArguments);
        AssertExactInterfaceClosure(type, behaviorInterface);
    }

    static void AssertDataBehaviorSurface(NullabilityInfoContext nullability)
    {
        Type type = typeof(DataBehavior<,>);
        AssertPublicInstantiableGenericClass(type, 2);
        Type[] typeArguments = type.GetGenericArguments();
        AssertSagaConstraint(typeArguments[0]);
        AssertReferenceTypeConstraint(typeArguments[1]);
        AssertNoDeclaredPublicDataMembers(type);

        ConstructorInfo constructor = Assert.Single(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        ParameterInfo behavior = Assert.Single(constructor.GetParameters());
        Assert.Equal(typeof(IBehavior<>).MakeGenericType(typeArguments[0]), behavior.ParameterType);
        Assert.Equal("behavior", behavior.Name);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(behavior).ReadState);

        MethodInfo[] methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        Assert.Equal(2, methods.Length);
        AssertSimpleMethod(AssertMethod(methods, nameof(IVisitable.Accept), 0), typeof(void), typeof(IStateMachineVisitor), "visitor", nullability);
        AssertSimpleMethod(AssertMethod(methods, nameof(IProbeSite.Probe), 0), typeof(void), typeof(ProbeContext), "context", nullability);

        Type behaviorInterface = typeof(IBehavior<,>).MakeGenericType(typeArguments);
        AssertExactInterfaceClosure(type, behaviorInterface);
        Type closedType = typeof(DataBehavior<TestSaga, Message>);
        InterfaceMapping map = closedType.GetInterfaceMap(typeof(IBehavior<TestSaga, Message>));
        AssertExplicitInterfaceTarget(map, nameof(IBehavior<TestSaga, Message>.ExecuteAsync), typeof(IBehaviorContext<TestSaga, Message>), nullability);
        AssertExplicitInterfaceTarget(map, nameof(IBehavior<TestSaga, Message>.FaultedAsync), typeof(IBehaviorExceptionContext<TestSaga, Message, MarkerException>), nullability);
    }

    static void AssertExplicitInterfaceTarget(
        InterfaceMapping map,
        string methodName,
        Type expectedParameterType,
        NullabilityInfoContext nullability)
    {
        int index = Array.FindIndex(map.InterfaceMethods, method => method.Name == methodName);
        Assert.True(index >= 0);
        MethodInfo target = map.TargetMethods[index];
        if (target.IsGenericMethodDefinition)
        {
            Type[] arguments = target.GetGenericArguments();
            Assert.Single(arguments);
            AssertExceptionConstraint(arguments[0]);
            expectedParameterType = expectedParameterType.GetGenericTypeDefinition()
                .MakeGenericType(typeof(TestSaga), typeof(Message), arguments[0]);
        }

        Assert.True(target.IsPrivate);
        Assert.True(target.IsFinal);
        Assert.True(target.IsVirtual);
        Assert.Equal(typeof(Task), target.ReturnType);
        Assert.Equal(typeof(DataBehavior<TestSaga, Message>), target.DeclaringType);
        ParameterInfo parameter = Assert.Single(target.GetParameters());
        Assert.Equal(expectedParameterType, parameter.ParameterType);
        Assert.Equal("context", parameter.Name);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(parameter).ReadState);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(target.ReturnParameter).ReadState);
    }

    static MethodInfo AssertMethod(IEnumerable<MethodInfo> methods, string name, int genericArity) =>
        Assert.Single(methods, method => method.Name == name && method.GetGenericArguments().Length == genericArity);

    static void AssertSimpleMethod(
        MethodInfo method,
        Type returnType,
        Type parameterType,
        string parameterName,
        NullabilityInfoContext nullability)
    {
        Assert.False(method.IsStatic);
        Assert.Equal(returnType, method.ReturnType);
        ParameterInfo parameter = Assert.Single(method.GetParameters());
        Assert.Equal(parameterType, parameter.ParameterType);
        Assert.Equal(parameterName, parameter.Name);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(parameter).ReadState);
        if (returnType != typeof(void))
            Assert.Equal(NullabilityState.NotNull, nullability.Create(method.ReturnParameter).ReadState);
    }

    static void AssertPublicInstantiableGenericClass(Type type, int genericArity)
    {
        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.False(type.IsAbstract);
        Assert.False(type.IsSealed);
        Assert.True(type.IsGenericTypeDefinition);
        Assert.Equal(genericArity, type.GetGenericArguments().Length);
        Assert.Equal(typeof(object), type.BaseType);
    }

    static void AssertOnlyPublicParameterlessConstructor(Type type)
    {
        ConstructorInfo constructor = Assert.Single(type.GetConstructors(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.True(constructor.IsPublic);
        Assert.False(constructor.IsStatic);
        Assert.Empty(constructor.GetParameters());
    }

    static void AssertNoDeclaredPublicDataMembers(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        Assert.Empty(type.GetFields(flags));
        Assert.Empty(type.GetProperties(flags));
        Assert.Empty(type.GetEvents(flags));
        Assert.Empty(type.GetNestedTypes(BindingFlags.Public));
    }

    static void AssertExactInterfaceClosure(Type type, Type primaryInterface)
    {
        Type[] expected = primaryInterface.GetInterfaces().Append(primaryInterface).OrderBy(item => item.FullName).ToArray();
        Type[] actual = type.GetInterfaces().OrderBy(item => item.FullName).ToArray();
        Assert.Equal(expected, actual);
    }

    static void AssertSagaConstraint(Type parameter)
    {
        Assert.Equal(GenericParameterAttributes.ReferenceTypeConstraint,
            parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal([typeof(ISagaStateMachineInstance)], parameter.GetGenericParameterConstraints());
    }

    static void AssertReferenceTypeConstraint(Type parameter)
    {
        Assert.Equal(GenericParameterAttributes.ReferenceTypeConstraint,
            parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Empty(parameter.GetGenericParameterConstraints());
    }

    static void AssertExceptionConstraint(Type parameter)
    {
        Assert.Equal(GenericParameterAttributes.None,
            parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal([typeof(Exception)], parameter.GetGenericParameterConstraints());
    }

    static T CreateStrictProxy<T>()
        where T : class => DispatchProxy.Create<T, StrictProxy>();

    static T CreateExceptionContext<T>(IEvent @event, Exception exception)
        where T : class
    {
        T context = DispatchProxy.Create<T, StrictProxy>();
        ((StrictProxy)(object)context).Handler = method => method.Name switch
        {
            "get_Event" => @event,
            "get_Exception" => exception,
            _ => throw new Xunit.Sdk.XunitException($"Unexpected context member: {method.Name}"),
        };
        return context;
    }

    static ProbeContext CreateProbe(out RecordingProbeProxy recorder)
    {
        ProbeContext context = DispatchProxy.Create<ProbeContext, RecordingProbeProxy>();
        recorder = (RecordingProbeProxy)(object)context;
        recorder.Context = context;
        return context;
    }

    sealed record BehaviorVisit(string Kind, object Behavior);

    sealed class RecordingVisitor(Exception? failure = null) : IStateMachineVisitor
    {
        public List<BehaviorVisit> Calls { get; } = [];

        public void Visit<T>(IBehavior<T> behavior)
            where T : class, ISagaStateMachineInstance => Record("untyped", behavior);

        public void Visit<T, TMessage>(IBehavior<T, TMessage> behavior)
            where T : class, ISagaStateMachineInstance
            where TMessage : class => Record("typed", behavior);

        public void Visit(IState state, Action<IState> next) => throw Unexpected();
        public void Visit(IEvent @event, Action<IEvent> next) => throw Unexpected();
        public void Visit<TMessage>(IEvent<TMessage> @event, Action<IEvent<TMessage>> next) where TMessage : class => throw Unexpected();
        public void Visit(IStateMachineActivity activity) => throw Unexpected();
        public void Visit(IStateMachineExceptionActivity activity, Action<IStateMachineExceptionActivity> next) => throw Unexpected();
        public void Visit<T>(IBehavior<T> behavior, Action<IBehavior<T>> next) where T : class, ISagaStateMachineInstance => throw Unexpected();
        public void Visit<T, TMessage>(IBehavior<T, TMessage> behavior, Action<IBehavior<T, TMessage>> next)
            where T : class, ISagaStateMachineInstance where TMessage : class => throw Unexpected();
        public void Visit(IStateMachineActivity activity, Action<IStateMachineActivity> next) => throw Unexpected();

        void Record(string kind, object behavior)
        {
            Calls.Add(new BehaviorVisit(kind, behavior));
            if (failure is not null)
                throw failure;
        }

        static Exception Unexpected() => new Xunit.Sdk.XunitException("An unrelated visitor overload was called.");
    }

    sealed class RecordingBehavior : IBehavior<TestSaga>
    {
        public int AcceptCalls { get; private set; }
        public int ProbeCalls { get; private set; }
        public int TypedExecuteCalls { get; private set; }
        public int TypedFaultCalls { get; private set; }
        public int UntypedExecuteCalls { get; private set; }
        public int UntypedFaultCalls { get; private set; }
        public int TotalCalls => AcceptCalls + ProbeCalls + TypedExecuteCalls + TypedFaultCalls + UntypedExecuteCalls + UntypedFaultCalls;
        public IStateMachineVisitor? LastVisitor { get; private set; }
        public ProbeContext? LastProbe { get; private set; }
        public object? LastTypedExecuteContext { get; private set; }
        public object? LastTypedFaultContext { get; private set; }
        public Task TypedExecuteTask { get; init; } = Task.CompletedTask;
        public Task TypedFaultTask { get; init; } = Task.CompletedTask;
        public Exception? Failure { get; set; }

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

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context)
        {
            UntypedExecuteCalls++;
            ThrowIfConfigured();
            return Task.CompletedTask;
        }

        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context)
            where T : class
        {
            TypedExecuteCalls++;
            LastTypedExecuteContext = context;
            ThrowIfConfigured();
            return TypedExecuteTask;
        }

        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context)
            where T : class
            where TException : Exception
        {
            TypedFaultCalls++;
            LastTypedFaultContext = context;
            ThrowIfConfigured();
            return TypedFaultTask;
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context)
            where TException : Exception
        {
            UntypedFaultCalls++;
            ThrowIfConfigured();
            return Task.CompletedTask;
        }

        void ThrowIfConfigured()
        {
            if (Failure is not null)
                throw Failure;
        }
    }

    public class StrictProxy : DispatchProxy
    {
        public Func<MethodInfo, object?>? Handler { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null)
                throw new Xunit.Sdk.XunitException("The proxy received a null target method.");
            if (Handler is not null)
                return Handler(targetMethod);
            throw new Xunit.Sdk.XunitException($"Unexpected context member: {targetMethod.Name}");
        }
    }

    public class RecordingProbeProxy : DispatchProxy
    {
        public ProbeContext Context { get; set; } = null!;
        public Exception? Failure { get; set; }
        public int Calls { get; private set; }
        public List<string> ScopeNames { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Calls++;
            if (Failure is not null)
                throw Failure;
            if (targetMethod?.Name == nameof(ProbeContext.CreateScope))
            {
                ScopeNames.Add(Assert.IsType<string>(args![0]));
                return Context;
            }

            throw new Xunit.Sdk.XunitException($"Unexpected probe member: {targetMethod?.Name}");
        }
    }

    public sealed class TestSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public IState CurrentState { get; set; } = null!;
    }

    public sealed class OtherSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public IState CurrentState { get; set; } = null!;
    }

    public sealed record Message;
    public sealed record OtherMessage;
    public sealed record ColdEmptyMessage;
    public sealed record ColdFaultedMessage;

    public sealed class ColdEmptySaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public IState CurrentState { get; set; } = null!;
    }

    public sealed class ColdFaultedSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public IState CurrentState { get; set; } = null!;
    }

    public sealed class ColdTypedEmptySaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public IState CurrentState { get; set; } = null!;
    }

    public sealed class ColdTypedFaultedSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public IState CurrentState { get; set; } = null!;
    }

    public sealed class MarkerException(string message) : Exception(message);
}
