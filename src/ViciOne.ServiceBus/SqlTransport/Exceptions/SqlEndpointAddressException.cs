using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Represents an error related to sql endpoint address.</summary>
public sealed class SqlEndpointAddressException :
    AbstractUriException
{
    /// <summary>Initializes a new instance.</summary>
    public SqlEndpointAddressException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="address">The address.</param>
    /// <param name="message">The message to process.</param>
    public SqlEndpointAddressException(Uri address, string message)
        : base(address, message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="address">The address.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public SqlEndpointAddressException(Uri address, string message, Exception innerException)
        : base(address, message, innerException)
    {
    }
}
