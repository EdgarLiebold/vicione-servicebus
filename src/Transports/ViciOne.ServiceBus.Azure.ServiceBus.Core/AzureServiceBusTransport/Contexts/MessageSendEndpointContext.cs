using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public class MessageSendEndpointContext :
    BasePipeContext,
    SendEndpointContext
{
    readonly ServiceBusSender _client;

    public MessageSendEndpointContext(ConnectionContext connectionContext, ServiceBusSender client)
    {
        _client = client;
        ConnectionContext = connectionContext;
    }

    public ConnectionContext ConnectionContext { get; }

    public string EntityPath => _client.EntityPath;

    public Task SendAsync(ServiceBusMessage message, CancellationToken cancellationToken)
    {
        return _client.SendMessageAsync(message, cancellationToken);
    }

    public Task<long> ScheduleSendAsync(ServiceBusMessage message, DateTimeOffset scheduleEnqueueTimeUtc, CancellationToken cancellationToken)
    {
        return _client.ScheduleMessageAsync(message, scheduleEnqueueTimeUtc, cancellationToken);
    }

    public Task CancelScheduledSendAsync(long sequenceNumber, CancellationToken cancellationToken)
    {
        return _client.CancelScheduledMessageAsync(sequenceNumber, cancellationToken);
    }
}
