using System;

namespace ViciOne.ServiceBus.Internals;

internal static class DateTimeConstants
{
    public static readonly DateTimeOffset Epoch = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
}
