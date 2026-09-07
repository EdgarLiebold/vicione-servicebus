using System.Collections.Generic;
using System.Linq;
using QuikGraph;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Visualizer.Internal;

internal static class StateMachineGraphFactory
{
    internal static AdjacencyGraph<Vertex, Edge<Vertex>> Create(StateMachineGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        HashSet<Vertex> compositeTargets = graph.Edges
            .Where(static edge => edge.From.IsComposite)
            .Select(static edge => edge.To)
            .ToHashSet();
        HashSet<Vertex> targets = graph.Edges
            .Select(static edge => edge.To)
            .ToHashSet();
        List<Vertex> vertices = graph.Vertices
            .Where(vertex => targets.Contains(vertex) || vertex.Title == "Initial")
            .ToList();
        List<Edge> edges = graph.Edges
            .Where(edge => vertices.Contains(edge.From) && !compositeTargets.Contains(edge.From))
            .ToList();

        var result = new AdjacencyGraph<Vertex, Edge<Vertex>>();
        result.AddVertexRange(vertices);
        result.AddEdgeRange(edges.Select(static edge => new Edge<Vertex>(edge.From, edge.To)));

        return result;
    }
}
