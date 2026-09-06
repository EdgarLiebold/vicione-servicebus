using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to message retry limit exceeded.</summary>
public class MessageRetryLimitExceededException :
    TransportException
{
    /// <summary>Initializes a new instance.</summary>
    public MessageRetryLimitExceededException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="uri">The uri.</param>
    public MessageRetryLimitExceededException(Uri uri)
        : base(uri)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="uri">The uri.</param>
    /// <param name="message">The message to process.</param>
    public MessageRetryLimitExceededException(Uri uri, string message)
        : base(uri, message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="uri">The uri.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public MessageRetryLimitExceededException(Uri uri, string message, Exception innerException)
        : base(uri, message, innerException)
    {
    }
}
