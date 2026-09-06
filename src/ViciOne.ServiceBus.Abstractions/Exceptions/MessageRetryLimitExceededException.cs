using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to message retry limit exceeded.
/// </summary>
public class MessageRetryLimitExceededException :
    TransportException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public MessageRetryLimitExceededException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="uri">The uri value.</param>
    public MessageRetryLimitExceededException(Uri uri)
        : base(uri)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="uri">The uri value.</param>
    /// <param name="message">The message value.</param>
    public MessageRetryLimitExceededException(Uri uri, string message)
        : base(uri, message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="uri">The uri value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public MessageRetryLimitExceededException(Uri uri, string message, Exception innerException)
        : base(uri, message, innerException)
    {
    }
}
