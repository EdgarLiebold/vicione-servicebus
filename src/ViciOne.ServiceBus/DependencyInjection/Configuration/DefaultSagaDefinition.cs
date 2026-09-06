namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration for default saga.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class DefaultSagaDefinition<TSaga> :
    SagaDefinition<TSaga>
    where TSaga : class, ISaga
{
}
