namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures an exchange declaration and its binding to a queue or another exchange.</summary>
public interface IRabbitMqExchangeBindingConfigurator :
    IRabbitMqExchangeConfigurator
{
    /// <summary>Sets the routing key used by the exchange binding.</summary>
    string RoutingKey { set; }

    /// <summary>Sets the binding argument, or removes it if value is null.</summary>
    /// <param name="key">The RabbitMQ binding-argument key.</param>
    /// <param name="value">The argument value, or <see langword="null" /> to remove the argument.</param>
    void SetBindingArgument(string key, object? value);
}
