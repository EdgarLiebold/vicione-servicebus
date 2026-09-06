using System;
using System.Diagnostics;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Represents a vertex in a state-machine graph.</summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public class Vertex :
    IEquatable<Vertex>
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="type">The runtime type to inspect or use.</param>
    /// <param name="targetType">The runtime target type used by the operation.</param>
    /// <param name="title">The title.</param>
    /// <param name="isComposite">The is composite.</param>
    public Vertex(Type type, Type targetType, string title, bool isComposite)
    {
        VertexType = type;
        TargetType = targetType;
        Title = title;
        IsComposite = isComposite;
    }

    /// <summary>Gets the title.</summary>
    public string Title { get; }

    /// <summary>Gets the vertex type.</summary>
    public Type VertexType { get; }

    /// <summary>Gets the target type.</summary>
    public Type TargetType { get; }

    /// <summary>Gets a value indicating whether composite.</summary>
    public bool IsComposite { get; }

    /// <summary>Gets the debugger display.</summary>
    public string DebuggerDisplay => $"{VertexType.Name}({IsComposite}) {Title} -> {TargetType.Name}";

    /// <summary>Determines whether this instance equals the supplied value.</summary>
    /// <param name="other">The other.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(Vertex? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return string.Equals(Title, other.Title) && VertexType == other.VertexType && TargetType == other.TargetType;
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
        return Equals((Vertex)obj);
    }

    /// <summary>Gets hash code.</summary>
    /// <returns>The hash code for this instance.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Title?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ (VertexType?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ (TargetType?.GetHashCode() ?? 0);
            return hashCode;
        }
    }

    /// <summary>Returns the string representation of this instance.</summary>
    /// <returns>The converted string.</returns>
    public override string ToString()
    {
        return $"{VertexType.Name}({IsComposite}) {Title} -> {TargetType.Name}";
    }
}
