using System;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Identifies a named state-machine event that does not carry message data.</summary>
public class TriggerEvent :
    IEvent
{
    readonly string _name;

    /// <summary>Initializes an event with the name used for identity, ordering and diagnostics.</summary>
    /// <param name="name">The non-null event name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name" /> is null.</exception>
    public TriggerEvent(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        _name = name;
    }

    /// <summary>Gets the event name used for identity, ordering and diagnostics.</summary>
    public string Name => _name;

    /// <summary>Visits this event through the untyped event overload.</summary>
    /// <param name="visitor">The visitor receiving this event.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visitor" /> is null.</exception>
    public virtual void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);

        visitor.Visit(this, x =>
        {
        });
    }

    /// <summary>Writes the event name to the supplied diagnostic context.</summary>
    /// <param name="context">The context receiving the event metadata.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is null.</exception>
    public virtual void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Add("name", _name);
    }

    /// <summary>Orders events by ordinal name, after a null event.</summary>
    /// <param name="other">The event whose name is compared, or <see langword="null" />.</param>
    /// <returns>The ordinal name comparison, or one when <paramref name="other" /> is null.</returns>
    public int CompareTo(IEvent? other)
    {
        return other == null ? 1 : string.Compare(_name, other.Name, StringComparison.Ordinal);
    }

    /// <summary>Compares another trigger-event view by ordinal name.</summary>
    /// <param name="other">The trigger event compared with this instance.</param>
    /// <returns><see langword="true" /> when <paramref name="other" /> has the same ordinal name; otherwise, <see langword="false" />.</returns>
    public bool Equals(TriggerEvent? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return string.Equals(other._name, _name, StringComparison.Ordinal);
    }

    /// <summary>Compares another object using exact <see cref="TriggerEvent" /> type and ordinal name equality.</summary>
    /// <param name="obj">The object compared with this instance.</param>
    /// <returns><see langword="true" /> when <paramref name="obj" /> is a trigger event with the same ordinal name; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != typeof(TriggerEvent))
            return false;
        return Equals((TriggerEvent)obj);
    }

    /// <summary>Gets the event name's hash code.</summary>
    /// <returns>The ordinal name hash code for this instance.</returns>
    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(_name);
    }

    /// <summary>Returns the event name followed by its event marker.</summary>
    /// <returns>The display text in the form <c>Name (Event)</c>.</returns>
    public override string ToString()
    {
        return $"{_name} (Event)";
    }
}
