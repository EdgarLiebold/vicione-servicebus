using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Selects an eligible service instance when the coordinator allocates a job slot.</summary>
public interface IJobDistributionStrategy
{
    /// <summary>Selects an eligible service instance for a requested job allocation.</summary>
    /// <param name="requestContext">The allocation request and consume metadata.</param>
    /// <param name="distributionContext">The current capacity, allocations, and distribution metadata for the job type.</param>
    /// <param name="cancellationToken">The token that cancels selection.</param>
    /// <returns>The address of the selected instance, or <see langword="null" /> when no eligible instance is available.</returns>
    Task<Uri?> SelectInstanceAsync(
        ConsumeContext<AllocateJobSlot> requestContext,
        JobDistributionContext distributionContext,
        CancellationToken cancellationToken = default);
}
