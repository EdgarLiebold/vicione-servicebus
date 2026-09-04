using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to job not found.
/// </summary>
[Serializable]
public class JobNotFoundException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public JobNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public JobNotFoundException(string message)
        : base(message)
    {
    }
}
