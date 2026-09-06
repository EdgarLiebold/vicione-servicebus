using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Compares address equality values.</summary>
public class AddressEqualityComparer :
    IEqualityComparer<Uri>
{
    /// <summary>Exposes the comparer used by the containing type.</summary>
    public static readonly IEqualityComparer<Uri> Comparer = new AddressEqualityComparer();

    /// <summary>Determines whether this instance equals the supplied value.</summary>
    /// <param name="x">The <c>x</c> value.</param>
    /// <param name="y">The <c>y</c> value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(Uri? x, Uri? y)
    {
        return ReferenceEquals(x, y)
            || (x != null
                && y != null
                && x.Scheme.Equals(y.Scheme, StringComparison.OrdinalIgnoreCase)
                && x.Host.Equals(y.Host, StringComparison.OrdinalIgnoreCase)
                && x.Port.Equals(y.Port)
                && x.AbsolutePath.Equals(y.AbsolutePath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Gets hash code.</summary>
    /// <param name="obj">The obj.</param>
    /// <returns>The hash code for this instance.</returns>
    public int GetHashCode(Uri obj)
    {
        return obj.AbsolutePath.GetHashCode();
    }
}
