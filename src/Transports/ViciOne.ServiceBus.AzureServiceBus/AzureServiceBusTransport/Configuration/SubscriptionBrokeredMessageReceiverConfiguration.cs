using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Provides a subscription brokered message receiver configuration implementation.
/// </summary>
public class SubscriptionBrokeredMessageReceiverConfiguration :
    ReceiverConfiguration
{
    readonly IServiceBusSubscriptionEndpointConfiguration _endpointConfiguration;
    readonly IServiceBusHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    public SubscriptionBrokeredMessageReceiverConfiguration(IServiceBusHostConfiguration hostConfiguration,
        IServiceBusSubscriptionEndpointConfiguration endpointConfiguration)
        : base(endpointConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _endpointConfiguration = endpointConfiguration;
    }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
