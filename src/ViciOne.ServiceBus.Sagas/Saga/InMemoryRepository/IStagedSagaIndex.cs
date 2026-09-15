namespace ViciOne.ServiceBus.Saga;

/// <summary>Captures a saga's index key before publishing its dictionary membership.</summary>
/// <typeparam name="TSaga">The referenced saga state type.</typeparam>
internal interface IStagedSagaIndex<TSaga>
    where TSaga : class, ISaga
{
    SagaIndexRegistration Capture(SagaInstance<TSaga> instance);
    void Remove(SagaInstance<TSaga> instance);
}
