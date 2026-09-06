namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Defines the operations required by composite event status accessor.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface ICompositeEventStatusAccessor<in TSaga> :
    IProbeSite
{
    /// <summary>Retrieves the requested value.</summary>
    /// <param name="instance">The instance.</param>
    /// <returns>The requested value.</returns>
    CompositeEventStatus Get(TSaga instance);

    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="instance">The instance.</param>
    /// <param name="status">The status.</param>
    void Set(TSaga instance, CompositeEventStatus status);
}
