using System;
using System.Diagnostics;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Represents an amazon sqs host address value.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public readonly struct AmazonSqsHostAddress
{
    /// <summary>
    /// Defines the amazon sqs scheme value.
    /// </summary>
    public const string AmazonSqsScheme = "amazonsqs";

    /// <summary>
    /// Defines the scheme value.
    /// </summary>
    public readonly string Scheme;
    /// <summary>
    /// Defines the host value.
    /// </summary>
    public readonly string Host;
    /// <summary>
    /// Defines the scope value.
    /// </summary>
    public readonly string Scope;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    public AmazonSqsHostAddress(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (!address.IsAbsoluteUri)
            throw new AmazonSqsTransportConfigurationException("The Amazon SQS host URI must be absolute.");
        if (string.IsNullOrWhiteSpace(address.Host))
            throw new AmazonSqsTransportConfigurationException("The Amazon SQS host must be specified.");
        if (!string.IsNullOrEmpty(address.UserInfo))
            throw new AmazonSqsTransportConfigurationException("Credentials must not be embedded in an Amazon SQS host URI.");
        if (!string.IsNullOrEmpty(address.Query))
            throw new AmazonSqsTransportConfigurationException("Query parameters are not supported in an Amazon SQS host URI.");
        if (!string.IsNullOrEmpty(address.Fragment))
            throw new AmazonSqsTransportConfigurationException("Fragments are not supported in an Amazon SQS host URI.");

        var scheme = address.Scheme.ToLowerInvariant();
        switch (scheme)
        {
            case AmazonSqsScheme:
                Scheme = address.Scheme;
                Host = address.Host;

                ParseLeft(address, out Scheme, out Host, out Scope);
                break;

            default:
                throw new ArgumentException($"The address scheme is not supported: {address.Scheme}", nameof(address));
        }
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="host">The host value.</param>
    /// <param name="scope">The scope value.</param>
    public AmazonSqsHostAddress(string host, string? scope)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new AmazonSqsTransportConfigurationException("The Amazon SQS host must be specified.");

        Scheme = AmazonSqsScheme;
        Host = host;
        Scope = scope ?? "/";
    }

    static void ParseLeft(Uri address, out string scheme, out string host, out string scope)
    {
        scheme = address.Scheme;
        host = address.Host;

        scope = address.ParseHostPath();
    }

    /// <summary>
    /// Converts a value to <see cref="Uri" />.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    public static implicit operator Uri(in AmazonSqsHostAddress address)
    {
        var builder = new UriBuilder
        {
            Scheme = address.Scheme,
            Host = address.Host,
            Path = address.Scope == "/"
                ? "/"
                : $"/{Uri.EscapeDataString(address.Scope)}"
        };

        return builder.Uri;
    }

    Uri DebuggerDisplay => this;
}
