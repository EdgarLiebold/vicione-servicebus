using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the faulted container factory activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
/// <typeparam name="TActivity">The activity type.</typeparam>
public class FaultedContainerFactoryActivity<TSaga, TException, TActivity> :
    IStateMachineActivity<TSaga>
    where TActivity : class, IStateMachineActivity<TSaga>
    where TSaga : class, ISagaStateMachineInstance
    where TException : Exception
{
    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visitor"/> is null.</exception>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    public Task ExecuteAsync(IBehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.ExecuteAsync(context);
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    public Task ExecuteAsync<T>(IBehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.ExecuteAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TOtherException">The other exception type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    public Task FaultedAsync<TOtherException>(IBehaviorExceptionContext<TSaga, TOtherException> context, IBehavior<TSaga> next)
        where TOtherException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        if (context is IBehaviorExceptionContext<TSaga, TException> exceptionContext)
        {
            var activity = context.GetServiceOrCreateInstance<TActivity>();

            return activity.FaultedAsync(exceptionContext, next);
        }

        return next.FaultedAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <typeparam name="TOtherException">The other exception type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    public Task FaultedAsync<T, TOtherException>(IBehaviorExceptionContext<TSaga, T, TOtherException> context, IBehavior<TSaga, T> next)
        where T : class
        where TOtherException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        if (context is IBehaviorExceptionContext<TSaga, T, TException> exceptionContext)
        {
            var activity = context.GetServiceOrCreateInstance<TActivity>();

            return activity.FaultedAsync(exceptionContext, next);
        }

        return next.FaultedAsync(context);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateScope("containerActivityFactory");
    }
}


/// <summary>Executes the faulted container factory activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
/// <typeparam name="TActivity">The activity type.</typeparam>
public class FaultedContainerFactoryActivity<TSaga, TMessage, TException, TActivity> :
    IStateMachineActivity<TSaga, TMessage>
    where TActivity : class, IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, ISagaStateMachineInstance
    where TException : Exception
    where TMessage : class
{
    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateScope("containerActivityFactory");
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    public Task ExecuteAsync(IBehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.ExecuteAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The exception type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    public Task FaultedAsync<T>(IBehaviorExceptionContext<TSaga, TMessage, T> context, IBehavior<TSaga, TMessage> next)
        where T : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        if (context is IBehaviorExceptionContext<TSaga, TMessage, TException> exceptionContext)
        {
            var activity = context.GetServiceOrCreateInstance<TActivity>();

            return activity.FaultedAsync(exceptionContext, next);
        }

        return next.FaultedAsync(context);
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visitor"/> is null.</exception>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }
}
