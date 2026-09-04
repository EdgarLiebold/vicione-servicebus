namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for rabbit mq queue binding configurator.
/// </summary>
public interface IRabbitMqQueueBindingConfigurator :
    IRabbitMqQueueConfigurator,
    IRabbitMqExchangeBindingConfigurator
{
}
