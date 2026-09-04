using System;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a trigger event implementation.
/// </summary>
public class TriggerEvent :
    Event
{
    readonly string _name;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="name">The name value.</param>
    public TriggerEvent(string name)
    {
        _name = name;
    }

    /// <summary>
    /// Gets the name value.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public virtual void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this, x =>
        {
        });
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public virtual void Probe(ProbeContext context)
    {
        context.Add("name", _name);
    }

    /// <summary>
    /// Compares this instance with the supplied value.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns>The result of the operation.</returns>
    public int CompareTo(Event? other)
    {
        return other == null ? 1 : string.Compare(_name, other.Name, StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether this instance equals the supplied value.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(TriggerEvent other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Equals(other._name, _name);
    }

    /// <summary>
    /// Determines whether this instance equals the supplied value.
    /// </summary>
    /// <param name="obj">The obj value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
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

    /// <summary>
    /// Gets hash code.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override int GetHashCode()
    {
        return _name?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override string ToString()
    {
        return $"{_name} (Event)";
    }
}
