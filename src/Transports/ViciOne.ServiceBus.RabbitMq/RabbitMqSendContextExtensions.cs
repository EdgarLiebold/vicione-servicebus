using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures RabbitMQ-specific headers, priority, filtering, and publisher-confirm behavior on send contexts.</summary>
public static class RabbitMqSendContextExtensions
{
    const string StreamFilterValueHeaderName = "x-stream-filter-value";

    /// <summary>Sets an AMQP transport header on a RabbitMQ send context.</summary>
    /// <param name="context">The RabbitMQ send context.</param>
    /// <param name="key">The AMQP header key.</param>
    /// <param name="value">The header value.</param>
    public static void SetTransportHeader(this RabbitMqSendContext context, string key, object value)
    {
        SetHeader(context.BasicProperties, key, value);
    }

    /// <summary>Sets an AMQP header, formatting date values with the round-trip format.</summary>
    /// <param name="basicProperties">The message properties that own the header table.</param>
    /// <param name="key">The AMQP header key.</param>
    /// <param name="value">The header value; <see langword="null" /> is ignored.</param>
    public static void SetHeader(this IBasicProperties basicProperties, string key, object value)
    {
        if (value == null)
            return;

        basicProperties.Headers ??= new Dictionary<string, object?>();

        basicProperties.Headers[key] = value switch
        {
            DateTime dateTime => dateTime.ToString("O"),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O"),
            _ => value
        };
    }

    /// <summary>Sets the priority of a message sent to the broker.</summary>
    /// <param name="context">The send context carrying RabbitMQ transport state.</param>
    /// <param name="priority">The AMQP message priority.</param>
    public static void SetPriority(this SendContext context, byte priority)
    {
        if (!context.TryGetPayload(out RabbitMqSendContext? sendContext))
            throw new ArgumentException("The RabbitMqSendContext was not available");

        sendContext.BasicProperties.Priority = priority;
    }

    /// <summary>Sets the priority of a message sent to the broker.</summary>
    /// <param name="context">The send context that may carry RabbitMQ transport state.</param>
    /// <param name="priority">The AMQP message priority.</param>
    /// <returns><see langword="true" /> when a RabbitMQ send context was available and updated.</returns>
    public static bool TrySetPriority(this SendContext context, byte priority)
    {
        if (!context.TryGetPayload(out RabbitMqSendContext? sendContext))
            return false;

        sendContext.BasicProperties.Priority = priority;
        return true;
    }

    /// <summary>
    /// Sets whether the caller waits for the RabbitMQ client publish task, including publisher confirmation when enabled.
    /// When disabled, the caller returns after publish initiation while the transport continues to observe the task and hold its channel lease.
    /// </summary>
    /// <param name="context">The send context carrying RabbitMQ transport state.</param>
    /// <param name="awaitAck"><see langword="true"/> to propagate the publish outcome to the caller; <see langword="false"/> to observe it only internally.</param>
    public static void SetAwaitAck(this SendContext context, bool awaitAck)
    {
        if (!context.TryGetPayload(out RabbitMqSendContext? sendContext))
            throw new ArgumentException("The RabbitMqSendContext was not available");

        sendContext.AwaitAck = awaitAck;
    }

    /// <summary>
    /// Attempts to set whether the caller waits for the RabbitMQ client publish task, including publisher confirmation when enabled.
    /// When disabled, the caller returns after publish initiation while the transport continues to observe the task and hold its channel lease.
    /// </summary>
    /// <param name="context">The send context that may carry RabbitMQ transport state.</param>
    /// <param name="awaitAck"><see langword="true"/> to propagate the publish outcome to the caller; <see langword="false"/> to observe it only internally.</param>
    /// <returns><see langword="true"/> when the RabbitMQ send context was available and updated.</returns>
    public static bool TrySetAwaitAck(this SendContext context, bool awaitAck)
    {
        if (!context.TryGetPayload(out RabbitMqSendContext? sendContext))
            return false;

        sendContext.AwaitAck = awaitAck;
        return true;
    }

    /// <summary>Sets the filter value used for server-side streams filtering.</summary>
    /// <param name="context">The send context carrying RabbitMQ transport state.</param>
    /// <param name="value">The stream filter value written to the transport header.</param>
    public static void SetStreamFilterValue(this SendContext context, string value)
    {
        if (!context.TryGetPayload(out RabbitMqSendContext? sendContext))
            throw new ArgumentException("The RabbitMqSendContext was not available");

        sendContext.Headers.Set(StreamFilterValueHeaderName, value);
    }

    /// <summary>Sets the filter value used for server-side streams filtering.</summary>
    /// <param name="context">The send context that may carry RabbitMQ transport state.</param>
    /// <param name="value">The stream filter value written to the transport header.</param>
    /// <returns><see langword="true" /> when a RabbitMQ send context was available and updated.</returns>
    public static bool TrySetStreamFilterValue(this SendContext context, string value)
    {
        if (!context.TryGetPayload(out RabbitMqSendContext? sendContext))
            return false;

        sendContext.Headers.Set(StreamFilterValueHeaderName, value);
        return true;
    }
}
