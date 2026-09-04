using System.Text.Json;

#nullable enable
namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Stable, immutable JSON codec for ViciOne ServiceBus-owned infrastructure data such as persisted headers,
/// transport properties, scheduler data and repository metadata. Message payload configuration must never mutate
/// this codec, because doing so could make already persisted infrastructure state unreadable.
/// </summary>
public static class ServiceBusMetadataJson
{
    static readonly SystemTextJsonMessageSerializer _serializer;

    static ServiceBusMetadataJson()
    {
        Options = SystemTextJsonSerializerOptions.Freeze(SystemTextJsonSerializerOptions.CreateDefault());
        _serializer = new SystemTextJsonMessageSerializer(Options);
    }

    public static JsonSerializerOptions Options { get; }
    public static IObjectDeserializer ObjectDeserializer => _serializer;
    public static IMessageSerializer MessageSerializer => _serializer;
}
