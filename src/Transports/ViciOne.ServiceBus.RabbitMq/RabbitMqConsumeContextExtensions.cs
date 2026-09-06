using System;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides extension methods for rabbit mq consume context.
/// </summary>
public static class RabbitMqConsumeContextExtensions
{
    /// <summary>
    /// Gets rabbit mq timestamp.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public static DateTimeOffset? GetRabbitMqTimestamp(this ConsumeContext context)
    {
        if (context.ReceiveContext.TransportHeaders.TryGetHeader(MessageHeaders.TransportSentTime, out object? value))
        {
            if (value is AmqpTimestamp ts)
                return DateTimeConstants.Epoch + TimeSpan.FromSeconds(ts.UnixTime);
        }

        return null;
    }
}
