using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Carries the command for schedule recurring message.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ScheduleRecurringMessageCommand<T> :
    ScheduleRecurringMessage
    where T : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="schedule">The schedule.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="payload">The payload.</param>
    public ScheduleRecurringMessageCommand(RecurringSchedule schedule, Uri destination, T payload)
    {
        Schedule = schedule;

        Destination = destination;
        Payload = payload;

        PayloadType = MessageTypeCache<T>.MessageTypeNames.ToArray();
    }

    /// <summary>Gets or sets the schedule.</summary>
    public RecurringSchedule Schedule { get; private set; } = null!;
    /// <summary>Gets or sets the payload type.</summary>
    public string[] PayloadType { get; private set; } = null!;
    /// <summary>Gets or sets the destination.</summary>
    public Uri Destination { get; private set; } = null!;
    /// <summary>Gets or sets the payload.</summary>
    public object Payload { get; private set; } = null!;
    /// <summary>Returns the string representation of this instance.</summary>
    /// <returns>The converted string.</returns>
    public override string ToString()
    {
        return
            $"Group: {Schedule.ScheduleGroup}, Id: {Schedule.ScheduleId}, StartTime: {Schedule.StartTime}, EndTime: {Schedule.EndTime}, CronExpression: {Schedule.CronExpression}, TimeZone: {Schedule.TimeZoneId}";
    }
}


/// <summary>Carries the command for schedule recurring message.</summary>
public class ScheduleRecurringMessageCommand :
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
