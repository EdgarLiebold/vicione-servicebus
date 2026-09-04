namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a state machine interface type implementation.
/// </summary>
public partial class StateMachineInterfaceType<TInstance, TData> :
    IStateMachineInterfaceType
    where TInstance : class, ISaga, SagaStateMachineInstance
    where TData : class
{
    readonly ISagaConnectorFactory _connectorFactory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="machine">The machine value.</param>
    /// <param name="correlation">The correlation value.</param>
    public StateMachineInterfaceType(SagaStateMachine<TInstance> machine, EventCorrelation<TInstance, TData> correlation)
    {
        _connectorFactory = new StateMachineEventConnectorFactory(machine, correlation);
    }

    ISagaMessageConnector<T> IStateMachineInterfaceType.GetConnector<T>()
    {
        return _connectorFactory.CreateMessageConnector<T>();
    }
}
