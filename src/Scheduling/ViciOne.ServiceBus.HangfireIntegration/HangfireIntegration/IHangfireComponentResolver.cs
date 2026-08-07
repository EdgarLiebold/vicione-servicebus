// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.HangfireIntegration
{
    using System.Collections.Generic;
    using Hangfire;
    using Hangfire.Common;
    using Hangfire.Server;


    public interface IHangfireComponentResolver
    {
        IBackgroundJobClient BackgroundJobClient { get; }
        IRecurringJobManager RecurringJobManager { get; }
        ITimeZoneResolver TimeZoneResolver { get; }
        IJobFilterProvider JobFilterProvider { get; }
        IEnumerable<IBackgroundProcess> BackgroundProcesses { get; }
        JobStorage JobStorage { get; }
    }
}
