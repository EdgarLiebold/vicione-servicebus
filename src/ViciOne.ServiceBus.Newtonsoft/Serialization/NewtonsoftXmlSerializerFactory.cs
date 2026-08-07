// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Serialization
{
    using System.Net.Mime;


    public class NewtonsoftXmlSerializerFactory :
        ISerializerFactory
    {
        public ContentType ContentType => NewtonsoftXmlMessageSerializer.XmlContentType;

        public IMessageSerializer CreateSerializer()
        {
            return new NewtonsoftXmlMessageSerializer();
        }

        public IMessageDeserializer CreateDeserializer()
        {
            return new NewtonsoftXmlMessageDeserializer(NewtonsoftXmlJsonMessageSerializer.Deserializer);
        }
    }
}
