// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Serialization
{
    using System.Net.Mime;


    public class NewtonsoftRawXmlSerializerFactory :
        ISerializerFactory
    {
        readonly RawSerializerOptions _options;

        public NewtonsoftRawXmlSerializerFactory(RawSerializerOptions options)
        {
            _options = options;
        }

        public ContentType ContentType => RawXmlMessageSerializer.RawXmlContentType;

        public IMessageSerializer CreateSerializer()
        {
            return new RawXmlMessageSerializer(_options);
        }

        public IMessageDeserializer CreateDeserializer()
        {
            return new NewtonsoftRawXmlMessageDeserializer(NewtonsoftRawJsonMessageSerializer.Deserializer, _options);
        }
    }
}
