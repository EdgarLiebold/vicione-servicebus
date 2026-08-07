// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Serialization
{
    using System.Net.Mime;


    public class NServiceBusXmlSerializerFactory :
        ISerializerFactory
    {
        public ContentType ContentType => NServiceBusXmlMessageSerializer.XmlContentType;

        public IMessageSerializer CreateSerializer()
        {
            return new NServiceBusXmlMessageSerializer();
        }

        public IMessageDeserializer CreateDeserializer()
        {
            return new NServiceBusXmlMessageDeserializer(NewtonsoftJsonMessageSerializer.Deserializer);
        }
    }
}
