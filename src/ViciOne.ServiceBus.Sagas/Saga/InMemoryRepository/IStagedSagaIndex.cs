namespace ViciOne.ServiceBus.Saga;

/// <summary>Captures a saga's index key before publishing its dictionary membership.</summary>
/// <typeparam name="TSaga">The referenced saga state type.</typeparam>
internal interface IStagedSagaIndex<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Reads the required wrapper's key once and returns an unapplied registration.</summary>
    /// <param name="instance">The required wrapper whose key is captured.</param>
    SagaIndexRegistration Capture(SagaInstance<TSaga> instance);

    /// <summary>Removes the exact registered wrapper by its previously captured key.</summary>
    /// <param name="instance">The required wrapper, or an unregistered wrapper to leave unchanged.</param>
    void Remove(SagaInstance<TSaga> instance);
}
