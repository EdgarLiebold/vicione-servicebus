using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the faulted action activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public class FaultedActionActivity<TSaga, TException> :
    IStateMachineActivity<TSaga>
    where TException : Exception
    where TSaga : class, SagaStateMachineInstance
{
    readonly Action<BehaviorExceptionContext<TSaga, TException>> _action;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="action">The action.</param>
    public FaultedActionActivity(Action<BehaviorExceptionContext<TSaga, TException>> action)
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
        context.CreateScope("then-faulted");
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
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
        return next.ExecuteAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<T>(BehaviorExceptionContext<TSaga, T> context, IBehavior<TSaga> next)
        where T : Exception
    {
        if (context is BehaviorExceptionContext<TSaga, TException> exceptionContext)
            _action(exceptionContext);

        return next.FaultedAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<TData, T>(BehaviorExceptionContext<TSaga, TData, T> context, IBehavior<TSaga, TData> next)
        where TData : class
        where T : Exception
    {
        if (context is BehaviorExceptionContext<TSaga, TData, TException> exceptionContext)
            _action(exceptionContext);

        return next.FaultedAsync(context);
    }
}


/// <summary>Executes the faulted action activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public class FaultedActionActivity<TSaga, TMessage, TException> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TException : Exception
    where TMessage : class
{
    readonly Action<BehaviorExceptionContext<TSaga, TMessage, TException>> _action;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="action">The action.</param>
    public FaultedActionActivity(Action<BehaviorExceptionContext<TSaga, TMessage, TException>> action)
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
        context.CreateScope("then-faulted");
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<T>(BehaviorExceptionContext<TSaga, TMessage, T> context, IBehavior<TSaga, TMessage> next)
        where T : Exception
    {
        if (context is BehaviorExceptionContext<TSaga, TMessage, TException> exceptionContext)
            _action(exceptionContext);

        return next.FaultedAsync(context);
    }
}
