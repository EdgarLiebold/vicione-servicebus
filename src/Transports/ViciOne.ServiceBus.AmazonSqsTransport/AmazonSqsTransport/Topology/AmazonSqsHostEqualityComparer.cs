namespace ViciOne.ServiceBus.AmazonSqsTransport.Topology;

using System;
using System.Collections.Generic;


public sealed class AmazonSqsHostEqualityComparer :
    IEqualityComparer<AmazonSqsHostSettings>
{
    public static IEqualityComparer<AmazonSqsHostSettings> Default { get; } = new AmazonSqsHostEqualityComparer();

    public bool Equals(AmazonSqsHostSettings? x, AmazonSqsHostSettings? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (ReferenceEquals(x, null))
            return false;

        if (ReferenceEquals(y, null))
            return false;

        return x.ScopeTopics == y.ScopeTopics && x.HostAddress.Equals(y.HostAddress);
    }

    public int GetHashCode(AmazonSqsHostSettings obj)
    {
        return HashCode.Combine(obj.HostAddress, obj.ScopeTopics);
    }
}
