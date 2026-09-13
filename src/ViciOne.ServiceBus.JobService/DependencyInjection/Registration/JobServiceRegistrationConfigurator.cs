using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Configures the endpoint definition of the local job runtime.</summary>
/// <param name="registration">The local job-runtime registration.</param>
internal sealed class JobServiceRegistrationConfigurator(IJobServiceRegistration registration) :
    IJobServiceRegistrationConfigurator
{
    readonly IJobServiceRegistration _registration = registration ?? throw new ArgumentNullException(nameof(registration));

    /// <summary>Adds a callback that configures the local runtime before startup.</summary>
    /// <param name="configure">The runtime configuration callback.</param>
    /// <returns>This configurator.</returns>
    public IJobServiceRegistrationConfigurator ConfigureOptions(Action<JobConsumerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _registration.AddConfigureAction(configure);

        return this;
    }

    /// <summary>Configures the local job-execution endpoint.</summary>
    /// <param name="configure">The endpoint configuration callback.</param>
    public void ConfigureEndpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_registration.EndpointRegistrationConfigurator);
    }
}
