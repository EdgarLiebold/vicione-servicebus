using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Requests recurring delivery of a typed message.</summary>
/// <typeparam name="T">The message contract.</typeparam>
public sealed class ScheduleRecurringMessageCommand<T> :
    ScheduleRecurringMessage
    where T : class
{
    /// <summary>Creates a recurring-delivery command.</summary>
    /// <param name="schedule">The recurring schedule.</param>
    /// <param name="destination">The delivery destination.</param>
    /// <param name="payload">The message payload.</param>
    public ScheduleRecurringMessageCommand(RecurringSchedule schedule, Uri destination, T payload)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(payload);

        Schedule = schedule;
        Destination = destination;
        Payload = payload;

        PayloadType = MessageTypeCache<T>.MessageTypeNames.ToArray();
    }

    /// <summary>Gets or sets the schedule.</summary>
    public RecurringSchedule Schedule { get; }
    /// <summary>Gets or sets the payload type.</summary>
    public string[] PayloadType { get; }
    /// <summary>Gets or sets the destination.</summary>
    public Uri Destination { get; }
    /// <summary>Gets or sets the payload.</summary>
    public object Payload { get; }
    /// <summary>Returns the string representation of this instance.</summary>
    /// <returns>The converted string.</returns>
    public override string ToString()
    {
        return
            $"Group: {Schedule.ScheduleGroup}, Id: {Schedule.ScheduleId}, StartTime: {Schedule.StartTime}, EndTime: {Schedule.EndTime}, CronExpression: {Schedule.CronExpression}, TimeZone: {Schedule.TimeZoneId}";
    }
}


/// <summary>Requests recurring delivery of a runtime-typed message.</summary>
public sealed class ScheduleRecurringMessageCommand :
    ScheduleRecurringMessage
{
    /// <summary>Gets or sets the schedule.</summary>
    public RecurringSchedule Schedule { get; set; } = null!;
    /// <summary>Gets or sets the payload type.</summary>
    public string[] PayloadType { get; set; } = null!;
    /// <summary>Gets or sets the destination.</summary>
    public Uri Destination { get; set; } = null!;
    /// <summary>Gets or sets the payload.</summary>
    public object Payload { get; set; } = null!;
}
