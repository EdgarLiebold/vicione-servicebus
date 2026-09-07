using System;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Owns Azure Service Bus receive endpoints for a configured namespace.</summary>
public class ServiceBusHost :
    BaseHost,
    IServiceBusHost
{
    readonly IServiceBusHostConfiguration _hostConfiguration;

    /// <summary>Creates a host from namespace configuration and bus topology.</summary>
    /// <param name="hostConfiguration">The namespace connection and endpoint configuration.</param>
    /// <param name="busTopology">The Azure Service Bus send and publish topology.</param>
    public ServiceBusHost(IServiceBusHostConfiguration hostConfiguration, IServiceBusBusTopology busTopology)
        : base(hostConfiguration, busTopology)
    {
        _hostConfiguration = hostConfiguration;
        Topology = busTopology;
    }

    /// <summary>Gets the Azure Service Bus topology exposed by the host.</summary>
    public new IServiceBusBusTopology Topology { get; }

    /// <summary>Connects a receive endpoint using provider-neutral endpoint configuration.</summary>
    /// <param name="definition">The endpoint definition supplying name and common settings.</param>
    /// <param name="endpointNameFormatter">The optional formatter used to derive the queue name.</param>
    /// <param name="configureEndpoint">Optionally configures provider-neutral endpoint settings.</param>
    /// <returns>A handle used to observe readiness and stop the endpoint.</returns>
    public override IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        return ConnectReceiveEndpoint(definition, endpointNameFormatter,
            configureEndpoint == null ? null : endpoint => configureEndpoint(endpoint));
    }

    /// <summary>Connects a named queue using provider-neutral endpoint configuration.</summary>
    /// <param name="queueName">The namespace-relative queue name.</param>
    /// <param name="configureEndpoint">Optionally configures provider-neutral endpoint settings.</param>
    /// <returns>A handle used to observe readiness and stop the endpoint.</returns>
    public override IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        return ConnectReceiveEndpoint(queueName,
            configureEndpoint == null ? null : endpoint => configureEndpoint(endpoint));
    }

    /// <summary>Connects a receive endpoint from a definition using Azure Service Bus-specific configuration.</summary>
    /// <param name="definition">The endpoint definition supplying name and common settings.</param>
    /// <param name="endpointNameFormatter">The optional formatter used to derive the queue name.</param>
    /// <param name="configureEndpoint">Optionally configures Azure Service Bus-specific settings.</param>
    /// <returns>A handle used to observe readiness and stop the endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter = null,
        Action<IServiceBusReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        var queueName = definition.GetEndpointName(endpointNameFormatter ?? DefaultEndpointNameFormatter.Instance);

        return ConnectReceiveEndpoint(queueName, configurator =>
        {
            _hostConfiguration.ApplyEndpointDefinition(configurator, definition);
            configureEndpoint?.Invoke(configurator);
        });
    }

    /// <summary>Builds and starts a receive endpoint for a named queue.</summary>
    /// <param name="queueName">The namespace-relative queue name.</param>
    /// <param name="configure">Optionally configures Azure Service Bus-specific settings.</param>
    /// <returns>A handle used to observe readiness and stop the endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IServiceBusReceiveEndpointConfigurator>? configure = null)
    {
        LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var configuration = _hostConfiguration.CreateReceiveEndpointConfiguration(queueName, configure);

        configuration.Validate().ThrowIfContainsFailure("The receive endpoint configuration is invalid:");

        TransportLogMessages.ConnectReceiveEndpoint(configuration.InputAddress);

        configuration.Build(this);

        return ReceiveEndpoints.Start(configuration.Settings.Path);
    }

    /// <summary>Connects a subscription to the publish topic for a message contract.</summary>
    /// <typeparam name="T">The subscribed message contract.</typeparam>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="configure">Optionally configures Azure Service Bus-specific settings.</param>
    /// <returns>A handle used to observe readiness and stop the endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectSubscriptionEndpoint<T>(string subscriptionName,
        Action<IServiceBusSubscriptionEndpointConfigurator>? configure = null)
        where T : class
    {
        LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var endpointConfiguration = _hostConfiguration.CreateSubscriptionEndpointConfiguration<T>(subscriptionName, configure);

        return ConnectSubscriptionEndpoint(endpointConfiguration);
    }

    /// <summary>Connects a subscription to a named topic.</summary>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="topicName">The namespace-relative topic name.</param>
    /// <param name="configure">Optionally configures Azure Service Bus-specific settings.</param>
    /// <returns>A handle used to observe readiness and stop the endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectSubscriptionEndpoint(string subscriptionName, string topicName,
        Action<IServiceBusSubscriptionEndpointConfigurator>? configure = null)
    {
        LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var endpointConfiguration = _hostConfiguration.CreateSubscriptionEndpointConfiguration(subscriptionName, topicName, configure);

        return ConnectSubscriptionEndpoint(endpointConfiguration);
    }

    /// <summary>Adds namespace and connection-supervisor diagnostics to a probe.</summary>
    /// <param name="context">The probe receiving diagnostic values.</param>
    protected override void Probe(ProbeContext context)
    {
        context.Set(new
        {
            Type = "Azure Service Bus",
            _hostConfiguration.HostAddress,
        });

        _hostConfiguration.ConnectionContextSupervisor.Probe(context);
    }

    IHostReceiveEndpointHandle ConnectSubscriptionEndpoint(IServiceBusSubscriptionEndpointConfiguration configuration)
    {
        LogContext.Debug?.Log("Connect subscription endpoint: {Topic}/{SubscriptionName}", configuration.Settings.Path, configuration.Settings.Name);

        configuration.Validate().ThrowIfContainsFailure("The subscription endpoint configuration is invalid:");

        configuration.Build(this);

        return ReceiveEndpoints.Start(configuration.Settings.Path);
    }

    /// <summary>Gets the namespace connection supervisor owned by this host.</summary>
    /// <returns>The host agent handles.</returns>
    protected override IAgent[] GetAgentHandles()
    {
        return new IAgent[] { _hostConfiguration.ConnectionContextSupervisor };
    }
}
