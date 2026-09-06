using System;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for job service container configuration.</summary>
public static class JobServiceContainerConfigurationExtensions
{
    /// <summary>Configure the job server saga repositories to resolve from the container.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The bus registration context provided during configuration.</param>
    /// <returns>The job service configurator produced by the operation.</returns>
    public static IJobServiceConfigurator ConfigureSagaRepositories(this IJobServiceConfigurator configurator, IRegistrationContext context)
    {
        configurator.Repository = new DependencyInjectionSagaRepository<JobTypeSaga>(context);
        configurator.JobRepository = new DependencyInjectionSagaRepository<JobSaga>(context);
        configurator.JobAttemptRepository = new DependencyInjectionSagaRepository<JobAttemptSaga>(context);

        return configurator;
    }

}
