using System.Collections;
using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineGraphContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "catch-activity-contract")]
    public void CatchFaultActivity_ValidatesBehaviorAndExposesHandledExceptionType()
    {
        var activity = new CatchFaultActivity<GraphState, DerivedGraphException>(new EmptyBehavior<GraphState>());

        var exceptionActivity = Assert.IsAssignableFrom<IStateMachineExceptionActivity>(activity);
        Assert.Equal(typeof(DerivedGraphException), exceptionActivity.ExceptionType);
        Assert.Equal(
            "behavior",
            Assert.Throws<ArgumentNullException>(() =>
                new CatchFaultActivity<GraphState, DerivedGraphException>(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "valid-node-factories")]
    public void NodeFactories_CreatePreciseStateAndEventShapes()
    {
        StateMachineGraphNode state = StateMachineGraphNode.CreateState("Running");
        StateMachineGraphNode untypedEvent = StateMachineGraphNode.CreateEvent("Wake");
        StateMachineGraphNode messageEvent = StateMachineGraphNode.CreateEvent("Start", typeof(Start), true);
        StateMachineGraphNode exception = StateMachineGraphNode.CreateException(typeof(InvalidOperationException));

        Assert.Equal(StateMachineGraphNodeKind.State, state.Kind);
        Assert.Equal("Running", state.Name);
        Assert.Null(state.MessageType);
        Assert.Null(state.ExceptionType);
        Assert.False(state.IsCompositeEvent);
        Assert.Equal(StateMachineGraphNodeKind.Event, untypedEvent.Kind);
        Assert.Null(untypedEvent.MessageType);
        Assert.Null(untypedEvent.ExceptionType);
        Assert.False(untypedEvent.IsCompositeEvent);
        Assert.Equal(StateMachineGraphNodeKind.Event, messageEvent.Kind);
        Assert.Equal(typeof(Start), messageEvent.MessageType);
        Assert.Null(messageEvent.ExceptionType);
        Assert.True(messageEvent.IsCompositeEvent);
        Assert.Equal(StateMachineGraphNodeKind.Exception, exception.Kind);
        Assert.Equal(nameof(InvalidOperationException), exception.Name);
        Assert.Null(exception.MessageType);
        Assert.Equal(typeof(InvalidOperationException), exception.ExceptionType);
        Assert.False(exception.IsCompositeEvent);
        Assert.Equal("State: Running", state.ToString());
        Assert.Equal("Event: Start (Start)", messageEvent.ToString());
        Assert.Equal("Exception: InvalidOperationException", exception.ToString());

        Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => StateMachineGraphNode.CreateState(null!)).ParamName);
        Assert.Equal("name", Assert.Throws<ArgumentException>(() => StateMachineGraphNode.CreateState(" ")).ParamName);
        Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => StateMachineGraphNode.CreateEvent(null!)).ParamName);
        Assert.Equal("name", Assert.Throws<ArgumentException>(() => StateMachineGraphNode.CreateEvent(string.Empty)).ParamName);
        Assert.Equal("name", Assert.Throws<ArgumentException>(() => StateMachineGraphNode.CreateEvent("\t")).ParamName);
        Assert.Equal("exceptionType", Assert.Throws<ArgumentNullException>(() => StateMachineGraphNode.CreateException(null!)).ParamName);
        Assert.Equal("exceptionType", Assert.Throws<ArgumentException>(() => StateMachineGraphNode.CreateException(typeof(string))).ParamName);
        Assert.Equal("messageType", Assert.Throws<ArgumentException>(() => StateMachineGraphNode.CreateEvent("Value", typeof(int))).ParamName);
        Assert.Equal("messageType", Assert.Throws<ArgumentException>(() => StateMachineGraphNode.CreateEvent("Void", typeof(void))).ParamName);
        Assert.Equal("messageType", Assert.Throws<ArgumentException>(() => StateMachineGraphNode.CreateEvent("Open", typeof(List<>))).ParamName);
        Assert.Equal("messageType", Assert.Throws<ArgumentException>(() => StateMachineGraphNode.CreateEvent("Pointer", typeof(int).MakePointerType())).ParamName);
        Assert.Equal("messageType", Assert.Throws<ArgumentException>(() => StateMachineGraphNode.CreateEvent("ByRef", typeof(int).MakeByRefType())).ParamName);
        Assert.Equal("messageType", Assert.Throws<ArgumentException>(() => StateMachineGraphNode.CreateEvent("Static", typeof(StaticMessage))).ParamName);
        unsafe
        {
            Assert.Equal(
                "messageType",
                Assert.Throws<ArgumentException>(() => StateMachineGraphNode.CreateEvent("FunctionPointer", typeof(delegate*<void>))).ParamName);
        }
        Assert.Equal(
            "exceptionType",
            Assert.Throws<ArgumentException>(() => StateMachineGraphNode.CreateException(typeof(GenericGraphException<>))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "node-binding-identity")]
    public void NodesWithTheSameMetadata_RemainDistinctGraphElements()
    {
        StateMachineGraphNode firstBinding = StateMachineGraphNode.CreateEvent("Updated", typeof(Start), true);
        StateMachineGraphNode secondBinding = StateMachineGraphNode.CreateEvent("Updated", typeof(Start), true);

        Assert.NotSame(firstBinding, secondBinding);
        Assert.NotEqual(firstBinding, secondBinding);
        var graph = new StateMachineGraph([firstBinding, secondBinding], []);
        Assert.Equal([firstBinding, secondBinding], graph.Nodes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "edge-contract-and-endpoint-identity")]
    public void Edge_RequiresEndpointsAndUsesTheirGraphIdentity()
    {
        StateMachineGraphNode source = StateMachineGraphNode.CreateState("Initial");
        StateMachineGraphNode target = StateMachineGraphNode.CreateEvent("Start", typeof(Start));
        StateMachineGraphNode exception = StateMachineGraphNode.CreateException(typeof(InvalidOperationException));
        StateMachineGraphNode composite = StateMachineGraphNode.CreateEvent("Ready", isCompositeEvent: true);
        StateMachineGraphNode otherState = StateMachineGraphNode.CreateState("Running");
        var edge = new StateMachineGraphEdge(source, target, StateMachineGraphEdgeKind.EventBinding);
        var equivalent = new StateMachineGraphEdge(source, target, StateMachineGraphEdgeKind.EventBinding);
        var sameMetadata = new StateMachineGraphEdge(
            StateMachineGraphNode.CreateState("Initial"),
            StateMachineGraphNode.CreateEvent("Start", typeof(Start)),
            StateMachineGraphEdgeKind.EventBinding);

        Assert.Same(source, edge.Source);
        Assert.Same(target, edge.Target);
        Assert.Equal(StateMachineGraphEdgeKind.EventBinding, edge.Kind);
        Assert.Equal(edge, equivalent);
        Assert.Equal(edge.GetHashCode(), equivalent.GetHashCode());
        Assert.NotEqual(edge, sameMetadata);
        Assert.Equal("EventBinding: Initial -> Start", edge.ToString());
        Assert.NotEqual(edge, new StateMachineGraphEdge(target, source, StateMachineGraphEdgeKind.StateTransition));
        Assert.False(edge.Equals(null));
        Assert.False(edge.Equals(new object()));
        Assert.Equal(
            StateMachineGraphEdgeKind.StateTransition,
            new StateMachineGraphEdge(target, otherState, StateMachineGraphEdgeKind.StateTransition).Kind);
        Assert.Equal(
            StateMachineGraphEdgeKind.StateTransition,
            new StateMachineGraphEdge(exception, otherState, StateMachineGraphEdgeKind.StateTransition).Kind);
        Assert.Equal(
            StateMachineGraphEdgeKind.ExceptionHandler,
            new StateMachineGraphEdge(target, exception, StateMachineGraphEdgeKind.ExceptionHandler).Kind);
        Assert.Equal(
            StateMachineGraphEdgeKind.ExceptionHandler,
            new StateMachineGraphEdge(
                exception,
                StateMachineGraphNode.CreateException(typeof(ArgumentException)),
                StateMachineGraphEdgeKind.ExceptionHandler).Kind);
        Assert.Equal(
            StateMachineGraphEdgeKind.CompositeContribution,
            new StateMachineGraphEdge(target, composite, StateMachineGraphEdgeKind.CompositeContribution).Kind);
        Assert.Equal(
            StateMachineGraphEdgeKind.CompositeContribution,
            new StateMachineGraphEdge(exception, composite, StateMachineGraphEdgeKind.CompositeContribution).Kind);
        Assert.Equal(
            StateMachineGraphEdgeKind.StateInheritance,
            new StateMachineGraphEdge(source, otherState, StateMachineGraphEdgeKind.StateInheritance).Kind);

        Assert.Equal(
            "source",
            Assert.Throws<ArgumentNullException>(() =>
                new StateMachineGraphEdge(null!, target, StateMachineGraphEdgeKind.EventBinding)).ParamName);
        Assert.Equal(
            "target",
            Assert.Throws<ArgumentNullException>(() =>
                new StateMachineGraphEdge(source, null!, StateMachineGraphEdgeKind.EventBinding)).ParamName);
        Assert.Equal(
            "kind",
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StateMachineGraphEdge(source, target, (StateMachineGraphEdgeKind)int.MaxValue)).ParamName);
        Assert.Equal(
            "kind",
            Assert.Throws<ArgumentException>(() =>
                new StateMachineGraphEdge(target, composite, StateMachineGraphEdgeKind.EventBinding)).ParamName);
        Assert.Equal(
            "kind",
            Assert.Throws<ArgumentException>(() =>
                new StateMachineGraphEdge(source, otherState, StateMachineGraphEdgeKind.StateTransition)).ParamName);
        Assert.Equal(
            "kind",
            Assert.Throws<ArgumentException>(() =>
                new StateMachineGraphEdge(source, exception, StateMachineGraphEdgeKind.ExceptionHandler)).ParamName);
        Assert.Equal(
            "kind",
            Assert.Throws<ArgumentException>(() =>
                new StateMachineGraphEdge(target, otherState, StateMachineGraphEdgeKind.ExceptionHandler)).ParamName);
        Assert.Equal(
            "kind",
            Assert.Throws<ArgumentException>(() =>
                new StateMachineGraphEdge(source, composite, StateMachineGraphEdgeKind.CompositeContribution)).ParamName);
        Assert.Equal(
            "kind",
            Assert.Throws<ArgumentException>(() =>
                new StateMachineGraphEdge(target, StateMachineGraphNode.CreateEvent("Ordinary"), StateMachineGraphEdgeKind.CompositeContribution)).ParamName);
        Assert.Equal(
            "kind",
            Assert.Throws<ArgumentException>(() =>
                new StateMachineGraphEdge(target, otherState, StateMachineGraphEdgeKind.StateInheritance)).ParamName);
        Assert.Equal(
            "kind",
            Assert.Throws<ArgumentException>(() =>
                new StateMachineGraphEdge(source, target, StateMachineGraphEdgeKind.StateInheritance)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "immutable-ordered-snapshot")]
    public void Graph_CopiesInputsAndExposesOrderedReadOnlyCollections()
    {
        StateMachineGraphNode initial = StateMachineGraphNode.CreateState("Initial");
        StateMachineGraphNode start = StateMachineGraphNode.CreateEvent("Start", typeof(Start));
        var edge = new StateMachineGraphEdge(initial, start, StateMachineGraphEdgeKind.EventBinding);
        var nodes = new List<StateMachineGraphNode> { initial, start };
        var edges = new List<StateMachineGraphEdge> { edge };

        var graph = new StateMachineGraph(nodes, edges);
        nodes.Clear();
        edges.Clear();

        Assert.Equal([initial, start], graph.Nodes);
        Assert.Equal([edge], graph.Edges);
        Assert.True(Assert.IsAssignableFrom<IList>(graph.Nodes).IsReadOnly);
        Assert.True(Assert.IsAssignableFrom<IList>(graph.Edges).IsReadOnly);
        Assert.Throws<NotSupportedException>(() => Assert.IsAssignableFrom<IList<StateMachineGraphNode>>(graph.Nodes).Add(initial));
        Assert.Throws<NotSupportedException>(() => Assert.IsAssignableFrom<IList<StateMachineGraphEdge>>(graph.Edges).Clear());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "invalid-graph-rejection")]
    public void Graph_RejectsNullDuplicateAndDanglingInputs()
    {
        StateMachineGraphNode initial = StateMachineGraphNode.CreateState("Initial");
        StateMachineGraphNode start = StateMachineGraphNode.CreateEvent("Start", typeof(Start));
        var edge = new StateMachineGraphEdge(initial, start, StateMachineGraphEdgeKind.EventBinding);

        Assert.Equal("nodes", Assert.Throws<ArgumentNullException>(() => new StateMachineGraph(null!, [])).ParamName);
        Assert.Equal("edges", Assert.Throws<ArgumentNullException>(() => new StateMachineGraph([], null!)).ParamName);
        Assert.Equal("nodes", Assert.Throws<ArgumentException>(() => new StateMachineGraph([initial, null!], [])).ParamName);
        Assert.Equal("edges", Assert.Throws<ArgumentException>(() => new StateMachineGraph([initial, start], [edge, null!])).ParamName);
        Assert.Equal("nodes", Assert.Throws<ArgumentException>(() => new StateMachineGraph([initial, initial], [])).ParamName);
        Assert.Equal(2, new StateMachineGraph([initial, StateMachineGraphNode.CreateState("Initial")], []).Nodes.Count);
        Assert.Equal(
            "edges",
            Assert.Throws<ArgumentException>(() => new StateMachineGraph(
                [initial, start],
                [edge, new StateMachineGraphEdge(initial, start, StateMachineGraphEdgeKind.EventBinding)])).ParamName);
        Assert.Equal("edges", Assert.Throws<ArgumentException>(() => new StateMachineGraph([initial], [edge])).ParamName);
        Assert.Equal("edges", Assert.Throws<ArgumentException>(() => new StateMachineGraph([start], [edge])).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-GRAPH", "semantic-machine-projection-and-internal-visitor")]
    public void GetGraph_ProjectsSemanticNodesWhileKeepingTheConcreteVisitorInternal()
    {
        var machine = new GraphMachine();

        StateMachineGraph graph = machine.GetGraph();

        Assert.Contains(graph.Nodes, node =>
            node.Kind == StateMachineGraphNodeKind.State
            && node.Name == machine.Initial.Name
            && node.MessageType is null);
        Assert.Contains(graph.Nodes, node =>
            node.Kind == StateMachineGraphNodeKind.State
            && node.Name == machine.Running.Name
            && node.MessageType is null);
        Assert.Contains(graph.Nodes, node =>
            node.Kind == StateMachineGraphNodeKind.Event
            && node.Name == machine.Start.Name
            && node.MessageType == typeof(Start));
        Assert.Contains(graph.Edges, edge => edge.Source.Name == machine.Initial.Name && edge.Target.Name == machine.Start.Name);
        Assert.Contains(graph.Edges, edge => edge.Source.Name == machine.Start.Name && edge.Target.Name == machine.Running.Name);

        StateMachineGraph derivedCatchGraph = new DerivedCatchMachine().GetGraph();
        Assert.Contains(derivedCatchGraph.Nodes, node =>
            node.Kind == StateMachineGraphNodeKind.Exception
            && node.ExceptionType == typeof(DerivedGraphException));

        StateMachineGraph foreignStateGraph = new ForeignStateMachine().GetGraph();
        Assert.Contains(foreignStateGraph.Edges, edge =>
            edge.Source.Name == nameof(ViciOneServiceBusStateMachine<GraphState>.Initial)
            && edge.Target.Name == nameof(GraphMachine.Start)
            && edge.Kind == StateMachineGraphEdgeKind.EventBinding);

        Assembly assembly = typeof(StateMachineGraph).Assembly;
        Type visitor = assembly.GetType(
            "ViciOne.ServiceBus.SagaStateMachine.StateMachineGraphVisitor`1",
            throwOnError: false)
            ?? throw new InvalidDataException("The internal graph visitor is missing.");
        Assert.True(visitor.IsNotPublic);
        Assert.Null(assembly.GetType("ViciOne.ServiceBus.SagaStateMachine.GraphStateMachineVisitor`1", throwOnError: false));
        Assert.Null(assembly.GetType("ViciOne.ServiceBus.SagaStateMachine.Vertex", throwOnError: false));
        Assert.Null(assembly.GetType("ViciOne.ServiceBus.SagaStateMachine.Edge", throwOnError: false));
        Assert.Null(assembly.GetType("ViciOne.ServiceBus.SagaStateMachine.GraphStateMachineExtensions", throwOnError: false));

        StateMachine<GraphState> missingMachine = null!;
        Assert.Equal("machine", Assert.Throws<ArgumentNullException>(() => missingMachine.GetGraph()).ParamName);
    }

    public sealed record Start;

    public sealed record Stop;

    public sealed class GenericGraphException<T> : Exception
    {
    }

    static class StaticMessage
    {
    }

    sealed class GraphState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public State CurrentState { get; set; } = null!;
    }

    sealed class GraphMachine : ViciOneServiceBusStateMachine<GraphState>
    {
        public GraphMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(When(Start).TransitionTo(Running));
        }

        public State Running { get; private set; } = null!;

        public Event<Start> Start { get; private set; } = null!;
    }

    sealed class DerivedCatchMachine : ViciOneServiceBusStateMachine<GraphState>
    {
        public DerivedCatchMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(When(Start).Execute(new DerivedCatchActivity()).TransitionTo(Running));
        }

        public State Running { get; private set; } = null!;

        public Event Start { get; private set; } = null!;
    }

    sealed class DerivedCatchActivity : CatchFaultActivity<GraphState, DerivedGraphException>
    {
        public DerivedCatchActivity()
            : base(new EmptyBehavior<GraphState>())
        {
        }
    }

    sealed class DerivedGraphException : Exception
    {
    }

    sealed class ForeignStateMachine : StateMachine<GraphState>
    {
        readonly GraphMachine _inner = new();
        readonly State _visitedState;

        public ForeignStateMachine()
        {
            _visitedState = new ForeignState(_inner.Initial);
        }

        public IStateAccessor<GraphState> Accessor => _inner.Accessor;

        public string Name => ((StateMachine)_inner).Name;

        public IEnumerable<Event> Events => _inner.Events;

        public IEnumerable<State> States => [_visitedState];

        public Type InstanceType => typeof(GraphState);

        public State Initial => _visitedState;

        public State Final => _visitedState;

        public Event GetEvent(string name) => ((StateMachine)_inner).GetEvent(name);

        State StateMachine.GetState(string name) => GetState(name);

        public State<GraphState> GetState(string name) => _inner.GetState(name);

        public IEnumerable<Event> NextEvents(State state) => _inner.NextEvents(_inner.GetState(state.Name));

        public bool IsCompositeEvent(Event @event) => _inner.IsCompositeEvent(@event);

        public Task RaiseEventAsync(BehaviorContext<GraphState> context, CancellationToken cancellationToken = default) =>
            ((StateMachine<GraphState>)_inner).RaiseEventAsync(context, cancellationToken);

        public Task RaiseEventAsync<T>(BehaviorContext<GraphState, T> context, CancellationToken cancellationToken = default)
            where T : class => ((StateMachine<GraphState>)_inner).RaiseEventAsync(context, cancellationToken);

        public IDisposable ConnectEventObserver(IEventObserver<GraphState> observer) => _inner.ConnectEventObserver(observer);

        public IDisposable ConnectEventObserver(Event @event, IEventObserver<GraphState> observer) =>
            _inner.ConnectEventObserver(@event, observer);

        public IDisposable ConnectStateObserver(IStateObserver<GraphState> observer) => _inner.ConnectStateObserver(observer);

        public void Accept(StateMachineVisitor visitor) => _visitedState.Accept(visitor);

        public void Probe(ProbeContext context) => _inner.Probe(context);
    }

    sealed class ForeignState : State
    {
        readonly State _inner;

        public ForeignState(State inner)
        {
            _inner = inner;
        }

        public string Name => _inner.Name;

        public Event Enter => _inner.Enter;

        public Event Leave => _inner.Leave;

        public Event<State> BeforeEnter => _inner.BeforeEnter;

        public Event<State> AfterLeave => _inner.AfterLeave;

        public int CompareTo(State? other) => _inner.CompareTo(other);

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this, _ => { });

        public void Probe(ProbeContext context) => _inner.Probe(context);
    }
}
