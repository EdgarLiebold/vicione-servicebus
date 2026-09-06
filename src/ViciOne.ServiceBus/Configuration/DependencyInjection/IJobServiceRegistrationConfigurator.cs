using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures job service registration.</summary>
public interface IJobServiceRegistrationConfigurator
{
    /// <summary>Configure the job service options.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The job service registration configurator produced by the operation.</returns>
    IJobServiceRegistrationConfigurator Options(Action<JobConsumerOptions> configure);

    /// <summary>Configure the instance endpoint settings.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void Endpoint(Action<IEndpointRegistrationConfigurator> configure);
}
