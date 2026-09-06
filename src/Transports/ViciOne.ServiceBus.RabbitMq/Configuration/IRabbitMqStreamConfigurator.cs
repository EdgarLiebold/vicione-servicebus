using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures RabbitMQ stream retention and consumer offset behavior.</summary>
public interface IRabbitMqStreamConfigurator
{
    /// <summary>Sets the maximum stream length in bytes.</summary>
    long MaxLength { set; }

    /// <summary>Sets the maximum age of messages retained in the stream.</summary>
    TimeSpan MaxAge { set; }

    /// <summary>Sets the maximum segment size for the stream, in bytes.</summary>
    long MaxSegmentSize { set; }

    /// <summary>Sets the server-side stream filter value for the consumer.</summary>
    string Filter { set; }

    /// <summary>Starts consuming at an absolute stream offset.</summary>
    /// <param name="offset">The zero-based stream offset.</param>
    void FromOffset(long offset);

    /// <summary>Starts consuming with the first message at or after a timestamp.</summary>
    /// <param name="timestamp">The UTC-aware stream timestamp.</param>
    void FromTimestamp(DateTimeOffset timestamp);

    /// <summary>Starts consuming at the first retained message in the stream.</summary>
    void FromFirst();

    /// <summary>Starts consuming from RabbitMQ's <c>last</c> stream offset, at the beginning of the latest retained chunk.</summary>
    void FromLast();

    /// <summary>Sets the stable server-side consumer reference used to store the consumed offset.</summary>
    string Reference { set; }
}
