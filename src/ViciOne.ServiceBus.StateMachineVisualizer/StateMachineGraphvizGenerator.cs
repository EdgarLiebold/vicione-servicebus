using System;
using QuikGraph;
using QuikGraph.Graphviz;
using QuikGraph.Graphviz.Dot;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.StateMachineVisualizer.Internal;

namespace ViciOne.ServiceBus.StateMachineVisualizer;

/// <summary>Renders a state-machine graph as a Graphviz DOT document.</summary>
/// <remarks>
/// The constructor creates a private rendering snapshot that is not modified afterward. An instance can therefore
/// generate the same document repeatedly and can be shared by concurrent readers.
/// </remarks>
public sealed class StateMachineGraphvizGenerator
{
    readonly AdjacencyGraph<StateMachineGraphNode, TaggedEdge<StateMachineGraphNode, StateMachineGraphEdgeKind>> _graph;

    /// <summary>Creates a generator for the supplied state-machine graph.</summary>
    /// <param name="graph">The graph whose nodes and state-machine relationships are rendered.</param>
    /// <exception cref="ArgumentNullException"><paramref name="graph" /> is <see langword="null" />.</exception>
    public StateMachineGraphvizGenerator(StateMachineGraph graph)
    {
        _graph = StateMachineGraphFactory.Create(graph);
    }

    /// <summary>Generates the complete Graphviz DOT document.</summary>
    /// <returns>A DOT document containing every node and relationship captured by the constructor.</returns>
    public string Generate()
    {
        var algorithm = new GraphvizAlgorithm<StateMachineGraphNode, TaggedEdge<StateMachineGraphNode, StateMachineGraphEdgeKind>>(_graph);
        algorithm.FormatEdge += StyleEdge;
        algorithm.FormatVertex += StyleNode;
        return algorithm.Generate();
    }

    static void StyleEdge(
        object sender,
        FormatEdgeEventArgs<StateMachineGraphNode, TaggedEdge<StateMachineGraphNode, StateMachineGraphEdgeKind>> args)
    {
        if (args.Edge.Tag != StateMachineGraphEdgeKind.StateInheritance)
            return;

        args.EdgeFormat.Label.Value = "inherits";
        args.EdgeFormat.Style = GraphvizEdgeStyle.Dashed;
    }

    static void StyleNode(object sender, FormatVertexEventArgs<StateMachineGraphNode> args)
    {
        if (args.Vertex.Kind != StateMachineGraphNodeKind.State)
        {
            args.VertexFormat.Label = StateMachineNodeLabelFormatter.Format(args.Vertex, "<", ">");
            args.VertexFormat.FontColor = GraphvizColor.Black;
            args.VertexFormat.Shape = args.Vertex.IsCompositeEvent ? GraphvizVertexShape.InvHouse : GraphvizVertexShape.Rectangle;
        }
        else
        {
            args.VertexFormat.Label = args.Vertex.Name;
            args.VertexFormat.FillColor = GraphvizColor.White;
            args.VertexFormat.FontColor = GraphvizColor.Black;
            args.VertexFormat.Shape = GraphvizVertexShape.Ellipse;
        }
    }
}
