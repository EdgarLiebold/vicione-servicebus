using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to command.</summary>
public class CommandException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public CommandException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public CommandException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public CommandException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
