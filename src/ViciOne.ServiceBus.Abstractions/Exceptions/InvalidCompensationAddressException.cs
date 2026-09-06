using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to invalid compensation address.</summary>
public class InvalidCompensationAddressException :
    ActivityExecutionException
{
    /// <summary>Initializes a new instance.</summary>
    public InvalidCompensationAddressException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="address">The address.</param>
    public InvalidCompensationAddressException(Uri? address)
        : base($"An invalid compensation address was specified: {address}")
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public InvalidCompensationAddressException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public InvalidCompensationAddressException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
