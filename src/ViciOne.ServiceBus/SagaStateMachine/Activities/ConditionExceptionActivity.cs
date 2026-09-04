using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a condition exception activity implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TConditionException">The t condition exception type.</typeparam>
public class ConditionExceptionActivity<TSaga, TConditionException> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TConditionException : Exception
{
    readonly StateMachineAsyncExceptionCondition<TSaga, TConditionException> _condition;
    readonly IBehavior<TSaga> _elseBehavior;
    readonly IBehavior<TSaga> _thenBehavior;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="thenBehavior">The then behavior value.</param>
    /// <param name="elseBehavior">The else behavior value.</param>
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

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this, x => _thenBehavior.Accept(visitor));
        visitor.Visit(this, x => _elseBehavior.Accept(visitor));
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
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


/// <summary>
/// Provides a condition exception activity implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TConditionException">The t condition exception type.</typeparam>
public class ConditionExceptionActivity<TSaga, TMessage, TConditionException> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
    where TConditionException : Exception
{
    readonly StateMachineAsyncExceptionCondition<TSaga, TMessage, TConditionException> _condition;
    readonly IBehavior<TSaga> _elseBehavior;
    readonly IBehavior<TSaga> _thenBehavior;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="thenBehavior">The then behavior value.</param>
    /// <param name="elseBehavior">The else behavior value.</param>
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

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this, x => _thenBehavior.Accept(visitor));
        visitor.Visit(this, x => _elseBehavior.Accept(visitor));
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        throw new SagaStateMachineException("This activity requires a body with the event, but no body was specified.");
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        throw new SagaStateMachineException("This activity requires a body with the event, but no body was specified.");
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
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
