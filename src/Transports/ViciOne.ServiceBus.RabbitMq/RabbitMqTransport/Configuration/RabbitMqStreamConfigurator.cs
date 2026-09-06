using System;
using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Provides a rabbit mq stream configurator implementation.
/// </summary>
public class RabbitMqStreamConfigurator :
    IRabbitMqStreamConfigurator
{
    readonly RabbitMqReceiveSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    public RabbitMqStreamConfigurator(RabbitMqReceiveSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Gets or sets the max length value.
    /// </summary>
    public long MaxLength
    {
        set => _settings.QueueArguments[RabbitMQ.Client.Headers.XMaxLengthInBytes] = value;
    }

    /// <summary>
    /// Gets or sets the max age value.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the max segment size value.
    /// </summary>
    public long MaxSegmentSize
    {
        set => _settings.QueueArguments[RabbitMQ.Client.Headers.XStreamMaxSegmentSizeInBytes] = value;
    }

    /// <summary>
    /// Gets or sets the filter value.
    /// </summary>
    public string Filter
    {
        set => _settings.ConsumeArguments["x-stream-filter"] = value;
    }

    /// <summary>
    /// Performs the from offset operation.
    /// </summary>
    /// <param name="offset">The offset value.</param>
    public void FromOffset(long offset)
    {
        _settings.ConsumeArguments[RabbitMQ.Client.Headers.XStreamOffset] = offset;
    }

    /// <summary>
    /// Performs the from timestamp operation.
    /// </summary>
    /// <param name="timestamp">The timestamp value.</param>
    public void FromTimestamp(DateTimeOffset timestamp)
    {
        timestamp = timestamp.ToUniversalTime();

        _settings.ConsumeArguments.SetAmqpTimestamp(RabbitMQ.Client.Headers.XStreamOffset, timestamp);
    }

    /// <summary>
    /// Performs the from first operation.
    /// </summary>
    public void FromFirst()
    {
        _settings.ConsumeArguments[RabbitMQ.Client.Headers.XStreamOffset] = "first";
    }

    /// <summary>
    /// Performs the from last operation.
    /// </summary>
    public void FromLast()
    {
        _settings.ConsumeArguments[RabbitMQ.Client.Headers.XStreamOffset] = "last";
    }

    /// <summary>
    /// Gets or sets the reference value.
    /// </summary>
    public string Reference
    {
        set => _settings.ConsumeArguments["name"] = value;
    }
}
