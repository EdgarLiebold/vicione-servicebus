namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Defines the contract for composite event status accessor.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface ICompositeEventStatusAccessor<in TSaga> :
    IProbeSite
{
    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <returns>The result of the operation.</returns>
    CompositeEventStatus Get(TSaga instance);

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="status">The status value.</param>
    void Set(TSaga instance, CompositeEventStatus status);
}
