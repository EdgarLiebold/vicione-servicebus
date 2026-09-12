using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Identifies an endpoint that receives a future's terminal outcome.</summary>
public sealed class FutureSubscription :
    IEquatable<FutureSubscription>
{
    /// <summary>Creates a subscription for an endpoint and optional request.</summary>
    /// <param name="address">The endpoint that receives the future result.</param>
    /// <param name="requestId">The request identifier copied to the response.</param>
    public FutureSubscription(Uri address, Guid? requestId = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (!address.IsAbsoluteUri)
            throw new ArgumentException("The subscriber address must be an absolute URI.", nameof(address));
        if (requestId == Guid.Empty)
            throw new ArgumentException("The subscriber request identifier must not be empty.", nameof(requestId));

        RequestId = requestId;
        Address = address;
    }

    /// <summary>Gets the comparer that uses endpoint address and request identifier.</summary>
    public static IEqualityComparer<FutureSubscription> Comparer { get; } = new EqualityComparer();

    /// <summary>Gets the request identifier copied to the response.</summary>
    public Guid? RequestId { get; }
    /// <summary>Gets the endpoint that receives the future result.</summary>
    public Uri Address { get; }

    /// <summary>Determines whether another subscription has the same endpoint and request identifier.</summary>
    /// <param name="other">The subscription to compare.</param>
    /// <returns><see langword="true" /> when both subscription identities match; otherwise, <see langword="false" />.</returns>
    public bool Equals(FutureSubscription? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Nullable.Equals(RequestId, other.RequestId) && Equals(Address, other.Address);
    }

    /// <summary>Determines whether another object represents the same subscription.</summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true" /> when the object has the same subscription identity; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object? obj)
    {
        return obj is FutureSubscription other && Equals(other);
    }

    /// <summary>Returns a hash code for the endpoint address and request identifier.</summary>
    /// <returns>The hash code for the subscription identity.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(RequestId, Address);
    }

    /// <summary>Determines whether two subscriptions have equal identities.</summary>
    /// <param name="left">The first subscription.</param>
    /// <param name="right">The second subscription.</param>
    /// <returns><see langword="true" /> when both subscriptions are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(FutureSubscription? left, FutureSubscription? right)
    {
        return Equals(left, right);
    }

    /// <summary>Determines whether two subscriptions have different identities.</summary>
    /// <param name="left">The first subscription.</param>
    /// <param name="right">The second subscription.</param>
    /// <returns><see langword="true" /> when the subscriptions differ; otherwise, <see langword="false" />.</returns>
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
            return x.Equals(y);
        }

        public int GetHashCode(FutureSubscription obj)
        {
            ArgumentNullException.ThrowIfNull(obj);
            return obj.GetHashCode();
        }
    }
}
