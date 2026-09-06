using System;
using System.Diagnostics;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides an edge implementation.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public class Edge :
    IEquatable<Edge>
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="from">The from value.</param>
    /// <param name="to">The to value.</param>
    /// <param name="title">The title value.</param>
    public Edge(Vertex from, Vertex to, string title)
    {
        From = from;
        To = to;
        Title = title;
    }

    /// <summary>
    /// Gets the to value.
    /// </summary>
    public Vertex To { get; }

    /// <summary>
    /// Gets the from value.
    /// </summary>
    public Vertex From { get; }

    string Title { get; }

    /// <summary>
    /// Gets the debugger display value.
    /// </summary>
    public string DebuggerDisplay => ToString();

    /// <summary>
    /// Determines whether this instance equals the supplied value.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(Edge? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Equals(To, other.To) && Equals(From, other.From) && string.Equals(Title, other.Title);
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
        if (obj.GetType() != GetType())
            return false;
        return Equals((Edge)obj);
    }

    /// <summary>
    /// Gets hash code.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override string ToString()
    {
        return $"{From} -> {To} {Title}";
    }
}
