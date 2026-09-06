using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Exposes an Azure Service Bus sender for one entity.</summary>
public interface SendEndpointContext :
    NamespaceContext
{
    /// <summary>Gets the namespace-relative entity path.</summary>
    string EntityPath { get; }

    /// <summary>Sends a message immediately to the entity.</summary>
    /// <param name="message">The Azure Service Bus message to send.</param>
    /// <param name="cancellationToken">Cancels the broker send.</param>
    /// <returns>The Azure SDK send operation for <paramref name="message"/>.</returns>
    Task SendAsync(ServiceBusMessage message, CancellationToken cancellationToken);

    /// <summary>Schedules a message for future enqueue on the entity.</summary>
    /// <param name="message">The Azure Service Bus message to schedule.</param>
    /// <param name="scheduleEnqueueTimeUtc">The UTC instant at which the broker should enqueue the message.</param>
    /// <param name="cancellationToken">Cancels the scheduling request.</param>
    /// <returns>A task that produces the broker sequence number used for cancellation.</returns>
    Task<long> ScheduleSendAsync(ServiceBusMessage message, DateTimeOffset scheduleEnqueueTimeUtc, CancellationToken cancellationToken);

    /// <summary>Cancels a message previously scheduled on the entity.</summary>
    /// <param name="sequenceNumber">The broker sequence number returned when the message was scheduled.</param>
    /// <param name="cancellationToken">Cancels the cancellation request.</param>
    /// <returns>A task that completes when the broker processes the request.</returns>
    Task CancelScheduledSendAsync(long sequenceNumber, CancellationToken cancellationToken);
}
