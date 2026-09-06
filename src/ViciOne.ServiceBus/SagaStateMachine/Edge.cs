using System;
using System.Diagnostics;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Represents a directed edge in a state-machine graph.</summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public class Edge :
    IEquatable<Edge>
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="from">The from.</param>
    /// <param name="to">The to.</param>
    /// <param name="title">The title.</param>
    public Edge(Vertex from, Vertex to, string title)
    {
        From = from;
        To = to;
        Title = title;
    }

    /// <summary>Gets the to.</summary>
    public Vertex To { get; }

    /// <summary>Gets the from.</summary>
    public Vertex From { get; }

    string Title { get; }

    /// <summary>Gets the debugger display.</summary>
    public string DebuggerDisplay => ToString();

    /// <summary>Determines whether this instance equals the supplied value.</summary>
    /// <param name="other">The other.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(Edge? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Equals(To, other.To) && Equals(From, other.From) && string.Equals(Title, other.Title);
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
        if (obj.GetType() != GetType())
            return false;
        return Equals((Edge)obj);
    }

    /// <summary>Gets hash code.</summary>
    /// <returns>The hash code for this instance.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = To?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ (From?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ (Title?.GetHashCode() ?? 0);
            return hashCode;
        }
    }

    /// <summary>Returns the string representation of this instance.</summary>
    /// <returns>The converted string.</returns>
    public override string ToString()
    {
        return $"{From} -> {To} {Title}";
    }
}
