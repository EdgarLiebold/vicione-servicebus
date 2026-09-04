using System;

namespace ViciOne.ServiceBus.Scheduling;

public class CancelScheduledRecurringMessageCommand :
    CancelScheduledRecurringMessage
{
    public CancelScheduledRecurringMessageCommand()
    {
    }

    public CancelScheduledRecurringMessageCommand(string scheduleId, string scheduleGroup, DateTime timestamp)
    {
        Timestamp = timestamp;

        ScheduleId = scheduleId;
        ScheduleGroup = scheduleGroup;
    }

    public DateTime Timestamp { get; set; }
    public string ScheduleId { get; set; }
    public string ScheduleGroup { get; set; }
}
