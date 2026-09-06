using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the condition exception activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TConditionException">The condition exception type.</typeparam>
public class ConditionExceptionActivity<TSaga, TConditionException> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TConditionException : Exception
{
    readonly StateMachineAsyncExceptionCondition<TSaga, TConditionException> _condition;
    readonly IBehavior<TSaga> _elseBehavior;
    readonly IBehavior<TSaga> _thenBehavior;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenBehavior">The state-machine behavior executed when the condition is satisfied.</param>
    /// <param name="elseBehavior">The state-machine behavior executed when the condition is not satisfied.</param>
    public ConditionExceptionActivity(StateMachineAsyncExceptionCondition<TSaga, TConditionException> condition, IBehavior<TSaga> thenBehavior,
        IBehavior<TSaga> elseBehavior)
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
        return next.ExecuteAsync(context);
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        var behaviorContext = context as BehaviorExceptionContext<TSaga, TConditionException>;
        if (behaviorContext != null)
        {
            if (await _condition(behaviorContext).ConfigureAwait(false))
                await _thenBehavior.FaultedAsync(context).ConfigureAwait(false);
            else
                await _elseBehavior.FaultedAsync(context).ConfigureAwait(false);
        }

        await next.FaultedAsync(context).ConfigureAwait(false);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        var behaviorContext = context as BehaviorExceptionContext<TSaga, T, TConditionException>;
        if (behaviorContext != null)
        {
            if (await _condition(behaviorContext).ConfigureAwait(false))
                await _thenBehavior.FaultedAsync(context).ConfigureAwait(false);
            else
                await _elseBehavior.FaultedAsync(context).ConfigureAwait(false);
        }

        await next.FaultedAsync(context).ConfigureAwait(false);
    }
}


/// <summary>Executes the condition exception activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TConditionException">The condition exception type.</typeparam>
public class ConditionExceptionActivity<TSaga, TMessage, TConditionException> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
    where TConditionException : Exception
{
    readonly StateMachineAsyncExceptionCondition<TSaga, TMessage, TConditionException> _condition;
    readonly IBehavior<TSaga> _elseBehavior;
    readonly IBehavior<TSaga> _thenBehavior;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenBehavior">The state-machine behavior executed when the condition is satisfied.</param>
    /// <param name="elseBehavior">The state-machine behavior executed when the condition is not satisfied.</param>
    public ConditionExceptionActivity(StateMachineAsyncExceptionCondition<TSaga, TMessage, TConditionException> condition,
        IBehavior<TSaga> thenBehavior, IBehavior<TSaga> elseBehavior)
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
    public Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
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
        throw new SagaStateMachineException("This activity requires a body with the event, but no body was specified.");
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        var behaviorContext = context as BehaviorExceptionContext<TSaga, TMessage, TConditionException>;
        if (behaviorContext != null)
        {
            if (await _condition(behaviorContext).ConfigureAwait(false))
                await _thenBehavior.FaultedAsync(context).ConfigureAwait(false);
            else
                await _elseBehavior.FaultedAsync(context).ConfigureAwait(false);
        }

        await next.FaultedAsync(context).ConfigureAwait(false);
    }
}
