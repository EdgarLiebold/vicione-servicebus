using System;
using System.Collections.Generic;
using System.Diagnostics;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Represents a validated Amazon SQS queue or Amazon SNS topic address.</summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public readonly struct AmazonSqsEndpointAddress
{
    const string AutoDeleteKey = "autodelete";
    const string DurableKey = "durable";
    const string TemporaryKey = "temporary";
    const string TypeKey = "type";


    /// <summary>Identifies the kind of AWS messaging destination.</summary>
    public enum AddressType
    {
        /// <summary>Identifies an Amazon SQS queue.</summary>
        Queue = 0,
        /// <summary>Identifies an Amazon SNS topic.</summary>
        Topic = 1
    }


    static readonly ITypeConverter<AddressType, string> _parseConverter = new EnumTypeConverter<AddressType>();

    /// <summary>The Amazon SQS transport URI scheme.</summary>
    public readonly string Scheme;
    /// <summary>The AWS region host name.</summary>
    public readonly string Host;

    /// <summary>The optional logical entity-name scope.</summary>
    public readonly string? Scope;
    /// <summary>The queue or topic name.</summary>
    public readonly string Name;

    /// <summary>Whether the transport deletes the entity when its endpoint stops.</summary>
    public readonly bool AutoDelete;
    /// <summary>Whether the entity is retained after its endpoint stops.</summary>
    public readonly bool Durable;
    /// <summary>The kind of AWS messaging destination.</summary>
    public readonly AddressType Type;

    /// <summary>Parses a queue, topic, or absolute Amazon SQS endpoint address.</summary>
    /// <param name="hostAddress">The configured Amazon SQS host address used for relative addresses.</param>
    /// <param name="address">The destination address to parse.</param>
    /// <param name="type">The default destination kind when the address does not override it.</param>
    public AmazonSqsEndpointAddress(Uri hostAddress, Uri address, AddressType type = AddressType.Queue)
    {
        Durable = true;
        AutoDelete = false;
        Type = type;

        var scheme = address.Scheme.ToLowerInvariant();
        switch (scheme)
        {
            case AmazonSqsHostAddress.AmazonSqsScheme:
                Scheme = address.Scheme;
                Host = address.Host;

                address.ParseHostPathAndEntityName(out Scope, out Name);
                break;

            case "queue":
                ParseLeft(hostAddress, out Scheme, out Host, out Scope);

                Name = Uri.UnescapeDataString(address.AbsolutePath);
                break;

            case "topic":
                ParseLeft(hostAddress, out Scheme, out Host, out Scope);

                var topicName = Uri.UnescapeDataString(address.AbsolutePath);
                Name = Scope == "/" ? topicName : $"{Scope}_{topicName}";
                Type = AddressType.Topic;
                break;

            default:
                throw new ArgumentException($"The address scheme is not supported: {address.Scheme}", nameof(address));
        }

        var queryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in address.SplitQueryString())
        {
            if (!queryKeys.Add(key))
                throw new AmazonSqsTransportConfigurationException($"The endpoint address contains the option '{key}' more than once.");

            switch (key)
            {
                case TemporaryKey when TryParseBooleanOption(value, out var result):
                    AutoDelete = result;
                    Durable = !result;
                    break;

                case DurableKey when TryParseBooleanOption(value, out var result):
                    Durable = result;
                    break;

                case AutoDeleteKey when TryParseBooleanOption(value, out var result):
                    AutoDelete = result;
                    break;

                case TypeKey when value != null && _parseConverter.TryConvert(value, out var result):
                    Type = result;
                    break;

                case TemporaryKey:
                case DurableKey:
                case AutoDeleteKey:
                case TypeKey:
                    throw new AmazonSqsTransportConfigurationException($"The endpoint address option '{key}' has an invalid value '{value}'.");

                default:
                    throw new AmazonSqsTransportConfigurationException($"The endpoint address option '{key}' is not supported.");
            }
        }

        ValidateName(Name, Type);
    }

    /// <summary>Creates an AWS destination address from explicit entity settings.</summary>
    /// <param name="hostAddress">The configured Amazon SQS host address.</param>
    /// <param name="name">The queue or topic name.</param>
    /// <param name="durable">Whether the entity is retained after its endpoint stops.</param>
    /// <param name="autoDelete">Whether the transport deletes the entity when its endpoint stops.</param>
    /// <param name="type">The destination kind.</param>
    public AmazonSqsEndpointAddress(Uri hostAddress, string name, bool durable = true, bool autoDelete = false, AddressType type = AddressType.Queue)
    {
        ParseLeft(hostAddress, out Scheme, out Host, out Scope);

        Name = name;

        Durable = durable;
        AutoDelete = autoDelete;
        Type = type;

        ValidateName(Name, Type);
    }

    static void ParseLeft(Uri address, out string scheme, out string host, out string scope)
    {
        var hostAddress = new AmazonSqsHostAddress(address);
        scheme = hostAddress.Scheme;
        host = hostAddress.Host;
        scope = hostAddress.Scope;
    }

    /// <summary>Converts the destination address to its canonical absolute transport URI.</summary>
    /// <param name="address">The destination address.</param>
    /// <returns>An absolute URI containing the region, scope, entity name, and lifecycle options.</returns>
    public static implicit operator Uri(in AmazonSqsEndpointAddress address)
    {
        var builder = new UriBuilder
        {
            Scheme = address.Scheme,
            Host = address.Host,
            Path = address.Scope == "/" || address.Type == AddressType.Topic
                ? $"/{address.Name}"
                : $"/{address.Scope}/{address.Name}"
        };

        builder.Query += string.Join("&", address.GetQueryStringOptions());

        return builder.Uri;
    }

    /// <summary>Determines whether an AWS entity name uses the FIFO suffix.</summary>
    /// <param name="name">The queue or topic name.</param>
    /// <returns><see langword="true" /> when the name ends with <c>.fifo</c>, ignoring case; otherwise, <see langword="false" />.</returns>
    public static bool IsFifo(string name)
    {
        return name.EndsWith(".fifo", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Gets a relative Amazon SNS topic URI for this topic address and its lifecycle options.</summary>
    public Uri TopicAddress
    {
        get
        {
            if (Type != AddressType.Topic)
                throw new ArgumentException("Address was not a topic");

            var path = Scope == "/"
                ? $"{Name}"
                : $"{Scope}/{Name}";

            var builder = new UriBuilder($"topic:{path}");

            builder.Query += string.Join("&", GetQueryStringOptions());

            return builder.Uri;
        }
    }

    Uri DebuggerDisplay => this;

    IEnumerable<string> GetQueryStringOptions()
    {
        if (!Durable)
            yield return $"{DurableKey}=false";
        if (AutoDelete)
            yield return $"{AutoDeleteKey}=true";

        if (Type != AddressType.Queue)
            yield return $"{TypeKey}=topic";
    }

    static bool TryParseBooleanOption(string? value, out bool result)
    {
        if (value != null && bool.TryParse(value, out result))
            return true;

        result = default;
        return false;
    }

    static void ValidateName(string name, AddressType type)
    {
        if (type == AddressType.Queue)
            AmazonSqsEntityNameValidator.Validator.ThrowIfInvalidEntityName(name);
        else
            AmazonSnsTopicNameValidator.Validator.ThrowIfInvalidEntityName(name);
    }
}
