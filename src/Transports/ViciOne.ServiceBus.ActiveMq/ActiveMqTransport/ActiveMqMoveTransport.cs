using System;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMq.Middleware;
using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Copies received Apache NMS messages to a configured ActiveMQ queue.</summary>
/// <typeparam name="TSettings">The topology settings used to provision the destination.</typeparam>
public class ActiveMqMoveTransport<TSettings>
    where TSettings : class
{
    readonly Queue _destination;
    readonly ConfigureActiveMqTopologyFilter<TSettings> _topologyFilter;

    /// <summary>Creates a message-move transport.</summary>
    /// <param name="destination">The queue that receives copied messages.</param>
    /// <param name="topologyFilter">The filter that provisions the destination topology.</param>
    protected ActiveMqMoveTransport(Queue destination, ConfigureActiveMqTopologyFilter<TSettings> topologyFilter)
    {
        _topologyFilter = topologyFilter;
        _destination = destination;
    }

    /// <summary>Copies the current native message and its transport properties to the destination queue.</summary>
    /// <param name="context">The received-message context.</param>
    /// <param name="preSend">A callback that applies move-specific headers before sending.</param>
    /// <returns>A task that completes when the copied message has been sent.</returns>
    protected async Task MoveAsync(ReceiveContext context, Action<IMessage, SendHeaders> preSend)
    {
        if (!context.TryGetPayload(out SessionContext? sessionContext))
            throw new ArgumentException("The ReceiveContext must contain a SessionContext", nameof(context));

        if (!context.TryGetPayload(out ActiveMqMessageContext? messageContext))
            throw new ArgumentException("The ActiveMqMessageContext was not present", nameof(context));

        OneTimeContext<ConfigureTopologyContext<TSettings>> oneTimeContext = await _topologyFilter.ConfigureAsync(sessionContext).ConfigureAwait(false);

        var queue = await sessionContext.GetQueueAsync(_destination).ConfigureAwait(false);

        var message = messageContext.TransportMessage switch
        {
            IBytesMessage _ => sessionContext.CreateBytesMessage(context.Body.GetBytes()),
            ITextMessage _ => sessionContext.CreateTextMessage(context.Body.GetString()),
            _ => sessionContext.CreateMessage(),
        };

        CloneMessage(message, messageContext.TransportMessage, preSend);

        try
        {
            await sessionContext.SendAsync(queue, message, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            oneTimeContext.Evict();
            throw;
        }
    }

    static void CloneMessage(IMessage message, IMessage source, Action<IMessage, SendHeaders> preSend)
    {
        message.NMSReplyTo = source.NMSReplyTo;
        message.NMSDeliveryMode = source.NMSDeliveryMode;
        message.NMSCorrelationID = source.NMSCorrelationID;
        message.NMSPriority = source.NMSPriority;

        foreach (string key in source.Properties.Keys)
            message.Properties[key] = source.Properties[key];

        SendHeaders headers = new PrimitiveMapHeaders(message.Properties);

        headers.SetHostHeaders();

        preSend(message, headers);
    }
}
