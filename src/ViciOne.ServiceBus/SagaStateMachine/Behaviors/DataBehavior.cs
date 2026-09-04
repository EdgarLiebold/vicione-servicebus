using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Splits apart the data from the behavior so it can be invoked properly.
/// </summary>
/// <typeparam name="TSaga">The instance type</typeparam>
/// <typeparam name="TMessage">The event data type</typeparam>
public class DataBehavior<TSaga, TMessage> :
    IBehavior<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly IBehavior<TSaga> _behavior;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="behavior">The behavior value.</param>
    public DataBehavior(IBehavior<TSaga> behavior)
    {
        _behavior = behavior;
    }

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        _behavior.Accept(visitor);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        _behavior.Probe(context);
    }

    Task IBehavior<TSaga, TMessage>.ExecuteAsync(BehaviorContext<TSaga, TMessage> context)
    {
        return _behavior.ExecuteAsync(context);
    }

    Task IBehavior<TSaga, TMessage>.FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context)
    {
        return _behavior.FaultedAsync(context);
    }
}
