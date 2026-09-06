using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds an Azure Service Bus topic subscription endpoint and its runtime context.</summary>
public class ServiceBusSubscriptionEndpointBuilder :
    ReceiveEndpointBuilder
{
    readonly IServiceBusSubscriptionEndpointConfiguration _configuration;
    readonly IServiceBusHostConfiguration _hostConfiguration;

    /// <summary>Creates a builder for an Azure Service Bus subscription endpoint.</summary>
    /// <param name="hostConfiguration">The namespace and connection configuration.</param>
    /// <param name="configuration">The subscription endpoint configuration to build.</param>
    public ServiceBusSubscriptionEndpointBuilder(IServiceBusHostConfiguration hostConfiguration, IServiceBusSubscriptionEndpointConfiguration configuration)
        : base(configuration)
    {
        _hostConfiguration = hostConfiguration;
        _configuration = configuration;
    }

    /// <summary>Builds topic and subscription topology and creates the endpoint runtime context.</summary>
    /// <returns>The initialized Azure Service Bus receive endpoint context.</returns>
    public ServiceBusReceiveEndpointContext CreateReceiveEndpointContext()
    {
        var topologyLayout = BuildTopology(_configuration.Settings);

        return new ServiceBusEntityReceiveEndpointContext(_hostConfiguration, _configuration, topologyLayout, ClientContextFactory);
    }

    static BrokerTopology BuildTopology(SubscriptionSettings settings)
    {
        var topologyBuilder = new SubscriptionEndpointBrokerTopologyBuilder();

        topologyBuilder.Topic = topologyBuilder.CreateTopic(settings.CreateTopicOptions);

        topologyBuilder.CreateSubscription(topologyBuilder.Topic, settings.CreateSubscriptionOptions, settings.Rule, settings.Filter);

        return topologyBuilder.BuildBrokerTopology();
    }

    IClientContextSupervisor ClientContextFactory()
    {
        return _hostConfiguration.ConnectionContextSupervisor
            .CreateClientContextSupervisor(supervisor => new SubscriptionClientContextFactory(supervisor, _configuration.Settings));
    }
}
