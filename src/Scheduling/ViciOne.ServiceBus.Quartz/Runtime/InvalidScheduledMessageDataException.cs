using System;

namespace ViciOne.ServiceBus.Quartz.Runtime;

/// <summary>Reports persisted scheduled-message data that cannot produce a valid transport send context.</summary>
internal sealed class InvalidScheduledMessageDataException : Exception
{
    /// <summary>Initializes the exception with the data-processing failure.</summary>
    /// <param name="message">A description of the invalid persisted data.</param>
    /// <param name="innerException">The failure raised while reconstructing the send context.</param>
    public InvalidScheduledMessageDataException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
