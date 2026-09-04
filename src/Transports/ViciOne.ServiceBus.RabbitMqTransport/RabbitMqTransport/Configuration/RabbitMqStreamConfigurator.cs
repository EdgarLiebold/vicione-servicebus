using System;
using RabbitMQ.Client;

#nullable enable
namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration;

public class RabbitMqStreamConfigurator :
    IRabbitMqStreamConfigurator
{
    readonly RabbitMqReceiveSettings _settings;

    public RabbitMqStreamConfigurator(RabbitMqReceiveSettings settings)
    {
        _settings = settings;
    }

    public long MaxLength
    {
        set => _settings.QueueArguments[RabbitMQ.Client.Headers.XMaxLengthInBytes] = value;
    }

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

    public long MaxSegmentSize
    {
        set => _settings.QueueArguments[RabbitMQ.Client.Headers.XStreamMaxSegmentSizeInBytes] = value;
    }

    public string Filter
    {
        set => _settings.ConsumeArguments["x-stream-filter"] = value;
    }

    public void FromOffset(long offset)
    {
        _settings.ConsumeArguments[RabbitMQ.Client.Headers.XStreamOffset] = offset;
    }

    public void FromTimestamp(DateTimeOffset timestamp)
    {
        timestamp = timestamp.ToUniversalTime();

        _settings.ConsumeArguments.SetAmqpTimestamp(RabbitMQ.Client.Headers.XStreamOffset, timestamp);
    }

    public void FromFirst()
    {
        _settings.ConsumeArguments[RabbitMQ.Client.Headers.XStreamOffset] = "first";
    }

    public void FromLast()
    {
        _settings.ConsumeArguments[RabbitMQ.Client.Headers.XStreamOffset] = "last";
    }

    public string Reference
    {
        set => _settings.ConsumeArguments["name"] = value;
    }
}
