using System;

namespace ViciOne.ServiceBus.Scheduling;

public class ResumeScheduledRecurringMessageCommand :
    ResumeScheduledRecurringMessage
{
    public ResumeScheduledRecurringMessageCommand()
    {
    }

    public ResumeScheduledRecurringMessageCommand(string scheduleId, string scheduleGroup, DateTimeOffset timestamp)
    {
        Timestamp = timestamp;

        ScheduleId = scheduleId;
        ScheduleGroup = scheduleGroup;
    }

    public DateTimeOffset Timestamp { get; set; }
    public string ScheduleId { get; set; } = null!;
    public string ScheduleGroup { get; set; } = null!;
}
