using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Represents the graph for state machine.</summary>
public class StateMachineGraph
{
    readonly Edge[] _edges;
    readonly Vertex[] _vertices;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="vertices">The vertices.</param>
    /// <param name="edges">The edges.</param>
    public StateMachineGraph(IEnumerable<Vertex> vertices, IEnumerable<Edge> edges)
    {
        _vertices = vertices.ToArray();
        _edges = edges.ToArray();
    }

    /// <summary>Gets the vertices.</summary>
    public IEnumerable<Vertex> Vertices => _vertices;

    /// <summary>Gets the edges.</summary>
    public IEnumerable<Edge> Edges => _edges;
}
