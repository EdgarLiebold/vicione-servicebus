using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for send endpoint context.
/// </summary>
public interface SendEndpointContext :
    NamespaceContext
{
    /// <summary>
    /// The path of the messaging entity
    /// </summary>
    string EntityPath { get; }

    /// <summary>
    /// Send the message to the messaging entity
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SendAsync(ServiceBusMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Schedule a send in the future to the messaging entity
    /// </summary>
    /// <param name="message"></param>
    /// <param name="scheduleEnqueueTimeUtc"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<long> ScheduleSendAsync(ServiceBusMessage message, DateTimeOffset scheduleEnqueueTimeUtc, CancellationToken cancellationToken);

    /// <summary>
    /// Cancel a previously schedule send on the messaging entity
    /// </summary>
    /// <param name="sequenceNumber"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task CancelScheduledSendAsync(long sequenceNumber, CancellationToken cancellationToken);
}
