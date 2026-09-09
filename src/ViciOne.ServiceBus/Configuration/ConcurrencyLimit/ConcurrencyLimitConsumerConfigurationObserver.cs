using ViciOne.ServiceBus.Middleware.ConcurrencyLimiting;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures a concurrency limit for a consumer, on the consumer configurator, which is constrained to
/// the message types for that consumer, and only applies to the consumer prior to the consumer factory.
/// </summary>
/// <typeparam name="TConsumer">The consumer type.</typeparam>
internal sealed class ConcurrencyLimitConsumerConfigurationObserver<TConsumer> :
    IConsumerConfigurationObserver
    where TConsumer : class
{
    readonly IConsumerConfigurator<TConsumer> _configurator;

    /// <summary>Creates an observer that shares one concurrency budget across a consumer's message types.</summary>
    /// <param name="configurator">The consumer whose message pipelines receive the limiter.</param>
    /// <param name="concurrencyLimit">The positive initial shared limit.</param>
    /// <param name="limiterId">The optional identifier used by management commands.</param>
    public ConcurrencyLimitConsumerConfigurationObserver(IConsumerConfigurator<TConsumer> configurator, int concurrencyLimit,
        string? limiterId = null)
    {
        _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
        Limiter = new ConcurrencyLimiter(concurrencyLimit, limiterId);
    }

    /// <summary>Gets the limiter shared by the consumer's message pipelines.</summary>
    public IConcurrencyLimiter Limiter { get; }

    void IConsumerConfigurationObserver.ConsumerConfigured<T>(IConsumerConfigurator<T> configurator)
    {
        // The limiter belongs to message pipelines; the consumer-level notification has no pipeline to modify.
    }

    void IConsumerConfigurationObserver.ConsumerMessageConfigured<T, TMessage>(IConsumerMessageConfigurator<T, TMessage> configurator)
    {
        var specification = new ConcurrencyLimitConsumePipeSpecification<TMessage>(Limiter);

        _configurator.Message<TMessage>(x => x.AddPipeSpecification(specification));
    }
}
