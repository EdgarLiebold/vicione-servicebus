using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures job coordination endpoints directly on a service instance.</summary>
public static class JobServiceConfigurationExtensions
{
    /// <summary>
    /// Adds job coordination endpoints to a service instance using transport-native endpoint configuration.
    /// </summary>
    /// <typeparam name="TConfigurator">The transport-specific receive-endpoint configurator type.</typeparam>
    /// <param name="configurator">The service instance that owns the job consumers.</param>
    /// <param name="configure">The optional callback that configures coordination endpoints and repositories.</param>
    /// <returns>The supplied service-instance configurator.</returns>
    public static IServiceInstanceConfigurator<TConfigurator> ConfigureJobServiceEndpoints<TConfigurator>(
        this IServiceInstanceConfigurator<TConfigurator> configurator,
        Action<IJobServiceConfigurator>? configure = default)
        where TConfigurator : IReceiveEndpointConfigurator
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var jobServiceConfigurator = new JobServiceConfigurator<TConfigurator>(configurator);

        configure?.Invoke(jobServiceConfigurator);

        jobServiceConfigurator.ConfigureJobServiceEndpoints();

        return configurator;
    }

    /// <summary>
    /// Adds job coordination endpoints to a service instance using explicit options and an optional container context.
    /// </summary>
    /// <typeparam name="TConfigurator">The transport-specific receive-endpoint configurator type.</typeparam>
    /// <param name="configurator">The service instance that owns the job consumers.</param>
    /// <param name="options">The coordination and supervision options to apply.</param>
    /// <param name="context">The optional registration context used for scoped middleware.</param>
    /// <param name="configure">The optional callback that configures coordination endpoints and repositories.</param>
    /// <returns>The supplied service-instance configurator.</returns>
    public static IServiceInstanceConfigurator<TConfigurator> ConfigureJobServiceEndpoints<TConfigurator>(
        this IServiceInstanceConfigurator<TConfigurator> configurator,
        JobServiceOptions options, IRegistrationContext? context, Action<IJobServiceConfigurator>? configure = default)
        where TConfigurator : IReceiveEndpointConfigurator
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(options);

        var jobServiceConfigurator = new JobServiceConfigurator<TConfigurator>(configurator, options);

        configure?.Invoke(jobServiceConfigurator);

        jobServiceConfigurator.ConfigureJobServiceEndpoints(context);

        return configurator;
    }
}
