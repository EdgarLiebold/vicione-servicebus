using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.JobService;
using JobServiceState = ViciOne.ServiceBus.JobService.JobService;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for dependency injection job service registration.
/// </summary>
public static class DependencyInjectionJobServiceRegistrationExtensions
{
    /// <summary>
    /// Performs the register job service operation.
    /// </summary>
    /// <param name="collection">The collection value.</param>
    /// <param name="registrar">The registrar value.</param>
    /// <returns>The result of the operation.</returns>
    public static IJobServiceRegistration RegisterJobService(this IServiceCollection collection, IContainerRegistrar registrar)
    {
        collection.AddOptions<JobConsumerOptions>()
            .Validate(
                static options => options.HeartbeatInterval > TimeSpan.Zero,
                "Job service for bus 'default': HeartbeatInterval must be greater than zero. Set HeartbeatInterval to a positive duration.")
            .Validate(
                static options => options.RejectedJobDelay > TimeSpan.Zero,
                "Job service for bus 'default': RejectedJobDelay must be greater than zero. Set RejectedJobDelay to a positive duration.")
            .Validate(
                static options => options.TimeProvider is not null,
                "Job service for bus 'default': TimeProvider must not be null. Set TimeProvider to an application-owned clock.")
            .ValidateOnStart();
        return registrar.GetOrAddRegistration<IJobServiceRegistration>(typeof(JobServiceState), _ => new JobServiceRegistration());
    }
}
