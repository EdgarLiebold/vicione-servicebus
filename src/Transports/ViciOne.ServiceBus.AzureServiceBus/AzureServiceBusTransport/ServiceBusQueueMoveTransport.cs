using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Copies an Azure Service Bus delivery and its transport metadata to another entity.</summary>
public class ServiceBusQueueMoveTransport
{
    readonly Recycle<ISendEndpointContextSupervisor> _sendEndpointContext;

    /// <summary>Creates a move transport for a destination entity.</summary>
    /// <param name="supervisor">The namespace connection supervisor.</param>
    /// <param name="settings">The destination entity declaration and sender settings.</param>
    protected ServiceBusQueueMoveTransport(IConnectionContextSupervisor supervisor, SendSettings settings)
    {
        _sendEndpointContext = new Recycle<ISendEndpointContextSupervisor>(() => supervisor.CreateSendEndpointContextSupervisor(settings));
    }

    /// <summary>Copies the current delivery to the destination while preserving provider metadata.</summary>
    /// <param name="context">The source receive context.</param>
    /// <param name="preSend">Applies move-specific headers immediately before the copy is sent.</param>
    /// <returns>A task that completes when the destination sender accepts the copied message.</returns>
    protected Task MoveAsync(ReceiveContext context, Action<ServiceBusMessage, SendHeaders> preSend)
    {
        IPipe<SendEndpointContext> clientPipe = Pipe.ExecuteAwaited<SendEndpointContext>(async clientContext =>
        {
            if (!context.TryGetPayload(out ServiceBusMessageContext? messageContext))
                throw new ArgumentException("The ReceiveContext must contain a ServiceBusMessageContext (from Azure Service Bus)", nameof(context));

            ReadOnlyMemory<byte> body = context.GetBodyContent();

            var message = new ServiceBusMessage(BinaryData.FromBytes(body))
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

            var sendHeaders = DictionarySendHeaders.Wrap(message.ApplicationProperties);

            sendHeaders.SetHostHeaders();

            preSend(message, sendHeaders);

            await clientContext.SendAsync(message, clientContext.CancellationToken).ConfigureAwait(false);
        });

        return _sendEndpointContext.Supervisor.SendAsync(clientPipe, context.CancellationToken);
    }
}
