using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Manages recurring sends by publishing scheduler commands.</summary>
public static class PublishEndpointRecurringSchedulerExtensions
{
    /// <summary>Schedules a typed message for recurring delivery.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="endpoint">The message scheduler endpoint.</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent.</param>
    /// <param name="schedule">The schedule for the message to be delivered.</param>
    /// <param name="message">The message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(this IPublishEndpoint endpoint, Uri destinationAddress,
        RecurringSchedule schedule, T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        IRecurringMessageScheduler scheduler = new PublishRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync(destinationAddress, schedule, message, cancellationToken);
    }

    /// <summary>Schedules a typed message for recurring delivery through a typed send pipe.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="endpoint">The message scheduler endpoint.</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent.</param>
    /// <param name="schedule">The schedule for the message to be delivered.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(this IPublishEndpoint endpoint, Uri destinationAddress,
        RecurringSchedule schedule, T message,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        IRecurringMessageScheduler scheduler = new PublishRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync(destinationAddress, schedule, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a typed message for recurring delivery through an untyped send pipe.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="endpoint">The message scheduler endpoint.</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent.</param>
    /// <param name="schedule">The schedule for the message to be delivered.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(this IPublishEndpoint endpoint, Uri destinationAddress,
        RecurringSchedule schedule, T message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        IRecurringMessageScheduler scheduler = new PublishRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync(destinationAddress, schedule, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a runtime-typed message for recurring delivery.</summary>
    /// <param name="endpoint">The message scheduler endpoint.</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent.</param>
    /// <param name="schedule">The schedule for the message to be delivered.</param>
    /// <param name="message">The message object.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(this IPublishEndpoint endpoint, Uri destinationAddress, RecurringSchedule schedule,
        object message, CancellationToken cancellationToken = default)
    {
        IRecurringMessageScheduler scheduler = new PublishRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync(destinationAddress, schedule, message, cancellationToken);
    }

    /// <summary>Schedules a message as an explicit runtime contract type for recurring delivery.</summary>
    /// <param name="endpoint">The message scheduler endpoint.</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent.</param>
    /// <param name="schedule">The schedule for the message to be delivered.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The explicit contract type assigned to the scheduled message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(this IPublishEndpoint endpoint, Uri destinationAddress, RecurringSchedule schedule,
        object message, Type messageType, CancellationToken cancellationToken = default)
    {
        IRecurringMessageScheduler scheduler = new PublishRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync(destinationAddress, schedule, message, messageType, cancellationToken);
    }

    /// <summary>Schedules a runtime-typed message for recurring delivery through a send pipe.</summary>
    /// <param name="endpoint">The message scheduler endpoint.</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent.</param>
    /// <param name="schedule">The schedule for the message to be delivered.</param>
    /// <param name="message">The message object.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(this IPublishEndpoint endpoint, Uri destinationAddress, RecurringSchedule schedule,
        object message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        IRecurringMessageScheduler scheduler = new PublishRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync(destinationAddress, schedule, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a message as an explicit runtime contract type through a send pipe.</summary>
    /// <param name="endpoint">The message scheduler endpoint.</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent.</param>
    /// <param name="schedule">The schedule for the message to be delivered.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The explicit contract type assigned to the scheduled message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(this IPublishEndpoint endpoint, Uri destinationAddress, RecurringSchedule schedule,
        object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        IRecurringMessageScheduler scheduler = new PublishRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync(destinationAddress, schedule, message, messageType, pipe, cancellationToken);
    }

    /// <summary>Initializes and schedules a typed message for recurring delivery.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="endpoint">The message scheduler endpoint.</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent.</param>
    /// <param name="schedule">The schedule for the message to be delivered.</param>
    /// <param name="values">The property values used to initialize the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(this IPublishEndpoint endpoint, Uri destinationAddress,
        RecurringSchedule schedule, object values, CancellationToken cancellationToken = default)
        where T : class
    {
        IRecurringMessageScheduler scheduler = new PublishRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync<T>(destinationAddress, schedule, values, cancellationToken);
    }

    /// <summary>Initializes and schedules a typed message through a typed send pipe.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="endpoint">The message scheduler endpoint.</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent.</param>
    /// <param name="schedule">The schedule for the message to be delivered.</param>
    /// <param name="values">The property values used to initialize the message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(this IPublishEndpoint endpoint, Uri destinationAddress,
        RecurringSchedule schedule, object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        IRecurringMessageScheduler scheduler = new PublishRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync(destinationAddress, schedule, values, pipe, cancellationToken);
    }

    /// <summary>Initializes and schedules a typed message through an untyped send pipe.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="endpoint">The message scheduler endpoint.</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent.</param>
    /// <param name="schedule">The schedule for the message to be delivered.</param>
    /// <param name="values">The property values used to initialize the message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(this IPublishEndpoint endpoint, Uri destinationAddress,
        RecurringSchedule schedule, object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        IRecurringMessageScheduler scheduler = new PublishRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync<T>(destinationAddress, schedule, values, pipe, cancellationToken);
    }

    /// <summary>Cancels the schedule identified by a recurring-message handle.</summary>
    /// <typeparam name="T">The scheduled message type.</typeparam>
    /// <param name="endpoint">The endpoint of the scheduling service.</param>
    /// <param name="message">The schedule message reference.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the scheduler accepts the cancellation command.</returns>
    public static Task CancelScheduledRecurringSendAsync<T>(this IPublishEndpoint endpoint, ScheduledRecurringMessage<T> message, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(message.Schedule);

        return CancelScheduledRecurringSendAsync(endpoint, message.Schedule.ScheduleId, message.Schedule.ScheduleGroup, cancellationToken: cancellationToken);
    }

    /// <summary>Cancels a recurring schedule by its identifier and group.</summary>
    /// <param name="endpoint">The endpoint of the scheduling service.</param>
    /// <param name="scheduleId">The recurring schedule identifier.</param>
    /// <param name="scheduleGroup">The recurring schedule group.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the scheduler accepts the cancellation command.</returns>
    public static Task CancelScheduledRecurringSendAsync(this IPublishEndpoint endpoint, string scheduleId, string scheduleGroup, CancellationToken cancellationToken = default)
    {
        IRecurringMessageScheduler scheduler = new PublishRecurringMessageScheduler(endpoint);

        return scheduler.CancelScheduledRecurringSendAsync(scheduleId, scheduleGroup, cancellationToken: cancellationToken);
    }

    /// <summary>Pauses the schedule identified by a recurring-message handle.</summary>
    /// <typeparam name="T">The scheduled message type.</typeparam>
    /// <param name="endpoint">The endpoint of the scheduling service.</param>
    /// <param name="message">The schedule message reference.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the scheduler accepts the pause command.</returns>
    public static Task PauseScheduledRecurringSendAsync<T>(this IPublishEndpoint endpoint, ScheduledRecurringMessage<T> message, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(message.Schedule);

        return PauseScheduledRecurringSendAsync(endpoint, message.Schedule.ScheduleId, message.Schedule.ScheduleGroup, cancellationToken: cancellationToken);
    }

    /// <summary>Pauses a recurring schedule by its identifier and group.</summary>
    /// <param name="endpoint">The endpoint of the scheduling service.</param>
    /// <param name="scheduleId">The recurring schedule identifier.</param>
    /// <param name="scheduleGroup">The recurring schedule group.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the scheduler accepts the pause command.</returns>
    public static Task PauseScheduledRecurringSendAsync(this IPublishEndpoint endpoint, string scheduleId, string scheduleGroup, CancellationToken cancellationToken = default)
    {
        IRecurringMessageScheduler scheduler = new PublishRecurringMessageScheduler(endpoint);

        return scheduler.PauseScheduledRecurringSendAsync(scheduleId, scheduleGroup, cancellationToken: cancellationToken);
    }

    /// <summary>Resumes the schedule identified by a recurring-message handle.</summary>
    /// <typeparam name="T">The scheduled message type.</typeparam>
    /// <param name="endpoint">The endpoint of the scheduling service.</param>
    /// <param name="message">The schedule message reference.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the scheduler accepts the resume command.</returns>
    public static Task ResumeScheduledRecurringSendAsync<T>(this IPublishEndpoint endpoint, ScheduledRecurringMessage<T> message, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(message.Schedule);

        return ResumeScheduledRecurringSendAsync(endpoint, message.Schedule.ScheduleId, message.Schedule.ScheduleGroup, cancellationToken: cancellationToken);
    }

    /// <summary>Resumes a recurring schedule by its identifier and group.</summary>
    /// <param name="endpoint">The endpoint of the scheduling service.</param>
    /// <param name="scheduleId">The recurring schedule identifier.</param>
    /// <param name="scheduleGroup">The recurring schedule group.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the scheduler accepts the resume command.</returns>
    public static Task ResumeScheduledRecurringSendAsync(this IPublishEndpoint endpoint, string scheduleId, string scheduleGroup, CancellationToken cancellationToken = default)
    {
        IRecurringMessageScheduler scheduler = new PublishRecurringMessageScheduler(endpoint);

        return scheduler.ResumeScheduledRecurringSendAsync(scheduleId, scheduleGroup, cancellationToken: cancellationToken);
    }
}
