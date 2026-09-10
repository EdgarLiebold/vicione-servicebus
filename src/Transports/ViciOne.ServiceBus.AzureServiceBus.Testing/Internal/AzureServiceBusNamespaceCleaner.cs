using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Testing;

/// <summary>Deletes the queue and topic entities visible in an Azure Service Bus namespace.</summary>
internal static class AzureServiceBusNamespaceCleaner
{
    /// <summary>Enumerates the namespace once and deletes every entity returned by the service.</summary>
    /// <param name="client">The administration client for the namespace.</param>
    /// <param name="cancellationToken">The token that cancels enumeration and deletion.</param>
    /// <returns>The number of queues and topics deleted by this operation.</returns>
    internal static async Task<AzureServiceBusCleanupResult> CleanAsync(
        ServiceBusAdministrationClient client,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);

        var topicCount = 0;
        IList<TopicProperties> topics = await client
            .GetTopicsAsync(cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (TopicProperties topic in topics)
        {
            if (await DeleteTopicIfPresentAsync(client, topic.Name, cancellationToken).ConfigureAwait(false))
                topicCount++;
        }

        var queueCount = 0;
        IList<QueueProperties> queues = await client
            .GetQueuesAsync(cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (QueueProperties queue in queues)
        {
            if (await DeleteQueueIfPresentAsync(client, queue.Name, cancellationToken).ConfigureAwait(false))
                queueCount++;
        }

        return new AzureServiceBusCleanupResult(queueCount, topicCount);
    }

    static async Task<bool> DeleteTopicIfPresentAsync(
        ServiceBusAdministrationClient client,
        string name,
        CancellationToken cancellationToken)
    {
        try
        {
            await client.DeleteTopicAsync(name, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return false;
        }
    }

    static async Task<bool> DeleteQueueIfPresentAsync(
        ServiceBusAdministrationClient client,
        string name,
        CancellationToken cancellationToken)
    {
        try
        {
            await client.DeleteQueueAsync(name, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return false;
        }
    }
}

/// <summary>Reports the Azure Service Bus entities removed during namespace cleanup.</summary>
/// <param name="QueueCount">The number of deleted queues.</param>
/// <param name="TopicCount">The number of deleted topics.</param>
internal readonly record struct AzureServiceBusCleanupResult(int QueueCount, int TopicCount);
