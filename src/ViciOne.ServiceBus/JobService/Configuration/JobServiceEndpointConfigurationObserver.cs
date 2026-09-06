using System;
using ViciOne.ServiceBus.JobService;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes job service endpoint configuration events.</summary>
public class JobServiceEndpointConfigurationObserver :
    IEndpointConfigurationObserver
{
    readonly Action<IReceiveEndpointConfigurator> _configureEndpoint;
    readonly JobServiceSettings _settings;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    /// <param name="configureEndpoint">The configure endpoint.</param>
    public JobServiceEndpointConfigurationObserver(JobServiceSettings settings, Action<IReceiveEndpointConfigurator> configureEndpoint)
    {
        _settings = settings;
        _configureEndpoint = configureEndpoint;
    }

    /// <summary>Reports that endpoint has been configured.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void EndpointConfigured<T>(T configurator)
        where T : IReceiveEndpointConfigurator
    {
        configurator.ConnectConsumerConfigurationObserver(new JobServiceConsumerConfigurationObserver(configurator, _settings, _configureEndpoint));
    }
}
