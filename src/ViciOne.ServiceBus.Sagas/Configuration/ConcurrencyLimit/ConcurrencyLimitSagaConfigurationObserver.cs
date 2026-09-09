using ViciOne.ServiceBus.Middleware.ConcurrencyLimiting;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Shares one concurrency budget across every message pipeline configured for a saga.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class ConcurrencyLimitSagaConfigurationObserver<TSaga> :
    ISagaConfigurationObserver
    where TSaga : class, ISaga
{
    readonly ISagaConfigurator<TSaga> _configurator;

    /// <summary>Creates an observer that applies one shared concurrency budget to the saga's message pipelines.</summary>
    /// <param name="configurator">The saga whose message pipelines receive the limiter.</param>
    /// <param name="concurrencyLimit">The positive initial shared limit.</param>
    /// <param name="limiterId">The optional identifier used by management commands.</param>
    public ConcurrencyLimitSagaConfigurationObserver(ISagaConfigurator<TSaga> configurator, int concurrencyLimit, string? limiterId = null)
    {
        _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
        Limiter = new ConcurrencyLimiter(concurrencyLimit, limiterId);
    }

    /// <summary>Gets the limiter shared by the saga's message pipelines.</summary>
    public IConcurrencyLimiter Limiter { get; }

    void ISagaConfigurationObserver.SagaConfigured<T>(ISagaConfigurator<T> configurator)
    {
        // The limiter belongs to message pipelines; the saga-level notification has no pipeline to modify.
    }

    /// <summary>Leaves state-machine metadata unchanged because limiting is applied per message pipeline.</summary>
    /// <typeparam name="TInstance">The saga state type.</typeparam>
    /// <param name="configurator">The configured saga.</param>
    /// <param name="stateMachine">The state-machine definition.</param>
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
