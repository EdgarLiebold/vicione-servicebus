using System;
using ViciOne.ServiceBus.JobService;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Attaches job-consumer discovery to each configured service-instance endpoint.</summary>
internal sealed class JobServiceEndpointConfigurationObserver :
    IEndpointConfigurationObserver
{
    readonly Action<IReceiveEndpointConfigurator> _configureEndpoint;
    readonly JobServiceSettings _settings;

    /// <summary>Creates an observer for consumer endpoints owned by one job-service instance.</summary>
    /// <param name="settings">The owning job-service settings.</param>
    /// <param name="configureEndpoint">The callback that adds job coordination dependencies to an endpoint.</param>
    public JobServiceEndpointConfigurationObserver(JobServiceSettings settings, Action<IReceiveEndpointConfigurator> configureEndpoint)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _configureEndpoint = configureEndpoint ?? throw new ArgumentNullException(nameof(configureEndpoint));
    }

    /// <summary>Connects job-consumer discovery after a receive endpoint has been configured.</summary>
    /// <typeparam name="TConfigurator">The concrete receive-endpoint configurator type.</typeparam>
    /// <param name="configurator">The configured receive endpoint.</param>
    public void EndpointConfigured<TConfigurator>(TConfigurator configurator)
        where TConfigurator : IReceiveEndpointConfigurator
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.ConnectConsumerConfigurationObserver(new JobServiceConsumerConfigurationObserver(configurator, _settings, _configureEndpoint));
    }
}
