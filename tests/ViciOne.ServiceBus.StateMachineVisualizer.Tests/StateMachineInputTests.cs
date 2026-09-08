using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.StateMachineVisualizer.Tests;

public sealed class StateMachineInputTests
{
    private const string ExpectedGraphviz = """
        digraph G {
        0 [shape=ellipse, label="Initial"];
        1 [shape=ellipse, label="Running"];
        2 [shape=ellipse, label="Failed"];
        3 [shape=ellipse, label="Final"];
        4 [shape=ellipse, label="Suspended"];
        5 [shape=rectangle, label="Initialized"];
        6 [shape=rectangle, label="catch System.InvalidOperationException"];
        7 [shape=rectangle, label="Finished"];
        8 [shape=rectangle, label="Suspend"];
        9 [shape=rectangle, label="Resume"];
        10 [shape=rectangle, label="Resume"];
        11 [shape=rectangle, label="Restart<StateMachineInputTests.RestartData>"];
        0 -> 5;
        1 -> 7;
        1 -> 8;
        1 -> 9;
        2 -> 11;
        4 -> 10;
        5 -> 1;
        5 -> 6;
        6 -> 2;
        7 -> 3;
        8 -> 4;
        10 -> 1;
        11 -> 1;
        }
        """;

    private const string ExpectedMermaid = """
        flowchart TB;
            0(["Initial"]);
            1(["Running"]);
            2(["Failed"]);
            3(["Final"]);
            4(["Suspended"]);
            5["Initialized"];
            6["catch System.Exception"];
            7["Finished"];
            8["Suspend"];
            9["Resume"];
            10["Resume"];
            11["Restart«StateMachineInputTests.RestartData»"];
            0 --> 5;
            1 --> 7;
            1 --> 8;
            1 --> 9;
            2 --> 11;
            4 --> 10;
            5 --> 1;
            5 --> 6;
            6 --> 2;
            7 --> 3;
            8 --> 4;
            10 --> 1;
            11 --> 1;
        """;

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-INPUT", "declarative-state-machine")]
    public void DeclarativeStateMachineGraph_RendersStatesEventsAndEdges()
    {
        var machine = new DeclarativeMachine();

        StateMachineGraph graph = machine.GetGraph();
        string output = new StateMachineGraphvizGenerator(graph).Generate();

        Assert.Equal(StateMachineGraphFixtures.PlatformLines(ExpectedGraphviz), output);
        StateMachineGraphNode exception = Assert.Single(
            graph.Nodes,
            node => node.Name == nameof(InvalidOperationException));
        Assert.Equal(StateMachineGraphNodeKind.Exception, exception.Kind);
        Assert.Null(exception.MessageType);
        Assert.Equal(typeof(InvalidOperationException), exception.ExceptionType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-INPUT", "dynamic-state-machine")]
    public void DynamicStateMachineGraph_RendersStatesEventsAndEdges()
    {
        StateMachine<Instance> machine = ViciOneServiceBusStateMachine<Instance>.New(builder => builder
            .State("Running", out State running)
            .State("Suspended", out State suspended)
            .State("Failed", out State failed)
            .Event("Initialized", out Event initialized)
            .Event("Suspend", out Event suspend)
            .Event("Resume", out Event resume)
            .Event("Finished", out Event finished)
            .Event("Restart", out Event<RestartData> restart)
            .During(builder.Initial)
            .When(initialized, binder => binder
                .TransitionTo(running)
                .Catch<Exception>(handler => handler.TransitionTo(failed)))
            .During(running)
            .When(finished, binder => binder.Finalize())
            .When(suspend, binder => binder.TransitionTo(suspended))
            .Ignore(resume)
            .During(suspended)
            .When(resume, binder => binder.TransitionTo(running))
            .During(failed)
            .When(restart, context => context.Message.Name != null, binder => binder.TransitionTo(running)));

        string output = new StateMachineMermaidGenerator(machine.GetGraph()).Generate();

        Assert.Equal(StateMachineGraphFixtures.PlatformLines(ExpectedMermaid), output);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-INPUT", "request-derived-nodes")]
    public void RequestStateMachineGraph_RendersEveryRequestOutcome()
    {
        var machine = new RequestMachine();

        StateMachineGraph graph = machine.GetGraph();
        StateMachineGraphNode pending = Assert.Single(graph.Nodes, node => node.Name == machine.Process.Pending.Name);
        StateMachineGraphNode completed = Assert.Single(graph.Nodes, node => node.Name == machine.Completed.Name);
        StateMachineGraphNode failed = Assert.Single(graph.Nodes, node => node.Name == machine.Failed.Name);
        StateMachineGraphNode timedOut = Assert.Single(graph.Nodes, node => node.Name == machine.TimedOut.Name);
        AssertRequestOutcome(graph, pending, machine.Process.Completed.Name, completed);
        AssertRequestOutcome(graph, pending, machine.Process.Faulted.Name, failed);
        AssertRequestOutcome(graph, pending, machine.Process.TimeoutExpired.Name, timedOut);

        string output = new StateMachineGraphvizGenerator(graph).Generate();
        AssertGraphvizContainsEveryEdge(graph, output);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-INPUT", "unbound-declarations")]
    public void UnboundDeclarations_RemainPresentAsDisconnectedNodes()
    {
        var machine = new UnboundDeclarationsMachine();

        StateMachineGraph graph = machine.GetGraph();

        StateMachineGraphNode dormant = Assert.Single(graph.Nodes, node => node.Name == machine.Dormant.Name);
        StateMachineGraphNode wake = Assert.Single(graph.Nodes, node => node.Name == machine.Wake.Name);
        Assert.Equal(StateMachineGraphNodeKind.State, dormant.Kind);
        Assert.Equal(StateMachineGraphNodeKind.Event, wake.Kind);
        Assert.DoesNotContain(graph.Edges, edge => edge.Source.Equals(dormant) || edge.Target.Equals(dormant));
        Assert.DoesNotContain(graph.Edges, edge => edge.Source.Equals(wake) || edge.Target.Equals(wake));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-INPUT", "composite-machine-projection")]
    public void CompositeStateMachine_ProjectsItsContributorsTargetAndOrdinaryTransition()
    {
        var machine = new CompositeMachine();

        StateMachineGraph graph = machine.GetGraph();

        StateMachineGraphNode allReceived = Assert.Single(
            graph.Nodes,
            node => node.Name == machine.AllReceived.Name
                && graph.Edges.Any(edge =>
                    ReferenceEquals(edge.Source, node)
                    && edge.Kind == StateMachineGraphEdgeKind.StateTransition
                    && edge.Target.Name == machine.Completed.Name));
        Assert.Equal(StateMachineGraphNodeKind.Event, allReceived.Kind);
        Assert.True(allReceived.IsCompositeEvent);
        Assert.Contains(graph.Edges, edge =>
            edge.Source.Name == machine.First.Name
            && ReferenceEquals(edge.Target, allReceived)
            && edge.Kind == StateMachineGraphEdgeKind.CompositeContribution);
        Assert.Contains(graph.Edges, edge =>
            edge.Source.Name == machine.Second.Name
            && ReferenceEquals(edge.Target, allReceived)
            && edge.Kind == StateMachineGraphEdgeKind.CompositeContribution);
        Assert.Contains(graph.Edges, edge =>
            ReferenceEquals(edge.Source, allReceived)
            && edge.Target.Name == machine.Completed.Name
            && edge.Kind == StateMachineGraphEdgeKind.StateTransition);
        Assert.Contains(graph.Edges, edge =>
            edge.Source.Name == machine.Completed.Name
            && edge.Target.Name == machine.Restart.Name
            && edge.Kind == StateMachineGraphEdgeKind.EventBinding);
        Assert.Contains(graph.Edges, edge =>
            edge.Source.Name == machine.Restart.Name
            && edge.Target.Name == machine.Waiting.Name
            && edge.Kind == StateMachineGraphEdgeKind.StateTransition);

        string graphviz = new StateMachineGraphvizGenerator(graph).Generate();
        AssertGraphvizContainsEveryEdge(graph, graphviz);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-INPUT", "exception-event-name-collision")]
    public void ExceptionBranch_RemainsDistinctFromAnEventWithTheSameNameAndType()
    {
        var machine = new ExceptionNameCollisionMachine();

        StateMachineGraph graph = machine.GetGraph();

        StateMachineGraphNode[] sameNamedNodes = graph.Nodes
            .Where(node => node.Name == nameof(CollisionException))
            .ToArray();
        Assert.Equal(2, sameNamedNodes.Length);
        Assert.Contains(sameNamedNodes, node =>
            node.Kind == StateMachineGraphNodeKind.Event
            && node.MessageType == typeof(CollisionException)
            && node.ExceptionType is null);
        Assert.Contains(sameNamedNodes, node =>
            node.Kind == StateMachineGraphNodeKind.Exception
            && node.MessageType is null
            && node.ExceptionType == typeof(CollisionException));

        string graphviz = new StateMachineGraphvizGenerator(graph).Generate();
        string mermaid = new StateMachineMermaidGenerator(graph).Generate();
        Assert.Contains("CollisionException<StateMachineInputTests.CollisionException>", graphviz, StringComparison.Ordinal);
        Assert.Contains("catch ViciOne.ServiceBus.StateMachineVisualizer.Tests.StateMachineInputTests.CollisionException", graphviz, StringComparison.Ordinal);
        Assert.Contains("CollisionException«StateMachineInputTests.CollisionException»", mermaid, StringComparison.Ordinal);
        Assert.Contains("catch ViciOne.ServiceBus.StateMachineVisualizer.Tests.StateMachineInputTests.CollisionException", mermaid, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-INPUT", "binding-local-event-identity")]
    public void ReusedEvent_KeepsEachStateBindingAndItsOwnOutcome()
    {
        var machine = new BindingIdentityMachine();

        StateMachineGraph graph = machine.GetGraph();

        StateMachineGraphNode[] sharedBindings = graph.Nodes.Where(node => node.Name == machine.Shared.Name).ToArray();
        Assert.Equal(2, sharedBindings.Length);
        StateMachineGraphNode transitionBinding = Assert.Single(
            sharedBindings,
            node => graph.Edges.Any(edge => ReferenceEquals(edge.Source, node) && edge.Target.Name == machine.Second.Name));
        StateMachineGraphNode actionBinding = Assert.Single(sharedBindings, node => !ReferenceEquals(node, transitionBinding));
        Assert.Contains(graph.Edges, edge => edge.Source.Name == machine.First.Name && ReferenceEquals(edge.Target, transitionBinding));
        Assert.Contains(graph.Edges, edge => edge.Source.Name == machine.Third.Name && ReferenceEquals(edge.Target, actionBinding));
        Assert.DoesNotContain(graph.Edges, edge => ReferenceEquals(edge.Source, actionBinding));

        StateMachineGraphNode ignoredBinding = Assert.Single(graph.Nodes, node => node.Name == machine.Ignored.Name);
        Assert.Contains(graph.Edges, edge => edge.Source.Name == machine.First.Name && ReferenceEquals(edge.Target, ignoredBinding));
        Assert.DoesNotContain(graph.Edges, edge => ReferenceEquals(edge.Source, ignoredBinding));

        AssertBothRenderersPreserveGraphIdentity(graph);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-INPUT", "binding-local-exception-identity")]
    public void ReusedExceptionType_KeepsEachCatchSourceAndTarget()
    {
        var machine = new ReusedCatchMachine();

        StateMachineGraph graph = machine.GetGraph();

        StateMachineGraphNode[] exceptionBindings = graph.Nodes
            .Where(node => node.ExceptionType == typeof(CollisionException))
            .ToArray();
        Assert.Equal(2, exceptionBindings.Length);
        Assert.Contains(exceptionBindings, node =>
            graph.Edges.Any(edge => edge.Source.Name == machine.Start.Name && ReferenceEquals(edge.Target, node))
            && graph.Edges.Any(edge => ReferenceEquals(edge.Source, node) && edge.Target.Name == machine.Failed.Name));
        Assert.Contains(exceptionBindings, node =>
            graph.Edges.Any(edge => edge.Source.Name == machine.Retry.Name && ReferenceEquals(edge.Target, node))
            && graph.Edges.Any(edge => ReferenceEquals(edge.Source, node) && edge.Target.Name == machine.Suspended.Name));

        AssertBothRenderersPreserveGraphIdentity(graph);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-INPUT", "substate-inheritance")]
    public void SubstateGraph_SeparatesDeclaredAndInheritedEventsAndRendersInheritance()
    {
        var machine = new SubstateMachine();
        var parentState = Assert.IsAssignableFrom<State<Instance>>(machine.Parent);
        var childState = Assert.IsAssignableFrom<State<Instance>>(machine.Child);

        Assert.Contains(machine.Advance, parentState.DeclaredEvents);
        Assert.DoesNotContain(machine.Advance, childState.DeclaredEvents);
        Assert.Contains(machine.Advance, childState.Events);

        StateMachineGraph graph = machine.GetGraph();
        StateMachineGraphNode parent = Assert.Single(graph.Nodes, node => node.Name == machine.Parent.Name);
        StateMachineGraphNode child = Assert.Single(graph.Nodes, node => node.Name == machine.Child.Name);
        StateMachineGraphNode target = Assert.Single(graph.Nodes, node => node.Name == machine.Target.Name);
        StateMachineGraphNode advance = Assert.Single(graph.Nodes, node => node.Name == machine.Advance.Name);
        Assert.Contains(graph.Edges, edge =>
            ReferenceEquals(edge.Source, parent)
            && ReferenceEquals(edge.Target, advance)
            && edge.Kind == StateMachineGraphEdgeKind.EventBinding);
        Assert.Contains(graph.Edges, edge =>
            ReferenceEquals(edge.Source, advance)
            && ReferenceEquals(edge.Target, target)
            && edge.Kind == StateMachineGraphEdgeKind.StateTransition);
        Assert.Contains(graph.Edges, edge =>
            ReferenceEquals(edge.Source, child)
            && ReferenceEquals(edge.Target, parent)
            && edge.Kind == StateMachineGraphEdgeKind.StateInheritance);
        Assert.DoesNotContain(graph.Edges, edge =>
            ReferenceEquals(edge.Source, child)
            && ReferenceEquals(edge.Target, advance));

        int childIndex = IndexOf(graph, child);
        int parentIndex = IndexOf(graph, parent);
        string graphviz = new StateMachineGraphvizGenerator(graph).Generate();
        string mermaid = new StateMachineMermaidGenerator(graph).Generate();
        Assert.Contains($"{childIndex} -> {parentIndex} [", graphviz, StringComparison.Ordinal);
        Assert.Contains("label=\"inherits\"", graphviz, StringComparison.Ordinal);
        Assert.Contains("style=dashed", graphviz, StringComparison.Ordinal);
        Assert.Contains($"    {childIndex} -. inherits .-> {parentIndex};", mermaid, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-INPUT", "lifecycle-event-bindings")]
    public void LifecycleHooks_AreBoundToTheirStateAndKeepActionOnlyHooksVisible()
    {
        var machine = new LifecycleHookMachine();
        var runningState = Assert.IsAssignableFrom<State<Instance>>(machine.Running);

        Assert.Contains(machine.Running.Enter, runningState.DeclaredEvents);
        Assert.Contains(machine.Running.Leave, runningState.DeclaredEvents);

        StateMachineGraph graph = machine.GetGraph();
        StateMachineGraphNode running = Assert.Single(graph.Nodes, node => node.Name == machine.Running.Name);
        StateMachineGraphNode completed = Assert.Single(graph.Nodes, node => node.Name == machine.Completed.Name);
        StateMachineGraphNode enter = Assert.Single(graph.Nodes, node => node.Name == machine.Running.Enter.Name);
        StateMachineGraphNode leave = Assert.Single(graph.Nodes, node => node.Name == machine.Running.Leave.Name);
        Assert.Contains(graph.Edges, edge =>
            ReferenceEquals(edge.Source, running)
            && ReferenceEquals(edge.Target, enter)
            && edge.Kind == StateMachineGraphEdgeKind.EventBinding);
        Assert.Contains(graph.Edges, edge =>
            ReferenceEquals(edge.Source, enter)
            && ReferenceEquals(edge.Target, completed)
            && edge.Kind == StateMachineGraphEdgeKind.StateTransition);
        Assert.Contains(graph.Edges, edge =>
            ReferenceEquals(edge.Source, running)
            && ReferenceEquals(edge.Target, leave)
            && edge.Kind == StateMachineGraphEdgeKind.EventBinding);
        Assert.DoesNotContain(graph.Edges, edge => ReferenceEquals(edge.Source, leave));

        AssertBothRenderersPreserveGraphIdentity(graph);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-INPUT", "nested-exception-branches")]
    public void NestedCatch_ProjectsEachExceptionBranchAndItsOutcome()
    {
        var machine = new NestedCatchMachine();

        StateMachineGraph graph = machine.GetGraph();
        StateMachineGraphNode start = Assert.Single(graph.Nodes, node => node.Name == machine.Start.Name);
        StateMachineGraphNode outer = Assert.Single(
            graph.Nodes,
            node => node.ExceptionType == typeof(InvalidOperationException));
        StateMachineGraphNode inner = Assert.Single(
            graph.Nodes,
            node => node.ExceptionType == typeof(ArgumentException));
        StateMachineGraphNode failed = Assert.Single(graph.Nodes, node => node.Name == machine.Failed.Name);
        StateMachineGraphNode composite = Assert.Single(
            graph.Nodes,
            node => node.Name == machine.Composite.Name
                && graph.Edges.Any(edge =>
                    ReferenceEquals(edge.Source, outer)
                    && ReferenceEquals(edge.Target, node)
                    && edge.Kind == StateMachineGraphEdgeKind.CompositeContribution));
        Assert.Contains(graph.Edges, edge =>
            ReferenceEquals(edge.Source, start)
            && ReferenceEquals(edge.Target, outer)
            && edge.Kind == StateMachineGraphEdgeKind.ExceptionHandler);
        Assert.Contains(graph.Edges, edge =>
            ReferenceEquals(edge.Source, outer)
            && ReferenceEquals(edge.Target, inner)
            && edge.Kind == StateMachineGraphEdgeKind.ExceptionHandler);
        Assert.Contains(graph.Edges, edge =>
            ReferenceEquals(edge.Source, inner)
            && ReferenceEquals(edge.Target, failed)
            && edge.Kind == StateMachineGraphEdgeKind.StateTransition);
        Assert.True(composite.IsCompositeEvent);

        AssertBothRenderersPreserveGraphIdentity(graph);
    }

    private sealed class DeclarativeMachine : ViciOneServiceBusStateMachine<Instance>
    {
        public DeclarativeMachine()
        {
            During(
                Initial,
                When(Initialized)
                    .TransitionTo(Running)
                    .Catch<InvalidOperationException>(handler => handler.TransitionTo(Failed)));
            During(
                Running,
                When(Finished).Finalize(),
                When(Suspend).TransitionTo(Suspended),
                Ignore(Resume));
            During(Suspended, When(Resume).TransitionTo(Running));
            During(Failed, When(Restart, context => context.Message.Name != null).TransitionTo(Running));
        }

        public State Running { get; private set; } = null!;

        public State Suspended { get; private set; } = null!;

        public State Failed { get; private set; } = null!;

        public Event Initialized { get; private set; } = null!;

        public Event Suspend { get; private set; } = null!;

        public Event Resume { get; private set; } = null!;

        public Event Finished { get; private set; } = null!;

        public Event<RestartData> Restart { get; private set; } = null!;
    }

    private sealed class Instance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public State CurrentState { get; set; } = null!;

        public int CompositeStatus { get; set; }
    }

    private sealed class UnboundDeclarationsMachine : ViciOneServiceBusStateMachine<Instance>
    {
        public State Dormant { get; private set; } = null!;

        public Event Wake { get; private set; } = null!;
    }

    private sealed class CompositeMachine : ViciOneServiceBusStateMachine<CompositeInstance>
    {
        public CompositeMachine()
        {
            InstanceState(instance => instance.CurrentState);
            CompositeEvent(() => AllReceived, instance => instance.CompositeStatus, First, Second);
            Initially(When(Start).TransitionTo(Waiting));
            During(
                Waiting,
                When(First),
                When(Second),
                When(AllReceived).TransitionTo(Completed));
            During(Completed, When(Restart).TransitionTo(Waiting));
        }

        public State Waiting { get; private set; } = null!;

        public State Completed { get; private set; } = null!;

        public Event Start { get; private set; } = null!;

        public Event First { get; private set; } = null!;

        public Event Second { get; private set; } = null!;

        public Event AllReceived { get; private set; } = null!;

        public Event Restart { get; private set; } = null!;
    }

    private sealed class ExceptionNameCollisionMachine : ViciOneServiceBusStateMachine<Instance>
    {
        public ExceptionNameCollisionMachine()
        {
            During(
                Initial,
                When(Initialized)
                    .Catch<CollisionException>(handler => handler.TransitionTo(Failed)));
        }

        public State Failed { get; private set; } = null!;

        public Event Initialized { get; private set; } = null!;

        public Event<CollisionException> CollisionException { get; private set; } = null!;
    }

    private sealed class BindingIdentityMachine : ViciOneServiceBusStateMachine<BindingInstance>
    {
        public BindingIdentityMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(When(Begin).TransitionTo(First));
            During(
                First,
                When(Shared).TransitionTo(Second),
                Ignore(Ignored));
            During(Third, When(Shared).Then(context => context.Saga.Handled = true));
        }

        public State First { get; private set; } = null!;

        public State Second { get; private set; } = null!;

        public State Third { get; private set; } = null!;

        public Event Begin { get; private set; } = null!;

        public Event Shared { get; private set; } = null!;

        public Event Ignored { get; private set; } = null!;
    }

    private sealed class ReusedCatchMachine : ViciOneServiceBusStateMachine<Instance>
    {
        public ReusedCatchMachine()
        {
            During(
                Initial,
                When(Start).Catch<CollisionException>(handler => handler.TransitionTo(Failed)));
            During(
                Failed,
                When(Retry).Catch<CollisionException>(handler => handler.TransitionTo(Suspended)));
        }

        public State Failed { get; private set; } = null!;

        public State Suspended { get; private set; } = null!;

        public Event Start { get; private set; } = null!;

        public Event Retry { get; private set; } = null!;
    }

    private sealed class SubstateMachine : ViciOneServiceBusStateMachine<Instance>
    {
        public SubstateMachine()
        {
            SubState(() => Child, Parent);
            During(Parent, When(Advance).TransitionTo(Target));
        }

        public State Parent { get; private set; } = null!;

        public State Child { get; private set; } = null!;

        public State Target { get; private set; } = null!;

        public Event Advance { get; private set; } = null!;
    }

    private sealed class LifecycleHookMachine : ViciOneServiceBusStateMachine<Instance>
    {
        public LifecycleHookMachine()
        {
            Initially(When(Start).TransitionTo(Running));
            WhenEnter(Running, activity => activity.TransitionTo(Completed));
            WhenLeave(Running, activity => activity.Then(_ => { }));
        }

        public State Running { get; private set; } = null!;

        public State Completed { get; private set; } = null!;

        public Event Start { get; private set; } = null!;
    }

    private sealed class NestedCatchMachine : ViciOneServiceBusStateMachine<Instance>
    {
        public NestedCatchMachine()
        {
            CompositeEvent(() => Composite, instance => instance.CompositeStatus, Contributor);
            var accessor = new IntCompositeEventStatusAccessor<Instance>(
                typeof(Instance).GetProperty(nameof(Instance.CompositeStatus))!);
            During(
                Initial,
                When(Start).Catch<InvalidOperationException>(outer => outer
                    .Catch<ArgumentException>(inner => inner.TransitionTo(Failed))
                    .Add(new CompositeEventActivity<Instance>(
                        accessor,
                        1,
                        new CompositeEventStatus(1),
                        Composite,
                        CompositeEventOptions.None))));
        }

        public State Failed { get; private set; } = null!;

        public Event Start { get; private set; } = null!;

        public Event Contributor { get; private set; } = null!;

        public Event Composite { get; private set; } = null!;
    }

    private sealed class CompositeInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public State CurrentState { get; set; } = null!;

        public int CompositeStatus { get; set; }
    }

    private sealed class BindingInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public State CurrentState { get; set; } = null!;

        public bool Handled { get; set; }
    }

    public sealed class CollisionException : Exception
    {
    }

    private sealed class RequestMachine : ViciOneServiceBusStateMachine<RequestInstance>
    {
        public RequestMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Request(() => Process, instance => instance.RequestId);

            Initially(
                When(Start)
                    .Request(Process, context => context.InitAsync<RequestMessage>(new { context.Message.Id }))
                    .TransitionTo(Process.Pending));
            During(
                Process.Pending,
                When(Process.Completed).TransitionTo(Completed),
                When(Process.Faulted).TransitionTo(Failed),
                When(Process.TimeoutExpired).TransitionTo(TimedOut));
        }

        public State Completed { get; private set; } = null!;

        public State Failed { get; private set; } = null!;

        public State TimedOut { get; private set; } = null!;

        public Event<StartRequest> Start { get; private set; } = null!;

        public Request<RequestInstance, RequestMessage, RequestResponse> Process { get; private set; } = null!;
    }

    private sealed class RequestInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public Guid? RequestId { get; set; }
    }

    public sealed class StartRequest
    {
        public Guid Id { get; set; }
    }

    public sealed class RequestMessage
    {
        public Guid Id { get; set; }
    }

    public sealed class RequestResponse;

    public sealed class RestartData
    {
        public string? Name { get; set; }
    }

    static void AssertGraphvizContainsEveryEdge(StateMachineGraph graph, string output)
    {
        foreach (StateMachineGraphEdge edge in graph.Edges)
        {
            int sourceIndex = IndexOf(graph, edge.Source);
            int targetIndex = IndexOf(graph, edge.Target);
            string renderedEdge = edge.Kind == StateMachineGraphEdgeKind.StateInheritance
                ? $"{sourceIndex} -> {targetIndex} ["
                : $"{sourceIndex} -> {targetIndex};";
            Assert.Contains(renderedEdge, output, StringComparison.Ordinal);
        }
    }

    static void AssertBothRenderersPreserveGraphIdentity(StateMachineGraph graph)
    {
        string graphviz = new StateMachineGraphvizGenerator(graph).Generate();
        string mermaid = new StateMachineMermaidGenerator(graph).Generate();

        Assert.Equal(
            graph.Nodes.Count,
            graphviz.Split(Environment.NewLine).Count(line => line.Contains(" [shape=", StringComparison.Ordinal)));
        Assert.Equal(1 + graph.Nodes.Count + graph.Edges.Count, mermaid.Split(Environment.NewLine).Length);
        AssertGraphvizContainsEveryEdge(graph, graphviz);

        foreach (StateMachineGraphEdge edge in graph.Edges)
        {
            int sourceIndex = IndexOf(graph, edge.Source);
            int targetIndex = IndexOf(graph, edge.Target);
            string renderedEdge = edge.Kind == StateMachineGraphEdgeKind.StateInheritance
                ? $"    {sourceIndex} -. inherits .-> {targetIndex};"
                : $"    {sourceIndex} --> {targetIndex};";
            Assert.Contains(renderedEdge, mermaid, StringComparison.Ordinal);
        }
    }

    static void AssertRequestOutcome(
        StateMachineGraph graph,
        StateMachineGraphNode pending,
        string outcomeName,
        StateMachineGraphNode target)
    {
        StateMachineGraphNode outcome = Assert.Single(
            graph.Nodes,
            node => node.Name == outcomeName
                && graph.Edges.Any(edge =>
                    ReferenceEquals(edge.Source, pending)
                    && ReferenceEquals(edge.Target, node)
                    && edge.Kind == StateMachineGraphEdgeKind.EventBinding));
        Assert.Contains(graph.Edges, edge =>
            ReferenceEquals(edge.Source, outcome)
            && ReferenceEquals(edge.Target, target)
            && edge.Kind == StateMachineGraphEdgeKind.StateTransition);
    }

    static int IndexOf(StateMachineGraph graph, StateMachineGraphNode node) => Enumerable.Range(0, graph.Nodes.Count)
        .Single(index => ReferenceEquals(graph.Nodes[index], node));
}
