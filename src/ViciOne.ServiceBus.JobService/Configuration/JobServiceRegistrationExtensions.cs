using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection.Registration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers job consumers, coordination state machines, and distribution strategies.</summary>
public static class JobServiceRegistrationExtensions
{
    /// <summary>Registers the local job runtime and configures options shared by all job consumers.</summary>
    /// <param name="configurator">The bus registration to update.</param>
    /// <param name="configure">The optional callback that configures local job-consumer behavior.</param>
    /// <returns>A configurator for the local job-service instance endpoint.</returns>
    public static IJobServiceRegistrationConfigurator AddJobService(this IBusRegistrationConfigurator configurator,
        Action<JobConsumerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var registration = configurator.Services.RegisterJobService(configurator.Advanced().Registrar);

        if (configure is not null)
        {
            registration.AddConfigureAction(configure);
            configurator.Services.Configure(configure);
        }

        var registrationConfigurator = new JobServiceRegistrationConfigurator(registration);

        return registrationConfigurator;
    }

    /// <summary>Registers the job type, job lifecycle, and job-attempt coordination state machines.</summary>
    /// <param name="configurator">The bus registration to update.</param>
    /// <param name="configure">The optional callback that configures persistence coordination and supervision.</param>
    /// <returns>A configurator for the three job saga registrations.</returns>
    public static IJobSagaRegistrationConfigurator AddJobSagaStateMachines(this IBusRegistrationConfigurator configurator,
        Action<JobSagaOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var registrationConfigurator = new JobSagaRegistrationConfigurator(configurator, configure);

        return registrationConfigurator;
    }

    /// <summary>Registers a custom job distribution strategy unless one is already registered.</summary>
    /// <typeparam name="TStrategy">The scoped distribution strategy implementation.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <returns>The supplied service collection.</returns>
    public static IServiceCollection TryAddJobDistributionStrategy<TStrategy>(this IServiceCollection services)
        where TStrategy : class, IJobDistributionStrategy
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IJobDistributionStrategy, TStrategy>();

        return services;
    }

    /// <summary>
    /// Determines whether an endpoint name belongs to one of the registered job coordination state machines.
    /// </summary>
    /// <param name="context">The registration context that owns the endpoint definitions.</param>
    /// <param name="name">The endpoint name to compare.</param>
    /// <returns><see langword="true" /> when the name identifies a job coordination endpoint; otherwise, <see langword="false" />.</returns>
    public static bool IsJobServiceEndpoint(this IRegistrationContext context, string name)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

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
