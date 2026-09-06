using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Writes AMQP timestamps with an ISO-8601 fallback for out-of-range instants.</summary>
public static class AmqpTimestampExtensions
{
    /// <summary>Stores a timestamp as epoch seconds when representable, or as a round-trip string otherwise.</summary>
    /// <param name="dictionary">The AMQP field table to update.</param>
    /// <param name="key">The field-table key.</param>
    /// <param name="timestamp">The instant to store.</param>
    public static void SetAmqpTimestamp(this IDictionary<string, object?> dictionary, string key, DateTimeOffset timestamp)
    {
        dictionary[key] = TryConvert(timestamp, out AmqpTimestamp? result)
            ? result
            : timestamp.ToString("O");
    }

    static bool TryConvert(DateTimeOffset input, [NotNullWhen(true)] out AmqpTimestamp? result)
    {
        if (input >= DateTimeConstants.Epoch)
        {
            var timeSpan = input - DateTimeConstants.Epoch;
            if (timeSpan.TotalSeconds <= long.MaxValue)
            {
                result = new AmqpTimestamp((long)timeSpan.TotalSeconds);
                return true;
            }
        }

        result = default;
        return false;
    }
}
