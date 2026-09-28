using System;
using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Writes RabbitMQ stream retention and consumer-offset arguments.</summary>
public class RabbitMqStreamConfigurator :
    IRabbitMqStreamConfigurator
{
    readonly RabbitMqReceiveSettings _settings;

    /// <summary>Creates a stream configurator over receive queue and consumer settings.</summary>
    /// <param name="settings">The RabbitMQ receive settings to update.</param>
    public RabbitMqStreamConfigurator(RabbitMqReceiveSettings settings)
    {
        _settings = settings;
    }

    /// <summary>Sets the maximum retained stream length in bytes.</summary>
    public long MaxLength
    {
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Stream maximum length cannot be negative.");

            _settings.QueueArguments[RabbitMQ.Client.Headers.XMaxLengthInBytes] = value;
        }
    }

    /// <summary>Sets maximum stream age using RabbitMQ's largest exact whole-unit representation; subsecond values remove the limit.</summary>
    public TimeSpan MaxAge
    {
        set
        {
            if (value < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value), "Stream maximum age cannot be negative.");

            if (value >= TimeSpan.FromSeconds(1) && value.Ticks % TimeSpan.TicksPerSecond != 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Stream maximum age must use whole seconds.");

            if (value < TimeSpan.FromSeconds(1))
            {
                _settings.QueueArguments.Remove(RabbitMQ.Client.Headers.XMaxAge);
                return;
            }

            long ticks = value.Ticks;
            string text = ticks % TimeSpan.TicksPerDay == 0
                ? $"{ticks / TimeSpan.TicksPerDay}D"
                : ticks % TimeSpan.TicksPerHour == 0
                    ? $"{ticks / TimeSpan.TicksPerHour}h"
                    : ticks % TimeSpan.TicksPerMinute == 0
                        ? $"{ticks / TimeSpan.TicksPerMinute}m"
                        : $"{ticks / TimeSpan.TicksPerSecond}s";

            _settings.QueueArguments[RabbitMQ.Client.Headers.XMaxAge] = text;
        }
    }

    /// <summary>Sets the maximum stream segment size in bytes.</summary>
    public long MaxSegmentSize
    {
        set
        {
            if (value > 3_000_000_000L)
                throw new ArgumentOutOfRangeException(nameof(value), "Stream segment size cannot exceed 3,000,000,000 bytes.");

            _settings.QueueArguments[RabbitMQ.Client.Headers.XStreamMaxSegmentSizeInBytes] = value;
        }
    }

    /// <summary>Sets the server-side stream filter value for this consumer.</summary>
    public string Filter
    {
        set => _settings.ConsumeArguments["x-stream-filter"] = value;
    }

    /// <summary>Starts consumption at an absolute stream offset.</summary>
    /// <param name="offset">The first stream offset to consume.</param>
    public void FromOffset(long offset)
    {
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "Stream offset cannot be negative.");

        _settings.ConsumeArguments[RabbitMQ.Client.Headers.XStreamOffset] = offset;
    }

    /// <summary>Starts consumption at RabbitMQ's timestamp-based stream chunk position for a UTC instant.</summary>
    /// <param name="timestamp">The stream timestamp boundary.</param>
    public void FromTimestamp(DateTimeOffset timestamp)
    {
        if (timestamp < DateTimeOffset.UnixEpoch)
            throw new ArgumentOutOfRangeException(nameof(timestamp), "Stream timestamp must be at or after the Unix epoch.");

        timestamp = timestamp.ToUniversalTime();

        _settings.ConsumeArguments.SetAmqpTimestamp(RabbitMQ.Client.Headers.XStreamOffset, timestamp);
    }

    /// <summary>Starts consumption at the first retained stream message.</summary>
    public void FromFirst()
    {
        _settings.ConsumeArguments[RabbitMQ.Client.Headers.XStreamOffset] = "first";
    }

    /// <summary>Starts consumption at the beginning of RabbitMQ's latest retained chunk.</summary>
    public void FromLast()
    {
        _settings.ConsumeArguments[RabbitMQ.Client.Headers.XStreamOffset] = "last";
    }

    /// <summary>Sets the stable server-side consumer reference used for offset tracking.</summary>
    public string Reference
    {
        set => _settings.ConsumeArguments["name"] = value;
    }
}
