using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Adapts an Activity to a Data Activity context
/// </summary>
/// <typeparam name="TSaga"></typeparam>
/// <typeparam name="TMessage"></typeparam>
public class SlimActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly IStateMachineActivity<TSaga> _activity;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="activity">The activity value.</param>
    public SlimActivity(IStateMachineActivity<TSaga> activity)
    {
        _activity = activity;
    }

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        _activity.Accept(visitor);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        _activity.Probe(context);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        return _activity.ExecuteAsync(context, next);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        return _activity.FaultedAsync(context, next);
    }
}
