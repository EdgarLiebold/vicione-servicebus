using System;

namespace ViciOne.ServiceBus.Scheduling;

public class ScheduleRecurringMessageCommand<T> :
    ScheduleRecurringMessage
    where T : class
{
    public ScheduleRecurringMessageCommand(RecurringSchedule schedule, Uri destination, T payload)
    {
        Schedule = schedule;

        Destination = destination;
        Payload = payload;

        PayloadType = MessageTypeCache<T>.MessageTypeNames;
    }

    public RecurringSchedule Schedule { get; private set; } = null!;
    public string[] PayloadType { get; private set; } = null!;
    public Uri Destination { get; private set; } = null!;
    public object Payload { get; private set; } = null!;
    public override string ToString()
    {
        return
            $"Group: {Schedule.ScheduleGroup}, Id: {Schedule.ScheduleId}, StartTime: {Schedule.StartTime}, EndTime: {Schedule.EndTime}, CronExpression: {Schedule.CronExpression}, TimeZone: {Schedule.TimeZoneId}";
    }
}


public class ScheduleRecurringMessageCommand :
    ScheduleRecurringMessage
{
    public RecurringSchedule Schedule { get; set; } = null!;
    public string[] PayloadType { get; set; } = null!;
    public Uri Destination { get; set; } = null!;
    public object Payload { get; set; } = null!;
}
