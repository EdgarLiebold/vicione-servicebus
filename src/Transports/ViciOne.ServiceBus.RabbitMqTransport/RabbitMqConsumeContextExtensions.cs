using System;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Internals;

#nullable enable
namespace ViciOne.ServiceBus;

public static class RabbitMqConsumeContextExtensions
{
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
