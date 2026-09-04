using System;
using Quartz;

namespace ViciOne.ServiceBus.Quartz;

internal static class QuartzTriggerKey
{
    internal const string RecurringPrefix = "Recurring.Trigger.";

    public static TriggerKey ForRecurring(string scheduleId, string scheduleGroup)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleId);

        string name = scheduleId.StartsWith(RecurringPrefix, StringComparison.Ordinal)
            ? scheduleId
            : string.Concat(RecurringPrefix, scheduleId);

        return new TriggerKey(name, scheduleGroup);
    }

    public static string GetScheduleId(TriggerKey triggerKey)
    {
        ArgumentNullException.ThrowIfNull(triggerKey);

        return triggerKey.Name.StartsWith(RecurringPrefix, StringComparison.Ordinal)
            ? triggerKey.Name[RecurringPrefix.Length..]
            : triggerKey.Name;
    }
}
