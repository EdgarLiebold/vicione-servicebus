using System;
using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Configures the destination exchange and optional queue or exchange bindings for a RabbitMQ send transport.</summary>
public class RabbitMqSendSettings :
    RabbitMqExchangeConfigurator,
    SendSettings
{
    readonly List<ExchangeBindingPublishTopologySpecification> _exchangeBindings;
    bool _bindToQueue;
    string? _queueName;

    /// <summary>Creates send settings from a parsed RabbitMQ endpoint address.</summary>
    /// <param name="address">The endpoint address whose topology options initialize the settings.</param>
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

    /// <summary>Gets the broker-specific arguments for the optional bound queue.</summary>
    public IDictionary<string, object?> QueueArguments { get; }

    /// <summary>Projects the exchange and simple binding settings into a RabbitMQ endpoint address.</summary>
    /// <param name="hostAddress">The RabbitMQ host address.</param>
    /// <returns>The destination endpoint address.</returns>
    public RabbitMqEndpointAddress GetSendAddress(Uri hostAddress)
    {
        return new RabbitMqEndpointAddress(hostAddress, ExchangeName, ExchangeType, Durable, AutoDelete, _bindToQueue, _queueName,
            ExchangeArguments.TryGetValue("x-delayed-type", out var argument) && argument is string delayedType ? delayedType : default,
            _exchangeBindings.Count > 0 ? _exchangeBindings.Select(x => x.ExchangeName).ToArray() : default,
            singleActiveConsumer: QueueArguments.TryGetValue(RabbitMQ.Client.Headers.XSingleActiveConsumer, out var queueArgument)
                && queueArgument is true,
            alternateExchange: ExchangeArguments.TryGetValue(RabbitMQ.Client.Headers.AlternateExchange, out argument)
                && argument is string alternateExchange ? alternateExchange : default);
    }

    /// <summary>Builds the exchange declarations and optional queue or exchange bindings required for sending.</summary>
    /// <returns>The send broker topology, or an empty topology for RabbitMQ direct reply-to.</returns>
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

    /// <summary>Configures a queue to be declared and bound to the destination exchange.</summary>
    /// <param name="queueName">The queue name to declare.</param>
    public void BindToQueue(string queueName)
    {
        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(queueName));

        _bindToQueue = true;
        _queueName = queueName;
    }

    /// <summary>Configures a source exchange to be declared and bound to the destination exchange.</summary>
    /// <param name="exchangeName">The source exchange name.</param>
    /// <param name="configure">An optional callback that customizes the source exchange and binding.</param>
    public void BindToExchange(string exchangeName, Action<IRabbitMqExchangeBindingConfigurator>? configure = null)
    {
        var exchangeType = ExchangeArguments.TryGetValue("x-delayed-type", out var argument) && argument is string delayedType
            ? delayedType
            : RabbitMQ.Client.ExchangeType.Fanout;
        var specification = new ExchangeBindingPublishTopologySpecification(exchangeName, exchangeType, Durable, AutoDelete);

        configure?.Invoke(specification);

        _exchangeBindings.Add(specification);
    }

    /// <summary>Configures a source exchange binding from an endpoint address.</summary>
    /// <param name="address">The address that supplies the source exchange declaration.</param>
    public void BindToExchange(RabbitMqEndpointAddress address)
    {
        var specification = new ExchangeBindingPublishTopologySpecification(address.Name, address.ExchangeType, address.Durable, address.AutoDelete);

        _exchangeBindings.Add(specification);
    }

    /// <summary>Adds, replaces, or removes a broker-specific argument for the optional queue.</summary>
    /// <param name="key">The RabbitMQ queue argument name.</param>
    /// <param name="value">The argument value, or <see langword="null"/> to remove the argument.</param>
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

    /// <summary>Formats the exchange, queue binding, and declaration arguments for diagnostics.</summary>
    /// <returns>A diagnostic description of the send settings.</returns>
    public override string ToString()
    {
        return string.Join(", ", GetSettingStrings());
    }
}
