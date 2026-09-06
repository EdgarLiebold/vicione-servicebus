namespace ViciOne.ServiceBus.Configuration;

/// <summary>A consumer specification, that can be modified.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public interface IConsumerSpecification<TConsumer> :
    IConsumerConfigurator<TConsumer>,
    ISpecification
    where TConsumer : class
{
    /// <summary>Gets message specification.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message specification.</returns>
    IConsumerMessageSpecification<TConsumer, T> GetMessageSpecification<T>()
        where T : class;

    /// <summary>Apply any consumer-wide configurations to the message pipe, such as concurrency limit, etc.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipeConfigurator">The pipe configurator.</param>
    void ConfigureMessagePipe<T>(IPipeConfigurator<ConsumeContext<T>> pipeConfigurator)
        where T : class;
}
