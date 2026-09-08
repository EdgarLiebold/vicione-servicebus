using System;
using System.Diagnostics;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Represents a state, event, or handled exception in a state-machine graph.</summary>
/// <remarks>
/// Instances are immutable and can be shared safely between readers. Each instance identifies one graph occurrence;
/// nodes with the same metadata remain distinct so separate state-local event and exception branches cannot be conflated.
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class StateMachineGraphNode
{
    StateMachineGraphNode(
        StateMachineGraphNodeKind kind,
        string name,
        Type? messageType,
        Type? exceptionType,
        bool isCompositeEvent)
    {
        Kind = kind;
        Name = name;
        MessageType = messageType;
        ExceptionType = exceptionType;
        IsCompositeEvent = isCompositeEvent;
    }

    /// <summary>Gets the kind of state-machine element represented by this node.</summary>
    public StateMachineGraphNodeKind Kind { get; }

    /// <summary>Gets the semantic name of the represented state-machine element.</summary>
    public string Name { get; }

    /// <summary>Gets the message contract carried by an event, or <see langword="null" /> for states, untyped events, and exception nodes.</summary>
    public Type? MessageType { get; }

    /// <summary>Gets the handled exception type, or <see langword="null" /> for states and events.</summary>
    public Type? ExceptionType { get; }

    /// <summary>Gets whether this node represents a composite event; always <see langword="false" /> for states and exception nodes.</summary>
    public bool IsCompositeEvent { get; }

    /// <summary>Creates a node representing a state.</summary>
    /// <param name="name">The non-empty state name.</param>
    /// <returns>An immutable state node.</returns>
    /// <exception cref="ArgumentException"><paramref name="name" /> is empty or consists only of white-space characters.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
    public static StateMachineGraphNode CreateState(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new StateMachineGraphNode(StateMachineGraphNodeKind.State, name, null, null, false);
    }

    /// <summary>Creates a node representing an event.</summary>
    /// <param name="name">The non-empty event name.</param>
    /// <param name="messageType">The closed reference-type message contract carried by the event, or <see langword="null" /> for an untyped event.</param>
    /// <param name="isCompositeEvent">Whether the event is raised after a configured set of contributing events has occurred.</param>
    /// <returns>An immutable event node.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="name" /> is empty or consists only of white-space characters, or <paramref name="messageType" /> cannot be
    /// used as an event message contract.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
    public static StateMachineGraphNode CreateEvent(string name, Type? messageType = null, bool isCompositeEvent = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (messageType is not null
            && (messageType.IsValueType
                || messageType.ContainsGenericParameters
                || messageType.IsPointer
                || messageType.IsFunctionPointer
                || messageType.IsByRef
                || messageType.IsAbstract && messageType.IsSealed))
        {
            throw new ArgumentException("The event message type must be a closed, non-static reference type.", nameof(messageType));
        }

        return new StateMachineGraphNode(StateMachineGraphNodeKind.Event, name, messageType, null, isCompositeEvent);
    }

    /// <summary>Creates a node representing a handled exception branch.</summary>
    /// <param name="exceptionType">The exception type handled by the branch.</param>
    /// <returns>An immutable exception node named after <paramref name="exceptionType" />.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="exceptionType" /> is open or does not derive from <see cref="Exception" />.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="exceptionType" /> is <see langword="null" />.</exception>
    public static StateMachineGraphNode CreateException(Type exceptionType)
    {
        ArgumentNullException.ThrowIfNull(exceptionType);
        if (!typeof(Exception).IsAssignableFrom(exceptionType) || exceptionType.ContainsGenericParameters)
            throw new ArgumentException("The graph exception type must be a closed type derived from System.Exception.", nameof(exceptionType));

        return new StateMachineGraphNode(StateMachineGraphNodeKind.Exception, exceptionType.Name, null, exceptionType, false);
    }

    /// <summary>Returns a concise description of the graph node.</summary>
    /// <returns>The node kind and name, followed by the message type for a typed event.</returns>
    public override string ToString() => DebuggerDisplay;

    string DebuggerDisplay
    {
        get
        {
            if (Kind == StateMachineGraphNodeKind.Exception)
                return $"{Kind}: {Name}";

            return MessageType is null
                ? $"{Kind}: {Name}"
                : $"{Kind}: {Name} ({MessageType.Name})";
        }
    }
}
