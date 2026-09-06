namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures a RabbitMQ queue, its exchange, and the binding between them.</summary>
public interface IRabbitMqQueueBindingConfigurator :
    IRabbitMqQueueConfigurator,
    IRabbitMqExchangeBindingConfigurator
{
}
