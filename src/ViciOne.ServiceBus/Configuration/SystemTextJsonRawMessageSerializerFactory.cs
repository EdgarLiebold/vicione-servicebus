#nullable enable
namespace ViciOne.ServiceBus.Configuration
{
    using System;
    using System.Net.Mime;
    using System.Text.Json;
    using Serialization;


    public class SystemTextJsonRawMessageSerializerFactory :
        ISerializerFactory,
        IJsonSerializerFactory
    {
        readonly RawSerializerOptions _rawOptions;
        readonly Lazy<SystemTextJsonRawMessageSerializer>? _serializer;

        public SystemTextJsonRawMessageSerializerFactory(RawSerializerOptions options = RawSerializerOptions.Default)
        {
            _rawOptions = options;
        }

        SystemTextJsonRawMessageSerializerFactory(RawSerializerOptions rawOptions, JsonSerializerOptions options)
        {
            _rawOptions = rawOptions;
            _serializer = new Lazy<SystemTextJsonRawMessageSerializer>(() => new SystemTextJsonRawMessageSerializer(options, rawOptions));
        }

        public ContentType ContentType => SystemTextJsonRawMessageSerializer.JsonContentType;

        public IMessageSerializer CreateSerializer()
        {
            return GetSerializer();
        }

        public IMessageDeserializer CreateDeserializer()
        {
            return GetSerializer();
        }

        ISerializerFactory IJsonSerializerFactory.Bind(JsonSerializerOptions options)
        {
            return new SystemTextJsonRawMessageSerializerFactory(_rawOptions, options);
        }

        SystemTextJsonRawMessageSerializer GetSerializer()
        {
            return _serializer?.Value
                ?? throw new ConfigurationException("The raw System.Text.Json serializer factory must be bound to a serialization configuration before use.");
        }
    }
}
