using System;

namespace ViciOne.ServiceBus;

/// <summary>Provides an exception base that retains the endpoint address associated with a failure.</summary>
public abstract class AbstractUriException :
    ViciOneServiceBusException
{
    /// <summary>Creates an exception without an endpoint address.</summary>
    protected AbstractUriException()
    {
    }

    /// <summary>Creates an exception for the specified endpoint address.</summary>
    /// <param name="uri">The endpoint address associated with the failure.</param>
    protected AbstractUriException(Uri uri)
    {
        Uri = uri ?? throw new ArgumentNullException(nameof(uri));
    }

    /// <summary>Creates an exception for the specified endpoint address and failure message.</summary>
    /// <param name="uri">The endpoint address associated with the failure.</param>
    /// <param name="message">The description of the failure.</param>
    protected AbstractUriException(Uri uri, string message)
        : base(FormatMessage(uri, message))
    {
        Uri = uri;
    }

    /// <summary>Creates an exception for the specified endpoint address and underlying failure.</summary>
    /// <param name="uri">The endpoint address associated with the failure.</param>
    /// <param name="message">The description of the failure.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    protected AbstractUriException(Uri uri, string message, Exception innerException)
        : base(FormatMessage(uri, message), innerException)
    {
        Uri = uri;
    }

    /// <summary>Gets the endpoint address associated with the failure, when one was supplied.</summary>
    public Uri? Uri { get; }

    static string FormatMessage(Uri uri, string message)
    {
        ArgumentNullException.ThrowIfNull(uri);

        return $"{uri} => {message}";
    }
}
