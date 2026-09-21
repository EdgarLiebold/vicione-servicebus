using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the condition exception activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TConditionException">The condition exception type.</typeparam>
public class ConditionExceptionActivity<TSaga, TConditionException> :
    IStateMachineActivity<TSaga>
    where TSaga : class, ISagaStateMachineInstance
    where TConditionException : Exception
{
    readonly StateMachineAsyncExceptionCondition<TSaga, TConditionException> _condition;
    readonly IBehavior<TSaga> _elseBehavior;
    readonly IBehavior<TSaga> _thenBehavior;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenBehavior">The state-machine behavior executed when the condition is satisfied.</param>
    /// <param name="elseBehavior">The state-machine behavior executed when the condition is not satisfied.</param>
    /// <exception cref="ArgumentNullException"><paramref name="condition" />, <paramref name="thenBehavior" />, or <paramref name="elseBehavior" /> is <see langword="null" />.</exception>
    public ConditionExceptionActivity(StateMachineAsyncExceptionCondition<TSaga, TConditionException> condition, IBehavior<TSaga> thenBehavior,
        IBehavior<TSaga> elseBehavior)
    {
        _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        _thenBehavior = thenBehavior ?? throw new ArgumentNullException(nameof(thenBehavior));
        _elseBehavior = elseBehavior ?? throw new ArgumentNullException(nameof(elseBehavior));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateScope("condition");

        _thenBehavior.Probe(scope);
        _elseBehavior.Probe(scope);
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visitor" /> is <see langword="null" />.</exception>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this, x => _thenBehavior.Accept(visitor));
        visitor.Visit(this, x => _elseBehavior.Accept(visitor));
    }

    /// <summary>Passes forward processing to the next behavior.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    public Task ExecuteAsync(IBehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.ExecuteAsync(context);
    }

    /// <summary>Passes message processing to the next behavior.</summary>
    /// <typeparam name="T">The message type forwarded without evaluating the fault condition.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    public Task ExecuteAsync<T>(IBehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.ExecuteAsync(context);
    }

    /// <summary>Evaluates the configured condition for matching faults, invokes the selected behavior, and forwards the fault.</summary>
    /// <typeparam name="TException">The exception reported by the operation.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The configured condition returns <see langword="null" />.</exception>
    public async Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var behaviorContext = context as IBehaviorExceptionContext<TSaga, TConditionException>;
        if (behaviorContext != null)
        {
            Task<bool> conditionTask = _condition(behaviorContext)
                ?? throw new InvalidOperationException("The async condition returned null.");
            if (await conditionTask.ConfigureAwait(false))
                await _thenBehavior.FaultedAsync(context).ConfigureAwait(false);
            else
                await _elseBehavior.FaultedAsync(context).ConfigureAwait(false);
        }

        await next.FaultedAsync(context).ConfigureAwait(false);
    }

    /// <summary>Evaluates the configured condition for matching message faults, invokes the selected behavior, and forwards the fault.</summary>
    /// <typeparam name="T">The message contract associated with the fault.</typeparam>
    /// <typeparam name="TException">The exception reported by the operation.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The configured condition returns <see langword="null" />.</exception>
    public async Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var behaviorContext = context as IBehaviorExceptionContext<TSaga, T, TConditionException>;
        if (behaviorContext != null)
        {
            Task<bool> conditionTask = _condition(behaviorContext)
                ?? throw new InvalidOperationException("The async condition returned null.");
            if (await conditionTask.ConfigureAwait(false))
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
    where TSaga : class, ISagaStateMachineInstance
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
    /// <exception cref="ArgumentNullException"><paramref name="condition" />, <paramref name="thenBehavior" />, or <paramref name="elseBehavior" /> is <see langword="null" />.</exception>
    public ConditionExceptionActivity(StateMachineAsyncExceptionCondition<TSaga, TMessage, TConditionException> condition,
        IBehavior<TSaga> thenBehavior, IBehavior<TSaga> elseBehavior)
    {
        _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        _thenBehavior = thenBehavior ?? throw new ArgumentNullException(nameof(thenBehavior));
        _elseBehavior = elseBehavior ?? throw new ArgumentNullException(nameof(elseBehavior));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateScope("condition");

        _thenBehavior.Probe(scope);
        _elseBehavior.Probe(scope);
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visitor" /> is <see langword="null" />.</exception>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this, x => _thenBehavior.Accept(visitor));
        visitor.Visit(this, x => _elseBehavior.Accept(visitor));
    }

    /// <summary>Requires a message-bearing context for this message-specific activity.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="SagaStateMachineException">No message body was supplied for this message-specific activity.</exception>
    public Task ExecuteAsync(IBehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        throw new SagaStateMachineException("This activity requires a body with the event, but no body was specified.");
    }

    /// <summary>Passes message processing to the next behavior.</summary>
    /// <typeparam name="T">The message type forwarded without evaluating the message-fault condition.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    public Task ExecuteAsync<T>(IBehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.ExecuteAsync(context);
    }

    /// <summary>Requires a message-bearing fault context for this message-specific activity.</summary>
    /// <typeparam name="TException">The exception reported by the operation.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="SagaStateMachineException">No message body was supplied for this message-specific activity.</exception>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        throw new SagaStateMachineException("This activity requires a body with the event, but no body was specified.");
    }

    /// <summary>Evaluates the configured condition for matching message faults, invokes the selected behavior, and forwards the fault.</summary>
    /// <typeparam name="T">The message contract associated with the fault.</typeparam>
    /// <typeparam name="TException">The exception reported by the operation.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The configured condition returns <see langword="null" />.</exception>
    public async Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var behaviorContext = context as IBehaviorExceptionContext<TSaga, TMessage, TConditionException>;
        if (behaviorContext != null)
        {
            Task<bool> conditionTask = _condition(behaviorContext)
                ?? throw new InvalidOperationException("The async condition returned null.");
            if (await conditionTask.ConfigureAwait(false))
                await _thenBehavior.FaultedAsync(context).ConfigureAwait(false);
            else
                await _elseBehavior.FaultedAsync(context).ConfigureAwait(false);
        }

        await next.FaultedAsync(context).ConfigureAwait(false);
    }
}
