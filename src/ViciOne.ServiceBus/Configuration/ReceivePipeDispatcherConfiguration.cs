using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Stores and validates receive pipe dispatcher configuration.</summary>
public class ReceivePipeDispatcherConfiguration :
    ReceiverConfiguration,
    IReceiveEndpointConfigurator
{
    readonly IReceiveEndpointConfiguration _endpointConfiguration;
    readonly IHostConfiguration _hostConfiguration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="endpointConfiguration">The endpoint configuration.</param>
    public ReceivePipeDispatcherConfiguration(IHostConfiguration hostConfiguration, IReceiveEndpointConfiguration endpointConfiguration)
        : base(endpointConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _endpointConfiguration = endpointConfiguration;
    }

    /// <summary>Connects receive endpoint observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        return _endpointConfiguration.ConnectReceiveEndpointObserver(observer);
    }

    /// <summary>Builds the configured component.</summary>
    /// <returns>The configured component.</returns>
    public IReceivePipeDispatcher Build()
    {
        IReadOnlyList<ValidationResult> result = Validate().ThrowIfContainsFailure($"{GetType().Name} configuration is invalid:");

        try
        {
            var builder = new ReceiveEndpointBuilder(_endpointConfiguration);

            foreach (var specification in Specifications)
                specification.Configure(builder);

            return new ReceivePipeDispatcher(_endpointConfiguration.CreateReceivePipe(), _endpointConfiguration.ReceiveObservers, _hostConfiguration,
                _endpointConfiguration.InputAddress);
        }
        catch (Exception ex)
        {
            throw new ConfigurationException(result, global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Receive Pipe Dispatcher", "unknown", "An exception occurred during mediator creation", "Correct the named configuration before starting the host"), ex);
        }
    }
}
