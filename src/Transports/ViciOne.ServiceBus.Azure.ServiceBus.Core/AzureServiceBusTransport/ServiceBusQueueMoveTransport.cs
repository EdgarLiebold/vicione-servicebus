using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public class ServiceBusQueueMoveTransport
{
    readonly Recycle<ISendEndpointContextSupervisor> _sendEndpointContext;

    protected ServiceBusQueueMoveTransport(IConnectionContextSupervisor supervisor, SendSettings settings)
    {
        _sendEndpointContext = new Recycle<ISendEndpointContextSupervisor>(() => supervisor.CreateSendEndpointContextSupervisor(settings));
    }

    protected Task MoveAsync(ReceiveContext context, Action<ServiceBusMessage, SendHeaders> preSend)
    {
        IPipe<SendEndpointContext> clientPipe = Pipe.ExecuteAsync<SendEndpointContext>(async clientContext =>
        {
            if (!context.TryGetPayload(out ServiceBusMessageContext? messageContext))
                throw new ArgumentException("The ReceiveContext must contain a BrokeredMessageContext (from Azure Service Bus)", nameof(context));

            var body = context.GetBody();

            var message = new ServiceBusMessage(body)
            {
                ContentType = context.ContentType?.MediaType,
                TimeToLive = messageContext.TimeToLive,
                CorrelationId = messageContext.CorrelationId,
                MessageId = messageContext.MessageId,
                Subject = messageContext.Label,
                PartitionKey = messageContext.PartitionKey,
                ReplyTo = messageContext.ReplyTo
            };

            if (!string.IsNullOrWhiteSpace(messageContext.SessionId))
                message.SessionId = messageContext.SessionId;
            if (!string.IsNullOrWhiteSpace(messageContext.ReplyToSessionId))
                message.ReplyToSessionId = messageContext.ReplyToSessionId;

            foreach (KeyValuePair<string, object> property in messageContext.Properties.Where(x => !x.Key.StartsWith(MessageHeaders.Prefix, StringComparison.Ordinal)))
                message.ApplicationProperties.Set(new HeaderValue(property.Key, property.Value));

            var sendHeaders = new DictionarySendHeaders(message.ApplicationProperties, true);

            sendHeaders.SetHostHeaders();

            preSend(message, sendHeaders);

            await clientContext.SendAsync(message, clientContext.CancellationToken).ConfigureAwait(false);
        });

        return _sendEndpointContext.Supervisor.SendAsync(clientPipe, context.CancellationToken);
    }
}
