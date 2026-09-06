using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to recurring job.</summary>
public class RecurringJobException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public RecurringJobException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public RecurringJobException(string message)
        : base(message)
    {
    }
}
