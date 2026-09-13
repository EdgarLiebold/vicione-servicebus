using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Splits apart the data from the behavior so it can be invoked properly.</summary>
/// <typeparam name="TSaga">The instance type.</typeparam>
/// <typeparam name="TMessage">The event data type.</typeparam>
public class DataBehavior<TSaga, TMessage> :
    IBehavior<TSaga, TMessage>
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
{
    readonly IBehavior<TSaga> _behavior;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="behavior">The state-machine behavior to compose or inspect.</param>
    public DataBehavior(IBehavior<TSaga> behavior)
    {
        _behavior = behavior;
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(IStateMachineVisitor visitor)
    {
        _behavior.Accept(visitor);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        _behavior.Probe(context);
    }

    Task IBehavior<TSaga, TMessage>.ExecuteAsync(IBehaviorContext<TSaga, TMessage> context)
    {
        return _behavior.ExecuteAsync(context);
    }

    Task IBehavior<TSaga, TMessage>.FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TMessage, TException> context)
    {
        return _behavior.FaultedAsync(context);
    }
}
