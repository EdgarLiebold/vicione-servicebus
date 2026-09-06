using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Builds a standalone Azure Service Bus subscription receiver from validated endpoint configuration.</summary>
public class SubscriptionBrokeredMessageReceiverConfiguration :
    ReceiverConfiguration
{
    readonly IServiceBusSubscriptionEndpointConfiguration _endpointConfiguration;
    readonly IServiceBusHostConfiguration _hostConfiguration;

    /// <summary>Initializes the receiver configuration for a namespace and subscription endpoint.</summary>
    /// <param name="hostConfiguration">The namespace host configuration.</param>
    /// <param name="endpointConfiguration">The subscription endpoint configuration and middleware specifications.</param>
    public SubscriptionBrokeredMessageReceiverConfiguration(IServiceBusHostConfiguration hostConfiguration,
        IServiceBusSubscriptionEndpointConfiguration endpointConfiguration)
        : base(endpointConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _endpointConfiguration = endpointConfiguration;
    }

    /// <summary>Validates the configuration and creates a standalone message receiver.</summary>
    /// <returns>The receiver backed by the configured subscription endpoint context.</returns>
    public IServiceBusMessageReceiver Build()
    {
        IReadOnlyList<ValidationResult> result = Validate().ThrowIfContainsFailure($"{GetType().Name} configuration is invalid:");

        try
        {
            var builder = new ServiceBusSubscriptionEndpointBuilder(_hostConfiguration, _endpointConfiguration);

            foreach (var specification in Specifications)
                specification.Configure(builder);

            return new ServiceBusMessageReceiver(builder.CreateReceiveEndpointContext());
        }
        catch (Exception ex)
        {
            throw new ConfigurationException(result, global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Azure Service Bus", "unknown", "An exception occurred creating the BrokeredMessageReceiver", "Correct the named configuration before starting the host"), ex);
        }
    }
}
