using System;

namespace ViciOne.ServiceBus.Azure.Table.Infrastructure;

internal static class AzureTableStorageLimits
{
    internal const int MaximumCustomPropertyCount = 252;
    internal const int MaximumKeyCharacters = 1024;
    internal const int MaximumPropertyBytes = 64 * 1024;
    internal const int MaximumPropertyNameCharacters = 255;
    internal const int MaximumTransactionOperations = 100;

    internal static readonly DateTime MinimumDateTimeUtc =
        new(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    internal static readonly DateTimeOffset MinimumDateTimeOffset =
        new(1601, 1, 1, 0, 0, 0, TimeSpan.Zero);
}
