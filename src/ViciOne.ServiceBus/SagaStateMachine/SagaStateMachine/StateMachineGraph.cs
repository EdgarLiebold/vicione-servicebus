// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SagaStateMachine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;


    [Serializable]
    public class StateMachineGraph
    {
        readonly Edge[] _edges;
        readonly Vertex[] _vertices;

        public StateMachineGraph(IEnumerable<Vertex> vertices, IEnumerable<Edge> edges)
        {
            _vertices = vertices.ToArray();
            _edges = edges.ToArray();
        }

        public IEnumerable<Vertex> Vertices => _vertices;

        public IEnumerable<Edge> Edges => _edges;
    }
}
