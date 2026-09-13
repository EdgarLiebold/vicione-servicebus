using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Executes a long-running job under job-service lifecycle and concurrency control.</summary>
/// <typeparam name="TJob">The job message contract.</typeparam>
public interface IJobConsumer<in TJob> :
    IConsumer
    where TJob : class
{
    /// <summary>Runs a job to completion.</summary>
    /// <param name="context">The job message, attempt, and progress context.</param>
    /// <returns>The active job-execution task.</returns>
    Task RunAsync(IJobContext<TJob> context);
}
