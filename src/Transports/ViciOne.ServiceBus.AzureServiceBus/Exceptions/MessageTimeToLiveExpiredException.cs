using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Represents an error related to message time to live expired.
/// </summary>
[Serializable]
public class MessageTimeToLiveExpiredException :
    TransportException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public MessageTimeToLiveExpiredException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="uri">The uri value.</param>
    public MessageTimeToLiveExpiredException(Uri uri)
        : base(uri)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="uri">The uri value.</param>
    /// <param name="message">The message value.</param>
    public MessageTimeToLiveExpiredException(Uri uri, string message)
        : base(uri, message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="uri">The uri value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public MessageTimeToLiveExpiredException(Uri uri, string message, Exception innerException)
        : base(uri, message, innerException)
    {
    }
}
