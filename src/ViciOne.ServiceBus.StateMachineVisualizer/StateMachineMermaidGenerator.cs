using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuikGraph;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Visualizer.Internal;

namespace ViciOne.ServiceBus.Visualizer;

/// <summary>Generates Mermaid flowchart documents from state-machine graphs.</summary>
public sealed class StateMachineMermaidGenerator
{
    const string OpenBracket = "«";
    const string CloseBracket = "»";
    readonly AdjacencyGraph<Vertex, Edge<Vertex>> _graph;

    /// <summary>Creates a generator for a state-machine graph.</summary>
    /// <param name="graph">The state-machine graph to render.</param>
    public StateMachineMermaidGenerator(StateMachineGraph graph)
    {
        _graph = StateMachineGraphFactory.Create(graph);
    }

    /// <summary>Generates a Mermaid flowchart document.</summary>
    /// <returns>The complete Mermaid document.</returns>
    public string Generate()
    {
        StringBuilder output = new();
        List<Vertex> vertices = _graph.Vertices.ToList();

        output.Append("flowchart TB;");

        foreach (Edge<Vertex> edge in _graph.Edges)
        {
            var source = FormatVertex(edge.Source, vertices);
            var target = FormatVertex(edge.Target, vertices);
            var line = $"{Environment.NewLine}    {source} --> {target};";

            output.Append(line);
        }

        return output.ToString();
    }

    static string GetVertexLabel(Vertex vertex, bool includeOptionalType)
    {
        if (includeOptionalType && vertex.TargetType != typeof(Event) && vertex.TargetType != typeof(Exception))
        {
            if (vertex.TargetType.TryGetSingleClosedGenericArguments(typeof(Fault<>), out Type[] arguments))
                return $"{vertex.Title}{OpenBracket}{arguments[0].Name}{CloseBracket}";

            return $"{vertex.Title}{OpenBracket}{vertex.TargetType.Name}{CloseBracket}";
        }

        return vertex.Title;
    }

    static string FormatVertex(Vertex vertex, List<Vertex> vertices)
    {
        var index = vertices.IndexOf(vertex);

        if (vertex.VertexType == typeof(Event))
        {
            var vertexLabel = GetVertexLabel(vertex, true);

            if (vertex.IsComposite)
                return $"{index}[\\\"{vertexLabel}\"/]";

            return $"{index}[\"{vertexLabel}\"]";
        }

        return $"{index}([\"{GetVertexLabel(vertex, false)}\"])";
    }
}
