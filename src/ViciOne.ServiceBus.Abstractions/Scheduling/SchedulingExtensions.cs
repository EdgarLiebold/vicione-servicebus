using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides extension methods for scheduling.
/// </summary>
public static class SchedulingExtensions
{
    /// <summary>
    /// Gets scheduling token id.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public static Guid? GetSchedulingTokenId(this ConsumeContext context)
    {
        return context.Headers.Get(MessageHeaders.SchedulingTokenId, default(Guid?));
    }

    /// <summary>
    /// Gets quartz scheduled.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public static DateTimeOffset? GetQuartzScheduled(this ConsumeContext context)
    {
        return context.Headers.Get(MessageHeaders.Quartz.Scheduled, default(DateTimeOffset?));
    }

    /// <summary>
    /// Gets quartz sent.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public static DateTimeOffset? GetQuartzSent(this ConsumeContext context)
    {
        return context.Headers.Get(MessageHeaders.Quartz.Sent, default(DateTimeOffset?));
    }

    /// <summary>
    /// Gets quartz next scheduled.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public static DateTimeOffset? GetQuartzNextScheduled(this ConsumeContext context)
    {
        return context.Headers.Get(MessageHeaders.Quartz.NextScheduled, default(DateTimeOffset?));
    }

    /// <summary>
    /// Gets quartz previous sent.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public static DateTimeOffset? GetQuartzPreviousSent(this ConsumeContext context)
    {
        return context.Headers.Get(MessageHeaders.Quartz.PreviousSent, default(DateTimeOffset?));
    }

    /// <summary>
    /// Gets quartz schedule id.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public static string? GetQuartzScheduleId(this ConsumeContext context)
    {
        return context.Headers.Get(MessageHeaders.Quartz.ScheduleId, default(string?));
    }

    /// <summary>
    /// Gets quartz schedule group.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public static string? GetQuartzScheduleGroup(this ConsumeContext context)
    {
        return context.Headers.Get(MessageHeaders.Quartz.ScheduleGroup, default(string?));
    }
}
