using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced.Middleware;

namespace ViciOne.ServiceBus.Advanced.Initializers;

/// <summary>Provides scheduling overloads that initialize contract messages from object values.</summary>
public static class AdvancedScheduleInitializerExtensions
{
    /// <summary>Schedules a message initialized from object values.</summary>
    /// <typeparam name="T">The message contract to initialize and send.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="destination">The address that receives the scheduled message.</param>
    /// <param name="dueAt">The UTC instant at which the message becomes due.</param>
    /// <param name="values">An object whose public properties provide the message values.</param>
    /// <param name="cancellationToken">The token that cancels scheduling.</param>
    /// <returns>A task that produces the scheduled-message identity and delivery metadata.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this IMessageScheduler scheduler, Uri destination, DateTimeOffset dueAt,
        object values, CancellationToken cancellationToken = default)
        where T : class
    {
        return RequireInputs(scheduler, destination, values).ScheduleSendAsync<T>(destination, dueAt, values, cancellationToken);
    }

    /// <summary>Schedules an initialized message through a typed send-context pipe.</summary>
    /// <typeparam name="T">The message contract to initialize and send.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="destination">The address that receives the scheduled message.</param>
    /// <param name="dueAt">The UTC instant at which the message becomes due.</param>
    /// <param name="values">An object whose public properties provide the message values.</param>
    /// <param name="pipe">The pipe that configures the typed send context.</param>
    /// <param name="cancellationToken">The token that cancels scheduling.</param>
    /// <returns>A task that produces the scheduled-message identity and delivery metadata.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this IMessageScheduler scheduler, Uri destination, DateTimeOffset dueAt,
        object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return RequireInputs(scheduler, destination, values, pipe).ScheduleSendAsync(destination, dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Schedules an initialized message through an untyped send-context pipe.</summary>
    /// <typeparam name="T">The message contract to initialize and send.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="destination">The address that receives the scheduled message.</param>
    /// <param name="dueAt">The UTC instant at which the message becomes due.</param>
    /// <param name="values">An object whose public properties provide the message values.</param>
    /// <param name="pipe">The pipe that configures the untyped send context.</param>
    /// <param name="cancellationToken">The token that cancels scheduling.</param>
    /// <returns>A task that produces the scheduled-message identity and delivery metadata.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this IMessageScheduler scheduler, Uri destination, DateTimeOffset dueAt,
        object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return RequireInputs(scheduler, destination, values, pipe).ScheduleSendAsync<T>(destination, dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Schedules a publication initialized from object values.</summary>
    /// <typeparam name="T">The message contract to initialize and publish.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="dueAt">The UTC instant at which the publication becomes due.</param>
    /// <param name="values">An object whose public properties provide the message values.</param>
    /// <param name="cancellationToken">The token that cancels scheduling.</param>
    /// <returns>A task that produces the scheduled-publication identity and delivery metadata.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, DateTimeOffset dueAt, object values,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return RequireInputs(scheduler, values).SchedulePublishAsync<T>(dueAt, values, cancellationToken);
    }

    /// <summary>Schedules an initialized publication through a typed send-context pipe.</summary>
    /// <typeparam name="T">The message contract to initialize and publish.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="dueAt">The UTC instant at which the publication becomes due.</param>
    /// <param name="values">An object whose public properties provide the message values.</param>
    /// <param name="pipe">The pipe that configures the typed send context used for publication.</param>
    /// <param name="cancellationToken">The token that cancels scheduling.</param>
    /// <returns>A task that produces the scheduled-publication identity and delivery metadata.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, DateTimeOffset dueAt, object values,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return RequireInputs(scheduler, values, pipe).SchedulePublishAsync(dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Schedules an initialized publication through an untyped send-context pipe.</summary>
    /// <typeparam name="T">The message contract to initialize and publish.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="dueAt">The UTC instant at which the publication becomes due.</param>
    /// <param name="values">An object whose public properties provide the message values.</param>
    /// <param name="pipe">The pipe that configures the untyped send context used for publication.</param>
    /// <param name="cancellationToken">The token that cancels scheduling.</param>
    /// <returns>A task that produces the scheduled-publication identity and delivery metadata.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, DateTimeOffset dueAt, object values,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return RequireInputs(scheduler, values, pipe).SchedulePublishAsync<T>(dueAt, values, pipe, cancellationToken);
    }

    static IAdvancedMessageScheduler RequireInputs(IMessageScheduler scheduler, Uri destination, object values)
    {
        IAdvancedMessageScheduler advanced = scheduler.Advanced();
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(values);
        return advanced;
    }

    static IAdvancedMessageScheduler RequireInputs(IMessageScheduler scheduler, Uri destination, object values, object pipe)
    {
        IAdvancedMessageScheduler advanced = RequireInputs(scheduler, destination, values);
        ArgumentNullException.ThrowIfNull(pipe);
        return advanced;
    }

    static IAdvancedMessageScheduler RequireInputs(IMessageScheduler scheduler, object values)
    {
        IAdvancedMessageScheduler advanced = scheduler.Advanced();
        ArgumentNullException.ThrowIfNull(values);
        return advanced;
    }

    static IAdvancedMessageScheduler RequireInputs(IMessageScheduler scheduler, object values, object pipe)
    {
        IAdvancedMessageScheduler advanced = RequireInputs(scheduler, values);
        ArgumentNullException.ThrowIfNull(pipe);
        return advanced;
    }
}
