using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Selects the least-loaded eligible service instance, preferring the least recently used instance on ties.</summary>
internal sealed class DefaultJobDistributionStrategy :
    IJobDistributionStrategy
{
    /// <summary>Gets the stateless default strategy instance.</summary>
    public static IJobDistributionStrategy Instance { get; } = new DefaultJobDistributionStrategy();

    /// <inheritdoc />
    public Task<Uri?> SelectInstanceAsync(
        ConsumeContext<AllocateJobSlot> context,
        JobTypeInfo jobTypeInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(jobTypeInfo);
        cancellationToken.ThrowIfCancellationRequested();

        var instances = from instance in jobTypeInfo.Instances
                        join activeJob in jobTypeInfo.ActiveJobs on instance.Key equals activeJob.InstanceAddress into activeJobs
                        let activeJobCount = activeJobs.Count()
                        where activeJobCount < jobTypeInfo.ConcurrentJobLimit
                        orderby activeJobCount, instance.Value.Used
                        select instance.Key;

        return Task.FromResult(instances.FirstOrDefault());
    }
}
