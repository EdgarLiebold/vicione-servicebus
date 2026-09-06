using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService;

/// <summary>
/// Provides a default job distribution strategy implementation.
/// </summary>
public class DefaultJobDistributionStrategy :
    IJobDistributionStrategy
{
    /// <summary>
    /// Defines the instance value.
    /// </summary>
    public static readonly IJobDistributionStrategy Instance = new DefaultJobDistributionStrategy();

    /// <summary>
    /// Determines whether job slot available.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="jobTypeInfo">The job type info value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<ActiveJob?> IsJobSlotAvailableAsync(ConsumeContext<AllocateJobSlot> context, JobTypeInfo jobTypeInfo, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var instances = from i in jobTypeInfo.Instances
                                                                          join a in jobTypeInfo.ActiveJobs on i.Key equals a.InstanceAddress into ai
                                                                          where ai.Count() < jobTypeInfo.ConcurrentJobLimit
                                                                          orderby ai.Count(), i.Value.Used
                                                                          select new
                                                                          {
                                                                              Instance = i.Value,
                                                                              InstanceAddress = i.Key,
                                                                              InstanceCount = ai.Count()
                                                                          };

        var firstInstance = instances.FirstOrDefault();
        if (firstInstance == null)
            return null;

        return new ActiveJob
        {
            JobId = context.Message.JobId,
            InstanceAddress = firstInstance.InstanceAddress
        };
    }
}
