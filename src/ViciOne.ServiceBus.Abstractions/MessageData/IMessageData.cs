using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>
/// Defines the contract for message data.
/// </summary>
public interface IMessageData
{
    /// <summary>
    /// Returns the address of the message data
    /// </summary>
    Uri? Address { get; }

    /// <summary>
    /// True if the value is present in the message, and not null
    /// </summary>
    bool HasValue { get; }
}
