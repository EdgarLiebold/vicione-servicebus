using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes a nested saga behavior through a retry policy.</summary>
/// <typeparam name="TInstance">The saga instance type managed by the activity.</typeparam>
public class RetryActivity<TInstance> :
    IStateMachineActivity<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    readonly IBehavior<TInstance> _retryBehavior;
    readonly IRetryPolicy _retryPolicy;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="retryPolicy">The retry policy.</param>
    /// <param name="retryBehavior">The state-machine behavior that controls retry execution.</param>
    /// <exception cref="ArgumentNullException"><paramref name="retryPolicy"/> or <paramref name="retryBehavior"/> is null.</exception>
    public RetryActivity(IRetryPolicy retryPolicy, IBehavior<TInstance> retryBehavior)
    {
        ArgumentNullException.ThrowIfNull(retryPolicy);
        ArgumentNullException.ThrowIfNull(retryBehavior);

        _retryPolicy = retryPolicy;
        _retryBehavior = retryBehavior;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateScope("retry");

        _retryBehavior.Probe(scope);
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visitor"/> is null.</exception>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);

        visitor.Visit(this, x => _retryBehavior.Accept(visitor));
    }

    /// <summary>Executes the retry behavior and then the remaining untyped behavior.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    public async Task ExecuteAsync(IBehaviorContext<TInstance> context, IBehavior<TInstance> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        await _retryPolicy.RetryAsync(() => ExecuteRetryBehaviorAsync(context), context.CancellationToken).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Executes the retry behavior and then the remaining message behavior.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    public async Task ExecuteAsync<T>(IBehaviorContext<TInstance, T> context, IBehavior<TInstance, T> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        await _retryPolicy.RetryAsync(() => ExecuteRetryBehaviorAsync(context), context.CancellationToken).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the activity.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<TInstance, TException> context, IBehavior<TInstance> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        return next.FaultedAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <typeparam name="TException">The exception handled by the activity.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TInstance, T, TException> context, IBehavior<TInstance, T> next)
        where T : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        return next.FaultedAsync(context);
    }

    async Task ExecuteRetryBehaviorAsync(IBehaviorContext<TInstance> context)
    {
        try
        {
            await _retryBehavior.ExecuteAsync(context).ConfigureAwait(false);
        }
        catch (EventExecutionException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Throw(exception.InnerException);
        }
    }

    async Task ExecuteRetryBehaviorAsync<T>(IBehaviorContext<TInstance, T> context)
        where T : class
    {
        try
        {
            await _retryBehavior.ExecuteAsync(context).ConfigureAwait(false);
        }
        catch (EventExecutionException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Throw(exception.InnerException);
        }
    }
}


/// <summary>Executes a nested saga behavior through a retry policy for one message contract.</summary>
/// <typeparam name="TInstance">The saga instance type managed by the activity.</typeparam>
/// <typeparam name="TMessage">The message contract that activates the retry behavior.</typeparam>
public class RetryActivity<TInstance, TMessage> :
    IStateMachineActivity<TInstance>
    where TInstance : class, ISagaStateMachineInstance
    where TMessage : class
{
    readonly IBehavior<TInstance> _retryBehavior;
    readonly IRetryPolicy _retryPolicy;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="retryPolicy">The retry policy.</param>
    /// <param name="retryBehavior">The state-machine behavior that controls retry execution.</param>
    /// <exception cref="ArgumentNullException"><paramref name="retryPolicy"/> or <paramref name="retryBehavior"/> is null.</exception>
    public RetryActivity(IRetryPolicy retryPolicy, IBehavior<TInstance> retryBehavior)
    {
        ArgumentNullException.ThrowIfNull(retryPolicy);
        ArgumentNullException.ThrowIfNull(retryBehavior);

        _retryPolicy = retryPolicy;
        _retryBehavior = retryBehavior;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateScope("retry");

        _retryBehavior.Probe(scope);
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visitor"/> is null.</exception>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);

        visitor.Visit(this, x => _retryBehavior.Accept(visitor));
    }

    /// <summary>Rejects execution without an event body.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    /// <exception cref="SagaStateMachineException">The activity is executed without an event body.</exception>
    public Task ExecuteAsync(IBehaviorContext<TInstance> context, IBehavior<TInstance> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        throw new SagaStateMachineException("This activity requires a body with the event, but no body was specified.");
    }

    /// <summary>Executes the retry behavior for a compatible message and then invokes the remaining behavior.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    public async Task ExecuteAsync<T>(IBehaviorContext<TInstance, T> context, IBehavior<TInstance, T> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context is IBehaviorContext<TInstance, TMessage> behaviorContext)
            await _retryPolicy.RetryAsync(() => ExecuteRetryBehaviorAsync(behaviorContext), context.CancellationToken).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the activity.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<TInstance, TException> context, IBehavior<TInstance> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        return next.FaultedAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <typeparam name="TException">The exception handled by the activity.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TInstance, T, TException> context, IBehavior<TInstance, T> next)
        where T : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        return next.FaultedAsync(context);
    }

    async Task ExecuteRetryBehaviorAsync(IBehaviorContext<TInstance, TMessage> context)
    {
        try
        {
            await _retryBehavior.ExecuteAsync(context).ConfigureAwait(false);
        }
        catch (EventExecutionException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Throw(exception.InnerException);
        }
    }
}
