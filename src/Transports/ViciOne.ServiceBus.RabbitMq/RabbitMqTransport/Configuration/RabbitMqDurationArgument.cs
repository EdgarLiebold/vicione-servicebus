using System;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

internal static class RabbitMqDurationArgument
{
    public static object ToMilliseconds(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(value), "RabbitMQ duration arguments must be nonnegative.");

        if (value.Ticks % TimeSpan.TicksPerMillisecond != 0)
            throw new ArgumentOutOfRangeException(nameof(value), "RabbitMQ duration arguments must use whole milliseconds.");

        long milliseconds = value.Ticks / TimeSpan.TicksPerMillisecond;
        return milliseconds is >= int.MinValue and <= int.MaxValue
            ? (object)(int)milliseconds
            : milliseconds;
    }
}
