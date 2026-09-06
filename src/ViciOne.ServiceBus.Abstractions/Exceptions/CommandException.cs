using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to command.
/// </summary>
public class CommandException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public CommandException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public CommandException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public CommandException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
