namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates message connectors for a correlated state-machine event.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
public partial class StateMachineInterfaceType<TInstance, TData> :
    IStateMachineInterfaceType
    where TInstance : class, ISaga, ISagaStateMachineInstance
    where TData : class
{
    readonly ISagaConnectorFactory _connectorFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="machine">The machine.</param>
    /// <param name="correlation">The correlation.</param>
    public StateMachineInterfaceType(ISagaStateMachine<TInstance> machine, IEventCorrelation<TInstance, TData> correlation)
    {
        _connectorFactory = new StateMachineEventConnectorFactory(machine, correlation);
    }

    ISagaMessageConnector<T> IStateMachineInterfaceType.GetConnector<T>()
    {
        return _connectorFactory.CreateMessageConnector<T>();
    }
}
