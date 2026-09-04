using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides extension methods for send endpoint recurring scheduler.
/// </summary>
public static class SendEndpointRecurringSchedulerExtensions
{
    /// <summary>
    /// Schedule a message for recurring delivery using the specified schedule
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="endpoint">The message scheduler endpoint</param>
    /// <param name="message">The message</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="schedule">The schedule for the message to be delivered</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(this ISendEndpoint endpoint, Uri destinationAddress,
        RecurringSchedule schedule, T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        IRecurringMessageScheduler scheduler = new EndpointRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync(destinationAddress, schedule, message, cancellationToken);
    }

    /// <summary>
    /// Schedule a message for recurring delivery using the specified schedule
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="endpoint">The message scheduler endpoint</param>
    /// <param name="message">The message</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="schedule">The schedule for the message to be delivered</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(this ISendEndpoint endpoint, Uri destinationAddress,
        RecurringSchedule schedule, T message,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        IRecurringMessageScheduler scheduler = new EndpointRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync(destinationAddress, schedule, message, pipe, cancellationToken);
    }

    /// <summary>
    /// Schedule a message for recurring delivery using the specified schedule
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="endpoint">The message scheduler endpoint</param>
    /// <param name="message">The message</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="schedule">The schedule for the message to be delivered</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(this ISendEndpoint endpoint, Uri destinationAddress,
        RecurringSchedule schedule, T message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        IRecurringMessageScheduler scheduler = new EndpointRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync(destinationAddress, schedule, message, pipe, cancellationToken);
    }

    /// <summary>
    /// Schedule a message for recurring delivery using the specified schedule
    /// </summary>
    /// <param name="endpoint">The message scheduler endpoint</param>
    /// <param name="message">The message object</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="schedule">The schedule for the message to be delivered</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(this ISendEndpoint endpoint, Uri destinationAddress, RecurringSchedule schedule,
        object message, CancellationToken cancellationToken = default)
    {
        IRecurringMessageScheduler scheduler = new EndpointRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync(destinationAddress, schedule, message, cancellationToken);
    }

    /// <summary>
    /// Schedule a message for recurring delivery using the specified schedule
    /// </summary>
    /// <param name="endpoint">The message scheduler endpoint</param>
    /// <param name="message">The message object</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="schedule">The schedule for the message to be delivered</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired)</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(this ISendEndpoint endpoint, Uri destinationAddress, RecurringSchedule schedule,
        object message, Type messageType, CancellationToken cancellationToken = default)
    {
        IRecurringMessageScheduler scheduler = new EndpointRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync(destinationAddress, schedule, message, messageType, cancellationToken);
    }

    /// <summary>
    /// Schedule a message for recurring delivery using the specified schedule
    /// </summary>
    /// <param name="endpoint">The message scheduler endpoint</param>
    /// <param name="message">The message object</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="schedule">The schedule for the message to be delivered</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(this ISendEndpoint endpoint, Uri destinationAddress, RecurringSchedule schedule,
        object message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        IRecurringMessageScheduler scheduler = new EndpointRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync(destinationAddress, schedule, message, pipe, cancellationToken);
    }

    /// <summary>
    /// Schedule a message for recurring delivery using the specified schedule
    /// </summary>
    /// <param name="endpoint">The message scheduler endpoint</param>
    /// <param name="message">The message object</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="schedule">The schedule for the message to be delivered</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired)</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(this ISendEndpoint endpoint, Uri destinationAddress, RecurringSchedule schedule,
        object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        IRecurringMessageScheduler scheduler = new EndpointRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync(destinationAddress, schedule, message, messageType, pipe, cancellationToken);
    }

    /// <summary>
    /// Schedule a message for recurring delivery using the specified schedule
    /// </summary>
    /// <typeparam name="T">The interface type to send</typeparam>
    /// <param name="endpoint">The message scheduler endpoint</param>
    /// <param name="values">The property values to initialize on the interface</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="schedule">The schedule for the message to be delivered</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(this ISendEndpoint endpoint, Uri destinationAddress,
        RecurringSchedule schedule, object values, CancellationToken cancellationToken = default)
        where T : class
    {
        IRecurringMessageScheduler scheduler = new EndpointRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync<T>(destinationAddress, schedule, values, cancellationToken);
    }

    /// <summary>
    /// Schedule a message for recurring delivery using the specified schedule
    /// </summary>
    /// <typeparam name="T">The interface type to send</typeparam>
    /// <param name="endpoint">The message scheduler endpoint</param>
    /// <param name="values">The property values to initialize on the interface</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="schedule">The schedule for the message to be delivered</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(this ISendEndpoint endpoint, Uri destinationAddress,
        RecurringSchedule schedule, object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        IRecurringMessageScheduler scheduler = new EndpointRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync(destinationAddress, schedule, values, pipe, cancellationToken);
    }

    /// <summary>
    /// Schedule a message for recurring delivery using the specified schedule
    /// </summary>
    /// <typeparam name="T">The interface type to send</typeparam>
    /// <param name="endpoint">The message scheduler endpoint</param>
    /// <param name="values">The property values to initialize on the interface</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="schedule">The schedule for the message to be delivered</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(this ISendEndpoint endpoint, Uri destinationAddress,
        RecurringSchedule schedule, object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        IRecurringMessageScheduler scheduler = new EndpointRecurringMessageScheduler(endpoint);

        return scheduler.ScheduleRecurringSendAsync<T>(destinationAddress, schedule, values, pipe, cancellationToken);
    }

    /// <summary>
    /// Cancel a scheduled message using the scheduled message instance
    /// </summary>
    /// <param name="endpoint">The endpoint of the scheduling service</param>
    /// <param name="message">The schedule message reference</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task CancelScheduledRecurringSendAsync<T>(this ISendEndpoint endpoint, ScheduledRecurringMessage<T> message, CancellationToken cancellationToken = default)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return CancelScheduledRecurringSendAsync(endpoint, message.Schedule.ScheduleId, message.Schedule.ScheduleGroup, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Cancel a scheduled message using the tokenId that was returned when the message was scheduled.
    /// </summary>
    /// <param name="endpoint">The endpoint of the scheduling service</param>
    /// <param name="scheduleId">The scheduleId from the recurring schedule</param>
    /// <param name="scheduleGroup">The scheduleGroup from the recurring schedule</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task CancelScheduledRecurringSendAsync(this ISendEndpoint endpoint, string scheduleId, string scheduleGroup, CancellationToken cancellationToken = default)
    {
        IRecurringMessageScheduler scheduler = new EndpointRecurringMessageScheduler(endpoint);

        return scheduler.CancelScheduledRecurringSendAsync(scheduleId, scheduleGroup, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Pause a scheduled message using the scheduled message instance
    /// </summary>
    /// <param name="endpoint">The endpoint of the scheduling service</param>
    /// <param name="message">The schedule message reference</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task PauseScheduledRecurringSendAsync<T>(this ISendEndpoint endpoint, ScheduledRecurringMessage<T> message, CancellationToken cancellationToken = default)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return PauseScheduledRecurringSendAsync(endpoint, message.Schedule.ScheduleId, message.Schedule.ScheduleGroup, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Pause a scheduled message using the tokenId that was returned when the message was scheduled.
    /// </summary>
    /// <param name="endpoint">The endpoint of the scheduling service</param>
    /// <param name="scheduleId">The scheduleId from the recurring schedule</param>
    /// <param name="scheduleGroup">The scheduleGroup from the recurring schedule</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task PauseScheduledRecurringSendAsync(this ISendEndpoint endpoint, string scheduleId, string scheduleGroup, CancellationToken cancellationToken = default)
    {
        IRecurringMessageScheduler scheduler = new EndpointRecurringMessageScheduler(endpoint);

        return scheduler.PauseScheduledRecurringSendAsync(scheduleId, scheduleGroup, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Resume a scheduled message using the scheduled message instance
    /// </summary>
    /// <param name="endpoint">The endpoint of the scheduling service</param>
    /// <param name="message">The schedule message reference</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task ResumeScheduledRecurringSendAsync<T>(this ISendEndpoint endpoint, ScheduledRecurringMessage<T> message, CancellationToken cancellationToken = default)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return ResumeScheduledRecurringSendAsync(endpoint, message.Schedule.ScheduleId, message.Schedule.ScheduleGroup, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Resume a scheduled message using the tokenId that was returned when the message was scheduled.
    /// </summary>
    /// <param name="endpoint">The endpoint of the scheduling service</param>
    /// <param name="scheduleId">The scheduleId from the recurring schedule</param>
    /// <param name="scheduleGroup">The scheduleGroup from the recurring schedule</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task ResumeScheduledRecurringSendAsync(this ISendEndpoint endpoint, string scheduleId, string scheduleGroup, CancellationToken cancellationToken = default)
    {
        IRecurringMessageScheduler scheduler = new EndpointRecurringMessageScheduler(endpoint);

        return scheduler.ResumeScheduledRecurringSendAsync(scheduleId, scheduleGroup, cancellationToken: cancellationToken);
    }
}
