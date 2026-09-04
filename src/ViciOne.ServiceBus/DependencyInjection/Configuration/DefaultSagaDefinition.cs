namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a default saga definition implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class DefaultSagaDefinition<TSaga> :
    SagaDefinition<TSaga>
    where TSaga : class, ISaga
{
}
