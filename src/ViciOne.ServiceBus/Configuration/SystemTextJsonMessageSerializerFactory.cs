#nullable enable
namespace ViciOne.ServiceBus.Configuration
{
    using System;
    using System.Net.Mime;
    using System.Text.Json;
    using Serialization;


    public class SystemTextJsonMessageSerializerFactory :
        ISerializerFactory,
        IJsonSerializerFactory
    {
        readonly Lazy<SystemTextJsonMessageSerializer>? _serializer;

        public SystemTextJsonMessageSerializerFactory()
        {
        }

        SystemTextJsonMessageSerializerFactory(JsonSerializerOptions options)
        {
            _serializer = new Lazy<SystemTextJsonMessageSerializer>(() => new SystemTextJsonMessageSerializer(options));
        }

        public ContentType ContentType => SystemTextJsonMessageSerializer.JsonContentType;

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
            return new SystemTextJsonMessageSerializerFactory(options);
        }

        SystemTextJsonMessageSerializer GetSerializer()
        {
            return _serializer?.Value
                ?? throw new ConfigurationException("The System.Text.Json serializer factory must be bound to a serialization configuration before use.");
        }
    }
}
