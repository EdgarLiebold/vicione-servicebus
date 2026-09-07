namespace ViciOne.ServiceBus.Configuration;

/// <summary>A consumer specification, that can be modified.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface ISagaSpecification<TSaga> :
    ISagaConfigurator<TSaga>,
    ISpecification
    where TSaga : class, ISaga
{
    /// <summary>Gets message specification.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message specification.</returns>
    ISagaMessageSpecification<TSaga, T> GetMessageSpecification<T>()
        where T : class;

    /// <summary>Apply any saga-wide configurations to the message pipe, such as concurrency limit, etc.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipeConfigurator">The pipe configurator.</param>
    void ConfigureMessagePipe<T>(IPipeConfigurator<ConsumeContext<T>> pipeConfigurator)
        where T : class;
}
