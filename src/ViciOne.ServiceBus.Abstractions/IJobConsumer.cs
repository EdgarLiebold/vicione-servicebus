using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines a message consumer which runs a job asynchronously, without waiting, which is monitored by Conductor
/// services, to monitor the job, limit concurrency, etc.
/// </summary>
/// <typeparam name="TJob">The job message type.</typeparam>
public interface IJobConsumer<in TJob> :
    IConsumer
    where TJob : class
{
    /// <summary>Runs the configured operation.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RunAsync(JobContext<TJob> context);
}
