using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Provides an immutable snapshot of a state machine's nodes and directed edges.</summary>
/// <remarks>
/// The constructor copies and validates both input sequences, so the snapshot can be shared safely between readers.
/// Both sequence references are validated before enumeration, and invalid nodes are rejected before edges are enumerated.
/// Node membership uses reference identity, which preserves separate state-local occurrences with identical metadata.
/// </remarks>
public sealed class StateMachineGraph
{
    readonly ReadOnlyCollection<StateMachineGraphEdge> _edges;
    readonly ReadOnlyCollection<StateMachineGraphNode> _nodes;

    /// <summary>Creates a validated graph snapshot.</summary>
    /// <param name="nodes">The node instances that belong to the graph; the same instance cannot occur more than once.</param>
    /// <param name="edges">The unique endpoint-and-kind relationships whose exact endpoint instances belong to <paramref name="nodes" />.</param>
    /// <exception cref="ArgumentException">
    /// A sequence contains a <see langword="null" /> element, either sequence contains duplicates, or an edge endpoint does not
    /// belong to <paramref name="nodes" />.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="nodes" /> or <paramref name="edges" /> is <see langword="null" />.
    /// </exception>
    public StateMachineGraph(IEnumerable<StateMachineGraphNode> nodes, IEnumerable<StateMachineGraphEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);

        StateMachineGraphNode[] nodeSnapshot = nodes.ToArray();
        if (nodeSnapshot.Any(static node => node is null))
            throw new ArgumentException("Graph nodes cannot contain null elements.", nameof(nodes));

        HashSet<StateMachineGraphNode> nodeSet = nodeSnapshot.ToHashSet();
        if (nodeSet.Count != nodeSnapshot.Length)
            throw new ArgumentException("Graph nodes must be unique.", nameof(nodes));

        StateMachineGraphEdge[] edgeSnapshot = edges.ToArray();
        if (edgeSnapshot.Any(static edge => edge is null))
            throw new ArgumentException("Graph edges cannot contain null elements.", nameof(edges));
        if (edgeSnapshot.ToHashSet().Count != edgeSnapshot.Length)
            throw new ArgumentException("Graph edges must be unique.", nameof(edges));
        if (edgeSnapshot.Any(edge => !nodeSet.Contains(edge.Source) || !nodeSet.Contains(edge.Target)))
            throw new ArgumentException("Every graph edge endpoint must belong to the graph nodes.", nameof(edges));

        _nodes = Array.AsReadOnly(nodeSnapshot);
        _edges = Array.AsReadOnly(edgeSnapshot);
    }

    /// <summary>Gets the ordered, read-only graph nodes.</summary>
    public IReadOnlyList<StateMachineGraphNode> Nodes => _nodes;

    /// <summary>Gets the ordered, read-only directed edges.</summary>
    public IReadOnlyList<StateMachineGraphEdge> Edges => _edges;
}
