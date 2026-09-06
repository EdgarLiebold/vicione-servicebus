using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection.Registration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for job service registration.</summary>
public static class JobServiceRegistrationExtensions
{
    /// <summary>Set the job consumer options (optional, not required to use job consumers).</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">Configure the job consumer options using this callback.</param>
    /// <returns>The job service registration configurator produced by the operation.</returns>
    public static IJobServiceRegistrationConfigurator SetJobConsumerOptions(this IBusRegistrationConfigurator configurator,
        Action<JobConsumerOptions>? configure = null)
    {
        var registration = configurator.Services.RegisterJobService(configurator.Advanced().Registrar);

        registration.AddConfigureAction(configure);
        if (configure is not null)
            configurator.Services.Configure(configure);

        var registrationConfigurator = new JobServiceRegistrationConfigurator(configurator, registration);

        return registrationConfigurator;
    }

    /// <summary>Add registrations for the job service saga state machines.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">Configure the job saga options.</param>
    /// <returns>The job saga registration configurator produced by the operation.</returns>
    public static IJobSagaRegistrationConfigurator AddJobSagaStateMachines(this IBusRegistrationConfigurator configurator,
        Action<JobSagaOptions>? configure = null)
    {
        var registrationConfigurator = new JobSagaRegistrationConfigurator(configurator, configure);

        return registrationConfigurator;
    }

    /// <summary>Register a custom job distribution strategy for the job saga state machines.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <returns>The service collection produced by the operation.</returns>
    public static IServiceCollection TryAddJobDistributionStrategy<T>(this IServiceCollection services)
        where T : class, IJobDistributionStrategy
    {
        services.TryAddScoped<IJobDistributionStrategy, T>();

        return services;
    }

    /// <summary>
    /// Compares the <paramref name="name" /> to the known job saga endpoints and returns true if the name matches.
    /// Use this inside an AddConfigureEndpointsCallback to avoid adding filters to the job saga endpoints.
    /// </summary>
    /// <param name="context">The registration context.</param>
    /// <param name="name">The endpoint name.</param>
    /// <returns>true if matched, otherwise false.</returns>
    public static bool IsJobServiceEndpoint(this IRegistrationContext context, string name)
    {
        var selector = context.GetRequiredService<IContainerSelector>();

        var formatter = selector.GetEndpointNameFormatter(context);

        IEndpointDefinition? endpointDefinition = selector.GetEndpointDefinition<JobSaga>(context);
        if (string.Equals(endpointDefinition?.GetEndpointName(formatter), name, StringComparison.OrdinalIgnoreCase))
            return true;

        endpointDefinition = selector.GetEndpointDefinition<JobTypeSaga>(context);
        if (string.Equals(endpointDefinition?.GetEndpointName(formatter), name, StringComparison.OrdinalIgnoreCase))
            return true;

        endpointDefinition = selector.GetEndpointDefinition<JobAttemptSaga>(context);
        if (string.Equals(endpointDefinition?.GetEndpointName(formatter), name, StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(formatter.Saga<JobTypeSaga>(), name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(formatter.Saga<JobSaga>(), name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(formatter.Saga<JobAttemptSaga>(), name, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
