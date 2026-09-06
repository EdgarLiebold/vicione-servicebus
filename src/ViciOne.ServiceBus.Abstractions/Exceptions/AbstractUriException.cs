using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to abstract uri.
/// </summary>
public abstract class AbstractUriException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    protected AbstractUriException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="uri">The uri value.</param>
    protected AbstractUriException(Uri uri)
    {
        Uri = uri;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="uri">The uri value.</param>
    /// <param name="message">The message value.</param>
    protected AbstractUriException(Uri uri, string message)
        : base($"{uri} => {message}")
    {
        Uri = uri;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="uri">The uri value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    protected AbstractUriException(Uri uri, string message, Exception innerException)
        : base($"{uri} => {message}", innerException)
    {
        Uri = uri;
    }

    /// <summary>
    /// Gets or sets the uri value.
    /// </summary>
    public Uri? Uri { get; protected set; }
}
