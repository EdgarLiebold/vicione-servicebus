using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineStateAccessorDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-STORAGE", "default-initial-exact-once-and-cancellation-preserved")]
    public async Task DefaultAccessor_FirstReadInitializesExactlyOnceEvenWithAPreCanceledAccessorTokenAsync()
    {
        var machine = new DefaultAccessorMachine();
        var instance = new DefaultAccessorInstance();
        var observer = new RecordingObserver<DefaultAccessorInstance>();

        await WithBehaviorContextAsync(machine, instance, async context =>
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            using (machine.ConnectStateObserver(observer))
            {
                Assert.Same(machine.Initial, await machine.Accessor.GetAsync(context, cancellation.Token));
                Assert.Same(machine.Initial, await machine.Accessor.GetAsync(context, cancellation.Token));
            }
        });

        Assert.Same(machine.Initial, instance.CurrentState);
        StateChange<DefaultAccessorInstance> change = Assert.Single(observer.Changes);
        Assert.Same(instance, change.Instance);
        Assert.Null(change.Previous);
        Assert.Same(machine.Initial, change.Current);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-STORAGE", "default-indexer-state-property-rejected")]
    public async Task DefaultAccessor_IndexerOnlyStatePropertyFailsWithTheConfigurationDiagnosticAsync()
    {
        var machine = new IndexerOnlyMachine();
        var instance = new IndexerOnlyInstance();

        await WithBehaviorContextAsync(machine, instance, async context =>
        {
            SagaStateMachineException exception = await Assert.ThrowsAsync<SagaStateMachineException>(
                () => machine.Accessor.GetAsync(context, TestContext.Current.CancellationToken));

            Assert.Equal("The InstanceState was not configured, and no public State property exists.", exception.Message);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-STORAGE", "raw-state-name-canonical-read-and-predicate")]
    public async Task RawAccessor_ReadAndPredicateUseTheOwningMachinesCanonicalStateByNameAsync()
    {
        var owner = new AccessorMachine();
        var foreign = new AccessorMachine();
        var instance = new AccessorInstance();
        Func<AccessorInstance, bool> isRunning = owner.Accessor.GetStateExpression(owner.Running).Compile();

        Assert.False(isRunning(instance));

        instance.RawState = foreign.Running;

        Assert.NotSame(owner.Running, instance.RawState);
        Assert.Same(owner.Running, await StateMachineTestExecution.GetStateAsync(owner, instance));
        Assert.True(isRunning(instance));
        Assert.False(owner.Accessor.GetStateExpression(owner.Initial).Compile()(instance));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-STORAGE", "null-state-elements-consistent-across-accessors-and-index")]
    public void StateCollections_RejectNullElementsWithTheStatesParameter()
    {
        var machine = new AccessorMachine();
        var observer = new CompletedObserver<AccessorInstance>();
        object index = CreateStateIndex(machine, machine.Running);
        IStateAccessor<AccessorInstance>[] accessors =
        [
            CreateRawAccessor(machine, observer),
            CreateStringAccessor(machine, observer),
            CreateIntAccessor(index, observer),
        ];

        foreach (IStateAccessor<AccessorInstance> accessor in accessors)
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(
                () => accessor.GetStateExpression(machine.Running, null!));
            Assert.Equal("states", exception.ParamName);
        }

        ArgumentException indexException = Assert.Throws<ArgumentException>(
            () => CreateNested<AccessorInstance>(
                "StateAccessorIndex",
                machine,
                AsTypedState(machine.Initial),
                AsTypedState(machine.Final),
                new IState[] { machine.Running, null! }));
        Assert.Equal("states", indexException.ParamName);
    }

    [Theory]
    [InlineData(AccessorStorageKind.Raw)]
    [InlineData(AccessorStorageKind.String)]
    [InlineData(AccessorStorageKind.Integer)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-STORAGE", "null-observer-task-deterministic-failure-matrix")]
    public async Task StateMutation_WhenObserverReturnsNullTaskFailsDeterministicallyAsync(AccessorStorageKind storageKind)
    {
        var machine = new AccessorMachine();
        var observer = new NullTaskObserver<AccessorInstance>();
        object index = CreateStateIndex(machine, machine.Running);
        IStateAccessor<AccessorInstance> accessor = storageKind switch
        {
            AccessorStorageKind.Raw => CreateRawAccessor(machine, observer),
            AccessorStorageKind.String => CreateStringAccessor(machine, observer),
            AccessorStorageKind.Integer => CreateIntAccessor(index, observer),
            _ => throw new ArgumentOutOfRangeException(nameof(storageKind), storageKind, "Unknown storage kind."),
        };

        var instance = new AccessorInstance();
        await WithBehaviorContextAsync(machine, instance, async context =>
        {
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => accessor.SetAsync(context, AsTypedState(machine.Running), TestContext.Current.CancellationToken));

            Assert.Equal("The state observer returned no notification task.", exception.Message);
        });

        switch (storageKind)
        {
            case AccessorStorageKind.Raw:
                Assert.Same(machine.Running, instance.RawState);
                break;
            case AccessorStorageKind.String:
                Assert.Equal(machine.Running.Name, instance.StringState);
                break;
            case AccessorStorageKind.Integer:
                Assert.Equal(3, instance.IntState);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(storageKind), storageKind, "Unknown storage kind.");
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-STORAGE", "state-index-order-bounds-dedup-and-parameterized-diagnostics")]
    public void StateAccessorIndex_PreservesReservedOrderDeduplicatesAndReportsInputParameters()
    {
        var machine = new AccessorMachine();
        object index = CreateStateIndex(machine, machine.Running, machine.Running, machine.Initial);
        PropertyInfo nameIndexer = GetIndexer(index, typeof(string));
        PropertyInfo integerIndexer = GetIndexer(index, typeof(int));

        Assert.Null(InvokeIndexer(integerIndexer, index, 0));
        Assert.Same(machine.Initial, InvokeIndexer(integerIndexer, index, 1));
        Assert.Same(machine.Final, InvokeIndexer(integerIndexer, index, 2));
        Assert.Same(machine.Running, InvokeIndexer(integerIndexer, index, 3));
        Assert.Equal(3, InvokeIndexer(nameIndexer, index, machine.Running.Name));

        ArgumentNullException nullName = Assert.Throws<ArgumentNullException>(() => InvokeIndexer(nameIndexer, index, null));
        Assert.Equal("name", nullName.ParamName);
        ArgumentException blankName = Assert.Throws<ArgumentException>(() => InvokeIndexer(nameIndexer, index, " "));
        Assert.Equal("name", blankName.ParamName);
        ArgumentException unknownName = Assert.Throws<ArgumentException>(() => InvokeIndexer(nameIndexer, index, "Missing"));
        Assert.Equal("name", unknownName.ParamName);
        Assert.Contains("Missing", unknownName.Message, StringComparison.Ordinal);

        ArgumentOutOfRangeException below = Assert.Throws<ArgumentOutOfRangeException>(() => InvokeIndexer(integerIndexer, index, -1));
        Assert.Equal("index", below.ParamName);
        ArgumentOutOfRangeException above = Assert.Throws<ArgumentOutOfRangeException>(() => InvokeIndexer(integerIndexer, index, 4));
        Assert.Equal("index", above.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-STORAGE", "accessor-constructor-null-dependency-boundaries")]
    public void AccessorConstructors_RejectEveryNullDependencyAtItsBoundary()
    {
        var machine = new AccessorMachine();
        var observer = new CompletedObserver<AccessorInstance>();
        object index = CreateStateIndex(machine, machine.Running);
        IStateAccessor<AccessorInstance> rawAccessor = CreateRawAccessor(machine, observer);
        Expression<Func<AccessorInstance, IState?>> rawExpression = instance => instance.RawState;
        Expression<Func<AccessorInstance, string>> stringExpression = instance => instance.StringState;
        Expression<Func<AccessorInstance, int>> intExpression = instance => instance.IntState;
        IState<AccessorInstance> initial = AsTypedState(machine.Initial);
        IState<AccessorInstance> final = AsTypedState(machine.Final);
        IState[] states = [machine.Running];

        (string TypeName, object?[] Arguments, string Parameter)[] cases =
        [
            ("RawStateAccessor", [null, rawExpression, observer], "machine"),
            ("RawStateAccessor", [machine, null, observer], "currentStateExpression"),
            ("RawStateAccessor", [machine, rawExpression, null], "observer"),
            ("StringStateAccessor", [null, stringExpression, observer], "machine"),
            ("StringStateAccessor", [machine, null, observer], "currentStateExpression"),
            ("StringStateAccessor", [machine, stringExpression, null], "observer"),
            ("IntStateAccessor", [null, index, observer], "currentStateExpression"),
            ("IntStateAccessor", [intExpression, null, observer], "index"),
            ("IntStateAccessor", [intExpression, index, null], "observer"),
            ("DefaultInstanceStateAccessor", [null, initial, observer], "machine"),
            ("DefaultInstanceStateAccessor", [machine, null, observer], "initialState"),
            ("DefaultInstanceStateAccessor", [machine, initial, null], "observer"),
            ("InitialIfNullStateAccessor", [null, rawAccessor], "initialState"),
            ("InitialIfNullStateAccessor", [initial, null], "stateAccessor"),
            ("StateAccessorIndex", [null, initial, final, states], "stateMachine"),
            ("StateAccessorIndex", [machine, null, final, states], "initial"),
            ("StateAccessorIndex", [machine, initial, null, states], "final"),
            ("StateAccessorIndex", [machine, initial, final, null], "states"),
        ];

        foreach ((string typeName, object?[] arguments, string parameter) in cases)
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => CreateNested<AccessorInstance>(typeName, arguments));
            Assert.Equal(parameter, exception.ParamName);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-STORAGE", "accessor-entry-null-boundaries")]
    public async Task AccessorEntries_RejectNullInputsBeforeDereferencingOrLazySelectionAsync()
    {
        var machine = new AccessorMachine();
        var observer = new CompletedObserver<AccessorInstance>();
        object index = CreateStateIndex(machine, machine.Running);
        IStateAccessor<AccessorInstance> raw = CreateRawAccessor(machine, observer);
        IStateAccessor<AccessorInstance>[] accessors =
        [
            raw,
            CreateStringAccessor(machine, observer),
            CreateIntAccessor(index, observer),
            (IStateAccessor<AccessorInstance>)CreateNested<AccessorInstance>(
                "DefaultInstanceStateAccessor", machine, AsTypedState(machine.Initial), observer),
            (IStateAccessor<AccessorInstance>)CreateNested<AccessorInstance>(
                "InitialIfNullStateAccessor", AsTypedState(machine.Initial), raw),
        ];

        foreach (IStateAccessor<AccessorInstance> accessor in accessors)
        {
            ArgumentNullException getException = await Assert.ThrowsAsync<ArgumentNullException>(
                () => accessor.GetAsync(null!, TestContext.Current.CancellationToken));
            Assert.Equal("context", getException.ParamName);

            ArgumentNullException contextException = await Assert.ThrowsAsync<ArgumentNullException>(
                () => accessor.SetAsync(null!, AsTypedState(machine.Running), TestContext.Current.CancellationToken));
            Assert.Equal("context", contextException.ParamName);

            ArgumentNullException statesException = Assert.Throws<ArgumentNullException>(
                () => accessor.GetStateExpression(null!));
            Assert.Equal("states", statesException.ParamName);

            ArgumentNullException probeException = Assert.Throws<ArgumentNullException>(() => accessor.Probe(null!));
            Assert.Equal("context", probeException.ParamName);
        }

        await WithBehaviorContextAsync(machine, new AccessorInstance(), async context =>
        {
            foreach (IStateAccessor<AccessorInstance> accessor in accessors)
            {
                ArgumentNullException stateException = await Assert.ThrowsAsync<ArgumentNullException>(
                    () => accessor.SetAsync(context, null!, TestContext.Current.CancellationToken));
                Assert.Equal("state", stateException.ParamName);
            }
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-STORAGE", "default-delegation-probe-and-multiple-property-diagnostic")]
    public async Task DefaultAccessor_DelegatesSetExpressionAndProbeAndRejectsMultipleConventionPropertiesAsync()
    {
        var machine = new AccessorMachine();
        var observer = new RecordingObserver<AccessorInstance>();
        var accessor = (IStateAccessor<AccessorInstance>)CreateNested<AccessorInstance>(
            "DefaultInstanceStateAccessor",
            machine,
            AsTypedState(machine.Initial),
            observer);
        ProbeContext probe = DispatchProxy.Create<ProbeContext, RecordingProbeContext>();

        accessor.Probe(probe);
        Assert.Contains(
            ((RecordingProbeContext)(object)probe).Values,
            value => value.Key == "currentStateProperty" && Equals(value.Value, nameof(AccessorInstance.RawState)));

        var instance = new AccessorInstance();
        await WithBehaviorContextAsync(machine, instance, async context =>
        {
            await accessor.SetAsync(context, AsTypedState(machine.Running), TestContext.Current.CancellationToken);
        });

        Assert.Same(machine.Running, instance.RawState);
        Assert.True(accessor.GetStateExpression(machine.Initial, machine.Running).Compile()(instance));
        Assert.Single(observer.Changes);

        var ambiguousMachine = new MultipleConventionMachine();
        SagaStateMachineException exception = Assert.Throws<SagaStateMachineException>(
            () => ambiguousMachine.Accessor.GetStateExpression(ambiguousMachine.Initial));
        Assert.Equal(
            "The InstanceState was not configured, and could not be automatically identified as multiple State properties exist.",
            exception.Message);
    }

    [Theory]
    [InlineData(AccessorStorageKind.Raw)]
    [InlineData(AccessorStorageKind.String)]
    [InlineData(AccessorStorageKind.Integer)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-STORAGE", "unchanged-set-and-expression-cardinality-matrix")]
    public async Task ConfiguredAccessor_UnchangedSetSkipsObservationAndExpressionsHandleEmptyAndMultipleStatesAsync(
        AccessorStorageKind storageKind)
    {
        var machine = new AccessorMachine();
        var observer = new RecordingObserver<AccessorInstance>();
        object index = CreateStateIndex(machine, machine.Running);
        IStateAccessor<AccessorInstance> accessor = storageKind switch
        {
            AccessorStorageKind.Raw => CreateRawAccessor(machine, observer),
            AccessorStorageKind.String => CreateStringAccessor(machine, observer),
            AccessorStorageKind.Integer => CreateIntAccessor(index, observer),
            _ => throw new ArgumentOutOfRangeException(nameof(storageKind), storageKind, "Unknown storage kind."),
        };
        var instance = new AccessorInstance();
        ProbeContext probe = DispatchProxy.Create<ProbeContext, RecordingProbeContext>();
        string expectedProperty = storageKind switch
        {
            AccessorStorageKind.Raw => nameof(AccessorInstance.RawState),
            AccessorStorageKind.String => nameof(AccessorInstance.StringState),
            AccessorStorageKind.Integer => nameof(AccessorInstance.IntState),
            _ => throw new ArgumentOutOfRangeException(nameof(storageKind), storageKind, "Unknown storage kind."),
        };

        accessor.Probe(probe);
        Assert.Contains(
            ((RecordingProbeContext)(object)probe).Values,
            value => value.Key == "currentStateProperty" && Equals(value.Value, expectedProperty));

        ArgumentOutOfRangeException empty = Assert.Throws<ArgumentOutOfRangeException>(
            () => accessor.GetStateExpression([]));
        Assert.Equal("states", empty.ParamName);

        await WithBehaviorContextAsync(machine, instance, async context =>
        {
            IState<AccessorInstance> running = AsTypedState(machine.Running);
            await accessor.SetAsync(context, running, TestContext.Current.CancellationToken);
            await accessor.SetAsync(context, running, TestContext.Current.CancellationToken);
        });

        Assert.Single(observer.Changes);
        Func<AccessorInstance, bool> isInitialOrRunning = accessor
            .GetStateExpression(machine.Initial, machine.Running)
            .Compile();
        Assert.True(isInitialOrRunning(instance));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-STORAGE", "state-index-incompatible-instance-state-diagnostic")]
    public void StateAccessorIndex_RejectsStatesForAnotherSagaInstanceType()
    {
        var machine = new AccessorMachine();
        var foreignMachine = new ForeignMachine();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateNested<AccessorInstance>(
                "StateAccessorIndex",
                machine,
                AsTypedState(machine.Initial),
                AsTypedState(machine.Final),
                new IState[] { foreignMachine.Foreign }));

        Assert.Equal("states", exception.ParamName);
        Assert.Contains("compatible with the saga instance type", exception.Message, StringComparison.Ordinal);
    }

    private static IStateAccessor<AccessorInstance> CreateRawAccessor(
        AccessorMachine machine,
        IStateObserver<AccessorInstance> observer)
    {
        Expression<Func<AccessorInstance, IState?>> expression = instance => instance.RawState;
        return (IStateAccessor<AccessorInstance>)CreateNested<AccessorInstance>("RawStateAccessor", machine, expression, observer);
    }

    private static IStateAccessor<AccessorInstance> CreateStringAccessor(
        AccessorMachine machine,
        IStateObserver<AccessorInstance> observer)
    {
        Expression<Func<AccessorInstance, string>> expression = instance => instance.StringState;
        return (IStateAccessor<AccessorInstance>)CreateNested<AccessorInstance>("StringStateAccessor", machine, expression, observer);
    }

    private static IStateAccessor<AccessorInstance> CreateIntAccessor(
        object index,
        IStateObserver<AccessorInstance> observer)
    {
        Expression<Func<AccessorInstance, int>> expression = instance => instance.IntState;
        return (IStateAccessor<AccessorInstance>)CreateNested<AccessorInstance>("IntStateAccessor", expression, index, observer);
    }

    private static object CreateStateIndex(AccessorMachine machine, params IState[] states) =>
        CreateNested<AccessorInstance>(
            "StateAccessorIndex",
            machine,
            AsTypedState(machine.Initial),
            AsTypedState(machine.Final),
            states);

    private static IState<AccessorInstance> AsTypedState(IState state) =>
        Assert.IsAssignableFrom<IState<AccessorInstance>>(state);

    private static object CreateNested<TInstance>(string typeName, params object?[] arguments)
        where TInstance : class, ISagaStateMachineInstance
    {
        Type type = typeof(ViciOneServiceBusStateMachine<TInstance>).GetNestedType(typeName, BindingFlags.NonPublic)
            ?? throw new Xunit.Sdk.XunitException($"Nested accessor type '{typeName}' was not found.");
        if (type.ContainsGenericParameters)
            type = type.MakeGenericType(typeof(TInstance));

        try
        {
            return Activator.CreateInstance(
                    type,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    binder: null,
                    args: arguments,
                    culture: null)
                ?? throw new Xunit.Sdk.XunitException($"Nested accessor type '{typeName}' was not created.");
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static PropertyInfo GetIndexer(object target, Type parameterType) =>
        target.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(property => property.GetIndexParameters() is [{ ParameterType: var type }] && type == parameterType);

    private static object? InvokeIndexer(PropertyInfo indexer, object target, object? argument)
    {
        try
        {
            return indexer.GetValue(target, [argument]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static async Task WithBehaviorContextAsync<TInstance>(
        ViciOneServiceBusStateMachine<TInstance> machine,
        TInstance instance,
        Func<IBehaviorContext<TInstance>, Task> action)
        where TInstance : class, ISagaStateMachineInstance
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ConsumeContext<StateMachineSignal> consumeContext = InMemoryOutboxTestContextFactory.Create(new StateMachineSignal());
        var sagaInstance = new SagaInstance<TInstance>(instance);
        await sagaInstance.MarkInUseAsync(cancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<TInstance, StateMachineSignal>(consumeContext, sagaInstance);
        IBehaviorContext<TInstance> behaviorContext =
            new ViciOneServiceBusStateMachine<TInstance>.BehaviorContextProxy(machine, sagaContext, machine.Initial.Enter);

        await action(behaviorContext);
    }

    public enum AccessorStorageKind
    {
        Raw,
        String,
        Integer,
    }

    private sealed class AccessorInstance : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public int IntState { get; set; }

        public IState? RawState { get; set; }

        public string StringState { get; set; } = string.Empty;
    }

    private sealed class AccessorMachine : ViciOneServiceBusStateMachine<AccessorInstance>
    {
        public AccessorMachine()
        {
            InstanceState(instance => instance.RawState!);
        }

        public IState Running { get; private set; } = null!;
    }

    private sealed class DefaultAccessorInstance : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public IState? CurrentState { get; set; }
    }

    private sealed class DefaultAccessorMachine : ViciOneServiceBusStateMachine<DefaultAccessorInstance>
    {
        public IState Running { get; private set; } = null!;
    }

    private sealed class IndexerOnlyInstance : ISagaStateMachineInstance
    {
        IState? _state;

        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public IState? this[int index]
        {
            get => _state;
            set => _state = value;
        }
    }

    private sealed class IndexerOnlyMachine : ViciOneServiceBusStateMachine<IndexerOnlyInstance>
    {
    }

    private sealed class MultipleConventionInstance : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public IState? FirstState { get; set; }

        public IState? SecondState { get; set; }
    }

    private sealed class MultipleConventionMachine : ViciOneServiceBusStateMachine<MultipleConventionInstance>
    {
    }

    private sealed class ForeignInstance : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
    }

    private sealed class ForeignMachine : ViciOneServiceBusStateMachine<ForeignInstance>
    {
        public IState Foreign { get; private set; } = null!;
    }

    private class RecordingProbeContext : DispatchProxy
    {
        public List<(string Key, object? Value)> Values { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(ProbeContext.Add) && args is { Length: >= 2 } && args[0] is string key)
                Values.Add((key, args[1]));

            if (targetMethod?.ReturnType == typeof(void) || targetMethod == null)
                return null;

            return targetMethod.ReturnType.IsValueType
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }
    }

    private sealed class CompletedObserver<TInstance> : IStateObserver<TInstance>
        where TInstance : class, ISagaStateMachineInstance
    {
        public Task StateChangedAsync(IBehaviorContext<TInstance> context, IState currentState, IState? previousState) =>
            Task.CompletedTask;
    }

    private sealed class NullTaskObserver<TInstance> : IStateObserver<TInstance>
        where TInstance : class, ISagaStateMachineInstance
    {
        public Task StateChangedAsync(IBehaviorContext<TInstance> context, IState currentState, IState? previousState) => null!;
    }

    private sealed class RecordingObserver<TInstance> : IStateObserver<TInstance>
        where TInstance : class, ISagaStateMachineInstance
    {
        public List<StateChange<TInstance>> Changes { get; } = [];

        public Task StateChangedAsync(IBehaviorContext<TInstance> context, IState currentState, IState? previousState)
        {
            Changes.Add(new StateChange<TInstance>(context.Saga, previousState, currentState));
            return Task.CompletedTask;
        }
    }

    private sealed record StateChange<TInstance>(TInstance Instance, IState? Previous, IState Current);
}
