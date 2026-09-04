using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Futures;

/// <summary>
/// Provides a future subscription implementation.
/// </summary>
public class FutureSubscription :
    IEquatable<FutureSubscription>
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="requestId">The request id value.</param>
    public FutureSubscription(Uri address, Guid? requestId = default)
    {
        RequestId = requestId;
        Address = address;
    }

    /// <summary>
    /// Gets the comparer value.
    /// </summary>
    public static IEqualityComparer<FutureSubscription> Comparer { get; } = new EqualityComparer();

    /// <summary>
    /// Gets the request id value.
    /// </summary>
    public Guid? RequestId { get; }
    /// <summary>
    /// Gets the address value.
    /// </summary>
    public Uri Address { get; }

    /// <summary>
    /// Determines whether this instance equals the supplied value.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(FutureSubscription? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Nullable.Equals(RequestId, other.RequestId) && Equals(Address, other.Address);
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
        return Equals((FutureSubscription)obj);
    }

    /// <summary>
    /// Gets hash code.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            return (RequestId.GetHashCode() * 397) ^ (Address != null ? Address.GetHashCode() : 0);
        }
    }

    /// <summary>
    /// Applies the <c>==</c> operator.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(FutureSubscription? left, FutureSubscription? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Applies the <c>!=</c> operator.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(FutureSubscription? left, FutureSubscription? right)
    {
        return !Equals(left, right);
    }


    sealed class EqualityComparer :
        IEqualityComparer<FutureSubscription>
    {
        public bool Equals(FutureSubscription? x, FutureSubscription? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            return Nullable.Equals(x.RequestId, y.RequestId) && Equals(x.Address, y.Address);
        }

        public int GetHashCode(FutureSubscription obj)
        {
            unchecked
            {
                return (obj.RequestId.GetHashCode() * 397) ^ (obj.Address != null ? obj.Address.GetHashCode() : 0);
            }
        }
    }
}
