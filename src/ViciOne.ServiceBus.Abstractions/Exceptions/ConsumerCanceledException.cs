using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports an unexpected cancellation while a consumer processes a message.</summary>
public sealed class ConsumerCanceledException :
    ViciOneServiceBusException
{
    /// <summary>Creates a consumer-cancellation exception without a custom message.</summary>
    public ConsumerCanceledException()
    {
    }

    /// <summary>Creates a consumer-cancellation exception with the specified failure message.</summary>
    /// <param name="message">The description of the unexpected cancellation.</param>
    public ConsumerCanceledException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a consumer-cancellation exception with the cancellation cause.</summary>
    /// <param name="message">The description of the unexpected cancellation.</param>
    /// <param name="innerException">The cancellation exception observed by the consumer.</param>
    public ConsumerCanceledException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
