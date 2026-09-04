using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a condition activity implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class ConditionActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly StateMachineAsyncCondition<TSaga> _condition;
    readonly IBehavior<TSaga> _elseBehavior;
    readonly IBehavior<TSaga> _thenBehavior;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="thenBehavior">The then behavior value.</param>
    /// <param name="elseBehavior">The else behavior value.</param>
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
    public async Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        if (await _condition(context).ConfigureAwait(false))
            await _thenBehavior.ExecuteAsync(context).ConfigureAwait(false);
        else
            await _elseBehavior.ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        if (await _condition(context).ConfigureAwait(false))
            await _thenBehavior.ExecuteAsync(context).ConfigureAwait(false);
        else
            await _elseBehavior.ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
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
        return next.FaultedAsync(context);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }
}


/// <summary>
/// Provides a condition activity implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ConditionActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly StateMachineAsyncCondition<TSaga, TMessage> _condition;
    readonly IBehavior<TSaga> _elseBehavior;
    readonly IBehavior<TSaga> _thenBehavior;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="thenBehavior">The then behavior value.</param>
    /// <param name="elseBehavior">The else behavior value.</param>
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
        return next.FaultedAsync(context);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }
}
