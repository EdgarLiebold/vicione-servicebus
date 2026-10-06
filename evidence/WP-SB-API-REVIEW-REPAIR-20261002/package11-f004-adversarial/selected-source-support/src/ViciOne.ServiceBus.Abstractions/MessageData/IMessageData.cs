using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Describes an optional inline or repository-backed message-data value.</summary>
public interface IMessageData
{
    /// <summary>Gets the repository address, or <see langword="null" /> when a populated value has not been stored or is inline-only.</summary>
    /// <remarks>Check <see cref="HasValue"/> before accessing an optional handle's address. Empty handles throw when their address is read.</remarks>
    /// <exception cref="MessageDataException">The handle is empty and has no repository address.</exception>
    Uri? Address { get; }

    /// <summary>Gets whether a non-null value is available.</summary>
    bool HasValue { get; }
}
