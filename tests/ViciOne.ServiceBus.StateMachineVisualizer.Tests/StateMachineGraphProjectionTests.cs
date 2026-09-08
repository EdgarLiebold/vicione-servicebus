using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.StateMachineVisualizer.Tests;

public sealed class StateMachineGraphProjectionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-GRAPH-PROJECTION", "composite-relationships")]
    public void CompositeEventProjection_PreservesContributorAndOrdinaryRelationships()
    {
        StateMachineGraph compositeGraph = StateMachineGraphFixtures.Composite(true);
        StateMachineGraph ordinaryGraph = StateMachineGraphFixtures.Composite(false);
        string composite = new StateMachineGraphvizGenerator(compositeGraph).Generate();
        string ordinary = new StateMachineGraphvizGenerator(ordinaryGraph).Generate();
        string compositeMermaid = new StateMachineMermaidGenerator(compositeGraph).Generate();

        Assert.Contains("5 [shape=invhouse, label=\"AllReceived\"]", composite, StringComparison.Ordinal);
        Assert.Contains("2 -> 4;", composite, StringComparison.Ordinal);
        Assert.Contains("2 -> 6;", composite, StringComparison.Ordinal);
        Assert.Contains("2 -> 7;", composite, StringComparison.Ordinal);
        Assert.Contains("7 -> 1;", composite, StringComparison.Ordinal);
        Assert.Contains("    2 --> 4;", compositeMermaid, StringComparison.Ordinal);
        Assert.Contains("    2 --> 6;", compositeMermaid, StringComparison.Ordinal);
        Assert.Contains("    2 --> 7;", compositeMermaid, StringComparison.Ordinal);
        Assert.Contains("    7 --> 1;", compositeMermaid, StringComparison.Ordinal);
        Assert.Contains("    5[\\\"AllReceived\"/];", compositeMermaid, StringComparison.Ordinal);
        Assert.Equal(compositeGraph.Edges.Count, composite.Split(Environment.NewLine).Count(line => line.Contains(" -> ", StringComparison.Ordinal)));
        Assert.Equal(compositeGraph.Edges.Count, compositeMermaid.Split(Environment.NewLine).Count(line => line.Contains(" --> ", StringComparison.Ordinal)));
        Assert.Contains("2 -> 4;", ordinary, StringComparison.Ordinal);
        Assert.Contains("2 -> 6;", ordinary, StringComparison.Ordinal);
        Assert.Contains("2 -> 7;", ordinary, StringComparison.Ordinal);
        Assert.Equal(ordinaryGraph.Edges.Count, ordinary.Split(Environment.NewLine).Count(line => line.Contains(" -> ", StringComparison.Ordinal)));
        Assert.NotEqual(ordinary, composite);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-EMPTY", "valid-empty-documents")]
    public void EmptyGraph_ProducesValidEmptyDocuments()
    {
        var graph = new StateMachineGraph([], []);

        string graphviz = new StateMachineGraphvizGenerator(graph).Generate();
        string mermaid = new StateMachineMermaidGenerator(graph).Generate();

        Assert.Equal(StateMachineGraphFixtures.PlatformLines("digraph G {\n}"), graphviz);
        Assert.Equal("flowchart TB;", mermaid);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-GRAPH-PROJECTION", "disconnected-nodes")]
    public void DisconnectedNodes_RemainVisible()
    {
        StateMachineGraph graph = StateMachineGraphFixtures.Disconnected();

        string graphviz = new StateMachineGraphvizGenerator(graph).Generate();
        string mermaid = new StateMachineMermaidGenerator(graph).Generate();

        Assert.Equal(
            StateMachineGraphFixtures.PlatformLines(
                "digraph G {\n0 [shape=ellipse, label=\"Dormant\"];\n1 [shape=rectangle, label=\"Wake\"];\n}"),
            graphviz);
        Assert.Equal(
            StateMachineGraphFixtures.PlatformLines("flowchart TB;\n    0([\"Dormant\"]);\n    1[\"Wake\"];"),
            mermaid);
    }
}
