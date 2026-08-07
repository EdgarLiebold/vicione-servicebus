// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Serialization
{
    using System.Net.Mime;


    public class NewtonsoftEncryptedSerializerFactory :
        ISerializerFactory
    {
        readonly ICryptoStreamProvider _provider;

        public NewtonsoftEncryptedSerializerFactory(ICryptoStreamProvider provider)
        {
            _provider = provider;
        }

        public ContentType ContentType => EncryptedMessageSerializer.EncryptedContentType;

        public IMessageSerializer CreateSerializer()
        {
            return new EncryptedMessageSerializer(_provider);
        }

        public IMessageDeserializer CreateDeserializer()
        {
            return new EncryptedMessageDeserializer(BsonMessageSerializer.Deserializer, _provider);
        }
    }
}
