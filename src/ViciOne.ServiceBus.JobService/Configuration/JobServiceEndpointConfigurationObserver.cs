using System;
using ViciOne.ServiceBus.JobService;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Attaches job-consumer discovery to each configured service-instance endpoint.</summary>
/// <param name="settings">The owning job-service settings.</param>
/// <param name="configureEndpoint">The callback that adds job coordination dependencies to an endpoint.</param>
internal sealed class JobServiceEndpointConfigurationObserver(
    IJobServiceSettings settings,
    Action<IReceiveEndpointConfigurator> configureEndpoint) :
    IEndpointConfigurationObserver
{
    readonly Action<IReceiveEndpointConfigurator> _configureEndpoint =
        configureEndpoint ?? throw new ArgumentNullException(nameof(configureEndpoint));
    readonly IJobServiceSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

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
