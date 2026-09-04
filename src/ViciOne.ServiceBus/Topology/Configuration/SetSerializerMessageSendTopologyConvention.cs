using System.Net.Mime;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a set serializer message send topology convention implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class SetSerializerMessageSendTopologyConvention<TMessage> :
    ISetSerializerMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    ContentType? _contentType;

    bool IMessageSendTopologyConvention.TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
    {
        convention = this as IMessageSendTopologyConvention<T>;

        return convention != null;
    }

    bool IMessageSendTopologyConvention<TMessage>.TryGetMessageSendTopology(
        [NotNullWhen(true)] out IMessageSendTopology<TMessage>? messageSendTopology)
    {
        if (_contentType != null)
        {
            messageSendTopology = new SetSerializerMessageSendTopology<TMessage>(_contentType);
            return true;
        }

        messageSendTopology = null;
        return false;
    }

    /// <summary>
    /// Sets serializer.
    /// </summary>
    /// <param name="contentType">The content type value.</param>
    public void SetSerializer(ContentType contentType)
    {
        _contentType = contentType;
    }
}
