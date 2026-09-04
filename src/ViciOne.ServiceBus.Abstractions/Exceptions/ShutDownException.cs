using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to shut down.
/// </summary>
[Serializable]
public class ShutDownException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public ShutDownException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
