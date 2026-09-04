using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to invalid compensation address.
/// </summary>
[Serializable]
public class InvalidCompensationAddressException :
    ActivityExecutionException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public InvalidCompensationAddressException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    public InvalidCompensationAddressException(Uri? address)
        : base($"An invalid compensation address was specified: {address}")
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public InvalidCompensationAddressException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public InvalidCompensationAddressException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
