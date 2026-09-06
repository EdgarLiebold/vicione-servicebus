namespace ViciOne.ServiceBus.Advanced.Initializers;

/// <summary>Provides scheduling overloads that initialize contract messages from object values.</summary>
public static class AdvancedScheduleInitializerExtensions
{
    /// <summary>Schedules a message initialized from object values.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="scheduler">The scheduler used by the operation.</param>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this IMessageScheduler scheduler, Uri destination, DateTimeOffset dueAt,
        object values, CancellationToken cancellationToken = default)
        where T : class
    {
        return scheduler.Advanced().ScheduleSendAsync<T>(destination, dueAt, values, cancellationToken);
    }

    /// <summary>Schedules an initialized message through a typed send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="scheduler">The scheduler used by the operation.</param>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this IMessageScheduler scheduler, Uri destination, DateTimeOffset dueAt,
        object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return scheduler.Advanced().ScheduleSendAsync(destination, dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Schedules an initialized message through an untyped send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="scheduler">The scheduler used by the operation.</param>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this IMessageScheduler scheduler, Uri destination, DateTimeOffset dueAt,
        object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return scheduler.Advanced().ScheduleSendAsync<T>(destination, dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Schedules a publication initialized from object values.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="scheduler">The scheduler used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, DateTimeOffset dueAt, object values,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return scheduler.Advanced().SchedulePublishAsync<T>(dueAt, values, cancellationToken);
    }

    /// <summary>Schedules an initialized publication through a typed send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="scheduler">The scheduler used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, DateTimeOffset dueAt, object values,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return scheduler.Advanced().SchedulePublishAsync(dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Schedules an initialized publication through an untyped send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="scheduler">The scheduler used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, DateTimeOffset dueAt, object values,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return scheduler.Advanced().SchedulePublishAsync<T>(dueAt, values, pipe, cancellationToken);
    }
}
