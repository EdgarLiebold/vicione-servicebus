using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the retry activity.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
public class RetryActivity<TInstance> :
    IStateMachineActivity<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    readonly IBehavior<TInstance> _retryBehavior;
    readonly IRetryPolicy _retryPolicy;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="retryPolicy">The retry policy.</param>
    /// <param name="retryBehavior">The state-machine behavior that controls retry execution.</param>
    public RetryActivity(IRetryPolicy retryPolicy, IBehavior<TInstance> retryBehavior)
    {
        _retryPolicy = retryPolicy;
        _retryBehavior = retryBehavior;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("retry");

        _retryBehavior.Probe(scope);
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(IStateMachineVisitor visitor)
    {
        visitor.Visit(this, x => _retryBehavior.Accept(visitor));
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync(IBehaviorContext<TInstance> context, IBehavior<TInstance> next)
    {
        await _retryPolicy.RetryAsync(() => ExecuteRetryBehaviorAsync(context), context.CancellationToken);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync<T>(IBehaviorContext<TInstance, T> context, IBehavior<TInstance, T> next)
        where T : class
    {
        await _retryPolicy.RetryAsync(() => ExecuteRetryBehaviorAsync(context), context.CancellationToken);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<TInstance, TException> context, IBehavior<TInstance> next)
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
    public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TInstance, T, TException> context, IBehavior<TInstance, T> next)
        where T : class
        where TException : Exception
    {
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
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
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
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}


/// <summary>Executes the retry activity.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
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
    public RetryActivity(IRetryPolicy retryPolicy, IBehavior<TInstance> retryBehavior)
    {
        _retryPolicy = retryPolicy;
        _retryBehavior = retryBehavior;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("retry");

        _retryBehavior.Probe(scope);
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(IStateMachineVisitor visitor)
    {
        visitor.Visit(this, x => _retryBehavior.Accept(visitor));
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(IBehaviorContext<TInstance> context, IBehavior<TInstance> next)
    {
        throw new SagaStateMachineException("This activity requires a body with the event, but no body was specified.");
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync<T>(IBehaviorContext<TInstance, T> context, IBehavior<TInstance, T> next)
        where T : class
    {
        if (context is IBehaviorContext<TInstance, TMessage> behaviorContext)
            await _retryPolicy.RetryAsync(() => ExecuteRetryBehaviorAsync(behaviorContext), context.CancellationToken);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<TInstance, TException> context, IBehavior<TInstance> next)
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
    public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TInstance, T, TException> context, IBehavior<TInstance, T> next)
        where T : class
        where TException : Exception
    {
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
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
