using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Optionally cleans the configured Azure Service Bus namespace when the host starts.</summary>
public class AzureServiceBusTestHarnessHostedService :
    IHostedService
{
    readonly ILogger<AzureServiceBusTestHarnessHostedService> _logger;
    readonly AzureServiceBusTestHarnessOptions _testOptions;
    readonly AzureServiceBusTransportOptions _transportOptions;

    /// <summary>Initializes the hosted service from transport and test-harness options.</summary>
    /// <param name="transportOptions">The options containing the namespace connection string.</param>
    /// <param name="testOptions">The options that control startup cleanup.</param>
    /// <param name="logger">The logger used to report deleted entities.</param>
    public AzureServiceBusTestHarnessHostedService(IOptions<AzureServiceBusTransportOptions> transportOptions,
        IOptions<AzureServiceBusTestHarnessOptions> testOptions, ILogger<AzureServiceBusTestHarnessHostedService> logger)
    {
        _logger = logger;
        _transportOptions = transportOptions.Value;
        _testOptions = testOptions.Value;
    }

    /// <summary>Cleans the namespace when startup cleanup is enabled.</summary>
    /// <param name="cancellationToken">The host-start cancellation token, checked before cleanup begins.</param>
    /// <returns>A task that completes after optional namespace cleanup.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested(); if (_testOptions.CleanNamespace)
            await CleanAsync();
    }

    /// <summary>Completes immediately because the hosted service owns no running background operation.</summary>
    /// <param name="cancellationToken">The host-stop cancellation token.</param>
    /// <returns>A completed task, or a canceled task when cancellation was already requested.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    async Task CleanAsync()
    {
        var managementClient = new ServiceBusAdministrationClient(_transportOptions.ConnectionString);

        var topicCount = 0;
        var queueCount = 0;

        AsyncPageable<TopicProperties> pageableTopics = managementClient.GetTopicsAsync();
        IList<TopicProperties> topics = await pageableTopics.ToListAsync();
        while (topics.Count > 0)
        {
            foreach (var topic in topics)
            {
                await managementClient.DeleteTopicAsync(topic.Name);
                topicCount++;
            }

            topics = await managementClient.GetTopicsAsync().ToListAsync();
        }

        AsyncPageable<QueueProperties> pageableQueues = managementClient.GetQueuesAsync();
        IList<QueueProperties> queues = await pageableQueues.ToListAsync();
        while (queues.Count > 0)
        {
            foreach (var queue in queues)
            {
                await managementClient.DeleteQueueAsync(queue.Name);
                queueCount++;
            }

            queues = await managementClient.GetQueuesAsync().ToListAsync();
        }

        if (topicCount > 0 || queueCount > 0)
            _logger.LogInformation("Removed {QueueCount} queue(s), {TopicCount} topics(s)", queueCount, topicCount);
    }
}
