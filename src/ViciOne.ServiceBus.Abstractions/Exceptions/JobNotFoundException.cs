using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to job not found.</summary>
public class JobNotFoundException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public JobNotFoundException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public JobNotFoundException(string message)
        : base(message)
    {
    }
}
