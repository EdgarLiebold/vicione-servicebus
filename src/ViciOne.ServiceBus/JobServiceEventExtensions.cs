using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides extension methods for job service event.
/// </summary>
public static class JobServiceEventExtensions
{
    /// <summary>
    /// Returns the job from the message
    /// </summary>
    /// <typeparam name="TJob"></typeparam>
    /// <param name="context"></param>
    /// <returns></returns>
    public static TJob? GetJob<TJob>(this ConsumeContext<StartJob> context)
        where TJob : class
    {
        return context.Advanced().SerializerContext.DeserializeObject<TJob>(context.Message.Job);
    }

    /// <summary>
    /// Gets job.
    /// </summary>
    /// <typeparam name="TJob">The t job type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public static TJob? GetJob<TJob>(this ConsumeContext<FaultJob> context)
        where TJob : class
    {
        return context.Advanced().SerializerContext.DeserializeObject<TJob>(context.Message.Job);
    }

    /// <summary>
    /// Gets job.
    /// </summary>
    /// <typeparam name="TJob">The t job type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public static TJob? GetJob<TJob>(this ConsumeContext<CompleteJob> context)
        where TJob : class
    {
        return context.Advanced().SerializerContext.DeserializeObject<TJob>(context.Message.Job);
    }

    /// <summary>
    /// Gets job.
    /// </summary>
    /// <typeparam name="TJob">The t job type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public static TJob? GetJob<TJob>(this ConsumeContext<JobCompleted> context)
        where TJob : class
    {
        return context.Advanced().SerializerContext.DeserializeObject<TJob>(context.Message.Job);
    }

    /// <summary>
    /// Gets job.
    /// </summary>
    /// <typeparam name="TJob">The t job type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public static TJob? GetJob<TJob>(this ConsumeContext<JobFaulted> context)
        where TJob : class
    {
        return context.Advanced().SerializerContext.DeserializeObject<TJob>(context.Message.Job);
    }
}
