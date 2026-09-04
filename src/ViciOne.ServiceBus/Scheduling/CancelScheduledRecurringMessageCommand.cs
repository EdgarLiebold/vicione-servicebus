using System;

namespace ViciOne.ServiceBus.Scheduling;

public class CancelScheduledRecurringMessageCommand :
    CancelScheduledRecurringMessage
{
    public CancelScheduledRecurringMessageCommand()
    {
    }

    public CancelScheduledRecurringMessageCommand(string scheduleId, string scheduleGroup, DateTimeOffset timestamp)
    {
        Timestamp = timestamp;

        ScheduleId = scheduleId;
        ScheduleGroup = scheduleGroup;
    }

    public DateTimeOffset Timestamp { get; set; }
    public string ScheduleId { get; set; } = null!;
    public string ScheduleGroup { get; set; } = null!;
}
