using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Reads scheduling metadata from consumed messages.</summary>
public static class SchedulingExtensions
{
    /// <summary>Gets the token that identifies a scheduled message.</summary>
    /// <param name="context">The consumed message context.</param>
    /// <returns>The scheduling token, or <see langword="null" /> when the message was not scheduled.</returns>
    public static Guid? GetSchedulingTokenId(this ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Headers.Get(MessageHeaders.SchedulingTokenId, default(Guid?));
    }

    /// <summary>Gets the time at which Quartz registered the schedule.</summary>
    /// <param name="context">The consumed message context.</param>
    /// <returns>The registration time, or <see langword="null" /> when the header is absent.</returns>
    public static DateTimeOffset? GetQuartzScheduled(this ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Headers.Get(MessageHeaders.Quartz.Scheduled, default(DateTimeOffset?));
    }

    /// <summary>Gets the time at which Quartz fired the current occurrence.</summary>
    /// <param name="context">The consumed message context.</param>
    /// <returns>The trigger time, or <see langword="null" /> when the header is absent.</returns>
    public static DateTimeOffset? GetQuartzSent(this ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Headers.Get(MessageHeaders.Quartz.Sent, default(DateTimeOffset?));
    }

    /// <summary>Gets the next occurrence calculated by Quartz.</summary>
    /// <param name="context">The consumed message context.</param>
    /// <returns>The next occurrence, or <see langword="null" /> when none is available.</returns>
    public static DateTimeOffset? GetQuartzNextScheduled(this ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Headers.Get(MessageHeaders.Quartz.NextScheduled, default(DateTimeOffset?));
    }

    /// <summary>Gets the previous occurrence fired by Quartz.</summary>
    /// <param name="context">The consumed message context.</param>
    /// <returns>The previous occurrence, or <see langword="null" /> when none is available.</returns>
    public static DateTimeOffset? GetQuartzPreviousSent(this ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Headers.Get(MessageHeaders.Quartz.PreviousSent, default(DateTimeOffset?));
    }

    /// <summary>Gets the Quartz schedule identifier.</summary>
    /// <param name="context">The consumed message context.</param>
    /// <returns>The schedule identifier, or <see langword="null" /> when the header is absent.</returns>
    public static string? GetQuartzScheduleId(this ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Headers.Get(MessageHeaders.Quartz.ScheduleId, default(string?));
    }

    /// <summary>Gets the Quartz schedule group.</summary>
    /// <param name="context">The consumed message context.</param>
    /// <returns>The schedule group, or <see langword="null" /> when the header is absent.</returns>
    public static string? GetQuartzScheduleGroup(this ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Headers.Get(MessageHeaders.Quartz.ScheduleGroup, default(string?));
    }
}
