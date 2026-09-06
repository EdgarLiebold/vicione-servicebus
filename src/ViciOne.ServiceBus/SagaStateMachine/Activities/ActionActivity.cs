using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the action activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class ActionActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly Action<BehaviorContext<TSaga>> _action = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="action">The action.</param>
    public ActionActivity(Action<BehaviorContext<TSaga>> action)
    {
        _action = action;
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("then");
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        _action(context);

        return next.ExecuteAsync(context);
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync<TData>(BehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
        where TData : class
    {
        _action(context);

        return next.ExecuteAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context,
        IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }
}


/// <summary>Executes the action activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ActionActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly Action<BehaviorContext<TSaga, TMessage>> _action;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="action">The action.</param>
    public ActionActivity(Action<BehaviorContext<TSaga, TMessage>> action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("then");
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        _action(context);

        return next.ExecuteAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }
}
