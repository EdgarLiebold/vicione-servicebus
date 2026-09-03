namespace ViciOne.ServiceBus.DependencyInjection.Registration
{
    /// <summary>
    /// Registration seam used to decorate a consumer factory without coupling the runtime core to a specific decorator implementation.
    /// </summary>
    public interface IConsumerFactoryDecoratorRegistration<TConsumer>
        where TConsumer : class, IConsumer
    {
        IConsumerFactory<TConsumer> DecorateConsumerFactory(IConsumerFactory<TConsumer> consumerFactory);
    }
}
