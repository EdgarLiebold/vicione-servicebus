using System;
using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Provides a rabbit mq send settings implementation.
/// </summary>
public class RabbitMqSendSettings :
    RabbitMqExchangeConfigurator,
    SendSettings
{
    readonly List<ExchangeBindingPublishTopologySpecification> _exchangeBindings;
    bool _bindToQueue;
    string? _queueName;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    public RabbitMqSendSettings(RabbitMqEndpointAddress address)
        : base(address.Name, address.ExchangeType, address.Durable, address.AutoDelete)
    {
        _exchangeBindings = new List<ExchangeBindingPublishTopologySpecification>();

        QueueArguments = new Dictionary<string, object?>();

        if (address.BindToQueue)
            BindToQueue(address.QueueName ?? address.Name);

        if (!string.IsNullOrWhiteSpace(address.DelayedType))
            SetExchangeArgument("x-delayed-type", address.DelayedType);

        foreach (var exchange in address.BindExchanges)
            BindToExchange(exchange);

        if (!string.IsNullOrWhiteSpace(address.AlternateExchange))
            SetExchangeArgument(RabbitMQ.Client.Headers.AlternateExchange, address.AlternateExchange);

        if (address.SingleActiveConsumer)
            SetQueueArgument(RabbitMQ.Client.Headers.XSingleActiveConsumer, true);
    }

    /// <summary>
    /// Gets the queue arguments value.
    /// </summary>
    public IDictionary<string, object?> QueueArguments { get; }

    /// <summary>
    /// Gets send address.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <returns>The result of the operation.</returns>
    public RabbitMqEndpointAddress GetSendAddress(Uri hostAddress)
    {
        return new RabbitMqEndpointAddress(hostAddress, ExchangeName, ExchangeType, Durable, AutoDelete, _bindToQueue, _queueName,
            ExchangeArguments.TryGetValue("x-delayed-type", out var argument) && argument is string delayedType ? delayedType : default,
            _exchangeBindings.Count > 0 ? _exchangeBindings.Select(x => x.ExchangeName).ToArray() : default,
            alternateExchange: ExchangeArguments.TryGetValue(RabbitMQ.Client.Headers.AlternateExchange, out argument)
                && argument is string alternateExchange ? alternateExchange : default);
    }

    /// <summary>
    /// Gets broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        if (ExchangeName.Equals(RabbitMqExchangeNames.ReplyTo, StringComparison.OrdinalIgnoreCase))
            return builder.BuildBrokerTopology();

        builder.Exchange = builder.ExchangeDeclare(ExchangeName, ExchangeType, Durable, AutoDelete, ExchangeArguments);

        foreach (var specification in _exchangeBindings)
            specification.Apply(builder);

        if (_bindToQueue)
        {
            var queue = builder.QueueDeclare(_queueName ?? ExchangeName, Durable, AutoDelete, false, QueueArguments);

            builder.QueueBind(builder.Exchange, queue, "", new Dictionary<string, object?>());
        }

        return builder.BuildBrokerTopology();
    }

    /// <summary>
    /// Performs the bind to queue operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    public void BindToQueue(string queueName)
    {
        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(queueName));

        _bindToQueue = true;
        _queueName = queueName;
    }

    /// <summary>
    /// Performs the bind to exchange operation.
    /// </summary>
    /// <param name="exchangeName">The exchange name value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void BindToExchange(string exchangeName, Action<IRabbitMqExchangeBindingConfigurator>? configure = null)
    {
        var exchangeType = ExchangeArguments.TryGetValue("x-delayed-type", out var argument) && argument is string delayedType
            ? delayedType
            : RabbitMQ.Client.ExchangeType.Fanout;
        var specification = new ExchangeBindingPublishTopologySpecification(exchangeName, exchangeType, Durable, AutoDelete);

        configure?.Invoke(specification);

        _exchangeBindings.Add(specification);
    }

    /// <summary>
    /// Performs the bind to exchange operation.
    /// </summary>
    /// <param name="address">The address value.</param>
    public void BindToExchange(RabbitMqEndpointAddress address)
    {
        var specification = new ExchangeBindingPublishTopologySpecification(address.Name, address.ExchangeType, address.Durable, address.AutoDelete);

        _exchangeBindings.Add(specification);
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

    IEnumerable<string> GetSettingStrings()
    {
        if (Durable)
            yield return "durable";

        if (AutoDelete)
            yield return "auto-delete";

        if (ExchangeType != RabbitMQ.Client.ExchangeType.Fanout)
            yield return ExchangeType;

        if (_bindToQueue)
            yield return $"bind->{_queueName}";

        if (ExchangeArguments != null)
        {
            foreach (KeyValuePair<string, object?> argument in ExchangeArguments)
                yield return $"e:{argument.Key}={argument.Value}";
        }

        if (QueueArguments != null)
        {
            foreach (KeyValuePair<string, object?> argument in QueueArguments)
                yield return $"q:{argument.Key}={argument.Value}";
        }
    }

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override string ToString()
    {
        return string.Join(", ", GetSettingStrings());
    }
}
