using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Visualizer;
using Xunit;

namespace ViciOne.ServiceBus.StateMachineVisualizer.Tests;

public sealed class StateMachineGraphFilteringTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-GRAPH-FILTER", "composite-target-outgoing-edges")]
    public void CompositeEventAssignment_RemovesOutgoingEdgesFromItsTargetState()
    {
        string composite = new StateMachineGraphvizGenerator(StateMachineGraphFixtures.Composite(true))
            .Generate();
        string ordinary = new StateMachineGraphvizGenerator(StateMachineGraphFixtures.Composite(false))
            .Generate();

        Assert.Contains("5 [shape=invhouse, label=\"AllReceived\"]", composite, StringComparison.Ordinal);
        Assert.DoesNotContain("2 -> 4;", composite, StringComparison.Ordinal);
        Assert.DoesNotContain("2 -> 6;", composite, StringComparison.Ordinal);
        Assert.Contains("2 -> 4;", ordinary, StringComparison.Ordinal);
        Assert.Contains("2 -> 6;", ordinary, StringComparison.Ordinal);
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
}
