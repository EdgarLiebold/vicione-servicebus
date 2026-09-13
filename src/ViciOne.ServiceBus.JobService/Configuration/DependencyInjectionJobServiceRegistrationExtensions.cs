using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.JobService;
using JobServiceState = ViciOne.ServiceBus.JobService.JobService;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers the local job runtime and its validated options in dependency injection.</summary>
internal static class DependencyInjectionJobServiceRegistrationExtensions
{
    /// <summary>Registers the local runtime, its consumer kind, and validated options.</summary>
    /// <param name="collection">The service collection that owns the runtime options.</param>
    /// <param name="registrar">The bus registration store that owns the runtime registration.</param>
    /// <returns>The unique job-service registration for the bus.</returns>
    public static IJobServiceRegistration RegisterJobService(this IServiceCollection collection, IContainerRegistrar registrar)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);

        JobServiceCorrelationConventions.Register();
        JobConsumerConventionRegistration.Register();
        collection.TryAddEnumerable(ServiceDescriptor.Singleton<IConsumerKind, JobConsumerKind>());
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
        var registration = registrar.GetOrAddRegistration<IJobServiceRegistration>(typeof(JobServiceState), _ => new JobServiceRegistration());
        registrar.GetOrAddRegistration<IConsumerKindHost>(typeof(JobServiceState), _ => registration);
        return registration;
    }
}
