namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// A consumer specification, that can be modified
/// </summary>
/// <typeparam name="TConsumer"></typeparam>
public interface IConsumerSpecification<TConsumer> :
    IConsumerConfigurator<TConsumer>,
    ISpecification
    where TConsumer : class
{
    /// <summary>
    /// Gets message specification.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    IConsumerMessageSpecification<TConsumer, T> GetMessageSpecification<T>()
        where T : class;

    /// <summary>
    /// Apply any consumer-wide configurations to the message pipe, such as concurrency limit, etc.
    /// </summary>
    /// <param name="pipeConfigurator"></param>
    /// <typeparam name="T"></typeparam>
    void ConfigureMessagePipe<T>(IPipeConfigurator<ConsumeContext<T>> pipeConfigurator)
        where T : class;
}
