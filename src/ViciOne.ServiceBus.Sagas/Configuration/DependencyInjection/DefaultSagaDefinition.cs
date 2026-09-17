namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides the fallback definition used when a saga has no explicitly registered definition.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class DefaultSagaDefinition<TSaga> :
    SagaDefinition<TSaga>
    where TSaga : class, ISaga
{
}
