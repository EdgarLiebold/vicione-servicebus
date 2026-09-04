using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to recurring job.
/// </summary>
[Serializable]
public class RecurringJobException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RecurringJobException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public RecurringJobException(string message)
        : base(message)
    {
    }
}
