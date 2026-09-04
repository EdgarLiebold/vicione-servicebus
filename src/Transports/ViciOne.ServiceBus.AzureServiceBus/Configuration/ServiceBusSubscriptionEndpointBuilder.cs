using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a service bus subscription endpoint builder implementation.
/// </summary>
public class ServiceBusSubscriptionEndpointBuilder :
    ReceiveEndpointBuilder
{
    readonly IServiceBusSubscriptionEndpointConfiguration _configuration;
    readonly IServiceBusHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="configuration">The configuration callback.</param>
    public ServiceBusSubscriptionEndpointBuilder(IServiceBusHostConfiguration hostConfiguration, IServiceBusSubscriptionEndpointConfiguration configuration)
        : base(configuration)
    {
        _hostConfiguration = hostConfiguration;
        _configuration = configuration;
    }

    /// <summary>
    /// Creates receive endpoint context.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
