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
        set => _settings.QueueArguments[RabbitMQ.Client.Headers.XMaxLengthInBytes] = value;
    }

    /// <summary>Sets maximum stream age using RabbitMQ's largest applicable whole-unit representation.</summary>
    public TimeSpan MaxAge
    {
        set
        {
            string? text = null;
            if (value.TotalDays >= 1)
                text = $"{value.TotalDays:F0}D";
            else if (value.TotalHours >= 1)
                text = $"{value.TotalHours:F0}h";
            else if (value.TotalMinutes >= 1)
                text = $"{value.TotalMinutes:F0}m";
            else if (value.TotalSeconds >= 1)
                text = $"{value.TotalSeconds:F0}s";

            if (text == null)
                _settings.QueueArguments.Remove(RabbitMQ.Client.Headers.XMaxAge);
            else
                _settings.QueueArguments[RabbitMQ.Client.Headers.XMaxAge] = text;
        }
    }

    /// <summary>Sets the maximum stream segment size in bytes.</summary>
    public long MaxSegmentSize
    {
        set => _settings.QueueArguments[RabbitMQ.Client.Headers.XStreamMaxSegmentSizeInBytes] = value;
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
        _settings.ConsumeArguments[RabbitMQ.Client.Headers.XStreamOffset] = offset;
    }

    /// <summary>Starts consumption at the first message at or after a UTC instant.</summary>
    /// <param name="timestamp">The stream timestamp boundary.</param>
    public void FromTimestamp(DateTimeOffset timestamp)
    {
        timestamp = timestamp.ToUniversalTime();

        _settings.ConsumeArguments.SetAmqpTimestamp(RabbitMQ.Client.Headers.XStreamOffset, timestamp);
    }

    /// <summary>Starts consumption at the first retained stream message.</summary>
    public void FromFirst()
    {
        _settings.ConsumeArguments[RabbitMQ.Client.Headers.XStreamOffset] = "first";
    }

    /// <summary>Starts consumption with messages appended after the consumer begins.</summary>
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
