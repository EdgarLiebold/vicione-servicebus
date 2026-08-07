// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Mime;


    public interface ISerialization :
        IProbeSite
    {
        ContentType DefaultContentType { get; }

        IMessageSerializer GetMessageSerializer(ContentType? contentType = null);

        bool TryGetMessageSerializer(ContentType contentType, [NotNullWhen(true)] out IMessageSerializer? serializer);

        IMessageDeserializer GetMessageDeserializer(ContentType? contentType = null);

        bool TryGetMessageDeserializer(ContentType contentType, [NotNullWhen(true)] out IMessageDeserializer? deserializer);
    }
}
