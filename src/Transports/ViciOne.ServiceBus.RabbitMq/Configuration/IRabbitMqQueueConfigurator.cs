using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures a queue/exchange pair in RabbitMQ.</summary>
public interface IRabbitMqQueueConfigurator :
    IRabbitMqExchangeConfigurator
{
    /// <summary>
    /// Specify that the queue is exclusive to this process and cannot be accessed by other processes
    /// at the same time.
    /// </summary>
    bool Exclusive { set; }

    /// <summary>Sets the queue to be lazy (using less memory).</summary>
    bool Lazy { set; }

    /// <summary>Sets the unused-queue expiration applied through <c>x-expires</c>; positive values must use whole milliseconds.</summary>
    TimeSpan? QueueExpiration { set; }

    /// <summary>
    /// Enables RabbitMQ single-active-consumer semantics so one consumer receives deliveries while
    /// other registered consumers remain available for failover.
    /// </summary>
    bool SingleActiveConsumer { set; }

    /// <summary>Sets or removes an argument passed to RabbitMQ when declaring the queue.</summary>
    /// <param name="key">The argument key.</param>
    /// <param name="value">The argument value.</param>
    void SetQueueArgument(string key, object? value);

    /// <summary>Sets a queue argument from a nonnegative duration converted to whole milliseconds; <c>x-expires</c> must be positive.</summary>
    /// <param name="key">The RabbitMQ queue-argument key.</param>
    /// <param name="value">The duration to convert to whole milliseconds.</param>
    void SetQueueArgument(string key, TimeSpan value);

    /// <summary>Enables priority delivery and sets the queue's maximum message priority.</summary>
    /// <param name="maxPriority">The highest accepted priority.</param>
    void EnablePriority(byte maxPriority);

    /// <summary>Configures the queue as a RabbitMQ quorum queue.</summary>
    /// <param name="replicationFactor">
    /// Optional, if specified must be greater than zero and less or equal to the number of cluster nodes.
    /// An odd value is recommended.
    /// </param>
    void SetQuorumQueue(int? replicationFactor = default);
}
