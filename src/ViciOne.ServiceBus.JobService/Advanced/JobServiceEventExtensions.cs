using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides typed access to serialized job payloads carried by job-service lifecycle events.</summary>
public static class JobServiceEventExtensions
{
    /// <summary>Deserializes the job carried by a start event.</summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    /// <param name="context">The start-event context.</param>
    /// <returns>The deserialized job, or <see langword="null"/> when the payload represents null.</returns>
    public static TJob? GetJob<TJob>(this ConsumeContext<IStartJob> context)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Advanced().SerializerContext.DeserializeObject<TJob>(context.Message.Job);
    }

    /// <summary>Deserializes the job carried by a fault event.</summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    /// <param name="context">The fault-event context.</param>
    /// <returns>The deserialized job, or <see langword="null"/> when the payload represents null.</returns>
    public static TJob? GetJob<TJob>(this ConsumeContext<IFaultJob> context)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Advanced().SerializerContext.DeserializeObject<TJob>(context.Message.Job);
    }

    /// <summary>Deserializes the job carried by a completion event.</summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    /// <param name="context">The completion-event context.</param>
    /// <returns>The deserialized job, or <see langword="null"/> when the payload represents null.</returns>
    public static TJob? GetJob<TJob>(this ConsumeContext<ICompleteJob> context)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Advanced().SerializerContext.DeserializeObject<TJob>(context.Message.Job);
    }

    /// <summary>Deserializes the job carried by a completed-job notification.</summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    /// <param name="context">The completed-job context.</param>
    /// <returns>The deserialized job, or <see langword="null"/> when the payload represents null.</returns>
    public static TJob? GetJob<TJob>(this ConsumeContext<IJobCompleted> context)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Advanced().SerializerContext.DeserializeObject<TJob>(context.Message.Job);
    }

    /// <summary>Deserializes the job carried by a faulted-job notification.</summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    /// <param name="context">The faulted-job context.</param>
    /// <returns>The deserialized job, or <see langword="null"/> when the payload represents null.</returns>
    public static TJob? GetJob<TJob>(this ConsumeContext<IJobFaulted> context)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Advanced().SerializerContext.DeserializeObject<TJob>(context.Message.Job);
    }
}
