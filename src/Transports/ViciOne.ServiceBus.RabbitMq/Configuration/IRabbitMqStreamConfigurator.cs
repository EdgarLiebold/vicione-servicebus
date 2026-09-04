using System;

#nullable enable
namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for rabbit mq stream configurator.
/// </summary>
public interface IRabbitMqStreamConfigurator
{
    /// <summary>
    /// Set the maximum length of the stream, in bytes
    /// </summary>
    long MaxLength { set; }

    /// <summary>
    /// Set the maximum age of messages in the stream
    /// </summary>
    TimeSpan MaxAge { set; }

    /// <summary>
    /// Set the maximum segment size for the stream
    /// </summary>
    long MaxSegmentSize { set; }

    /// <summary>
    /// Set the stream filter value for the consumer
    /// </summary>
    string Filter { set; }

    /// <summary>
    /// Begin consuming messages from the specified offset
    /// </summary>
    /// <param name="offset"></param>
    void FromOffset(long offset);

    /// <summary>
    /// Begin consuming messages from the specified timestamp
    /// </summary>
    /// <param name="timestamp"></param>
    void FromTimestamp(DateTimeOffset timestamp);

    /// <summary>
    /// Begin consuming messages from the first message in the stream
    /// </summary>
    void FromFirst();

    /// <summary>
    /// Begin consuming messages from the last message in the stream
    /// </summary>
    void FromLast();

    /// <summary>
    /// Consumer reference name.
    /// Used to identify the consumer server side when storing the messages offset.
    /// </summary>
    string Reference { set; }
}
