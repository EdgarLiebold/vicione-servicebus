using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.RabbitMqTransport.Topology;

namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration;

public abstract class RabbitMqExchangeBindingConfigurator :
    RabbitMqExchangeConfigurator,
    IRabbitMqExchangeBindingConfigurator
{
    protected RabbitMqExchangeBindingConfigurator(string exchangeName, string exchangeType, bool durable = true, bool autoDelete = false,
        string routingKey = null)
        : base(exchangeName, exchangeType, durable, autoDelete)
    {
        RoutingKey = routingKey ?? "";

        BindingArguments = new Dictionary<string, object>();
    }

    protected RabbitMqExchangeBindingConfigurator(Exchange exchange, string routingKey = null)
        : base(exchange)
    {
        RoutingKey = routingKey ?? "";

        BindingArguments = new Dictionary<string, object>();
    }

    public IDictionary<string, object> BindingArguments { get; }

    public void SetBindingArgument(string key, object value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (value == null)
            BindingArguments.Remove(key);
        else
            BindingArguments[key] = value;
    }

    public string RoutingKey { get; set; }
}
