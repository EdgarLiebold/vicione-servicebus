using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides an azure service bus test harness implementation.
/// </summary>
public class AzureServiceBusTestHarness :
    BusTestHarness
{
    Uri? _inputQueueAddress;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="serviceUri">The service uri value.</param>
    /// <param name="namedKeyCredential">The named key credential value.</param>
    /// <param name="inputQueueName">The input queue name value.</param>
    public AzureServiceBusTestHarness(Uri serviceUri, AzureNamedKeyCredential namedKeyCredential, string? inputQueueName = null)
    {
        if (serviceUri == null)
            throw new ArgumentNullException(nameof(serviceUri));

        HostAddress = serviceUri;
        NamedKeyCredential = namedKeyCredential;

        InputQueueName = inputQueueName ?? "input_queue";

        ConfigureMessageScheduler = true;
    }

    /// <summary>
    /// Gets the named key credential value.
    /// </summary>
    public AzureNamedKeyCredential NamedKeyCredential { get; }
    /// <summary>
    /// Gets the input queue name value.
    /// </summary>
    public override string InputQueueName { get; }
    /// <summary>
    /// Gets or sets the configure message scheduler value.
    /// </summary>
    public bool ConfigureMessageScheduler { get; set; }

    /// <summary>
    /// Gets the input queue address value.
    /// </summary>
    public override Uri InputQueueAddress => _inputQueueAddress
        ?? throw new InvalidOperationException("The input queue address is not available before the bus has been created.");
    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public Uri HostAddress { get; }

    /// <summary>
    /// Occurs when on configure service bus bus.
    /// </summary>
    public event Action<IServiceBusBusFactoryConfigurator>? OnConfigureServiceBusBus;
    /// <summary>
    /// Occurs when on configure service bus receive endpoint.
    /// </summary>
    public event Action<IServiceBusReceiveEndpointConfigurator>? OnConfigureServiceBusReceiveEndpoint;

    /// <summary>
    /// Configures service bus bus.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    protected virtual void ConfigureServiceBusBus(IServiceBusBusFactoryConfigurator configurator)
    {
        OnConfigureServiceBusBus?.Invoke(configurator);
    }

    /// <summary>
    /// Configures service bus receive endpoint.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    protected virtual void ConfigureServiceBusReceiveEndpoint(IServiceBusReceiveEndpointConfigurator configurator)
    {
        OnConfigureServiceBusReceiveEndpoint?.Invoke(configurator);
    }

    /// <summary>
    /// Performs the clean operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Creates bus.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
