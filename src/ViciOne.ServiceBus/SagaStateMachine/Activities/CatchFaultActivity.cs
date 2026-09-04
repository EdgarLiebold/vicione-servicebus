using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Catches an exception of a specific type and compensates using the behavior
/// </summary>
/// <typeparam name="TSaga"></typeparam>
/// <typeparam name="TException"></typeparam>
public class CatchFaultActivity<TSaga, TException> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TException : Exception
{
    readonly IBehavior<TSaga> _behavior;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="behavior">The behavior value.</param>
    public CatchFaultActivity(IBehavior<TSaga> behavior)
    {
        _behavior = behavior;
    }

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this, x => _behavior.Accept(visitor));
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("catch");

        scope.Add("exceptionType", TypeCache<TException>.ShortName);

        _behavior.Probe(scope.CreateScope("behavior"));
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
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task FaultedAsync<T>(BehaviorExceptionContext<TSaga, T> context, IBehavior<TSaga> next)
        where T : Exception
    {
        if (context is BehaviorExceptionContext<TSaga, TException> exceptionContext)
        {
            await _behavior.FaultedAsync(exceptionContext).ConfigureAwait(false);

            // if the compensate returns, we should go forward normally
            await next.ExecuteAsync(context).ConfigureAwait(false);
        }
        else
            await next.FaultedAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task FaultedAsync<TMessage, T>(BehaviorExceptionContext<TSaga, TMessage, T> context, IBehavior<TSaga, TMessage> next)
        where TMessage : class
        where T : Exception
    {
        if (context is BehaviorExceptionContext<TSaga, TMessage, TException> exceptionContext)
        {
            await _behavior.FaultedAsync(exceptionContext).ConfigureAwait(false);

            // if the compensation returns, we should go forward normally
            await next.ExecuteAsync(context).ConfigureAwait(false);
        }
        else
            await next.FaultedAsync(context).ConfigureAwait(false);
    }
}
