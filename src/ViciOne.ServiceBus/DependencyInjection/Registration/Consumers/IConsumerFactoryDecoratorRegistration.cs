namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Registration seam used to decorate a consumer factory without coupling the runtime core to a specific decorator implementation.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public interface IConsumerFactoryDecoratorRegistration<TConsumer>
    where TConsumer : class, IConsumer
{
    /// <summary>Decorates consumer factory.</summary>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <returns>The consumer factory produced by the operation.</returns>
    IConsumerFactory<TConsumer> DecorateConsumerFactory(IConsumerFactory<TConsumer> consumerFactory);
}
