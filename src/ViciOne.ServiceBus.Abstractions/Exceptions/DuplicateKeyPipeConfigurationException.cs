using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports an attempt to register more than one pipe segment with the same key.</summary>
public sealed class DuplicateKeyPipeConfigurationException :
    PipeConfigurationException
{
    /// <summary>Creates a duplicate-key exception without a custom message.</summary>
    public DuplicateKeyPipeConfigurationException()
    {
    }

    /// <summary>Creates a duplicate-key exception with the specified failure message.</summary>
    /// <param name="message">The description of the duplicate pipe key.</param>
    public DuplicateKeyPipeConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a duplicate-key exception with an underlying failure.</summary>
    /// <param name="message">The description of the duplicate pipe key.</param>
    /// <param name="innerException">The exception raised while registering the keyed pipe segment.</param>
    public DuplicateKeyPipeConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
