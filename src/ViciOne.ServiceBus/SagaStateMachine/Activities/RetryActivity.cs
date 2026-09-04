using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a retry activity implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
public class RetryActivity<TInstance> :
    IStateMachineActivity<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    readonly IBehavior<TInstance> _retryBehavior;
    readonly IRetryPolicy _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="retryBehavior">The retry behavior value.</param>
    public RetryActivity(IRetryPolicy retryPolicy, IBehavior<TInstance> retryBehavior)
    {
        _retryPolicy = retryPolicy;
        _retryBehavior = retryBehavior;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("retry");

        _retryBehavior.Probe(scope);
    }

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this, x => _retryBehavior.Accept(visitor));
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExecuteAsync(BehaviorContext<TInstance> context, IBehavior<TInstance> next)
    {
        await _retryPolicy.RetryAsync(() => ExecuteRetryBehaviorAsync(context), context.CancellationToken);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExecuteAsync<T>(BehaviorContext<TInstance, T> context, IBehavior<TInstance, T> next)
        where T : class
    {
        await _retryPolicy.RetryAsync(() => ExecuteRetryBehaviorAsync(context), context.CancellationToken);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TInstance, TException> context, IBehavior<TInstance> next)
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
    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TInstance, T, TException> context, IBehavior<TInstance, T> next)
        where T : class
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }

    async Task ExecuteRetryBehaviorAsync(BehaviorContext<TInstance> context)
    {
        try
        {
            await _retryBehavior.ExecuteAsync(context).ConfigureAwait(false);
        }
        catch (EventExecutionException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    async Task ExecuteRetryBehaviorAsync<T>(BehaviorContext<TInstance, T> context)
        where T : class
    {
        try
        {
            await _retryBehavior.ExecuteAsync(context).ConfigureAwait(false);
        }
        catch (EventExecutionException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}


/// <summary>
/// Provides a retry activity implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class RetryActivity<TInstance, TMessage> :
    IStateMachineActivity<TInstance>
    where TInstance : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly IBehavior<TInstance> _retryBehavior;
    readonly IRetryPolicy _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="retryBehavior">The retry behavior value.</param>
    public RetryActivity(IRetryPolicy retryPolicy, IBehavior<TInstance> retryBehavior)
    {
        _retryPolicy = retryPolicy;
        _retryBehavior = retryBehavior;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("retry");

        _retryBehavior.Probe(scope);
    }

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this, x => _retryBehavior.Accept(visitor));
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TInstance> context, IBehavior<TInstance> next)
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
    public async Task ExecuteAsync<T>(BehaviorContext<TInstance, T> context, IBehavior<TInstance, T> next)
        where T : class
    {
        if (context is BehaviorContext<TInstance, TMessage> behaviorContext)
            await _retryPolicy.RetryAsync(() => ExecuteRetryBehaviorAsync(behaviorContext), context.CancellationToken);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TInstance, TException> context, IBehavior<TInstance> next)
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
    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TInstance, T, TException> context, IBehavior<TInstance, T> next)
        where T : class
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }

    async Task ExecuteRetryBehaviorAsync(BehaviorContext<TInstance, TMessage> context)
    {
        try
        {
            await _retryBehavior.ExecuteAsync(context).ConfigureAwait(false);
        }
        catch (EventExecutionException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
