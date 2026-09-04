using System;
using ViciOne.ServiceBus.JobService;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a job service endpoint configuration observer implementation.
/// </summary>
public class JobServiceEndpointConfigurationObserver :
    IEndpointConfigurationObserver
{
    readonly Action<IReceiveEndpointConfigurator> _configureEndpoint;
    readonly JobServiceSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public JobServiceEndpointConfigurationObserver(JobServiceSettings settings, Action<IReceiveEndpointConfigurator> configureEndpoint)
    {
        _settings = settings;
        _configureEndpoint = configureEndpoint;
    }

    /// <summary>
    /// Performs the endpoint configured operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void EndpointConfigured<T>(T configurator)
        where T : IReceiveEndpointConfigurator
    {
        configurator.ConnectConsumerConfigurationObserver(new JobServiceConsumerConfigurationObserver(configurator, _settings, _configureEndpoint));
    }
}
