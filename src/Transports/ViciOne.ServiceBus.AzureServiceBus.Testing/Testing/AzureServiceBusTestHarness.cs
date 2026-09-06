using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Runs a bus test harness against an Azure Service Bus namespace.</summary>
public class AzureServiceBusTestHarness :
    BusTestHarness
{
    Uri? _inputQueueAddress;

    /// <summary>Initializes the harness for an Azure Service Bus namespace and input queue.</summary>
    /// <param name="serviceUri">The namespace URI.</param>
    /// <param name="namedKeyCredential">The credential used for transport and administration operations.</param>
    /// <param name="inputQueueName">The input queue name, or <see langword="null"/> to use <c>input_queue</c>.</param>
    public AzureServiceBusTestHarness(Uri serviceUri, AzureNamedKeyCredential namedKeyCredential, string? inputQueueName = null)
    {
        if (serviceUri == null)
            throw new ArgumentNullException(nameof(serviceUri));

        HostAddress = serviceUri;
        NamedKeyCredential = namedKeyCredential;

        InputQueueName = inputQueueName ?? "input_queue";

        ConfigureMessageScheduler = true;
    }

    /// <summary>Gets the credential used to access the Azure Service Bus namespace.</summary>
    public AzureNamedKeyCredential NamedKeyCredential { get; }
    /// <summary>Gets the queue on which the harness receives test messages.</summary>
    public override string InputQueueName { get; }
    /// <summary>Gets or sets whether the bus uses the Azure Service Bus message scheduler.</summary>
    public bool ConfigureMessageScheduler { get; set; }

    /// <summary>Gets the input queue address after the receive endpoint has been configured.</summary>
    public override Uri InputQueueAddress => _inputQueueAddress
        ?? throw new InvalidOperationException("The input queue address is not available before the bus has been created.");
    /// <summary>Gets the Azure Service Bus namespace address.</summary>
    public Uri HostAddress { get; }

    /// <summary>Occurs while the harness configures the Azure Service Bus bus factory.</summary>
    public event Action<IServiceBusBusFactoryConfigurator>? OnConfigureServiceBusBus;
    /// <summary>Occurs while the harness configures its Azure Service Bus receive endpoint.</summary>
    public event Action<IServiceBusReceiveEndpointConfigurator>? OnConfigureServiceBusReceiveEndpoint;

    /// <summary>Invokes registered callbacks for provider-specific bus configuration.</summary>
    /// <param name="configurator">The Azure Service Bus factory configurator.</param>
    protected virtual void ConfigureServiceBusBus(IServiceBusBusFactoryConfigurator configurator)
    {
        OnConfigureServiceBusBus?.Invoke(configurator);
    }

    /// <summary>Invokes registered callbacks for provider-specific receive-endpoint configuration.</summary>
    /// <param name="configurator">The Azure Service Bus receive-endpoint configurator.</param>
    protected virtual void ConfigureServiceBusReceiveEndpoint(IServiceBusReceiveEndpointConfigurator configurator)
    {
        OnConfigureServiceBusReceiveEndpoint?.Invoke(configurator);
    }

    /// <summary>Deletes every topic and queue from the configured Azure Service Bus namespace.</summary>
    /// <param name="cancellationToken">The token that cancels enumeration, deletion, or the retry delay.</param>
    /// <returns>A task that completes when the namespace contains no topics or queues.</returns>
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

    /// <summary>Creates the bus and captures the configured input queue address.</summary>
    /// <returns>A task that produces the configured bus control.</returns>
    protected override async Task<IBusControl> CreateBusAsync()
    {
        return ViciOne.ServiceBus.Advanced.Bus.Factory.CreateUsingAzureServiceBus(x =>
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
                x.ConfigureServiceBusMessageScheduler();

            x.ReceiveEndpoint(InputQueueName, e =>
            {
                ConfigureReceiveEndpoint(e);

                ConfigureServiceBusReceiveEndpoint(e);

                _inputQueueAddress = e.InputAddress;
            });
        });
    }
}
