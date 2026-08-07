// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.Net.Mime;


    public interface ISerializerFactory
    {
        ContentType ContentType { get; }

        IMessageSerializer CreateSerializer();

        IMessageDeserializer CreateDeserializer();
    }
}
