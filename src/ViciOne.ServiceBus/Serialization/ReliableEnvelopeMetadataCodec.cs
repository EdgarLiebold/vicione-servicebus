using System.Runtime.Serialization;
using System.Text.Json;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>
/// Encodes and restores the bounded ServiceBus-owned part of a durable envelope. Payload bytes and stable contract
/// identity remain separate; this codec owns headers, transport properties and request/routing addresses only.
/// </summary>
public static class ReliableEnvelopeMetadataCodec
{
    const int CurrentVersion = 1;

    /// <summary>Snapshots infrastructure metadata before a send context crosses the reliable-store boundary.</summary>
    public static ReadOnlyMemory<byte> Capture<T>(SendContext<T> context, DateTimeOffset capturedAt)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        var properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (context is TransportSendContext<T> transportContext)
        {
            transportContext.WritePropertiesTo(properties);
            // DueAt is persisted by the reliable outbox itself. Reapplying transport Delay would schedule twice.
            properties.Remove("Delay");
        }

        string? headers = ServiceBusMetadataJson.ObjectDeserializer.SerializeDictionary(context.Headers.GetAll());
        string? serializedProperties = properties.Count == 0
            ? null
            : ServiceBusMetadataJson.ObjectDeserializer.SerializeDictionary(properties);
        var document = new MetadataDocument
        {
            Version = CurrentVersion,
            RequestId = context.RequestId,
            ConversationId = context.ConversationId,
            InitiatorId = context.InitiatorId,
            SourceAddress = context.SourceAddress?.AbsoluteUri,
            ResponseAddress = context.ResponseAddress?.AbsoluteUri,
            FaultAddress = context.FaultAddress?.AbsoluteUri,
            SentTime = context.SentTime ?? capturedAt,
            ExpirationTime = context.TimeToLive.HasValue ? capturedAt + context.TimeToLive.Value : null,
            Headers = headers,
            Properties = serializedProperties,
        };
        return JsonSerializer.SerializeToUtf8Bytes(document, ServiceBusMetadataJson.Options);
    }

    /// <summary>Restores validated infrastructure metadata onto a transport-owned replay context.</summary>
    public static void Apply(SendContext context, ReadOnlyMemory<byte> metadata, DateTimeOffset replayedAt)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (metadata.IsEmpty)
            return;

        MetadataDocument document = JsonSerializer.Deserialize<MetadataDocument>(
            metadata.Span,
            ServiceBusMetadataJson.Options)
            ?? throw new SerializationException("Reliable envelope metadata was empty after deserialization.");
        if (document.Version != CurrentVersion)
        {
            throw new SerializationException(
                $"Reliable envelope metadata version '{document.Version}' is unsupported; expected '{CurrentVersion}'.");
        }

        context.RequestId = document.RequestId;
        context.ConversationId = document.ConversationId;
        context.InitiatorId = document.InitiatorId;
        context.SourceAddress = ParseAddress(document.SourceAddress, nameof(document.SourceAddress));
        context.ResponseAddress = ParseAddress(document.ResponseAddress, nameof(document.ResponseAddress));
        context.FaultAddress = ParseAddress(document.FaultAddress, nameof(document.FaultAddress));
        if (document.ExpirationTime.HasValue)
        {
            TimeSpan remaining = document.ExpirationTime.Value - replayedAt;
            context.TimeToLive = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        Dictionary<string, object?>? headers = ServiceBusMetadataJson.ObjectDeserializer
            .DeserializeDictionary<object?>(document.Headers);
        if (headers is not null)
        {
            foreach ((string key, object? value) in headers)
            {
                if (value is not null)
                    context.Headers.Set(key, value);
            }
        }

        Dictionary<string, object>? properties = ServiceBusMetadataJson.ObjectDeserializer
            .DeserializeDictionary<object>(document.Properties);
        if (properties is not null && context is TransportSendContext transportContext)
            transportContext.ReadPropertiesFrom(properties);
    }

    static Uri? ParseAddress(string? value, string property)
    {
        if (value is null)
            return null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? address))
            throw new SerializationException($"Reliable envelope metadata property '{property}' is not an absolute URI.");
        return address;
    }

    sealed class MetadataDocument
    {
        public int Version { get; set; }

        public Guid? RequestId { get; set; }

        public Guid? ConversationId { get; set; }

        public Guid? InitiatorId { get; set; }

        public string? SourceAddress { get; set; }

        public string? ResponseAddress { get; set; }

        public string? FaultAddress { get; set; }

        public DateTimeOffset? SentTime { get; set; }

        public DateTimeOffset? ExpirationTime { get; set; }

        public string? Headers { get; set; }

        public string? Properties { get; set; }
    }
}
