using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService;
/// <summary>Used by the <see cref="JobTypeStateMachine" /> to determine if a job slot should be allocated to a job.</summary>
public interface IJobDistributionStrategy
{
    /// <summary>
    /// Determine if a job slot is available and return an <see cref="ActiveJob"/> instance if the job can be assigned to a job consumer instance.
    /// If no instance is available or the concurrency limits would be exceeded, return null.
    /// </summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="jobTypeInfo">The job type info.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An <see cref="ActiveJob"/> if the job can be assigned to a job consumer instance, or null.</returns>
    Task<ActiveJob?> IsJobSlotAvailableAsync(ConsumeContext<AllocateJobSlot> context, JobTypeInfo jobTypeInfo, CancellationToken cancellationToken = default);
}
