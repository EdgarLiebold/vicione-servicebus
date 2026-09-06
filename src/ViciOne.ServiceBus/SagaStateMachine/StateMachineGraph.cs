using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a state machine graph implementation.
/// </summary>
public class StateMachineGraph
{
    readonly Edge[] _edges;
    readonly Vertex[] _vertices;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="vertices">The vertices value.</param>
    /// <param name="edges">The edges value.</param>
    public StateMachineGraph(IEnumerable<Vertex> vertices, IEnumerable<Edge> edges)
    {
        _vertices = vertices.ToArray();
        _edges = edges.ToArray();
    }

    /// <summary>
    /// Gets the vertices value.
    /// </summary>
    public IEnumerable<Vertex> Vertices => _vertices;

    /// <summary>
    /// Gets the edges value.
    /// </summary>
    public IEnumerable<Edge> Edges => _edges;
}
