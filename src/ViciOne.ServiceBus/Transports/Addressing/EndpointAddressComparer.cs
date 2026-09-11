using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Compares endpoint addresses by scheme, host, port, and case-sensitive path while ignoring
/// user information, query parameters, and fragments.
/// </summary>
public sealed class EndpointAddressComparer :
    IEqualityComparer<Uri>
{
    EndpointAddressComparer()
    {
    }

    /// <summary>Gets the shared endpoint-address comparer.</summary>
    public static EndpointAddressComparer Instance { get; } = new();

    /// <summary>Determines whether two endpoint addresses identify the same transport path.</summary>
    /// <param name="x">The first address to compare.</param>
    /// <param name="y">The second address to compare.</param>
    /// <returns><see langword="true" /> when the endpoint identities are equal; otherwise, <see langword="false" />.</returns>
    public bool Equals(Uri? x, Uri? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (x is null || y is null || x.IsAbsoluteUri != y.IsAbsoluteUri)
            return false;

        if (!x.IsAbsoluteUri)
            return string.Equals(x.OriginalString, y.OriginalString, StringComparison.Ordinal);

        return string.Equals(x.Scheme, y.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.IdnHost, y.IdnHost, StringComparison.OrdinalIgnoreCase)
            && x.Port == y.Port
            && string.Equals(x.AbsolutePath, y.AbsolutePath, StringComparison.Ordinal);
    }

    /// <summary>Returns a hash code for the endpoint identity represented by an address.</summary>
    /// <param name="obj">The address to hash.</param>
    /// <returns>A hash code consistent with <see cref="Equals(Uri?, Uri?)" />.</returns>
    public int GetHashCode(Uri obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        if (!obj.IsAbsoluteUri)
            return StringComparer.Ordinal.GetHashCode(obj.OriginalString);

        var hash = new HashCode();
        hash.Add(obj.Scheme, StringComparer.OrdinalIgnoreCase);
        hash.Add(obj.IdnHost, StringComparer.OrdinalIgnoreCase);
        hash.Add(obj.Port);
        hash.Add(obj.AbsolutePath, StringComparer.Ordinal);
        return hash.ToHashCode();
    }
}
