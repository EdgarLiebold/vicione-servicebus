using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to future not found.
/// </summary>
public class FutureNotFoundException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public FutureNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <param name="id">The id value.</param>
    public FutureNotFoundException(Type type, Guid id)
        : base($"Future {TypeCache.GetShortName(type)}({id}) not found")
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public FutureNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public FutureNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
