using System.Collections.Generic;
using System.Linq;
using QuikGraph;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.StateMachineVisualizer.Internal;

internal static class StateMachineGraphFactory
{
    internal static AdjacencyGraph<StateMachineGraphNode, TaggedEdge<StateMachineGraphNode, StateMachineGraphEdgeKind>> Create(
        StateMachineGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        List<StateMachineGraphNode> visibleNodes = [.. graph.Nodes];
        List<StateMachineGraphEdge> visibleEdges = [.. graph.Edges];

        var result = new AdjacencyGraph<StateMachineGraphNode, TaggedEdge<StateMachineGraphNode, StateMachineGraphEdgeKind>>();
        result.AddVertexRange(visibleNodes);
        result.AddEdgeRange(visibleEdges.Select(static edge =>
            new TaggedEdge<StateMachineGraphNode, StateMachineGraphEdgeKind>(edge.Source, edge.Target, edge.Kind)));

        return result;
    }
}
