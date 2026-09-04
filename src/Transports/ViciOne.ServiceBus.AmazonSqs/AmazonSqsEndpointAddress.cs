using System;
using System.Collections.Generic;
using System.Diagnostics;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Represents an amazon sqs endpoint address value.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public readonly struct AmazonSqsEndpointAddress
{
    const string AutoDeleteKey = "autodelete";
    const string DurableKey = "durable";
    const string TemporaryKey = "temporary";
    const string TypeKey = "type";


    /// <summary>
    /// Specifies the available address type values.
    /// </summary>
    public enum AddressType
    {
        /// <summary>
        /// Indicates queue.
        /// </summary>
        Queue = 0,
        /// <summary>
        /// Indicates topic.
        /// </summary>
        Topic = 1
    }


    static readonly ITypeConverter<AddressType, string> _parseConverter = new EnumTypeConverter<AddressType>();

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
    public readonly string? Scope;
    /// <summary>
    /// Defines the name value.
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// Defines the auto delete value.
    /// </summary>
    public readonly bool AutoDelete;
    /// <summary>
    /// Defines the durable value.
    /// </summary>
    public readonly bool Durable;
    /// <summary>
    /// Defines the type value.
    /// </summary>
    public readonly AddressType Type;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <param name="address">The address value.</param>
    /// <param name="type">The type value.</param>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <param name="name">The name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    /// <param name="type">The type value.</param>
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

    /// <summary>
    /// Converts a value to <see cref="Uri" />.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Determines whether fifo.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsFifo(string name)
    {
        return name.EndsWith(".fifo", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the topic address value.
    /// </summary>
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
