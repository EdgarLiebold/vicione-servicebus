using System;
using System.Globalization;
using System.Text;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.StateMachineVisualizer.Internal;

namespace ViciOne.ServiceBus.StateMachineVisualizer;

/// <summary>Renders a state-machine graph as a Mermaid flowchart document.</summary>
/// <remarks>
/// The constructor captures a private rendering projection that is not modified afterward. An instance can therefore
/// generate the same document repeatedly and can be shared by concurrent readers. Labels are encoded so state and
/// event names cannot alter the generated Mermaid syntax.
/// </remarks>
public sealed class StateMachineMermaidGenerator
{
    const string OpenBracket = "«";
    const string CloseBracket = "»";
    readonly StateMachineGraphProjection _graph;

    /// <summary>Creates a generator for the supplied state-machine graph.</summary>
    /// <param name="graph">The graph whose nodes and state-machine relationships are rendered.</param>
    /// <exception cref="ArgumentNullException"><paramref name="graph" /> is <see langword="null" />.</exception>
    public StateMachineMermaidGenerator(StateMachineGraph graph)
    {
        _graph = new StateMachineGraphProjection(graph);
    }

    /// <summary>Generates the complete Mermaid flowchart document.</summary>
    /// <returns>A Mermaid document containing every node and relationship, using LF line endings.</returns>
    public string Generate()
    {
        StringBuilder output = new();
        output.Append("flowchart TB;");

        for (var index = 0; index < _graph.Nodes.Count; index++)
        {
            StateMachineGraphNode node = _graph.Nodes[index];
            output.Append('\n')
                .Append("    ")
                .Append(FormatNode(node, index))
                .Append(';');
        }

        foreach (StateMachineGraphEdge edge in _graph.Edges)
        {
            output.Append('\n')
                .Append("    ")
                .Append(FormatEdge(edge))
                .Append(';');
        }

        return output.ToString();
    }

    static string EscapeLabel(string label)
    {
        StringBuilder escaped = new(label.Length);

        for (var index = 0; index < label.Length; index++)
        {
            string? entity = SyntaxEntity(label[index]);
            if (entity is not null)
                escaped.Append(entity);
            else
                AppendUnicodeCharacter(escaped, label, ref index);
        }

        return escaped.ToString();
    }

    static string? SyntaxEntity(char character) => character switch
    {
        '&' => "#38;",
        '"' => "#quot;",
        '#' => "#35;",
        '<' => "#60;",
        '>' => "#62;",
        '\\' => "#92;",
        '[' => "#91;",
        ']' => "#93;",
        '`' => "#96;",
        '\r' => "#13;",
        '\n' => "#10;",
        _ => null
    };

    static void AppendUnicodeCharacter(StringBuilder escaped, string label, ref int index)
    {
        char character = label[index];
        if (char.IsHighSurrogate(character)
            && index + 1 < label.Length
            && char.IsLowSurrogate(label[index + 1]))
        {
            escaped.Append(character).Append(label[++index]);
        }
        else if (char.IsControl(character))
        {
            escaped.Append('#')
                .Append(((int)character).ToString(CultureInfo.InvariantCulture))
                .Append(';');
        }
        else if (char.IsSurrogate(character))
        {
            escaped.Append("#92;u")
                .Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
        }
        else
        {
            escaped.Append(character);
        }
    }

    static string FormatNode(StateMachineGraphNode node, int index)
    {
        string nodeId = InvariantIndex(index);
        if (node.Kind != StateMachineGraphNodeKind.State)
        {
            string nodeLabel = EscapeLabel(StateMachineNodeLabelFormatter.Format(node, OpenBracket, CloseBracket));

            if (node.IsCompositeEvent)
                return $"{nodeId}[\\\"{nodeLabel}\"/]";

            return $"{nodeId}[\"{nodeLabel}\"]";
        }

        return $"{nodeId}([\"{EscapeLabel(node.Name)}\"])";
    }

    string FormatEdge(StateMachineGraphEdge edge)
    {
        string source = InvariantIndex(_graph.IndexOf(edge.Source));
        string target = InvariantIndex(_graph.IndexOf(edge.Target));
        return edge.Kind == StateMachineGraphEdgeKind.StateInheritance
            ? $"{source} -. inherits .-> {target}"
            : $"{source} --> {target}";
    }

    static string InvariantIndex(int index) => index.ToString(CultureInfo.InvariantCulture);
}
