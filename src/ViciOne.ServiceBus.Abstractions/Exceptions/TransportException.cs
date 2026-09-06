using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to transport.
/// </summary>
public class TransportException :
    AbstractUriException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public TransportException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="uri">The uri value.</param>
    public TransportException(Uri uri)
        : base(uri)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="uri">The uri value.</param>
    /// <param name="message">The message value.</param>
    public TransportException(Uri uri, string message)
        : base(uri, message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="uri">The uri value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public TransportException(Uri uri, string message, Exception innerException)
        : base(uri, message, innerException)
    {
    }
}
