using System;

namespace ViciOne.ServiceBus.Scheduling;

public class PauseScheduledRecurringMessageCommand :
    PauseScheduledRecurringMessage
{
    public PauseScheduledRecurringMessageCommand()
    {
    }

    public PauseScheduledRecurringMessageCommand(string scheduleId, string scheduleGroup, DateTime timestamp)
    {
        Timestamp = timestamp;

        ScheduleId = scheduleId;
        ScheduleGroup = scheduleGroup;
    }

    public DateTime Timestamp { get; set; }
    public string ScheduleId { get; set; }
    public string ScheduleGroup { get; set; }
}
