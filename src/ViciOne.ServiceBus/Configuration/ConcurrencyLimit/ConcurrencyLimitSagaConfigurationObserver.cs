using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures a concurrency limit for a consumer, on the consumer configurator, which is constrained to
/// the message types for that consumer, and only applies to the consumer prior to the consumer factory.
/// </summary>
/// <typeparam name="TSaga">The consumer type.</typeparam>
public class ConcurrencyLimitSagaConfigurationObserver<TSaga> :
    ISagaConfigurationObserver
    where TSaga : class, ISaga
{
    readonly ISagaConfigurator<TSaga> _configurator;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="concurrentMessageLimit">The concurrent message limit.</param>
    /// <param name="id">The id.</param>
    public ConcurrencyLimitSagaConfigurationObserver(ISagaConfigurator<TSaga> configurator, int concurrentMessageLimit, string? id = null)
    {
        _configurator = configurator;
        Limiter = new ConcurrencyLimiter(concurrentMessageLimit, id);
    }

    /// <summary>Gets the limiter.</summary>
    public IConcurrencyLimiter Limiter { get; }

    void ISagaConfigurationObserver.SagaConfigured<T>(ISagaConfigurator<T> configurator)
    {
    }

    /// <summary>Reports that state machine saga has been configured.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="stateMachine">The state machine.</param>
    public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, object stateMachine)
        where TInstance : class
    {
    }

    void ISagaConfigurationObserver.SagaMessageConfigured<T, TMessage>(ISagaMessageConfigurator<T, TMessage> configurator)
    {
        var specification = new ConcurrencyLimitConsumePipeSpecification<TMessage>(Limiter);

        _configurator.Message<TMessage>(x => x.AddPipeSpecification(specification));
    }
}
