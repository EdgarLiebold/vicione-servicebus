using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures RabbitMQ stream retention and consumer offset behavior.</summary>
public interface IRabbitMqStreamConfigurator
{
    /// <summary>Sets the nonnegative maximum stream length in bytes.</summary>
    long MaxLength { set; }

    /// <summary>Sets the maximum age of messages retained in the stream. Whole-second values are represented exactly; nonnegative subsecond values remove the age limit.</summary>
    TimeSpan MaxAge { set; }

    /// <summary>Sets the maximum segment size for the stream, in bytes, up to RabbitMQ's 3,000,000,000-byte limit.</summary>
    long MaxSegmentSize { set; }

    /// <summary>Sets the server-side stream filter value for the consumer.</summary>
    string Filter { set; }

    /// <summary>Starts consuming at an absolute stream offset.</summary>
    /// <param name="offset">The nonnegative, zero-based stream offset.</param>
    void FromOffset(long offset);

    /// <summary>Starts consuming from RabbitMQ's chunk position for a timestamp; a chunk can include messages written shortly before the timestamp.</summary>
    /// <param name="timestamp">The UTC-aware stream timestamp at or after the Unix epoch.</param>
    void FromTimestamp(DateTimeOffset timestamp);

    /// <summary>Starts consuming at the first retained message in the stream.</summary>
    void FromFirst();

    /// <summary>Starts consuming from RabbitMQ's <c>last</c> stream offset, at the beginning of the latest retained chunk.</summary>
    void FromLast();

    /// <summary>Sets the stable server-side consumer reference used to store the consumed offset.</summary>
    string Reference { set; }
}
