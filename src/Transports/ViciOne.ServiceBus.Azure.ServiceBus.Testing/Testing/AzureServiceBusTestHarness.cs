using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.Testing;

public class AzureServiceBusTestHarness :
    BusTestHarness
{
    Uri? _inputQueueAddress;

    public AzureServiceBusTestHarness(Uri serviceUri, AzureNamedKeyCredential namedKeyCredential, string? inputQueueName = null)
    {
        if (serviceUri == null)
            throw new ArgumentNullException(nameof(serviceUri));

        HostAddress = serviceUri;
        NamedKeyCredential = namedKeyCredential;

        InputQueueName = inputQueueName ?? "input_queue";

        ConfigureMessageScheduler = true;
    }

    public AzureNamedKeyCredential NamedKeyCredential { get; }
    public override string InputQueueName { get; }
    public bool ConfigureMessageScheduler { get; set; }

    public override Uri InputQueueAddress => _inputQueueAddress
        ?? throw new InvalidOperationException("The input queue address is not available before the bus has been created.");
    public Uri HostAddress { get; }

    public event Action<IServiceBusBusFactoryConfigurator>? OnConfigureServiceBusBus;
    public event Action<IServiceBusReceiveEndpointConfigurator>? OnConfigureServiceBusReceiveEndpoint;

    protected virtual void ConfigureServiceBusBus(IServiceBusBusFactoryConfigurator configurator)
    {
        OnConfigureServiceBusBus?.Invoke(configurator);
    }

    protected virtual void ConfigureServiceBusReceiveEndpoint(IServiceBusReceiveEndpointConfigurator configurator)
    {
        OnConfigureServiceBusReceiveEndpoint?.Invoke(configurator);
    }

    public override async Task CleanAsync(CancellationToken cancellationToken = default)
    {
        var managementClient = CreateManagementClient();

        AsyncPageable<TopicProperties> pageableTopics = managementClient.GetTopicsAsync(cancellationToken: cancellationToken);
        IList<TopicProperties> topics = await pageableTopics.ToListAsync(cancellationToken: cancellationToken);
        while (topics.Count > 0)
        {
            foreach (var topic in topics)
                await managementClient.DeleteTopicAsync(topic.Name, cancellationToken: cancellationToken);

            await Task.Delay(500, cancellationToken);

            topics = await managementClient.GetTopicsAsync(cancellationToken: cancellationToken).ToListAsync(cancellationToken: cancellationToken);
        }

        AsyncPageable<QueueProperties> pageableQueues = managementClient.GetQueuesAsync(cancellationToken: cancellationToken);
        IList<QueueProperties> queues = await pageableQueues.ToListAsync(cancellationToken: cancellationToken);
        while (queues.Count > 0)
        {
            foreach (var queue in queues)
                await managementClient.DeleteQueueAsync(queue.Name, cancellationToken: cancellationToken);

            await Task.Delay(500, cancellationToken);

            queues = await managementClient.GetQueuesAsync(cancellationToken: cancellationToken).ToListAsync(cancellationToken: cancellationToken);
        }
    }

    ServiceBusAdministrationClient CreateManagementClient()
    {
        var endpoint = new UriBuilder(HostAddress) { Path = "" }.Uri.ToString();

        return new ServiceBusAdministrationClient(endpoint, NamedKeyCredential);
    }

    protected override async Task<IBusControl> CreateBusAsync()
    {
        return ViciOne.ServiceBus.Bus.Factory.CreateUsingAzureServiceBus(x =>
        {
            x.Host(HostAddress, h =>
            {
                h.NamedKey(s =>
                {
                    s.NamedKeyCredential = NamedKeyCredential;
                });
            });

            ConfigureBus(x);

            ConfigureServiceBusBus(x);

            if (ConfigureMessageScheduler)
                x.UseServiceBusMessageScheduler();

            x.ReceiveEndpoint(InputQueueName, e =>
            {
                ConfigureReceiveEndpoint(e);

                ConfigureServiceBusReceiveEndpoint(e);

                _inputQueueAddress = e.InputAddress;
            });
        });
    }
}
