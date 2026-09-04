using System.Net.Mime;

namespace ViciOne.ServiceBus;

public interface ISerializerFactory
{
    ContentType ContentType { get; }

    IMessageSerializer CreateSerializer();

    IMessageDeserializer CreateDeserializer();
}
