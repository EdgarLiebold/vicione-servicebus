using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports that a routing-slip activity supplied an unusable compensation address.</summary>
public sealed class InvalidCompensationAddressException :
    ActivityExecutionException
{
    /// <summary>Creates an invalid-address exception without address context.</summary>
    public InvalidCompensationAddressException()
    {
    }

    /// <summary>Creates an exception for the rejected compensation address.</summary>
    /// <param name="address">The rejected address, or <see langword="null" /> when none was supplied.</param>
    public InvalidCompensationAddressException(Uri? address)
        : base($"An invalid compensation address was specified: {address}")
    {
        Address = address;
    }

    /// <summary>Creates an invalid-address exception with the specified failure message.</summary>
    /// <param name="message">The description of the invalid compensation address.</param>
    public InvalidCompensationAddressException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an invalid-address exception with an underlying failure.</summary>
    /// <param name="message">The description of the invalid compensation address.</param>
    /// <param name="innerException">The exception raised while resolving the compensation address.</param>
    public InvalidCompensationAddressException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Gets the rejected compensation address, when one was supplied.</summary>
    public Uri? Address { get; }
}
