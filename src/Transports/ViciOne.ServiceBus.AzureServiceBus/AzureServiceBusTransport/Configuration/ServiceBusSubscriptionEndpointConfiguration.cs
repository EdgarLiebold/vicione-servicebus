using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Middleware;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Configures an Azure Service Bus topic-subscription receive endpoint and its topology.</summary>
public class ServiceBusSubscriptionEndpointConfiguration :
    ServiceBusEntityReceiveEndpointConfiguration,
    IServiceBusSubscriptionEndpointConfiguration,
    IServiceBusSubscriptionEndpointConfigurator
{
    readonly IServiceBusEndpointConfiguration _endpointConfiguration;
    readonly IServiceBusHostConfiguration _hostConfiguration;
    readonly Lazy<Uri> _inputAddress;
    readonly SubscriptionEndpointSettings _settings;

    /// <summary>Initializes a subscription endpoint for a namespace and topic.</summary>
    /// <param name="hostConfiguration">The namespace host configuration.</param>
    /// <param name="settings">The topic, subscription, and processor settings.</param>
    /// <param name="endpointConfiguration">The endpoint pipeline and topology configuration.</param>
    public ServiceBusSubscriptionEndpointConfiguration(IServiceBusHostConfiguration hostConfiguration,
        SubscriptionEndpointSettings settings, IServiceBusEndpointConfiguration endpointConfiguration)
        : base(hostConfiguration, settings, endpointConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _endpointConfiguration = endpointConfiguration;
        _settings = settings;

        HostAddress = hostConfiguration.HostAddress;
        _inputAddress = new Lazy<Uri>(FormatInputAddress);
    }

    /// <summary>Gets the topic, subscription, and processor settings.</summary>
    public SubscriptionSettings Settings => _settings;

    /// <summary>Gets the namespace address.</summary>
    public override Uri HostAddress { get; }

    /// <summary>Gets the lazily formatted subscription input address.</summary>
    public override Uri InputAddress => _inputAddress.Value;

    /// <summary>Builds a receive-endpoint context from the current subscription configuration.</summary>
    /// <returns>The Azure Service Bus receive-endpoint context.</returns>
    public override ReceiveEndpointContext CreateReceiveEndpointContext()
    {
        return CreateServiceBusReceiveEndpointContext();
    }

    IServiceBusTopologyConfiguration IServiceBusEndpointConfiguration.Topology => _endpointConfiguration.Topology;

    /// <summary>Combines subscription-entity validation with the shared endpoint validation.</summary>
    /// <returns>All subscription and endpoint validation failures.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return _settings.SubscriptionConfigurator.Validate()
            .Concat(base.Validate());
    }

    /// <summary>Configures dead-letter transports and topology, then registers the receive endpoint with the host.</summary>
    /// <param name="host">The host that owns the receive endpoint.</param>
    public void Build(IHost host)
    {
        this.ConfigureDeadLetterQueueDeadLetterTransport();
        this.ConfigureDeadLetterQueueErrorTransport();

        var context = CreateServiceBusReceiveEndpointContext();

        ClientPipeConfigurator.UseFilter(new ConfigureServiceBusTopologyFilter<SubscriptionSettings>(_settings, context.BrokerTopology,
            _settings.RemoveSubscriptions, context));

        CreateReceiveEndpoint(host, context);
    }

    /// <summary>Sets the filter for the subscription's default rule.</summary>
    public RuleFilter Filter
    {
        set => _settings.Filter = value;
    }

    /// <summary>Sets the complete rule created with the subscription.</summary>
    public CreateRuleOptions Rule
    {
        set => _settings.Rule = value;
    }

    ServiceBusReceiveEndpointContext CreateServiceBusReceiveEndpointContext()
    {
        var builder = new ServiceBusSubscriptionEndpointBuilder(_hostConfiguration, this);

        ApplySpecifications(builder);

        return builder.CreateReceiveEndpointContext();
    }

    Uri FormatInputAddress()
    {
        return _settings.GetInputAddress(_hostConfiguration.HostAddress, _settings.Path);
    }
}
