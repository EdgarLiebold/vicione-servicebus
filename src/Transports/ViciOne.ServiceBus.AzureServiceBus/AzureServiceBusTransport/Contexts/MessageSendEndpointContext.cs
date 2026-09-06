using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Wraps an Azure Service Bus sender as a supervised send-endpoint context.</summary>
public class MessageSendEndpointContext :
    BasePipeContext,
    SendEndpointContext
{
    readonly ServiceBusSender _client;

    /// <summary>Initializes the endpoint context for a sender and its owning namespace connection.</summary>
    /// <param name="connectionContext">The namespace connection that created the sender.</param>
    /// <param name="client">The Azure Service Bus sender.</param>
    public MessageSendEndpointContext(ConnectionContext connectionContext, ServiceBusSender client)
    {
        _client = client;
        ConnectionContext = connectionContext;
    }

    /// <summary>Gets the owning namespace connection.</summary>
    public ConnectionContext ConnectionContext { get; }

    /// <summary>Gets the sender's destination entity path.</summary>
    public string EntityPath => _client.EntityPath;

    /// <summary>Sends a message to the sender's queue or topic.</summary>
    /// <param name="message">The Azure Service Bus message to send.</param>
    /// <param name="cancellationToken">The token that cancels the send.</param>
    /// <returns>A task that completes when the SDK send completes.</returns>
    public Task SendAsync(ServiceBusMessage message, CancellationToken cancellationToken)
    {
        return _client.SendMessageAsync(message, cancellationToken);
    }

    /// <summary>Schedules a message for future enqueue on the destination.</summary>
    /// <param name="message">The Azure Service Bus message to schedule.</param>
    /// <param name="scheduleEnqueueTimeUtc">The UTC enqueue time.</param>
    /// <param name="cancellationToken">The token that cancels the scheduling request.</param>
    /// <returns>A task that produces the broker sequence number used to cancel the scheduled message.</returns>
    public Task<long> ScheduleSendAsync(ServiceBusMessage message, DateTimeOffset scheduleEnqueueTimeUtc, CancellationToken cancellationToken)
    {
        return _client.ScheduleMessageAsync(message, scheduleEnqueueTimeUtc, cancellationToken);
    }

    /// <summary>Cancels a previously scheduled message.</summary>
    /// <param name="sequenceNumber">The broker sequence number returned when the message was scheduled.</param>
    /// <param name="cancellationToken">The token that cancels the cancellation request.</param>
    /// <returns>A task that completes when the broker accepts the cancellation.</returns>
    public Task CancelScheduledSendAsync(long sequenceNumber, CancellationToken cancellationToken)
    {
        return _client.CancelScheduledMessageAsync(sequenceNumber, cancellationToken);
    }
}
