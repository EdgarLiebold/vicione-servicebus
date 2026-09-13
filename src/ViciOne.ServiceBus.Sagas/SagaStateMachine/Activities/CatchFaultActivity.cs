using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Catches an exception of a specific type and compensates using the behavior.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public class CatchFaultActivity<TSaga, TException> :
    IStateMachineActivity<TSaga>,
    IStateMachineExceptionActivity
    where TSaga : class, ISagaStateMachineInstance
    where TException : Exception
{
    readonly IBehavior<TSaga> _behavior;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="behavior">The state-machine behavior to compose or inspect.</param>
    /// <exception cref="ArgumentNullException"><paramref name="behavior" /> is <see langword="null" />.</exception>
    public CatchFaultActivity(IBehavior<TSaga> behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);
        _behavior = behavior;
    }

    /// <summary>Gets the exception type handled by the activity.</summary>
    public Type ExceptionType => typeof(TException);

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(IStateMachineVisitor visitor)
    {
        visitor.Visit(this, x => _behavior.Accept(visitor));
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("catch");

        scope.Add("exceptionType", TypeCache<TException>.ShortName);

        _behavior.Probe(scope.CreateScope("behavior"));
    }

    /// <summary>Passes forward processing to the next behavior.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(IBehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>Passes message processing to the next behavior.</summary>
    /// <typeparam name="T">The message contract carried by the event.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync<T>(IBehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>Compensates a matching exception or forwards it to the next fault behavior.</summary>
    /// <typeparam name="T">The exception type received by the fault pipeline.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task FaultedAsync<T>(IBehaviorExceptionContext<TSaga, T> context, IBehavior<TSaga> next)
        where T : Exception
    {
        if (context is IBehaviorExceptionContext<TSaga, TException> exceptionContext)
        {
            await _behavior.FaultedAsync(exceptionContext).ConfigureAwait(false);

            // Successful compensation resumes the forward pipeline.
            await next.ExecuteAsync(context).ConfigureAwait(false);
        }
        else
            await next.FaultedAsync(context).ConfigureAwait(false);
    }

    /// <summary>Compensates a matching message-processing exception or forwards it to the next fault behavior.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <typeparam name="T">The exception type received by the fault pipeline.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task FaultedAsync<TMessage, T>(IBehaviorExceptionContext<TSaga, TMessage, T> context, IBehavior<TSaga, TMessage> next)
        where TMessage : class
        where T : Exception
    {
        if (context is IBehaviorExceptionContext<TSaga, TMessage, TException> exceptionContext)
        {
            await _behavior.FaultedAsync(exceptionContext).ConfigureAwait(false);

            // Successful compensation resumes the forward pipeline.
            await next.ExecuteAsync(context).ConfigureAwait(false);
        }
        else
            await next.FaultedAsync(context).ConfigureAwait(false);
    }
}
