#nullable enable
namespace ViciOne.ServiceBus.Serialization
{
    using System;
    using System.Net.Mime;
    using System.Runtime.Serialization;
    using System.Text.Json;
    using Initializers;
    using Initializers.TypeConverters;


    public class SystemTextJsonMessageSerializer :
        IMessageDeserializer,
        IMessageSerializer,
        IObjectDeserializer
    {
        public static readonly ContentType JsonContentType = new ContentType("application/vnd.vicione.servicebus+json");

        readonly JsonSerializerOptions _options;

        static SystemTextJsonMessageSerializer()
        {
            GlobalTopology.MarkMessageTypeNotConsumable(typeof(JsonElement));
        }

        public SystemTextJsonMessageSerializer(JsonSerializerOptions options, ContentType? contentType = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (!_options.IsReadOnly)
                throw new ArgumentException("The serializer requires an immutable JsonSerializerOptions snapshot.", nameof(options));

            ContentType = contentType ?? JsonContentType;
        }

        public ContentType ContentType { get; }

        public void Probe(ProbeContext context)
        {
            var scope = context.CreateScope("json");
            scope.Add("contentType", ContentType.MediaType);
            scope.Add("provider", "System.Text.Json");
        }

        public ConsumeContext Deserialize(ReceiveContext receiveContext)
        {
            return new BodyConsumeContext(receiveContext, Deserialize(receiveContext.Body, receiveContext.TransportHeaders, receiveContext.InputAddress));
        }

        public SerializerContext Deserialize(MessageBody body, Headers headers, Uri? destinationAddress = null)
        {
            try
            {
                JsonElement? bodyElement = body is JsonMessageBody jsonMessageBody
                    ? jsonMessageBody.GetJsonElement(_options)
                    : JsonSerializer.Deserialize<JsonElement>(body.GetBytes(), _options);

                var envelope = bodyElement?.Deserialize<MessageEnvelope>(_options);
                if (envelope == null)
                    throw new SerializationException("Message envelope not found");

                var messageContext = new EnvelopeMessageContext(envelope, this);

                var messageTypes = envelope.MessageType ?? [];

                return new SystemTextJsonSerializerContext(this, _options, ContentType, messageContext, messageTypes, envelope);
            }
            catch (SerializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SerializationException("An error occured while deserializing the message envelope", ex);
            }
        }

        public MessageBody GetMessageBody(string text)
        {
            return new StringMessageBody(text);
        }

        public MessageBody GetMessageBody<T>(SendContext<T> context)
            where T : class
        {
            return new SystemTextJsonMessageBody<T>(context, _options);
        }

        public T? DeserializeObject<T>(object? value, T? defaultValue = default)
            where T : class
        {
            switch (value)
            {
                case null:
                    return defaultValue;
                case T returnValue:
                    return returnValue;
                case string text when string.IsNullOrWhiteSpace(text):
                    return defaultValue;
                case string text when TypeConverterCache.TryGetTypeConverter(out ITypeConverter<T, string>? typeConverter)
                    && typeConverter.TryConvert(text, out var result):
                    return result;
                case string text:
                    return JsonSerializer.Deserialize<JsonElement>(text, _options).GetObject<T>(_options);
                case JsonElement jsonElement:
                    return jsonElement.GetObject<T>(_options);
            }

            var element = JsonSerializer.SerializeToElement(value, _options);

            return element.ValueKind == JsonValueKind.Null
                ? defaultValue
                : element.GetObject<T>(_options);
        }

        public T? DeserializeObject<T>(object? value, T? defaultValue = null)
            where T : struct
        {
            switch (value)
            {
                case null:
                    return defaultValue;
                case T returnValue:
                    return returnValue;
                case string text when string.IsNullOrWhiteSpace(text):
                    return defaultValue;
                case string text when TypeConverterCache.TryGetTypeConverter(out ITypeConverter<T, string>? typeConverter)
                    && typeConverter.TryConvert(text, out var result):
                    return result;
                case string text:
                    return JsonSerializer.Deserialize<T>(text, _options);
                case JsonElement jsonElement:
                    return jsonElement.Deserialize<T>(_options);
            }

            var element = JsonSerializer.SerializeToElement(value, _options);

            return element.ValueKind == JsonValueKind.Null
                ? defaultValue
                : element.Deserialize<T>(_options);
        }

        public MessageBody SerializeObject(object? value)
        {
            if (value == null)
                return new EmptyMessageBody();

            return new SystemTextJsonObjectMessageBody(value, _options);
        }
    }
}
