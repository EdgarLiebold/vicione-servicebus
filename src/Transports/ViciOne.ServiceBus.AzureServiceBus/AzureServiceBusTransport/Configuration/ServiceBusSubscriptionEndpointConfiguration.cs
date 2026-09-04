using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Middleware;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Provides a service bus subscription endpoint configuration implementation.
/// </summary>
public class ServiceBusSubscriptionEndpointConfiguration :
    ServiceBusEntityReceiveEndpointConfiguration,
    IServiceBusSubscriptionEndpointConfiguration,
    IServiceBusSubscriptionEndpointConfigurator
{
    readonly IServiceBusEndpointConfiguration _endpointConfiguration;
    readonly IServiceBusHostConfiguration _hostConfiguration;
    readonly Lazy<Uri> _inputAddress;
    readonly SubscriptionEndpointSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="settings">The settings value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
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

    /// <summary>
    /// Gets the settings value.
    /// </summary>
    public SubscriptionSettings Settings => _settings;

    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public override Uri HostAddress { get; }

    /// <summary>
    /// Gets the input address value.
    /// </summary>
    public override Uri InputAddress => _inputAddress.Value;

    /// <summary>
    /// Creates receive endpoint context.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override ReceiveEndpointContext CreateReceiveEndpointContext()
    {
        return CreateServiceBusReceiveEndpointContext();
    }

    IServiceBusTopologyConfiguration IServiceBusEndpointConfiguration.Topology => _endpointConfiguration.Topology;

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return _settings.SubscriptionConfigurator.Validate()
            .Concat(base.Validate());
    }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <param name="host">The host value.</param>
    public void Build(IHost host)
    {
        this.ConfigureDeadLetterQueueDeadLetterTransport();
        this.ConfigureDeadLetterQueueErrorTransport();

        var context = CreateServiceBusReceiveEndpointContext();

        ClientPipeConfigurator.UseFilter(new ConfigureServiceBusTopologyFilter<SubscriptionSettings>(_settings, context.BrokerTopology,
            _settings.RemoveSubscriptions, context));

        CreateReceiveEndpoint(host, context);
    }

    /// <summary>
    /// Gets or sets the filter value.
    /// </summary>
    public RuleFilter Filter
    {
        set => _settings.Filter = value;
    }

    /// <summary>
    /// Gets or sets the rule value.
    /// </summary>
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
