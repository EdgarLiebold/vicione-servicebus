using System.Collections.Generic;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.StateMachineVisualizer.Internal;

internal sealed class StateMachineGraphProjection
{
    readonly IReadOnlyDictionary<StateMachineGraphNode, int> _indexes;

    internal StateMachineGraphProjection(StateMachineGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        Nodes = graph.Nodes;
        Dictionary<StateMachineGraphNode, int> indexes = new(Nodes.Count, ReferenceEqualityComparer.Instance);
        var edgesBySource = new List<StateMachineGraphEdge>?[Nodes.Count];

        for (var index = 0; index < Nodes.Count; index++)
            indexes.Add(Nodes[index], index);

        foreach (StateMachineGraphEdge edge in graph.Edges)
        {
            int sourceIndex = indexes[edge.Source];
            (edgesBySource[sourceIndex] ??= []).Add(edge);
        }

        var edges = new List<StateMachineGraphEdge>(graph.Edges.Count);
        foreach (List<StateMachineGraphEdge>? sourceEdges in edgesBySource)
        {
            if (sourceEdges is not null)
                edges.AddRange(sourceEdges);
        }

        Edges = edges.AsReadOnly();
        _indexes = indexes;
    }

    internal IReadOnlyList<StateMachineGraphEdge> Edges { get; }

    internal IReadOnlyList<StateMachineGraphNode> Nodes { get; }

    internal int IndexOf(StateMachineGraphNode node) => _indexes[node];
}
