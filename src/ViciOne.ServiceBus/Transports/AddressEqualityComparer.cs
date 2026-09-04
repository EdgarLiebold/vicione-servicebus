using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides an address equality comparer implementation.
/// </summary>
public class AddressEqualityComparer :
    IEqualityComparer<Uri>
{
    /// <summary>
    /// Defines the comparer value.
    /// </summary>
    public static readonly IEqualityComparer<Uri> Comparer = new AddressEqualityComparer();

    /// <summary>
    /// Determines whether this instance equals the supplied value.
    /// </summary>
    /// <param name="x">The x value.</param>
    /// <param name="y">The y value.</param>
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

    /// <summary>
    /// Gets hash code.
    /// </summary>
    /// <param name="obj">The obj value.</param>
    /// <returns>The result of the operation.</returns>
    public int GetHashCode(Uri obj)
    {
        return obj.AbsolutePath.GetHashCode();
    }
}
