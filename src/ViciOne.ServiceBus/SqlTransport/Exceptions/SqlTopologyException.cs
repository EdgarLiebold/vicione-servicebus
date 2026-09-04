using System;

#nullable enable
namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Represents an error related to sql topology.
/// </summary>
[Serializable]
public class SqlTopologyException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public SqlTopologyException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public SqlTopologyException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public SqlTopologyException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
