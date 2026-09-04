namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// Registration seam used to decorate a consumer factory without coupling the runtime core to a specific decorator implementation.
/// </summary>
public interface IConsumerFactoryDecoratorRegistration<TConsumer>
    where TConsumer : class, IConsumer
{
    /// <summary>
    /// Performs the decorate consumer factory operation.
    /// </summary>
    /// <param name="consumerFactory">The consumer factory value.</param>
    /// <returns>The result of the operation.</returns>
    IConsumerFactory<TConsumer> DecorateConsumerFactory(IConsumerFactory<TConsumer> consumerFactory);
}
