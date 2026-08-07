// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
using NUnit.Framework;

[assembly: Parallelizable(ParallelScope.None)]
[assembly: LevelOfParallelism(1)]


namespace ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests
{
    using System;
    using System.Threading.Tasks;
    using Internals;


    [SetUpFixture]
    public class AzureServiceBusTestSetUpFixture
    {
        [OneTimeSetUp]
        public async Task Before_any()
        {
            await CleanupNamespace();
        }

        async Task CleanupNamespace()
        {
            try
            {
                var managementClient = Configuration.GetManagementClient();

                var pageableTopics = managementClient.GetTopicsAsync();
                var topics = await pageableTopics.ToListAsync();
                while (topics.Count > 0)
                {
                    foreach (var topic in topics)
                        await managementClient.DeleteTopicAsync(topic.Name);

                    topics = await managementClient.GetTopicsAsync().ToListAsync();
                }

                var pageableQueues = managementClient.GetQueuesAsync();
                var queues = await pageableQueues.ToListAsync();
                while (queues.Count > 0)
                {
                    foreach (var queue in queues)
                        await managementClient.DeleteQueueAsync(queue.Name);

                    queues = await managementClient.GetQueuesAsync().ToListAsync();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}
