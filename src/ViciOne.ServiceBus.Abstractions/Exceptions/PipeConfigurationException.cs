using System;

namespace ViciOne.ServiceBus;

/// <summary>Provides the exception base for invalid middleware-pipe configuration.</summary>
public class PipeConfigurationException :
    ViciOneServiceBusException
{
    /// <summary>Creates a pipe-configuration exception without a custom message.</summary>
    public PipeConfigurationException()
    {
    }

    /// <summary>Creates a pipe-configuration exception with the specified failure message.</summary>
    /// <param name="message">The description of the invalid pipe configuration.</param>
    public PipeConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a pipe-configuration exception with an underlying failure.</summary>
    /// <param name="message">The description of the invalid pipe configuration.</param>
    /// <param name="innerException">The exception raised while configuring the pipe.</param>
    public PipeConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
