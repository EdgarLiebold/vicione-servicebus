using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Describes an optional inline or repository-backed message-data value.</summary>
public interface IMessageData
{
    /// <summary>Gets the repository address, or <see langword="null" /> when the value is empty or inline-only.</summary>
    Uri? Address { get; }

    /// <summary>Gets whether a non-null value is available.</summary>
    bool HasValue { get; }
}
