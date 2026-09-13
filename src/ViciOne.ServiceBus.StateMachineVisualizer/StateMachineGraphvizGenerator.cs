using System;
using System.Globalization;
using System.Text;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.StateMachineVisualizer.Internal;

namespace ViciOne.ServiceBus.StateMachineVisualizer;

/// <summary>Renders a state-machine graph as a Graphviz DOT document.</summary>
/// <remarks>
/// The constructor captures a private rendering projection that is not modified afterward. An instance can therefore
/// generate the same document repeatedly and can be shared by concurrent readers. Labels are escaped so state and
/// event names cannot alter the generated DOT syntax.
/// </remarks>
public sealed class StateMachineGraphvizGenerator
{
    readonly StateMachineGraphProjection _graph;

    /// <summary>Creates a generator for the supplied state-machine graph.</summary>
    /// <param name="graph">The graph whose nodes and state-machine relationships are rendered.</param>
    /// <exception cref="ArgumentNullException"><paramref name="graph" /> is <see langword="null" />.</exception>
    public StateMachineGraphvizGenerator(StateMachineGraph graph)
    {
        _graph = new StateMachineGraphProjection(graph);
    }

    /// <summary>Generates the complete Graphviz DOT document.</summary>
    /// <returns>A DOT document containing every node and relationship, using LF line endings.</returns>
    public string Generate()
    {
        StringBuilder output = new();
        output.Append("digraph G {");
        for (var index = 0; index < _graph.Nodes.Count; index++)
        {
            StateMachineGraphNode node = _graph.Nodes[index];
            AppendNode(output, node, index);
        }

        foreach (StateMachineGraphEdge edge in _graph.Edges)
            AppendEdge(output, edge);

        return output.Append('\n').Append('}').ToString();
    }

    void AppendEdge(StringBuilder output, StateMachineGraphEdge edge)
    {
        output.Append('\n')
            .Append(InvariantIndex(_graph.IndexOf(edge.Source)))
            .Append(" -> ")
            .Append(InvariantIndex(_graph.IndexOf(edge.Target)));

        if (edge.Kind == StateMachineGraphEdgeKind.StateInheritance)
            output.Append(" [style=dashed, label=\"inherits\"]");

        output.Append(';');
    }

    static void AppendEscapedLabel(StringBuilder output, string label)
    {
        for (var index = 0; index < label.Length; index++)
        {
            char character = label[index];
            switch (character)
            {
                case '\\':
                    output.Append(@"\\");
                    break;
                case '"':
                    output.Append("\\\"");
                    break;
                case '\n':
                    output.Append(@"\n");
                    break;
                case '\r':
                    output.Append(@"\n");
                    if (index + 1 < label.Length && label[index + 1] == '\n')
                        index++;
                    break;
                default:
                    if (char.IsHighSurrogate(character)
                        && index + 1 < label.Length
                        && char.IsLowSurrogate(label[index + 1]))
                    {
                        output.Append(character).Append(label[++index]);
                    }
                    else if (char.IsControl(character) || char.IsSurrogate(character))
                    {
                        output.Append(@"\\u")
                            .Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        output.Append(character);
                    }

                    break;
            }
        }
    }

    static void AppendNode(StringBuilder output, StateMachineGraphNode node, int index)
    {
        string shape = node.Kind == StateMachineGraphNodeKind.State
            ? "ellipse"
            : node.IsCompositeEvent
                ? "invhouse"
                : "rectangle";
        string label = node.Kind == StateMachineGraphNodeKind.State
            ? node.Name
            : StateMachineNodeLabelFormatter.Format(node, "<", ">");

        output.Append('\n')
            .Append(InvariantIndex(index))
            .Append(" [shape=")
            .Append(shape)
            .Append(", label=\"");
        AppendEscapedLabel(output, label);
        output.Append("\"];");
    }

    static string InvariantIndex(int index) => index.ToString(CultureInfo.InvariantCulture);
}
