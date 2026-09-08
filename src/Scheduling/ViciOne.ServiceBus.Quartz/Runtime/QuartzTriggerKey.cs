using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Quartz;

namespace ViciOne.ServiceBus.Quartz.Runtime;

internal static class QuartzTriggerKey
{
    internal const string RecurringPrefix = "Recurring.Trigger.";

    public static TriggerKey ForOneTime(Guid tokenId, string schedulerNamespace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schedulerNamespace);
        return new TriggerKey(tokenId.ToString("N"), schedulerNamespace);
    }

    public static TriggerKey ForRecurring(string scheduleId, string scheduleGroup, string schedulerNamespace)
    {
        string partitionKey = GetPartitionKey(scheduleId, scheduleGroup);
        ArgumentException.ThrowIfNullOrWhiteSpace(schedulerNamespace);
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(partitionKey));
        return new TriggerKey(
            string.Concat(RecurringPrefix, Convert.ToHexStringLower(digest.AsSpan(0, 16))),
            schedulerNamespace);
    }

    public static string GetPartitionKey(string scheduleId, string scheduleGroup)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleGroup);

        return string.Concat(
            scheduleGroup.Length.ToString(CultureInfo.InvariantCulture),
            ":",
            scheduleGroup,
            scheduleId);
    }
}
