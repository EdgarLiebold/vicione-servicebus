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
            CorrelationId = NewId.NextGuid();
            Timestamp = timestamp;

            ScheduleId = scheduleId;
            ScheduleGroup = scheduleGroup;
        }

        public Guid CorrelationId { get; set; }
        public DateTime Timestamp { get; set; }
        public string ScheduleId { get; set; }
        public string ScheduleGroup { get; set; }
    }
}
