using System.Net.Mime;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by set serializer message send topology convention.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface ISetSerializerMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    /// <summary>Sets serializer.</summary>
    /// <param name="contentType">The runtime content type used by the operation.</param>
    void SetSerializer(ContentType contentType);
}
