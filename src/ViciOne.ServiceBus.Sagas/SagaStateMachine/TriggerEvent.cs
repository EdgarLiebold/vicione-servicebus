using System;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Carries the trigger event data.</summary>
public class TriggerEvent :
    IEvent
{
    readonly string _name;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="name">The name.</param>
    public TriggerEvent(string name)
    {
        _name = name;
    }

    /// <summary>Gets the name.</summary>
    public string Name => _name;

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public virtual void Accept(IStateMachineVisitor visitor)
    {
        visitor.Visit(this, x =>
        {
        });
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public virtual void Probe(ProbeContext context)
    {
        context.Add("name", _name);
    }

    /// <summary>Compares this instance with the supplied value.</summary>
    /// <param name="other">The other.</param>
    /// <returns>The int produced by the operation.</returns>
    public int CompareTo(IEvent? other)
    {
        return other == null ? 1 : string.Compare(_name, other.Name, StringComparison.Ordinal);
    }

    /// <summary>Determines whether this instance equals the supplied value.</summary>
    /// <param name="other">The other.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(TriggerEvent other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Equals(other._name, _name);
    }

    /// <summary>Determines whether this instance equals the supplied value.</summary>
    /// <param name="obj">The obj.</param>
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

    /// <summary>Gets hash code.</summary>
    /// <returns>The hash code for this instance.</returns>
    public override int GetHashCode()
    {
        return _name?.GetHashCode() ?? 0;
    }

    /// <summary>Returns the string representation of this instance.</summary>
    /// <returns>The converted string.</returns>
    public override string ToString()
    {
        return $"{_name} (Event)";
    }
}
