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
        ConsumeContext<IAllocateJobSlot> requestContext,
        JobDistributionContext distributionContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(distributionContext);
        cancellationToken.ThrowIfCancellationRequested();

        var instances = from instance in distributionContext.ServiceInstances
                        join allocation in distributionContext.ActiveAllocations on instance.Key equals allocation.InstanceAddress into allocations
                        let allocationCount = allocations.Count()
                        where allocationCount < distributionContext.ConcurrentJobLimit
                        orderby allocationCount, instance.Value.LastAllocationAt
                        select instance.Key;

        return Task.FromResult(instances.FirstOrDefault());
    }
}
