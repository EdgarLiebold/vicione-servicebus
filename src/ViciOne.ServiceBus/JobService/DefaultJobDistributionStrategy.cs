using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Defines the strategy for default job distribution.</summary>
public class DefaultJobDistributionStrategy :
    IJobDistributionStrategy
{
    /// <summary>Exposes the instance used by the containing type.</summary>
    public static readonly IJobDistributionStrategy Instance = new DefaultJobDistributionStrategy();

    /// <summary>Determines whether job slot available.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="jobTypeInfo">The job type info.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the is job slot available outcome.</returns>
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
