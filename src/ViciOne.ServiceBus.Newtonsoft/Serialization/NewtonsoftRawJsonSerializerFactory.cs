// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Serialization
{
    using System.Net.Mime;


    public class NewtonsoftRawJsonSerializerFactory :
        ISerializerFactory
    {
        readonly RawSerializerOptions _options;

        public NewtonsoftRawJsonSerializerFactory(RawSerializerOptions options)
        {
            _options = options;
        }

        public ContentType ContentType => NewtonsoftRawJsonMessageSerializer.RawJsonContentType;

        public IMessageSerializer CreateSerializer()
        {
            return new NewtonsoftRawJsonMessageSerializer(_options);
        }

        public IMessageDeserializer CreateDeserializer()
        {
            return new NewtonsoftRawJsonMessageDeserializer(NewtonsoftRawJsonMessageSerializer.Deserializer, _options);
        }
    }
}
