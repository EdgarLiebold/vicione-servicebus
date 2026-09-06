using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to message not consumed.</summary>
public class MessageNotConsumedException :
    TransportException
{
    /// <summary>Initializes a new instance.</summary>
    public MessageNotConsumedException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="uri">The uri.</param>
    public MessageNotConsumedException(Uri uri)
        : base(uri)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="uri">The uri.</param>
    /// <param name="message">The message to process.</param>
    public MessageNotConsumedException(Uri uri, string message)
        : base(uri, message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="uri">The uri.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public MessageNotConsumedException(Uri uri, string message, Exception innerException)
        : base(uri, message, innerException)
    {
    }
}
