using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Exposes AMQP properties and RabbitMQ delivery metadata through the receive-header abstraction.</summary>
public class RabbitMqHeaderProvider :
    IHeaderProvider
{
    readonly RabbitMqBasicConsumeContext _context;

    /// <summary>Creates a header provider over one RabbitMQ delivery.</summary>
    /// <param name="context">The delivery metadata and AMQP properties.</param>
    public RabbitMqHeaderProvider(RabbitMqBasicConsumeContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>Enumerates transport metadata and every non-null, nonblank AMQP header.</summary>
    /// <returns>The normalized receive headers.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        if (!string.IsNullOrWhiteSpace(_context.Exchange))
            yield return new KeyValuePair<string, object>(RabbitMqHeaders.Exchange, _context.Exchange);
        if (!string.IsNullOrWhiteSpace(_context.RoutingKey))
            yield return new KeyValuePair<string, object>(RabbitMqHeaders.RoutingKey, _context.RoutingKey);
        yield return new KeyValuePair<string, object>(RabbitMqHeaders.DeliveryTag, _context.DeliveryTag);
        if (!string.IsNullOrWhiteSpace(_context.ConsumerTag))
            yield return new KeyValuePair<string, object>(RabbitMqHeaders.ConsumerTag, _context.ConsumerTag);
        if (!string.IsNullOrWhiteSpace(_context.Properties.MessageId))
            yield return new KeyValuePair<string, object>(nameof(MessageHeaders.MessageId), _context.Properties.MessageId);
        if (!string.IsNullOrWhiteSpace(_context.Properties.CorrelationId))
            yield return new KeyValuePair<string, object>(nameof(_context.Properties.CorrelationId), _context.Properties.CorrelationId);

        if (_context.Properties.IsHeadersPresent() && _context.Properties.Headers != null)
        {
            foreach (KeyValuePair<string, object?> header in _context.Properties.Headers)
            {
                if (IsReservedHeader(header.Key))
                    continue;

                var value = header.Value;

                if (value is byte[] bytes)
                {
                    var text = Encoding.UTF8.GetString(bytes);

                    if (!string.IsNullOrWhiteSpace(text))
                        yield return new KeyValuePair<string, object>(header.Key, text);
                }
                else if (value is string s)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                        yield return new KeyValuePair<string, object>(header.Key, s);
                }
                else if (value != null)
                    yield return new KeyValuePair<string, object>(header.Key, value);
            }
        }
    }

    /// <summary>Reads an AMQP header or synthesized RabbitMQ delivery header.</summary>
    /// <param name="key">The case-insensitive header key.</param>
    /// <param name="value">The normalized non-null header value when present.</param>
    /// <returns><see langword="true" /> when a usable value is available.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        if (MessageHeaders.TransportSentTime.Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            if (!_context.Properties.IsTimestampPresent())
            {
                value = null;
                return false;
            }

            try
            {
                value = DateTimeOffset.FromUnixTimeSeconds(_context.Properties.Timestamp.UnixTime);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                value = null;
                return false;
            }
        }

        if (RabbitMqHeaders.Exchange.Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            value = _context.Exchange;
            return !string.IsNullOrWhiteSpace(value as string);
        }

        if (RabbitMqHeaders.RoutingKey.Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            value = _context.RoutingKey;
            return !string.IsNullOrWhiteSpace(value as string);
        }

        if (RabbitMqHeaders.DeliveryTag.Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            value = _context.DeliveryTag;
            return value != default;
        }

        if (RabbitMqHeaders.ConsumerTag.Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            value = _context.ConsumerTag;
            return !string.IsNullOrWhiteSpace(value as string);
        }

        if (nameof(_context.Properties.MessageId).Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            value = _context.Properties.MessageId;
            return !string.IsNullOrWhiteSpace(value as string);
        }

        if (nameof(_context.Properties.CorrelationId).Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            value = _context.Properties.CorrelationId;
            return !string.IsNullOrWhiteSpace(value as string);
        }

        if (_context.Properties.IsHeadersPresent() && _context.Properties.Headers != null)
        {
            foreach (KeyValuePair<string, object?> header in _context.Properties.Headers)
            {
                if (header.Key.Equals(key, StringComparison.OrdinalIgnoreCase) && TryNormalize(header.Value, out value))
                    return true;
            }
        }

        value = null;
        return false;
    }

    static bool IsReservedHeader(string key)
    {
        return RabbitMqHeaders.Exchange.Equals(key, StringComparison.OrdinalIgnoreCase)
            || RabbitMqHeaders.RoutingKey.Equals(key, StringComparison.OrdinalIgnoreCase)
            || RabbitMqHeaders.DeliveryTag.Equals(key, StringComparison.OrdinalIgnoreCase)
            || RabbitMqHeaders.ConsumerTag.Equals(key, StringComparison.OrdinalIgnoreCase)
            || MessageHeaders.MessageId.Equals(key, StringComparison.OrdinalIgnoreCase)
            || MessageHeaders.CorrelationId.Equals(key, StringComparison.OrdinalIgnoreCase)
            || MessageHeaders.TransportSentTime.Equals(key, StringComparison.OrdinalIgnoreCase);
    }

    static bool TryNormalize(object? source, [NotNullWhen(true)] out object? value)
    {
        if (source is byte[] bytes)
        {
            value = Encoding.UTF8.GetString(bytes);
            return !string.IsNullOrWhiteSpace(value as string);
        }

        value = source;
        return source is string text
            ? !string.IsNullOrWhiteSpace(text)
            : source != null;
    }
}
