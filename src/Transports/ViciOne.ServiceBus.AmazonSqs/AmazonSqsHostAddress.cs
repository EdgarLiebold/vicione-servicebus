using System;
using System.Diagnostics;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Represents a validated Amazon SQS region and logical entity scope.</summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public readonly struct AmazonSqsHostAddress
{
    /// <summary>The URI scheme for the Amazon SQS transport.</summary>
    public const string AmazonSqsScheme = "amazonsqs";

    /// <summary>The transport URI scheme.</summary>
    public readonly string Scheme;
    /// <summary>The AWS region host name.</summary>
    public readonly string Host;
    /// <summary>The logical entity-name scope.</summary>
    public readonly string Scope;

    /// <summary>Parses and validates an absolute Amazon SQS host address.</summary>
    /// <param name="address">The absolute transport host URI.</param>
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

    /// <summary>Creates an Amazon SQS host address from a region host name and optional scope.</summary>
    /// <param name="host">The AWS region host name.</param>
    /// <param name="scope">The logical entity-name scope, or <see langword="null" /> for the root scope.</param>
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

    /// <summary>Converts the host address to its canonical absolute transport URI.</summary>
    /// <param name="address">The Amazon SQS host address.</param>
    /// <returns>An absolute URI containing the region host and logical scope.</returns>
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
