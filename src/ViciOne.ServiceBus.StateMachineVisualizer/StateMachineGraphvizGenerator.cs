using System;
using QuikGraph;
using QuikGraph.Graphviz;
using QuikGraph.Graphviz.Dot;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Visualizer.Internal;

namespace ViciOne.ServiceBus.Visualizer;

/// <summary>Generates Graphviz DOT documents from state-machine graphs.</summary>
public sealed class StateMachineGraphvizGenerator
{
    readonly AdjacencyGraph<Vertex, Edge<Vertex>> _graph;

    /// <summary>Creates a generator for a state-machine graph.</summary>
    /// <param name="graph">The state-machine graph to render.</param>
    public StateMachineGraphvizGenerator(StateMachineGraph graph)
    {
        _graph = StateMachineGraphFactory.Create(graph);
    }

    /// <summary>Generates a Graphviz DOT document.</summary>
    /// <returns>The complete DOT document.</returns>
    public string Generate()
    {
        var algorithm = new GraphvizAlgorithm<Vertex, Edge<Vertex>>(_graph);
        algorithm.FormatVertex += VertexStyler;
        return algorithm.Generate();
    }

    static void VertexStyler(object sender, FormatVertexEventArgs<Vertex> args)
    {
        args.VertexFormat.Label = args.Vertex.Title;

        if (args.Vertex.VertexType == typeof(Event))
        {
            args.VertexFormat.FontColor = GraphvizColor.Black;
            args.VertexFormat.Shape = args.Vertex.IsComposite ? GraphvizVertexShape.InvHouse : GraphvizVertexShape.Rectangle;

            if (args.Vertex.TargetType != typeof(Event) && args.Vertex.TargetType != typeof(Exception))
            {
                if (args.Vertex.TargetType.TryGetSingleClosedGenericArguments(typeof(Fault<>), out Type[] arguments))
                    args.VertexFormat.Label += "<" + arguments[0].Name + ">";
                else
                    args.VertexFormat.Label += "<" + args.Vertex.TargetType.Name + ">";
            }
        }
        else
        {
            switch (args.Vertex.Title)
            {
                case "Initial":
                    args.VertexFormat.FillColor = GraphvizColor.White;
                    break;
                case "Final":
                    args.VertexFormat.FillColor = GraphvizColor.White;
                    break;
                default:
                    args.VertexFormat.FillColor = GraphvizColor.White;
                    args.VertexFormat.FontColor = GraphvizColor.Black;
                    break;
            }

            args.VertexFormat.Shape = GraphvizVertexShape.Ellipse;
        }
    }
}
