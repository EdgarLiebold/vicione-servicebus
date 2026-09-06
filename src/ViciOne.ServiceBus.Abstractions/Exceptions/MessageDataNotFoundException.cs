using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to message data not found.</summary>
public class MessageDataNotFoundException :
    MessageDataException
{
    /// <summary>Initializes a new instance.</summary>
    public MessageDataNotFoundException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="address">The address.</param>
    public MessageDataNotFoundException(Uri address)
        : base($"The message data was not found: {address}")
    {
    }
}
