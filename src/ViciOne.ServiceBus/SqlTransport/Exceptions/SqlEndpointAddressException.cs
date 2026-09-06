using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Represents an error related to sql endpoint address.
/// </summary>
public sealed class SqlEndpointAddressException :
    AbstractUriException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public SqlEndpointAddressException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="message">The message value.</param>
    public SqlEndpointAddressException(Uri address, string message)
        : base(address, message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public SqlEndpointAddressException(Uri address, string message, Exception innerException)
        : base(address, message, innerException)
    {
    }
}
