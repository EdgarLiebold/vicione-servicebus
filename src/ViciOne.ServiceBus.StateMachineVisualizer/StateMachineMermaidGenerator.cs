using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuikGraph;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Visualizer.Abstractions;

namespace ViciOne.ServiceBus.Visualizer;

/// <summary>Generates state machine mermaid values.</summary>
public class StateMachineMermaidGenerator : StateMachineGenerator
{
    const string OpenBracket = "«";
    const string CloseBracket = "»";

    /// <summary>Initializes a new instance.</summary>
    /// <param name="data">The data.</param>
    public StateMachineMermaidGenerator(StateMachineGraph data)
        : base(data)
    {
    }

    /// <summary>Creates mermaid file.</summary>
    /// <returns>The created mermaid file.</returns>
    public string CreateMermaidFile()
    {
        StringBuilder output = new();
        List<Vertex> vertices = Graph.Vertices.ToList();

        output.Append("flowchart TB;");

        foreach (Edge<Vertex> edge in Graph.Edges)
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
