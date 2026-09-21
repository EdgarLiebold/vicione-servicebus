using System;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Reads RabbitMQ delivery metadata from consume contexts.</summary>
public static class RabbitMqConsumeContextExtensions
{
    /// <summary>Reads the AMQP sender timestamp as an absolute UTC instant.</summary>
    /// <param name="context">The consumed message context.</param>
    /// <returns>The normalized sender timestamp, or <see langword="null" /> when the transport header is absent or invalid.</returns>
    public static DateTimeOffset? GetRabbitMqTimestamp(this ConsumeContext context)
    {
        if (context.ReceiveContext.TransportHeaders.TryGetHeader(MessageHeaders.TransportSentTime, out object? value))
        {
            if (value is DateTimeOffset timestamp)
                return timestamp;

            if (value is AmqpTimestamp ts)
            {
                try
                {
                    return DateTimeOffset.FromUnixTimeSeconds(ts.UnixTime);
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }
        }

        return null;
    }
}
