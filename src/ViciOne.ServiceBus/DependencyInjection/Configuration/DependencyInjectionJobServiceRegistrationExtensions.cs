using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.JobService;
using JobServiceState = ViciOne.ServiceBus.JobService.JobService;

namespace ViciOne.ServiceBus.Configuration;

public static class DependencyInjectionJobServiceRegistrationExtensions
{
    public static IJobServiceRegistration RegisterJobService(this IServiceCollection collection, IContainerRegistrar registrar)
    {
        collection.AddOptions<JobConsumerOptions>();
        return registrar.GetOrAddRegistration<IJobServiceRegistration>(typeof(JobServiceState), _ => new JobServiceRegistration());
    }
}
