using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a message send endpoint context implementation.
/// </summary>
public class MessageSendEndpointContext :
    BasePipeContext,
    SendEndpointContext
{
    readonly ServiceBusSender _client;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connectionContext">The connection context value.</param>
    /// <param name="client">The client value.</param>
    public MessageSendEndpointContext(ConnectionContext connectionContext, ServiceBusSender client)
    {
        _client = client;
        ConnectionContext = connectionContext;
    }

    /// <summary>
    /// Gets the connection context value.
    /// </summary>
    public ConnectionContext ConnectionContext { get; }

    /// <summary>
    /// Gets the entity path value.
    /// </summary>
    public string EntityPath => _client.EntityPath;

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(ServiceBusMessage message, CancellationToken cancellationToken)
    {
        return _client.SendMessageAsync(message, cancellationToken);
    }

    /// <summary>
    /// Schedules send.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="scheduleEnqueueTimeUtc">The schedule enqueue time utc value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<long> ScheduleSendAsync(ServiceBusMessage message, DateTimeOffset scheduleEnqueueTimeUtc, CancellationToken cancellationToken)
    {
        return _client.ScheduleMessageAsync(message, scheduleEnqueueTimeUtc, cancellationToken);
    }

    /// <summary>
    /// Determines whether the current value can cel scheduled send.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task CancelScheduledSendAsync(long sequenceNumber, CancellationToken cancellationToken)
    {
        return _client.CancelScheduledMessageAsync(sequenceNumber, cancellationToken);
    }
}
