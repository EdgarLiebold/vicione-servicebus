using System;
using System.Collections.Generic;
using System.Text;
using QuikGraph;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.StateMachineVisualizer.Internal;

namespace ViciOne.ServiceBus.StateMachineVisualizer;

/// <summary>Renders a state-machine graph as a Mermaid flowchart document.</summary>
/// <remarks>
/// The constructor creates a private rendering snapshot that is not modified afterward. An instance can therefore
/// generate the same document repeatedly and can be shared by concurrent readers. Labels are encoded so state and
/// event names cannot alter the generated Mermaid syntax.
/// </remarks>
public sealed class StateMachineMermaidGenerator
{
    const string OpenBracket = "«";
    const string CloseBracket = "»";
    readonly AdjacencyGraph<StateMachineGraphNode, TaggedEdge<StateMachineGraphNode, StateMachineGraphEdgeKind>> _graph;

    /// <summary>Creates a generator for the supplied state-machine graph.</summary>
    /// <param name="graph">The graph whose nodes and state-machine relationships are rendered.</param>
    /// <exception cref="ArgumentNullException"><paramref name="graph" /> is <see langword="null" />.</exception>
    public StateMachineMermaidGenerator(StateMachineGraph graph)
    {
        _graph = StateMachineGraphFactory.Create(graph);
    }

    /// <summary>Generates the complete Mermaid flowchart document.</summary>
    /// <returns>A Mermaid document containing every node and relationship captured by the constructor.</returns>
    public string Generate()
    {
        StringBuilder output = new();
        List<StateMachineGraphNode> nodes = [.. _graph.Vertices];
        Dictionary<StateMachineGraphNode, int> indexes = new(nodes.Count);

        output.Append("flowchart TB;");

        for (var index = 0; index < nodes.Count; index++)
        {
            StateMachineGraphNode node = nodes[index];
            indexes.Add(node, index);
            output.Append(Environment.NewLine)
                .Append("    ")
                .Append(FormatNode(node, index))
                .Append(';');
        }

        foreach (TaggedEdge<StateMachineGraphNode, StateMachineGraphEdgeKind> edge in _graph.Edges)
        {
            output.Append(Environment.NewLine)
                .Append("    ")
                .Append(FormatEdge(edge, indexes))
                .Append(';');
        }

        return output.ToString();
    }

    static string EscapeLabel(string label)
    {
        StringBuilder escaped = new(label.Length);

        foreach (char character in label)
        {
            switch (character)
            {
                case '&':
                    escaped.Append("#38;");
                    break;
                case '"':
                    escaped.Append("#quot;");
                    break;
                case '#':
                    escaped.Append("#35;");
                    break;
                case '<':
                    escaped.Append("#60;");
                    break;
                case '>':
                    escaped.Append("#62;");
                    break;
                case '\\':
                    escaped.Append("#92;");
                    break;
                case '[':
                    escaped.Append("#91;");
                    break;
                case ']':
                    escaped.Append("#93;");
                    break;
                case '`':
                    escaped.Append("#96;");
                    break;
                case '\r':
                    escaped.Append("#13;");
                    break;
                case '\n':
                    escaped.Append("#10;");
                    break;
                default:
                    escaped.Append(character);
                    break;
            }
        }

        return escaped.ToString();
    }

    static string FormatNode(StateMachineGraphNode node, int index)
    {
        if (node.Kind != StateMachineGraphNodeKind.State)
        {
            string nodeLabel = EscapeLabel(StateMachineNodeLabelFormatter.Format(node, OpenBracket, CloseBracket));

            if (node.IsCompositeEvent)
                return $"{index}[\\\"{nodeLabel}\"/]";

            return $"{index}[\"{nodeLabel}\"]";
        }

        return $"{index}([\"{EscapeLabel(node.Name)}\"])";
    }

    static string FormatEdge(
        TaggedEdge<StateMachineGraphNode, StateMachineGraphEdgeKind> edge,
        IReadOnlyDictionary<StateMachineGraphNode, int> indexes) => edge.Tag == StateMachineGraphEdgeKind.StateInheritance
        ? $"{indexes[edge.Source]} -. inherits .-> {indexes[edge.Target]}"
        : $"{indexes[edge.Source]} --> {indexes[edge.Target]}";
}
