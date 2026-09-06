using System;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Resolves directly configured job-saga repositories from dependency injection.</summary>
public static class JobServiceContainerConfigurationExtensions
{
    /// <summary>Uses the saga repositories registered in the dependency-injection container.</summary>
    /// <param name="configurator">The job-service configuration to update.</param>
    /// <param name="context">The bus registration context provided during configuration.</param>
    /// <returns>The supplied job-service configurator.</returns>
    public static IJobServiceConfigurator ConfigureSagaRepositories(this IJobServiceConfigurator configurator, IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

        configurator.JobTypeRepository = new DependencyInjectionSagaRepository<JobTypeSaga>(context);
        configurator.JobRepository = new DependencyInjectionSagaRepository<JobSaga>(context);
        configurator.JobAttemptRepository = new DependencyInjectionSagaRepository<JobAttemptSaga>(context);

        return configurator;
    }
}
