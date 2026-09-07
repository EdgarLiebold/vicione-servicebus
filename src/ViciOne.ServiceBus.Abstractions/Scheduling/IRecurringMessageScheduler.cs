using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Schedules recurring messages and controls their lifecycle.</summary>
public interface IRecurringMessageScheduler
{
    /// <summary>Gets the clock used to timestamp scheduling commands.</summary>
    TimeProvider TimeProvider { get; }

    /// <summary>Schedules a recurring message for delivery to a destination.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="destinationAddress">The address to which each occurrence will be delivered.</param>
    /// <param name="schedule">The recurring delivery schedule.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(Uri destinationAddress, RecurringSchedule schedule, T message,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a recurring message with a typed send pipe.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="destinationAddress">The address to which each occurrence will be delivered.</param>
    /// <param name="schedule">The recurring delivery schedule.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(Uri destinationAddress, RecurringSchedule schedule, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a recurring message with an untyped send pipe.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="destinationAddress">The address to which each occurrence will be delivered.</param>
    /// <param name="schedule">The recurring delivery schedule.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(Uri destinationAddress, RecurringSchedule schedule, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a recurring object using its runtime contract type.</summary>
    /// <param name="destinationAddress">The address to which each occurrence will be delivered.</param>
    /// <param name="schedule">The recurring delivery schedule.</param>
    /// <param name="message">The message object.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(Uri destinationAddress, RecurringSchedule schedule, object message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a recurring object using an explicit contract type. The message must be assignable
    /// to that type.
    /// </summary>
    /// <param name="destinationAddress">The address to which each occurrence will be delivered.</param>
    /// <param name="schedule">The recurring delivery schedule.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired).</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(Uri destinationAddress, RecurringSchedule schedule, object message, Type messageType,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules a recurring object with an untyped send pipe.</summary>
    /// <param name="destinationAddress">The address to which each occurrence will be delivered.</param>
    /// <param name="schedule">The recurring delivery schedule.</param>
    /// <param name="message">The message object.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(Uri destinationAddress, RecurringSchedule schedule, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a recurring object with an explicit contract type and an untyped send pipe. The
    /// message must be assignable to the specified type.
    /// </summary>
    /// <param name="destinationAddress">The address to which each occurrence will be delivered.</param>
    /// <param name="schedule">The recurring delivery schedule.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired).</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage> ScheduleRecurringSendAsync(Uri destinationAddress, RecurringSchedule schedule, object message, Type messageType,
        IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes a recurring message contract from the supplied values and schedules it for
    /// delivery to a destination.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="destinationAddress">The address to which each occurrence will be delivered.</param>
    /// <param name="schedule">The recurring delivery schedule.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(Uri destinationAddress, RecurringSchedule schedule, object values,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Initializes a recurring message contract from the supplied values and schedules it with a
    /// typed send pipe.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="destinationAddress">The address to which each occurrence will be delivered.</param>
    /// <param name="schedule">The recurring delivery schedule.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(Uri destinationAddress, RecurringSchedule schedule, object values,
        IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Initializes a recurring message contract from the supplied values and schedules it with an
    /// untyped send pipe.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="destinationAddress">The address to which each occurrence will be delivered.</param>
    /// <param name="schedule">The recurring delivery schedule.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage<T>> ScheduleRecurringSendAsync<T>(Uri destinationAddress, RecurringSchedule schedule, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a recurring message for publication.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="schedule">The recurring publication schedule.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage<T>> ScheduleRecurringPublishAsync<T>(RecurringSchedule schedule, T message, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a recurring publication with a typed send pipe.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="schedule">The recurring publication schedule.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage<T>> ScheduleRecurringPublishAsync<T>(RecurringSchedule schedule, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a recurring publication with an untyped send pipe.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="schedule">The recurring publication schedule.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage<T>> ScheduleRecurringPublishAsync<T>(RecurringSchedule schedule, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a recurring publication using the object's runtime contract type.</summary>
    /// <param name="schedule">The recurring publication schedule.</param>
    /// <param name="message">The message object.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage> ScheduleRecurringPublishAsync(RecurringSchedule schedule, object message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a recurring publication using an explicit contract type. The message must be
    /// assignable to that type.
    /// </summary>
    /// <param name="schedule">The recurring publication schedule.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired).</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage> ScheduleRecurringPublishAsync(RecurringSchedule schedule, object message, Type messageType,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules a recurring object publication with an untyped send pipe.</summary>
    /// <param name="schedule">The recurring publication schedule.</param>
    /// <param name="message">The message object.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage> ScheduleRecurringPublishAsync(RecurringSchedule schedule, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a recurring object publication with an explicit contract type and an untyped send
    /// pipe. The message must be assignable to the specified type.
    /// </summary>
    /// <param name="schedule">The recurring publication schedule.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired).</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage> ScheduleRecurringPublishAsync(RecurringSchedule schedule, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes a recurring message contract from the supplied values and schedules it for
    /// publication.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="schedule">The recurring publication schedule.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage<T>> ScheduleRecurringPublishAsync<T>(RecurringSchedule schedule, object values,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Initializes a recurring message contract from the supplied values and schedules it for
    /// publication with a typed send pipe.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="schedule">The recurring publication schedule.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage<T>> ScheduleRecurringPublishAsync<T>(RecurringSchedule schedule, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Initializes a recurring message contract from the supplied values and schedules it for
    /// publication with an untyped send pipe.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="schedule">The recurring publication schedule.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    Task<ScheduledRecurringMessage<T>> ScheduleRecurringPublishAsync<T>(RecurringSchedule schedule, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Cancels a recurring schedule.</summary>
    /// <param name="scheduleId">The identifier of the recurring schedule.</param>
    /// <param name="scheduleGroup">The group that scopes the schedule identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CancelScheduledRecurringSendAsync(string scheduleId, string scheduleGroup, CancellationToken cancellationToken = default);

    /// <summary>Pauses a recurring schedule.</summary>
    /// <param name="scheduleId">The identifier of the recurring schedule.</param>
    /// <param name="scheduleGroup">The group that scopes the schedule identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PauseScheduledRecurringSendAsync(string scheduleId, string scheduleGroup, CancellationToken cancellationToken = default);

    /// <summary>Resumes a recurring schedule.</summary>
    /// <param name="scheduleId">The identifier of the recurring schedule.</param>
    /// <param name="scheduleGroup">The group that scopes the schedule identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ResumeScheduledRecurringSendAsync(string scheduleId, string scheduleGroup, CancellationToken cancellationToken = default);
}
