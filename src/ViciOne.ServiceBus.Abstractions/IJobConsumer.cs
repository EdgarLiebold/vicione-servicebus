using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>
/// Defines a message consumer which runs a job asynchronously, without waiting, which is monitored by Conductor
/// services, to monitor the job, limit concurrency, etc.
/// </summary>
/// <typeparam name="TJob">The job message type</typeparam>
public interface IJobConsumer<in TJob> :
    IConsumer
    where TJob : class
{
    Task RunAsync(JobContext<TJob> context);
}
