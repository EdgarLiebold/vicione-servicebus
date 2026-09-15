using System.Net.Mime;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures the serializer content type for one sent message contract.</summary>
/// <typeparam name="TMessage">The sent message contract type.</typeparam>
public interface ISetSerializerMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    /// <summary>Sets the serializer content type.</summary>
    /// <param name="contentType">The registered serializer content type.</param>
    void SetSerializer(ContentType contentType);
}
