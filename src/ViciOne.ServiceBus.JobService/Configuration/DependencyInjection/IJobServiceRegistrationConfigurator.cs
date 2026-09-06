using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures the service-instance endpoint used by local job consumers.</summary>
public interface IJobServiceRegistrationConfigurator
{
    /// <summary>Adds a callback that configures local job execution before the bus starts.</summary>
    /// <param name="configure">The local-runtime configuration callback.</param>
    /// <returns>This configurator.</returns>
    IJobServiceRegistrationConfigurator ConfigureOptions(Action<JobConsumerOptions> configure);

    /// <summary>Configures the service-instance endpoint that executes jobs locally.</summary>
    /// <param name="configure">The endpoint configuration callback.</param>
    void ConfigureEndpoint(Action<IEndpointRegistrationConfigurator> configure);
}
