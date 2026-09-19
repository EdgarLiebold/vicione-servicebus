using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Locates the unchanged application-body bytes in a copied transport envelope.</summary>
/// <remarks>
/// A deserializer may implement this optional contract to preserve precise body admission when
/// an envelope is forwarded without reserialization. The body must be one contiguous byte range
/// of the exact envelope supplied by the bus. A format that transforms or encrypts the body while
/// enclosing it cannot use this range contract.
/// </remarks>
public interface ICopiedEnvelopeBodyLocator
{
    /// <summary>Locates the unique serialized application body in the final envelope.</summary>
    /// <param name="envelope">The exact copied envelope bytes owned by the bus.</param>
    /// <param name="offset">The body's byte offset from the start of <paramref name="envelope"/>.</param>
    /// <param name="length">The body's byte length.</param>
    /// <returns><see langword="true"/> when a unique body range was found; otherwise, <see langword="false"/>.</returns>
    bool TryLocateSerializedBody(ReadOnlySpan<byte> envelope, out int offset, out int length);
}
