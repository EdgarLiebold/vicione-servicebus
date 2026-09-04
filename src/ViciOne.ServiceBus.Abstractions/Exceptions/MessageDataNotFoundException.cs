using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to message data not found.
/// </summary>
[Serializable]
public class MessageDataNotFoundException :
    MessageDataException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public MessageDataNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    public MessageDataNotFoundException(Uri address)
        : base($"The message data was not found: {address}")
    {
    }
}
