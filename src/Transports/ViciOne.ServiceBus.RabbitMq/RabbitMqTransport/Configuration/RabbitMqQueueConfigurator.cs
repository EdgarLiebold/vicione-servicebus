using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Stores a RabbitMQ queue declaration together with its same-name exchange declaration.</summary>
public class RabbitMqQueueConfigurator :
    RabbitMqExchangeConfigurator,
    IRabbitMqQueueConfigurator,
    Queue
{
    /// <summary>Creates matching queue and exchange settings.</summary>
    /// <param name="queueName">The queue and exchange name.</param>
    /// <param name="exchangeType">The RabbitMQ exchange type.</param>
    /// <param name="durable">Whether the queue and exchange survive broker restarts.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the queue and exchange when unused.</param>
    protected RabbitMqQueueConfigurator(string queueName, string exchangeType, bool durable, bool autoDelete)
        : base(queueName, exchangeType, durable, autoDelete)
    {
        QueueArguments = new Dictionary<string, object?>();

        QueueName = queueName;
    }

    /// <summary>Configures a non-exclusive quorum queue and removes incompatible priority settings.</summary>
    /// <param name="replicationFactor">The optional initial quorum-group size.</param>
    public void SetQuorumQueue(int? replicationFactor)
    {
        if (replicationFactor is < 1)
            throw new ArgumentOutOfRangeException(nameof(replicationFactor), "Must be greater than zero and less than or equal to the cluster size.");

        SetQueueArgument(RabbitMQ.Client.Headers.XQueueType, "quorum");
        Exclusive = false;

        QueueArguments.Remove(RabbitMQ.Client.Headers.XMaxPriority);

        if (replicationFactor.HasValue)
        {
            SetQueueArgument(RabbitMQ.Client.Headers.XQuorumInitialGroupSize, replicationFactor.Value);
        }
    }

    /// <summary>Sets or removes RabbitMQ single-active-consumer behavior.</summary>
    public bool SingleActiveConsumer
    {
        set
        {
            if (value)
                SetQueueArgument(RabbitMQ.Client.Headers.XSingleActiveConsumer, true);
            else
                QueueArguments.Remove(RabbitMQ.Client.Headers.XSingleActiveConsumer);
        }
    }

    /// <summary>Sets a queue argument, or removes it when the value is <see langword="null" />.</summary>
    /// <param name="key">The RabbitMQ queue-argument key.</param>
    /// <param name="value">The argument value.</param>
    public void SetQueueArgument(string key, object? value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (value == null)
            QueueArguments.Remove(key);
        else
            QueueArguments[key] = value;
    }

    /// <summary>Sets a queue argument from a nonnegative duration converted to whole milliseconds; <c>x-expires</c> must be positive.</summary>
    /// <param name="key">The RabbitMQ queue-argument key.</param>
    /// <param name="value">The duration to convert.</param>
    public void SetQueueArgument(string key, TimeSpan value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (key == RabbitMQ.Client.Headers.XExpires && value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(value), "RabbitMQ queue expiration must be positive.");

        SetQueueArgument(key, RabbitMqDurationArgument.ToMilliseconds(value));
    }

    /// <summary>Sets RabbitMQ queue mode to <c>lazy</c> or <c>default</c>.</summary>
    public bool Lazy
    {
        set => SetQueueArgument(RabbitMQ.Client.Headers.XQueueMode, value ? "lazy" : "default");
    }

    /// <summary>Enables queue priority delivery.</summary>
    /// <param name="maxPriority">The highest accepted message priority.</param>
    public void EnablePriority(byte maxPriority)
    {
        QueueArguments[RabbitMQ.Client.Headers.XMaxPriority] = (int)maxPriority;
    }

    /// <summary>Gets or sets whether the queue is exclusive to its declaring connection.</summary>
    public bool Exclusive { get; set; }

    /// <summary>Gets or sets how long an unused queue may remain before RabbitMQ deletes it; positive values must use whole milliseconds.</summary>
    public TimeSpan? QueueExpiration
    {
        get
        {
            if (QueueArguments.TryGetValue(RabbitMQ.Client.Headers.XExpires, out var value))
            {
                if (value is long milliseconds)
                    return TimeSpan.FromTicks(checked(milliseconds * TimeSpan.TicksPerMillisecond));
                if (value is int shortMilliseconds)
                    return TimeSpan.FromTicks((long)shortMilliseconds * TimeSpan.TicksPerMillisecond);
            }

            return null;
        }
        set
        {
            if (value.HasValue && value.Value > TimeSpan.Zero)
            {
                if (value.Value.Ticks % TimeSpan.TicksPerMillisecond != 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "RabbitMQ queue expiration must use whole milliseconds.");

                QueueArguments[RabbitMQ.Client.Headers.XExpires] = value.Value.Ticks / TimeSpan.TicksPerMillisecond;
            }
            else
                QueueArguments.Remove(RabbitMQ.Client.Headers.XExpires);
        }
    }

    /// <summary>Gets or sets the queue name.</summary>
    public string QueueName { get; set; }
    /// <summary>Gets the queue arguments.</summary>
    public IDictionary<string, object?> QueueArguments { get; }

    /// <summary>Creates an exchange endpoint address from the queue's same-name exchange settings.</summary>
    /// <param name="hostAddress">The RabbitMQ host and virtual-host address.</param>
    /// <returns>The normalized exchange endpoint address.</returns>
    public override RabbitMqEndpointAddress GetEndpointAddress(Uri hostAddress)
    {
        return new RabbitMqEndpointAddress(hostAddress, string.IsNullOrWhiteSpace(ExchangeName) ? QueueName : ExchangeName, ExchangeType, Durable, AutoDelete);
    }
}
