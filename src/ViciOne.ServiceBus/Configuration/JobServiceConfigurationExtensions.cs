using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus;

public static class JobServiceConfigurationExtensions
{

    /// <summary>
    /// Configures support for job consumers on the service instance, which supports executing long-running jobs without blocking the consumer pipeline.
    /// Job consumers use multiple state machines to track jobs, each of which runs on its own dedicated receive endpoint. Multiple service
    /// instances will use the competing consumer pattern, so a shared saga repository should be configured.
    /// </summary>
    /// <typeparam name="T">The transport receive endpoint configurator type</typeparam>
    /// <param name="configurator">The service instance</param>
    /// <param name="configure"></param>
    public static IServiceInstanceConfigurator<T> ConfigureJobServiceEndpoints<T>(this IServiceInstanceConfigurator<T> configurator,
        Action<IJobServiceConfigurator>? configure = default)
        where T : IReceiveEndpointConfigurator
    {
        var jobServiceConfigurator = new JobServiceConfigurator<T>(configurator);

        configure?.Invoke(jobServiceConfigurator);

        jobServiceConfigurator.ConfigureJobServiceEndpoints();

        return configurator;
    }

    /// <summary>
    /// Configures support for job consumers on the service instance, which supports executing long-running jobs without blocking the consumer pipeline.
    /// Job consumers use multiple state machines to track jobs, each of which runs on its own dedicated receive endpoint. Multiple service
    /// instances will use the competing consumer pattern, so a shared saga repository should be configured.
    /// </summary>
    /// <typeparam name="T">The transport receive endpoint configurator type</typeparam>
    /// <param name="configurator">The service instance</param>
    /// <param name="options"></param>
    /// <param name="context"></param>
    /// <param name="configure"></param>
    public static IServiceInstanceConfigurator<T> ConfigureJobServiceEndpoints<T>(this IServiceInstanceConfigurator<T> configurator,
        JobServiceOptions options, IRegistrationContext? context, Action<IJobServiceConfigurator>? configure = default)
        where T : IReceiveEndpointConfigurator
    {
        var jobServiceConfigurator = new JobServiceConfigurator<T>(configurator, options);

        configure?.Invoke(jobServiceConfigurator);

        jobServiceConfigurator.ConfigureJobServiceEndpoints(context);

        return configurator;
    }
}
