namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures the shared and message-specific pipelines for one consumer.</summary>
/// <typeparam name="TConsumer">The consumer implementation configured by the specification.</typeparam>
public interface IConsumerSpecification<TConsumer> :
    IConsumerConfigurator<TConsumer>,
    ISpecification
    where TConsumer : class
{
    /// <summary>Gets the specification for a message contract consumed by this consumer.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <returns>The message specification.</returns>
    IConsumerMessageSpecification<TConsumer, TMessage> GetMessageSpecification<TMessage>()
        where TMessage : class;

    /// <summary>Applies consumer-wide middleware, including the configured concurrency policy, to a message pipe.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="pipeConfigurator">The message pipe that receives consumer-wide middleware.</param>
    void ConfigureMessagePipe<TMessage>(IPipeConfigurator<ConsumeContext<TMessage>> pipeConfigurator)
        where TMessage : class;
}
