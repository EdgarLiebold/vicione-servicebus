using System;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds an Azure Service Bus queue endpoint, its subscriptions, and its runtime context.</summary>
public class ServiceBusReceiveEndpointBuilder :
    ReceiveEndpointBuilder
{
    readonly IServiceBusReceiveEndpointConfiguration _configuration;
    readonly IServiceBusHostConfiguration _hostConfiguration;

    /// <summary>Creates a builder for an Azure Service Bus receive endpoint.</summary>
    /// <param name="hostConfiguration">The namespace and connection configuration.</param>
    /// <param name="configuration">The queue endpoint configuration to build.</param>
    public ServiceBusReceiveEndpointBuilder(IServiceBusHostConfiguration hostConfiguration, IServiceBusReceiveEndpointConfiguration configuration)
        : base(configuration)
    {
        _hostConfiguration = hostConfiguration;
        _configuration = configuration;
    }

    /// <summary>Connects a typed consume pipe and adds its topic subscription when topology configuration is enabled.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="pipe">The typed consume pipe to connect.</param>
    /// <param name="options">Flags that control whether consume topology is configured.</param>
    /// <returns>A handle that disconnects the consume pipe.</returns>
    public override ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
    {
        if (_configuration.ConfigureConsumeTopology && options.HasFlag(ConnectPipeOptions.ConfigureConsumeTopology))
        {
            IServiceBusMessageConsumeTopologyConfigurator<T> topology = _configuration.Topology.Consume.GetMessageTopology<T>();
            if (topology.ConfigureConsumeTopology)
            {
                var subscriptionName = GenerateSubscriptionName();
                topology.Subscribe(subscriptionName);
            }
        }

        return base.ConnectConsumePipe(pipe, options);
    }

    /// <summary>Builds broker topology and creates the queue endpoint runtime context.</summary>
    /// <returns>The initialized Azure Service Bus receive endpoint context.</returns>
    public ServiceBusReceiveEndpointContext CreateReceiveEndpointContext()
    {
        var topologyLayout = BuildTopology(_configuration.Settings);

        return new ServiceBusEntityReceiveEndpointContext(_hostConfiguration, _configuration, topologyLayout, ClientContextFactory);
    }

    string GenerateSubscriptionName()
    {
        return _configuration.Topology.Publish.GenerateSubscriptionName(
            _configuration.Settings.GetCreateQueueOptions().Name, _configuration.HostAddress.Authority);
    }

    BrokerTopology BuildTopology(ReceiveSettings settings)
    {
        var topologyBuilder = new ReceiveEndpointBrokerTopologyBuilder();

        topologyBuilder.Queue = topologyBuilder.CreateQueue(settings.GetCreateQueueOptions());

        _configuration.Topology.Consume.Apply(topologyBuilder);

        return topologyBuilder.BuildBrokerTopology();
    }

    IClientContextSupervisor ClientContextFactory()
    {
        return _hostConfiguration.ConnectionContextSupervisor
            .CreateClientContextSupervisor(supervisor => new QueueClientContextFactory(supervisor, _configuration.Settings));
    }
}
