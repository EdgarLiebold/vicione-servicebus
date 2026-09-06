using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures additional source exchanges that feed an existing exchange binding.</summary>
public interface IRabbitMqExchangeToExchangeBindingConfigurator :
    IRabbitMqExchangeBindingConfigurator
{
    /// <summary>Adds another source exchange binding.</summary>
    /// <param name="exchangeName">The source exchange name.</param>
    /// <param name="configure">An optional callback that customizes the source exchange and binding.</param>
    void Bind(string exchangeName, Action<IRabbitMqExchangeToExchangeBindingConfigurator>? configure = null);
}
