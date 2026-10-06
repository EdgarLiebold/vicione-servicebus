using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Represents an error related to sql topology.</summary>
public class SqlTopologyException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public SqlTopologyException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The description of the topology failure.</param>
    public SqlTopologyException(string? message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The description of the topology failure.</param>
    /// <param name="innerException">The inner exception.</param>
    public SqlTopologyException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
