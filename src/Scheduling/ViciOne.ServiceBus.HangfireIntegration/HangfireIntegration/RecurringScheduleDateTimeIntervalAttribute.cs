// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.HangfireIntegration
{
    using System;
    using System.Linq;
    using Hangfire;
    using Hangfire.Client;
    using Hangfire.Common;
    using Hangfire.Server;


    class RecurringScheduleDateTimeIntervalAttribute :
        JobFilterAttribute,
        IClientFilter,
        IServerFilter
    {
        public void OnCreating(CreatingContext filterContext)
        {
            var now = DateTimeOffset.Now;
            var data = filterContext.Job.Args.OfType<HangfireRecurringScheduledMessageData>().Single();

            if (data.StartTime > now)
                filterContext.Canceled = true;
            else if (data.EndTime.HasValue && data.EndTime.Value < now)
            {
                filterContext.Canceled = true;
                if (!filterContext.Parameters.TryGetValue("RecurringJobId", out var recurringJobId))
                    return;
                RecurringJob.RemoveIfExists((string)recurringJobId);
            }
        }

        public void OnCreated(CreatedContext filterContext)
        {
        }

        public void OnPerforming(PerformingContext filterContext)
        {
            var now = DateTimeOffset.Now;
            var data = filterContext.BackgroundJob.Job.Args.OfType<HangfireRecurringScheduledMessageData>().Single();

            if (data.StartTime > now)
                filterContext.Canceled = true;
            else if (data.EndTime.HasValue && data.EndTime.Value < now)
                filterContext.Canceled = true;
        }

        public void OnPerformed(PerformedContext filterContext)
        {
        }
    }
}
