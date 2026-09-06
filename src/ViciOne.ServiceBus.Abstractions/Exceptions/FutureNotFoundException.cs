using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to future not found.</summary>
public class FutureNotFoundException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public FutureNotFoundException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="type">The runtime type to inspect or use.</param>
    /// <param name="id">The id.</param>
    public FutureNotFoundException(Type type, Guid id)
        : base($"Future {TypeCache.GetShortName(type)}({id}) not found")
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public FutureNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public FutureNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
