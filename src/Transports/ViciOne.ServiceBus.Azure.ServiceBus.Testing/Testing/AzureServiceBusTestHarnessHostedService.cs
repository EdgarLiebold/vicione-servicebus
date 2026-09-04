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

public class AzureServiceBusTestHarnessHostedService :
    IHostedService
{
    readonly ILogger<AzureServiceBusTestHarnessHostedService> _logger;
    readonly AzureServiceBusTestHarnessOptions _testOptions;
    readonly AzureServiceBusTransportOptions _transportOptions;

    public AzureServiceBusTestHarnessHostedService(IOptions<AzureServiceBusTransportOptions> transportOptions,
        IOptions<AzureServiceBusTestHarnessOptions> testOptions, ILogger<AzureServiceBusTestHarnessHostedService> logger)
    {
        _logger = logger;
        _transportOptions = transportOptions.Value;
        _testOptions = testOptions.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested(); if (_testOptions.CleanNamespace)
            await CleanAsync();
    }

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
