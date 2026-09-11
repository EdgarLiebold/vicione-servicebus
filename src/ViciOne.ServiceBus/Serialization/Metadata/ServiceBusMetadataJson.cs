using System.Text.Json;

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

    /// <summary>Gets the immutable JSON options for ServiceBus-owned persisted metadata.</summary>
    public static JsonSerializerOptions Options { get; }
    /// <summary>Gets the stable codec for individual metadata values and dictionaries.</summary>
    public static IObjectDeserializer ObjectDeserializer => _serializer;
    /// <summary>Gets the stable envelope serializer used by infrastructure integrations.</summary>
    public static IMessageSerializer MessageSerializer => _serializer;
}
