using System;
using System.Diagnostics;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Represents a directed relationship between two state-machine graph nodes.</summary>
/// <remarks>
/// Instances are immutable and can be shared safely between readers. Endpoint identity is reference-based so an edge
/// remains attached to the exact state-local graph occurrences supplied to the constructor.
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class StateMachineGraphEdge :
    IEquatable<StateMachineGraphEdge>
{
    /// <summary>Creates a directed relationship between two graph nodes.</summary>
    /// <param name="source">The node from which the edge originates.</param>
    /// <param name="target">The node at which the edge terminates.</param>
    /// <param name="kind">The state-machine relationship represented by the edge.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" /> or <paramref name="target" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">The endpoint kinds are incompatible with <paramref name="kind" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind" /> is not defined.</exception>
    public StateMachineGraphEdge(StateMachineGraphNode source, StateMachineGraphNode target, StateMachineGraphEdgeKind kind)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "The graph edge kind is not defined.");
        if (!HasValidEndpoints(source, target, kind))
            throw new ArgumentException("The graph edge endpoints are incompatible with the relationship kind.", nameof(kind));

        Source = source;
        Target = target;
        Kind = kind;
    }

    /// <summary>Gets the node from which this edge originates.</summary>
    public StateMachineGraphNode Source { get; }

    /// <summary>Gets the node at which this edge terminates.</summary>
    public StateMachineGraphNode Target { get; }

    /// <summary>Gets the state-machine relationship represented by this edge.</summary>
    public StateMachineGraphEdgeKind Kind { get; }

    /// <inheritdoc />
    public bool Equals(StateMachineGraphEdge? other) =>
        ReferenceEquals(this, other)
        || other is not null
        && ReferenceEquals(Source, other.Source)
        && ReferenceEquals(Target, other.Target)
        && Kind == other.Kind;

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as StateMachineGraphEdge);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Source, Target, Kind);

    /// <summary>Returns a concise description of the directed edge.</summary>
    /// <returns>The relationship kind followed by its source and target node names.</returns>
    public override string ToString() => DebuggerDisplay;

    string DebuggerDisplay => $"{Kind}: {Source.Name} -> {Target.Name}";

    static bool HasValidEndpoints(
        StateMachineGraphNode source,
        StateMachineGraphNode target,
        StateMachineGraphEdgeKind kind) => kind switch
        {
            StateMachineGraphEdgeKind.EventBinding =>
                source.Kind == StateMachineGraphNodeKind.State
                && target.Kind == StateMachineGraphNodeKind.Event,
            StateMachineGraphEdgeKind.StateTransition =>
                source.Kind != StateMachineGraphNodeKind.State
                && target.Kind == StateMachineGraphNodeKind.State,
            StateMachineGraphEdgeKind.ExceptionHandler =>
                source.Kind is StateMachineGraphNodeKind.Event or StateMachineGraphNodeKind.Exception
                && target.Kind == StateMachineGraphNodeKind.Exception,
            StateMachineGraphEdgeKind.CompositeContribution =>
                source.Kind is StateMachineGraphNodeKind.Event or StateMachineGraphNodeKind.Exception
                && target.Kind == StateMachineGraphNodeKind.Event
                && target.IsCompositeEvent,
            StateMachineGraphEdgeKind.StateInheritance =>
                source.Kind == StateMachineGraphNodeKind.State
                && target.Kind == StateMachineGraphNodeKind.State,
            _ => false,
        };
}
