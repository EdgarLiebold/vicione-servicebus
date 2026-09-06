using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the condition activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class ConditionActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly StateMachineAsyncCondition<TSaga> _condition;
    readonly IBehavior<TSaga> _elseBehavior;
    readonly IBehavior<TSaga> _thenBehavior;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenBehavior">The state-machine behavior executed when the condition is satisfied.</param>
    /// <param name="elseBehavior">The state-machine behavior executed when the condition is not satisfied.</param>
    public ConditionActivity(StateMachineAsyncCondition<TSaga> condition, IBehavior<TSaga> thenBehavior, IBehavior<TSaga> elseBehavior)
    {
        _condition = condition;
        _thenBehavior = thenBehavior;
        _elseBehavior = elseBehavior;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateScope("condition");

        _thenBehavior.Probe(scope);
        _elseBehavior.Probe(scope);
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this, x => _thenBehavior.Accept(visitor));
        visitor.Visit(this, x => _elseBehavior.Accept(visitor));
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        if (await _condition(context).ConfigureAwait(false))
            await _thenBehavior.ExecuteAsync(context).ConfigureAwait(false);
        else
            await _elseBehavior.ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        if (await _condition(context).ConfigureAwait(false))
            await _thenBehavior.ExecuteAsync(context).ConfigureAwait(false);
        else
            await _elseBehavior.ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
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
    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }
}


/// <summary>Executes the condition activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ConditionActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly StateMachineAsyncCondition<TSaga, TMessage> _condition;
    readonly IBehavior<TSaga> _elseBehavior;
    readonly IBehavior<TSaga> _thenBehavior;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenBehavior">The state-machine behavior executed when the condition is satisfied.</param>
    /// <param name="elseBehavior">The state-machine behavior executed when the condition is not satisfied.</param>
    public ConditionActivity(StateMachineAsyncCondition<TSaga, TMessage> condition, IBehavior<TSaga> thenBehavior, IBehavior<TSaga> elseBehavior)
    {
        _condition = condition;
        _thenBehavior = thenBehavior;
        _elseBehavior = elseBehavior;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateScope("condition");

        _thenBehavior.Probe(scope);
        _elseBehavior.Probe(scope);
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this, x => _thenBehavior.Accept(visitor));
        visitor.Visit(this, x => _elseBehavior.Accept(visitor));
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        throw new SagaStateMachineException("This activity requires a body with the event, but no body was specified.");
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        if (context is BehaviorContext<TSaga, TMessage> behaviorContext)
        {
            if (await _condition(behaviorContext).ConfigureAwait(false))
                await _thenBehavior.ExecuteAsync(behaviorContext).ConfigureAwait(false);
            else
                await _elseBehavior.ExecuteAsync(behaviorContext).ConfigureAwait(false);
        }

        await next.ExecuteAsync(context).ConfigureAwait(false);
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
    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }
}
