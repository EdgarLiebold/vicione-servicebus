using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Provides a rabbit mq queue configurator implementation.
/// </summary>
public class RabbitMqQueueConfigurator :
    RabbitMqExchangeConfigurator,
    IRabbitMqQueueConfigurator,
    Queue
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="exchangeType">The exchange type value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    protected RabbitMqQueueConfigurator(string queueName, string exchangeType, bool durable, bool autoDelete)
        : base(queueName, exchangeType, durable, autoDelete)
    {
        QueueArguments = new Dictionary<string, object?>();

        QueueName = queueName;
    }

    /// <summary>
    /// Sets quorum queue.
    /// </summary>
    /// <param name="replicationFactor">The replication factor value.</param>
    public void SetQuorumQueue(int? replicationFactor)
    {
        SetQueueArgument(RabbitMQ.Client.Headers.XQueueType, "quorum");
        Exclusive = false;

        QueueArguments.Remove(RabbitMQ.Client.Headers.XMaxPriority);

        if (replicationFactor.HasValue)
        {
            if (replicationFactor.Value < 1)
                throw new ArgumentOutOfRangeException(nameof(replicationFactor), "Must be greater than zero and less than or equal to the cluster size.");

            SetQueueArgument(RabbitMQ.Client.Headers.XQuorumInitialGroupSize, replicationFactor.Value);
        }
    }

    /// <summary>
    /// Gets or sets the single active consumer value.
    /// </summary>
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

    /// <summary>
    /// Sets queue argument.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public void SetQueueArgument(string key, object? value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (value == null)
            QueueArguments.Remove(key);
        else
            QueueArguments[key] = value;
    }

    /// <summary>
    /// Sets queue argument.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public void SetQueueArgument(string key, TimeSpan value)
    {
        var milliseconds = (int)value.TotalMilliseconds;

        SetQueueArgument(key, milliseconds);
    }

    /// <summary>
    /// Gets or sets the lazy value.
    /// </summary>
    public bool Lazy
    {
        set => SetQueueArgument(RabbitMQ.Client.Headers.XQueueMode, value ? "lazy" : "default");
    }

    /// <summary>
    /// Performs the enable priority operation.
    /// </summary>
    /// <param name="maxPriority">The max priority value.</param>
    public void EnablePriority(byte maxPriority)
    {
        QueueArguments[RabbitMQ.Client.Headers.XMaxPriority] = (int)maxPriority;
    }

    /// <summary>
    /// Gets or sets the exclusive value.
    /// </summary>
    public bool Exclusive { get; set; }

    /// <summary>
    /// Gets or sets the queue expiration value.
    /// </summary>
    public TimeSpan? QueueExpiration
    {
        get
        {
            if (QueueArguments.TryGetValue(RabbitMQ.Client.Headers.XExpires, out var value) && value is long milliseconds)
                return TimeSpan.FromMilliseconds(milliseconds);

            return null;
        }
        set
        {
            if (value.HasValue && value.Value > TimeSpan.Zero)
                QueueArguments[RabbitMQ.Client.Headers.XExpires] = (long)value.Value.TotalMilliseconds;
            else
                QueueArguments.Remove(RabbitMQ.Client.Headers.XExpires);
        }
    }

    /// <summary>
    /// Gets or sets the queue name value.
    /// </summary>
    public string QueueName { get; set; }
    /// <summary>
    /// Gets the queue arguments value.
    /// </summary>
    public IDictionary<string, object?> QueueArguments { get; }

    /// <summary>
    /// Gets endpoint address.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <returns>The result of the operation.</returns>
    public override RabbitMqEndpointAddress GetEndpointAddress(Uri hostAddress)
    {
        return new RabbitMqEndpointAddress(hostAddress, string.IsNullOrWhiteSpace(ExchangeName) ? QueueName : ExchangeName, ExchangeType, Durable, AutoDelete);
    }
}
