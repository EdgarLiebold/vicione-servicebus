namespace ViciOne.ServiceBus.Scheduling
{
    using System;


    public class ResumeScheduledRecurringMessageCommand :
        ResumeScheduledRecurringMessage
    {
        public ResumeScheduledRecurringMessageCommand()
        {
        }

        public ResumeScheduledRecurringMessageCommand(string scheduleId, string scheduleGroup, DateTime timestamp)
        {
            Timestamp = timestamp;

            ScheduleId = scheduleId;
            ScheduleGroup = scheduleGroup;
        }

        public DateTime Timestamp { get; set; }
        public string ScheduleId { get; set; }
        public string ScheduleGroup { get; set; }
    }
}
