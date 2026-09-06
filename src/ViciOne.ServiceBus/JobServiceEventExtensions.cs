using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides extension methods for job service event.</summary>
public static class JobServiceEventExtensions
{
    /// <summary>Returns the job from the message.</summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The job.</returns>
    public static TJob? GetJob<TJob>(this ConsumeContext<StartJob> context)
        where TJob : class
    {
        return context.Advanced().SerializerContext.DeserializeObject<TJob>(context.Message.Job);
    }

    /// <summary>Gets job.</summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The job.</returns>
    public static TJob? GetJob<TJob>(this ConsumeContext<FaultJob> context)
        where TJob : class
    {
        return context.Advanced().SerializerContext.DeserializeObject<TJob>(context.Message.Job);
    }

    /// <summary>Gets job.</summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The job.</returns>
    public static TJob? GetJob<TJob>(this ConsumeContext<CompleteJob> context)
        where TJob : class
    {
        return context.Advanced().SerializerContext.DeserializeObject<TJob>(context.Message.Job);
    }

    /// <summary>Gets job.</summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The job.</returns>
    public static TJob? GetJob<TJob>(this ConsumeContext<JobCompleted> context)
        where TJob : class
    {
        return context.Advanced().SerializerContext.DeserializeObject<TJob>(context.Message.Job);
    }

    /// <summary>Gets job.</summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The job.</returns>
    public static TJob? GetJob<TJob>(this ConsumeContext<JobFaulted> context)
        where TJob : class
    {
        return context.Advanced().SerializerContext.DeserializeObject<TJob>(context.Message.Job);
    }
}
