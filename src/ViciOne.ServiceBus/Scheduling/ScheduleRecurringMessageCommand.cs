using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// Provides a schedule recurring message command implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ScheduleRecurringMessageCommand<T> :
    ScheduleRecurringMessage
    where T : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="destination">The destination value.</param>
    /// <param name="payload">The payload value.</param>
    public ScheduleRecurringMessageCommand(RecurringSchedule schedule, Uri destination, T payload)
    {
        Schedule = schedule;

        Destination = destination;
        Payload = payload;

        PayloadType = MessageTypeCache<T>.MessageTypeNames.ToArray();
    }

    /// <summary>
    /// Gets or sets the schedule value.
    /// </summary>
    public RecurringSchedule Schedule { get; private set; } = null!;
    /// <summary>
    /// Gets or sets the payload type value.
    /// </summary>
    public string[] PayloadType { get; private set; } = null!;
    /// <summary>
    /// Gets or sets the destination value.
    /// </summary>
    public Uri Destination { get; private set; } = null!;
    /// <summary>
    /// Gets or sets the payload value.
    /// </summary>
    public object Payload { get; private set; } = null!;
    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override string ToString()
    {
        return
            $"Group: {Schedule.ScheduleGroup}, Id: {Schedule.ScheduleId}, StartTime: {Schedule.StartTime}, EndTime: {Schedule.EndTime}, CronExpression: {Schedule.CronExpression}, TimeZone: {Schedule.TimeZoneId}";
    }
}


/// <summary>
/// Provides a schedule recurring message command implementation.
/// </summary>
public class ScheduleRecurringMessageCommand :
    ScheduleRecurringMessage
{
    /// <summary>
    /// Gets or sets the schedule value.
    /// </summary>
    public RecurringSchedule Schedule { get; set; } = null!;
    /// <summary>
    /// Gets or sets the payload type value.
    /// </summary>
    public string[] PayloadType { get; set; } = null!;
    /// <summary>
    /// Gets or sets the destination value.
    /// </summary>
    public Uri Destination { get; set; } = null!;
    /// <summary>
    /// Gets or sets the payload value.
    /// </summary>
    public object Payload { get; set; } = null!;
}
