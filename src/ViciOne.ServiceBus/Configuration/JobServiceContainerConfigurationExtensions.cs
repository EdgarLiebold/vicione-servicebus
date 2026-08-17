namespace ViciOne.ServiceBus
{
    using System;
    using DependencyInjection;


    public static class JobServiceContainerConfigurationExtensions
    {
        /// <summary>
        /// Configure the job server saga repositories to resolve from the container.
        /// </summary>
        /// <param name="configurator"></param>
        /// <param name="context">The bus registration context provided during configuration</param>
        /// <returns></returns>
        public static IJobServiceConfigurator ConfigureSagaRepositories(this IJobServiceConfigurator configurator, IRegistrationContext context)
        {
            configurator.Repository = new DependencyInjectionSagaRepository<JobTypeSaga>(context);
            configurator.JobRepository = new DependencyInjectionSagaRepository<JobSaga>(context);
            configurator.JobAttemptRepository = new DependencyInjectionSagaRepository<JobAttemptSaga>(context);

            return configurator;
        }

    }
}
