namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// A consumer specification, that can be modified
/// </summary>
/// <typeparam name="TSaga"></typeparam>
public interface ISagaSpecification<TSaga> :
    ISagaConfigurator<TSaga>,
    ISpecification
    where TSaga : class, ISaga
{
    /// <summary>
    /// Gets message specification.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    ISagaMessageSpecification<TSaga, T> GetMessageSpecification<T>()
        where T : class;

    /// <summary>
    /// Apply any saga-wide configurations to the message pipe, such as concurrency limit, etc.
    /// </summary>
    /// <param name="pipeConfigurator"></param>
    /// <typeparam name="T"></typeparam>
    void ConfigureMessagePipe<T>(IPipeConfigurator<ConsumeContext<T>> pipeConfigurator)
        where T : class;
}
