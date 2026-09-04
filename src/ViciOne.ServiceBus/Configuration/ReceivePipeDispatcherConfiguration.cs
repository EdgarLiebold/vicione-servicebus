using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a receive pipe dispatcher configuration implementation.
/// </summary>
public class ReceivePipeDispatcherConfiguration :
    ReceiverConfiguration,
    IReceiveEndpointConfigurator
{
    readonly IReceiveEndpointConfiguration _endpointConfiguration;
    readonly IHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    public ReceivePipeDispatcherConfiguration(IHostConfiguration hostConfiguration, IReceiveEndpointConfiguration endpointConfiguration)
        : base(endpointConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _endpointConfiguration = endpointConfiguration;
    }

    /// <summary>
    /// Connects receive endpoint observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        return _endpointConfiguration.ConnectReceiveEndpointObserver(observer);
    }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
            throw new ConfigurationException(result, "An exception occurred during mediator creation", ex);
        }
    }
}
