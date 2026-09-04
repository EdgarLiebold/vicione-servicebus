using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides extension methods for rabbit mq send context.
/// </summary>
public static class RabbitMqSendContextExtensions
{
    const string StreamFilterValueHeaderName = "x-stream-filter-value";

    /// <summary>
    /// Sets transport header.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public static void SetTransportHeader(this RabbitMqSendContext context, string key, object value)
    {
        SetHeader(context.BasicProperties, key, value);
    }

    /// <summary>
    /// Sets header.
    /// </summary>
    /// <param name="basicProperties">The basic properties value.</param>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
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

    /// <summary>
    /// Sets the priority of a message sent to the broker
    /// </summary>
    /// <param name="context"></param>
    /// <param name="priority"></param>
    public static void SetPriority(this SendContext context, byte priority)
    {
        if (!context.TryGetPayload(out RabbitMqSendContext? sendContext))
            throw new ArgumentException("The RabbitMqSendContext was not available");

        sendContext.BasicProperties.Priority = priority;
    }

    /// <summary>
    /// Sets the priority of a message sent to the broker
    /// </summary>
    /// <param name="context"></param>
    /// <param name="priority"></param>
    public static bool TrySetPriority(this SendContext context, byte priority)
    {
        if (!context.TryGetPayload(out RabbitMqSendContext? sendContext))
            return false;

        sendContext.BasicProperties.Priority = priority;
        return true;
    }

    /// <summary>
    /// Sets whether the send should wait for the ack from the broker, or if it should
    /// return immediately after the message is sent by the client.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="awaitAck"></param>
    public static void SetAwaitAck(this SendContext context, bool awaitAck)
    {
        if (!context.TryGetPayload(out RabbitMqSendContext? sendContext))
            throw new ArgumentException("The RabbitMqSendContext was not available");

        sendContext.AwaitAck = awaitAck;
    }

    /// <summary>
    /// Sets whether the send should wait for the ack from the broker, or if it should
    /// return immediately after the message is sent by the client.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="awaitAck"></param>
    public static bool TrySetAwaitAck(this SendContext context, bool awaitAck)
    {
        if (!context.TryGetPayload(out RabbitMqSendContext? sendContext))
            return false;

        sendContext.AwaitAck = awaitAck;
        return true;
    }

    /// <summary>
    /// Sets the filter value used for server-side streams filtering.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="value"></param>
    public static void SetStreamFilterValue(this SendContext context, string value)
    {
        if (!context.TryGetPayload(out RabbitMqSendContext? sendContext))
            throw new ArgumentException("The RabbitMqSendContext was not available");

        sendContext.Headers.Set(StreamFilterValueHeaderName, value);
    }

    /// <summary>
    /// Sets the filter value used for server-side streams filtering.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="value"></param>
    public static bool TrySetStreamFilterValue(this SendContext context, string value)
    {
        if (!context.TryGetPayload(out RabbitMqSendContext? sendContext))
            return false;

        sendContext.Headers.Set(StreamFilterValueHeaderName, value);
        return true;
    }
}
