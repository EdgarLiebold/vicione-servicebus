namespace ViciOne.ServiceBus.Configuration;

/// <summary>Supplies convention defaults when a consumer has no explicit definition.</summary>
/// <typeparam name="TConsumer">The consumer implementation.</typeparam>
public class DefaultConsumerDefinition<TConsumer> :
    ConsumerDefinition<TConsumer>
    where TConsumer : class, IConsumer
{
}
