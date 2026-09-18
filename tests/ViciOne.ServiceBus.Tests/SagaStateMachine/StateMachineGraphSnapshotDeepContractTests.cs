using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineGraphSnapshotDeepContractTests
{
    const BindingFlags DeclaredPublic = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "iteration-215-graph-snapshot-and-extension-exact-public-surface")]
    public void PublicSurface_ExposesOnlyTheCanonicalImmutableSnapshotAndExtensionContracts()
    {
        Type graphType = typeof(StateMachineGraph);
        Assert.True(graphType.IsPublic);
        Assert.True(graphType.IsSealed);
        Assert.False(graphType.IsAbstract);
        Assert.Same(typeof(object), graphType.BaseType);
        Assert.Empty(graphType.GetInterfaces());

        var nullability = new NullabilityInfoContext();
        ConstructorInfo constructor = Assert.Single(graphType.GetConstructors(DeclaredPublic));
        Assert.Equal(
            [typeof(IEnumerable<StateMachineGraphNode>), typeof(IEnumerable<StateMachineGraphEdge>)],
            constructor.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal(["nodes", "edges"], constructor.GetParameters().Select(parameter => parameter.Name));
        Assert.All(constructor.GetParameters(), parameter =>
        {
            Assert.False(parameter.IsOptional);
            Assert.False(parameter.HasDefaultValue);
            NullabilityInfo parameterNullability = nullability.Create(parameter);
            Assert.Equal(NullabilityState.NotNull, parameterNullability.ReadState);
            Assert.Equal(NullabilityState.NotNull, parameterNullability.WriteState);
            NullabilityInfo elementNullability = Assert.Single(parameterNullability.GenericTypeArguments);
            Assert.Equal(NullabilityState.NotNull, elementNullability.ReadState);
            Assert.Equal(NullabilityState.NotNull, elementNullability.WriteState);
        });

        PropertyInfo[] properties = graphType.GetProperties(DeclaredPublic).OrderBy(property => property.Name, StringComparer.Ordinal).ToArray();
        Assert.Collection(
            properties,
            property => AssertReadOnlyProperty(property, "Edges", typeof(IReadOnlyList<StateMachineGraphEdge>), nullability),
            property => AssertReadOnlyProperty(property, "Nodes", typeof(IReadOnlyList<StateMachineGraphNode>), nullability));
        Assert.DoesNotContain(graphType.GetMethods(DeclaredPublic), method => !method.IsSpecialName);
        Assert.Empty(graphType.GetFields(DeclaredPublic));
        Assert.Empty(graphType.GetEvents(DeclaredPublic));
        Assert.DoesNotContain(
            graphType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic),
            IsPublicOrProtected);

        Type extensionsType = typeof(StateMachineGraphExtensions);
        Assert.True(extensionsType.IsPublic);
        Assert.True(extensionsType.IsAbstract);
        Assert.True(extensionsType.IsSealed);
        Assert.Same(typeof(object), extensionsType.BaseType);
        Assert.Empty(extensionsType.GetInterfaces());
        Assert.Empty(extensionsType.GetConstructors(DeclaredPublic));
        Assert.Empty(extensionsType.GetProperties(DeclaredPublic));
        Assert.Empty(extensionsType.GetFields(DeclaredPublic));
        Assert.Empty(extensionsType.GetEvents(DeclaredPublic));
        Assert.DoesNotContain(
            extensionsType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic),
            IsPublicOrProtected);

        MethodInfo getGraph = Assert.Single(extensionsType.GetMethods(DeclaredPublic));
        Assert.Equal("GetGraph", getGraph.Name);
        Assert.True(getGraph.IsPublic);
        Assert.True(getGraph.IsStatic);
        Assert.True(getGraph.IsGenericMethodDefinition);
        Assert.True(getGraph.IsDefined(typeof(ExtensionAttribute), inherit: false));
        Assert.Equal(typeof(StateMachineGraph), getGraph.ReturnType);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(getGraph.ReturnParameter).ReadState);

        Type sagaParameter = Assert.Single(getGraph.GetGenericArguments());
        Assert.Equal("TSaga", sagaParameter.Name);
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            sagaParameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal([typeof(ISagaStateMachineInstance)], sagaParameter.GetGenericParameterConstraints());

        ParameterInfo machine = Assert.Single(getGraph.GetParameters());
        Assert.Equal("machine", machine.Name);
        Assert.False(machine.IsOptional);
        Assert.False(machine.HasDefaultValue);
        Assert.True(machine.ParameterType.IsGenericType);
        Assert.Equal(typeof(IStateMachine<>), machine.ParameterType.GetGenericTypeDefinition());
        Assert.Same(sagaParameter, Assert.Single(machine.ParameterType.GetGenericArguments()));
        NullabilityInfo machineNullability = nullability.Create(machine);
        Assert.Equal(NullabilityState.NotNull, machineNullability.ReadState);
        Assert.Equal(NullabilityState.NotNull, machineNullability.WriteState);
        NullabilityInfo sagaNullability = Assert.Single(machineNullability.GenericTypeArguments);
        Assert.Equal(NullabilityState.NotNull, sagaNullability.ReadState);
        Assert.Equal(NullabilityState.NotNull, sagaNullability.WriteState);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "iteration-215-graph-top-level-null-short-circuit")]
    public void Constructor_ValidatesTopLevelNullsWithoutEnumeratingTheOtherSource()
    {
        var poisonedNodes = new PoisonEnumerable<StateMachineGraphNode>();
        var poisonedEdges = new PoisonEnumerable<StateMachineGraphEdge>();

        Assert.Equal(
            "nodes",
            Assert.Throws<ArgumentNullException>(() => new StateMachineGraph(null!, null!)).ParamName);

        Assert.Equal(
            "nodes",
            Assert.Throws<ArgumentNullException>(() => new StateMachineGraph(null!, poisonedEdges)).ParamName);
        Assert.Equal(0, poisonedEdges.GetEnumeratorCalls);

        Assert.Equal(
            "edges",
            Assert.Throws<ArgumentNullException>(() => new StateMachineGraph(poisonedNodes, null!)).ParamName);
        Assert.Equal(0, poisonedNodes.GetEnumeratorCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "iteration-215-graph-one-shot-ordered-immutable-snapshot")]
    public void Constructor_ConsumesEachSourceOnceAndFreezesExactOrderIdentityAndContents()
    {
        StateMachineGraphNode initial = StateMachineGraphNode.CreateState("Initial");
        StateMachineGraphNode start = StateMachineGraphNode.CreateEvent("Start", typeof(Start));
        StateMachineGraphNode running = StateMachineGraphNode.CreateState("Running");
        var binding = new StateMachineGraphEdge(initial, start, StateMachineGraphEdgeKind.EventBinding);
        var transition = new StateMachineGraphEdge(start, running, StateMachineGraphEdgeKind.StateTransition);
        var sourceNodes = new List<StateMachineGraphNode> { start, initial, running };
        var sourceEdges = new List<StateMachineGraphEdge> { transition, binding };
        var nodes = new SingleUseEnumerable<StateMachineGraphNode>(sourceNodes);
        var edges = new SingleUseEnumerable<StateMachineGraphEdge>(sourceEdges);

        var graph = new StateMachineGraph(nodes, edges);

        Assert.Equal(1, nodes.GetEnumeratorCalls);
        Assert.Equal(1, edges.GetEnumeratorCalls);
        Assert.Equal(1, nodes.DisposeCalls);
        Assert.Equal(1, edges.DisposeCalls);
        sourceNodes.Clear();
        sourceEdges.Clear();

        Assert.Same(graph.Nodes, graph.Nodes);
        Assert.Same(graph.Edges, graph.Edges);
        Assert.Collection(
            graph.Nodes,
            node => Assert.Same(start, node),
            node => Assert.Same(initial, node),
            node => Assert.Same(running, node));
        Assert.Collection(
            graph.Edges,
            edge => Assert.Same(transition, edge),
            edge => Assert.Same(binding, edge));

        IList<StateMachineGraphNode> exposedNodes = Assert.IsAssignableFrom<IList<StateMachineGraphNode>>(graph.Nodes);
        IList<StateMachineGraphEdge> exposedEdges = Assert.IsAssignableFrom<IList<StateMachineGraphEdge>>(graph.Edges);
        Assert.True(exposedNodes.IsReadOnly);
        Assert.True(exposedEdges.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => exposedNodes[0] = running);
        Assert.Throws<NotSupportedException>(() => exposedNodes.RemoveAt(0));
        Assert.Throws<NotSupportedException>(() => exposedEdges[0] = binding);
        Assert.Throws<NotSupportedException>(() => exposedEdges.RemoveAt(0));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "iteration-215-graph-node-first-identity-validation-diagnostics")]
    public void Constructor_RejectsInvalidNodesBeforeTouchingEdgesAndReportsExactDiagnosticClasses()
    {
        StateMachineGraphNode initial = StateMachineGraphNode.CreateState("Initial");
        StateMachineGraphNode start = StateMachineGraphNode.CreateEvent("Start", typeof(Start));
        StateMachineGraphNode sameMetadataInitial = StateMachineGraphNode.CreateState("Initial");
        var edge = new StateMachineGraphEdge(initial, start, StateMachineGraphEdgeKind.EventBinding);
        var equalEdge = new StateMachineGraphEdge(initial, start, StateMachineGraphEdgeKind.EventBinding);
        var poisonedEdges = new PoisonEnumerable<StateMachineGraphEdge>();

        ArgumentException nullNode = Assert.Throws<ArgumentException>(() =>
            new StateMachineGraph([initial, null!, initial], poisonedEdges));
        Assert.Equal("nodes", nullNode.ParamName);
        Assert.StartsWith("Graph nodes cannot contain null elements.", nullNode.Message, StringComparison.Ordinal);
        Assert.Equal(0, poisonedEdges.GetEnumeratorCalls);

        ArgumentException duplicateNode = Assert.Throws<ArgumentException>(() =>
            new StateMachineGraph([initial, initial], poisonedEdges));
        Assert.Equal("nodes", duplicateNode.ParamName);
        Assert.StartsWith("Graph nodes must be unique.", duplicateNode.Message, StringComparison.Ordinal);
        Assert.Equal(0, poisonedEdges.GetEnumeratorCalls);

        ArgumentException nullEdge = Assert.Throws<ArgumentException>(() =>
            new StateMachineGraph([initial, start], [edge, null!, equalEdge]));
        Assert.Equal("edges", nullEdge.ParamName);
        Assert.StartsWith("Graph edges cannot contain null elements.", nullEdge.Message, StringComparison.Ordinal);

        ArgumentException duplicateEdge = Assert.Throws<ArgumentException>(() =>
            new StateMachineGraph([initial, start], [edge, equalEdge]));
        Assert.Equal("edges", duplicateEdge.ParamName);
        Assert.StartsWith("Graph edges must be unique.", duplicateEdge.Message, StringComparison.Ordinal);

        ArgumentException duplicateDanglingEdge = Assert.Throws<ArgumentException>(() =>
            new StateMachineGraph([start], [edge, equalEdge]));
        Assert.Equal("edges", duplicateDanglingEdge.ParamName);
        Assert.StartsWith("Graph edges must be unique.", duplicateDanglingEdge.Message, StringComparison.Ordinal);

        var identityMismatch = new StateMachineGraphEdge(sameMetadataInitial, start, StateMachineGraphEdgeKind.EventBinding);
        ArgumentException danglingEdge = Assert.Throws<ArgumentException>(() =>
            new StateMachineGraph([initial, start], [identityMismatch]));
        Assert.Equal("edges", danglingEdge.ParamName);
        Assert.StartsWith("Every graph edge endpoint must belong to the graph nodes.", danglingEdge.Message, StringComparison.Ordinal);

        var graph = new StateMachineGraph([initial, sameMetadataInitial, start], [edge, identityMismatch]);
        Assert.Collection(
            graph.Nodes,
            node => Assert.Same(initial, node),
            node => Assert.Same(sameMetadataInitial, node),
            node => Assert.Same(start, node));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "iteration-215-graph-enumerator-failure-identity-and-disposal")]
    public void Constructor_PreservesEnumerationFailureIdentityAndShortCircuitsTheSecondSource()
    {
        StateMachineGraphNode initial = StateMachineGraphNode.CreateState("Initial");
        var nodeFailure = new InvalidOperationException("node enumeration failed");
        var nodes = new FailingEnumerable<StateMachineGraphNode>(initial, nodeFailure);
        var untouchedEdges = new PoisonEnumerable<StateMachineGraphEdge>();

        Assert.Same(
            nodeFailure,
            Assert.Throws<InvalidOperationException>(() => new StateMachineGraph(nodes, untouchedEdges)));
        Assert.Equal(1, nodes.GetEnumeratorCalls);
        Assert.Equal(1, nodes.DisposeCalls);
        Assert.Equal(0, untouchedEdges.GetEnumeratorCalls);

        StateMachineGraphNode start = StateMachineGraphNode.CreateEvent("Start", typeof(Start));
        var edge = new StateMachineGraphEdge(initial, start, StateMachineGraphEdgeKind.EventBinding);
        var validNodes = new SingleUseEnumerable<StateMachineGraphNode>([initial, start]);
        var edgeFailure = new InvalidOperationException("edge enumeration failed");
        var edges = new FailingEnumerable<StateMachineGraphEdge>(edge, edgeFailure);

        Assert.Same(
            edgeFailure,
            Assert.Throws<InvalidOperationException>(() => new StateMachineGraph(validNodes, edges)));
        Assert.Equal(1, validNodes.GetEnumeratorCalls);
        Assert.Equal(1, validNodes.DisposeCalls);
        Assert.Equal(1, edges.GetEnumeratorCalls);
        Assert.Equal(1, edges.DisposeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "iteration-215-get-graph-visit-and-declaration-order")]
    public void GetGraph_VisitsBeforeEnumeratingDeclarationsOnceAndReturnsTheirOrderedSnapshot()
    {
        var log = new List<string>();
        var states = new SingleUseEnumerable<IState>(
            [new TestState("Second"), new TestState("First")],
            () => log.Add("states"));
        var events = new SingleUseEnumerable<IEvent>(
            [new TriggerEvent("Later"), new TriggerEvent("Earlier")],
            () => log.Add("events"));
        var machine = new RecordingMachine(states, events, log);

        StateMachineGraph graph = machine.GetGraph();

        Assert.Equal(1, machine.AcceptCalls);
        Assert.Equal(1, states.GetEnumeratorCalls);
        Assert.Equal(1, events.GetEnumeratorCalls);
        Assert.Equal(1, states.DisposeCalls);
        Assert.Equal(1, events.DisposeCalls);
        Assert.Equal(
            ["accept", "states", "events", "composite:Later", "composite:Earlier"],
            log);
        Assert.Collection(
            graph.Nodes,
            node => AssertNode(node, StateMachineGraphNodeKind.State, "Second"),
            node => AssertNode(node, StateMachineGraphNodeKind.State, "First"),
            node => AssertNode(node, StateMachineGraphNodeKind.Event, "Later"),
            node => AssertNode(node, StateMachineGraphNodeKind.Event, "Earlier"));
        Assert.Empty(graph.Edges);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "iteration-215-get-graph-failure-identity-and-short-circuit")]
    public void GetGraph_PreservesAcceptAndDeclarationFailureIdentityWithoutContinuing()
    {
        var untouchedStates = new PoisonEnumerable<IState>();
        var untouchedEvents = new PoisonEnumerable<IEvent>();
        var acceptFailure = new InvalidOperationException("accept failed");
        var acceptMachine = new RecordingMachine(untouchedStates, untouchedEvents, [], acceptFailure);

        Assert.Same(
            acceptFailure,
            Assert.Throws<InvalidOperationException>(() => acceptMachine.GetGraph()));
        Assert.Equal(1, acceptMachine.AcceptCalls);
        Assert.Equal(0, untouchedStates.GetEnumeratorCalls);
        Assert.Equal(0, untouchedEvents.GetEnumeratorCalls);

        var stateFailure = new InvalidOperationException("states failed");
        var failingStates = new FailingEnumerable<IState>(new TestState("Observed"), stateFailure);
        var skippedEvents = new PoisonEnumerable<IEvent>();
        var declarationMachine = new RecordingMachine(failingStates, skippedEvents, []);

        Assert.Same(
            stateFailure,
            Assert.Throws<InvalidOperationException>(() => declarationMachine.GetGraph()));
        Assert.Equal(1, declarationMachine.AcceptCalls);
        Assert.Equal(1, failingStates.GetEnumeratorCalls);
        Assert.Equal(1, failingStates.DisposeCalls);
        Assert.Equal(0, skippedEvents.GetEnumeratorCalls);

        var validStates = new SingleUseEnumerable<IState>([new TestState("Observed")]);
        var eventFailure = new InvalidOperationException("events failed");
        var failingEvents = new FailingEnumerable<IEvent>(new TriggerEvent("Observed"), eventFailure);
        var eventDeclarationMachine = new RecordingMachine(validStates, failingEvents, []);

        Assert.Same(
            eventFailure,
            Assert.Throws<InvalidOperationException>(() => eventDeclarationMachine.GetGraph()));
        Assert.Equal(1, eventDeclarationMachine.AcceptCalls);
        Assert.Equal(1, validStates.GetEnumeratorCalls);
        Assert.Equal(1, validStates.DisposeCalls);
        Assert.Equal(1, failingEvents.GetEnumeratorCalls);
        Assert.Equal(1, failingEvents.DisposeCalls);
    }

    private static void AssertReadOnlyProperty(
        PropertyInfo property,
        string name,
        Type propertyType,
        NullabilityInfoContext nullability)
    {
        Assert.Equal(name, property.Name);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.False(property.CanWrite);
        Assert.NotNull(property.GetMethod);
        Assert.True(property.GetMethod.IsPublic);
        Assert.False(property.GetMethod.IsStatic);
        Assert.False(property.GetMethod.IsVirtual);
        NullabilityInfo propertyNullability = nullability.Create(property);
        Assert.Equal(NullabilityState.NotNull, propertyNullability.ReadState);
        Assert.Equal(NullabilityState.Unknown, propertyNullability.WriteState);
        NullabilityInfo elementNullability = Assert.Single(propertyNullability.GenericTypeArguments);
        Assert.Equal(NullabilityState.NotNull, elementNullability.ReadState);
        Assert.Equal(NullabilityState.NotNull, elementNullability.WriteState);
    }

    private static void AssertNode(StateMachineGraphNode node, StateMachineGraphNodeKind kind, string name)
    {
        Assert.Equal(kind, node.Kind);
        Assert.Equal(name, node.Name);
        Assert.Null(node.MessageType);
        Assert.Null(node.ExceptionType);
        Assert.False(node.IsCompositeEvent);
    }

    private static bool IsPublicOrProtected(Type type) =>
        type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamORAssem || type.IsNestedFamANDAssem;

    private sealed class PoisonEnumerable<T> : IEnumerable<T>
    {
        public int GetEnumeratorCalls { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            GetEnumeratorCalls++;
            throw new InvalidOperationException("This sequence must not be enumerated.");
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class SingleUseEnumerable<T>(IEnumerable<T> items, Action? onEnumeration = null) : IEnumerable<T>
    {
        public int GetEnumeratorCalls { get; private set; }
        public int DisposeCalls { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            GetEnumeratorCalls++;
            if (GetEnumeratorCalls != 1)
                throw new InvalidOperationException("The sequence was enumerated more than once.");

            onEnumeration?.Invoke();
            return Enumerate().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private IEnumerable<T> Enumerate()
        {
            try
            {
                foreach (T item in items)
                    yield return item;
            }
            finally
            {
                DisposeCalls++;
            }
        }
    }

    private sealed class FailingEnumerable<T>(T first, Exception failure) : IEnumerable<T>
    {
        public int GetEnumeratorCalls { get; private set; }
        public int DisposeCalls { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            GetEnumeratorCalls++;
            return Enumerate().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private IEnumerable<T> Enumerate()
        {
            try
            {
                yield return first;
                throw failure;
            }
            finally
            {
                DisposeCalls++;
            }
        }
    }

    private sealed class RecordingMachine(
        IEnumerable<IState> states,
        IEnumerable<IEvent> events,
        List<string> log,
        Exception? acceptFailure = null) : IStateMachine<GraphState>
    {
        public int AcceptCalls { get; private set; }
        public IStateAccessor<GraphState> Accessor => throw Unexpected();
        public string Name => nameof(RecordingMachine);
        public IEnumerable<IEvent> Events => events;
        public IEnumerable<IState> States => states;
        public Type InstanceType => typeof(GraphState);
        public IState Initial => throw Unexpected();
        public IState Final => throw Unexpected();

        public IEvent GetEvent(string name) => throw Unexpected();
        IState IStateMachine.GetState(string name) => throw Unexpected();
        public IState<GraphState> GetState(string name) => throw Unexpected();
        public IEnumerable<IEvent> NextEvents(IState state) => throw Unexpected();

        public bool IsCompositeEvent(IEvent @event)
        {
            log.Add($"composite:{@event.Name}");
            return false;
        }

        public Task RaiseEventAsync(IBehaviorContext<GraphState> context, CancellationToken cancellationToken = default) =>
            throw Unexpected();

        public Task RaiseEventAsync<T>(IBehaviorContext<GraphState, T> context, CancellationToken cancellationToken = default)
            where T : class => throw Unexpected();

        public IDisposable ConnectEventObserver(IEventObserver<GraphState> observer) => throw Unexpected();
        public IDisposable ConnectEventObserver(IEvent @event, IEventObserver<GraphState> observer) => throw Unexpected();
        public IDisposable ConnectStateObserver(IStateObserver<GraphState> observer) => throw Unexpected();

        public void Accept(IStateMachineVisitor visitor)
        {
            AcceptCalls++;
            log.Add("accept");
            if (acceptFailure is not null)
                throw acceptFailure;
        }

        public void Probe(ProbeContext context) => throw Unexpected();

        private static Exception Unexpected() => new InvalidOperationException("An unrelated machine member was called.");
    }

    private sealed class TestState(string name) : IState
    {
        public string Name { get; } = name;
        public IEvent Enter => throw Unexpected();
        public IEvent Leave => throw Unexpected();
        public IEvent<IState> BeforeEnter => throw Unexpected();
        public IEvent<IState> AfterLeave => throw Unexpected();
        public int CompareTo(IState? other) => string.Compare(Name, other?.Name, StringComparison.Ordinal);
        public void Accept(IStateMachineVisitor visitor) => throw Unexpected();
        public void Probe(ProbeContext context) => throw Unexpected();

        private static Exception Unexpected() => new InvalidOperationException("An unrelated state member was called.");
    }

    private sealed class GraphState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public IState CurrentState { get; set; } = null!;
    }

    private sealed record Start;
}
