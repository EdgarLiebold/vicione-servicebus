using System.Net.Mime;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for set serializer message send topology convention.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface ISetSerializerMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Sets serializer.
    /// </summary>
    /// <param name="contentType">The content type value.</param>
    void SetSerializer(ContentType contentType);
}
