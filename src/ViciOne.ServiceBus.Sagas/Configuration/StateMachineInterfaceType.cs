using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates message connectors for a correlated state-machine event.</summary>
/// <typeparam name="TInstance">The saga state handled by the state machine.</typeparam>
/// <typeparam name="TData">The message contract carried by the correlated event.</typeparam>
public partial class StateMachineInterfaceType<TInstance, TData> :
    IStateMachineInterfaceType
    where TInstance : class, ISaga, ISagaStateMachineInstance
    where TData : class
{
    readonly ISagaConnectorFactory _connectorFactory;

    /// <summary>Creates a connector factory from the state machine and event correlation.</summary>
    /// <param name="machine">The state machine consuming the correlated saga event.</param>
    /// <param name="correlation">The correlation supplying repository policy and message-dispatch filters.</param>
    public StateMachineInterfaceType(ISagaStateMachine<TInstance> machine, IEventCorrelation<TInstance, TData> correlation)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(correlation);

        _connectorFactory = new StateMachineEventConnectorFactory(machine, correlation);
    }

    ISagaMessageConnector<T> IStateMachineInterfaceType.GetConnector<T>()
    {
        if (typeof(T) != typeof(TInstance))
            throw new ArgumentException("The generic argument did not match the state machine instance type", nameof(T));

        return _connectorFactory.CreateMessageConnector<T>();
    }
}
