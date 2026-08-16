namespace ViciOne.ServiceBus
{
    public interface IRabbitMqQueueBindingConfigurator :
        IRabbitMqQueueConfigurator,
        IRabbitMqExchangeBindingConfigurator
    {
    }
}
