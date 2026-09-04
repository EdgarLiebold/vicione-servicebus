using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures a concurrency limit for a consumer, on the consumer configurator, which is constrained to
/// the message types for that consumer, and only applies to the consumer prior to the consumer factory.
/// </summary>
/// <typeparam name="TConsumer">The consumer type</typeparam>
public class ConcurrencyLimitConsumerConfigurationObserver<TConsumer> :
    IConsumerConfigurationObserver
    where TConsumer : class
{
    readonly IConsumerConfigurator<TConsumer> _configurator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="concurrentMessageLimit">The concurrent message limit value.</param>
    /// <param name="id">The id value.</param>
    public ConcurrencyLimitConsumerConfigurationObserver(IConsumerConfigurator<TConsumer> configurator, int concurrentMessageLimit, string? id = null)
    {
        _configurator = configurator;
        Limiter = new ConcurrencyLimiter(concurrentMessageLimit, id);
    }

    /// <summary>
    /// Gets the limiter value.
    /// </summary>
    public IConcurrencyLimiter Limiter { get; }

    void IConsumerConfigurationObserver.ConsumerConfigured<T>(IConsumerConfigurator<T> configurator)
    {
    }

    void IConsumerConfigurationObserver.ConsumerMessageConfigured<T, TMessage>(IConsumerMessageConfigurator<T, TMessage> configurator)
    {
        var specification = new ConcurrencyLimitConsumePipeSpecification<TMessage>(Limiter);

        _configurator.Message<TMessage>(x => x.AddPipeSpecification(specification));
    }
}
