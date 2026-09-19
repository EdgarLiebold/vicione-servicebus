using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Adapts a message-specific activity to an untyped saga behavior.</summary>
/// <typeparam name="TSaga">The saga instance type managed by the activity.</typeparam>
/// <typeparam name="TMessage">The message contract required by the nested activity.</typeparam>
public class DataConverterActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga>
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
{
    readonly IStateMachineActivity<TSaga, TMessage> _activity;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activity">The activity.</param>
    /// <exception cref="ArgumentNullException"><paramref name="activity"/> is null.</exception>
    public DataConverterActivity(IStateMachineActivity<TSaga, TMessage> activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        _activity = activity;
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visitor"/> is null.</exception>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);

        visitor.Visit(this, _ => _activity.Accept(visitor));
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _activity.Probe(context);
    }

    /// <summary>Rejects execution without an event body.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    /// <exception cref="SagaStateMachineException">The activity is executed without an event body.</exception>
    public Task ExecuteAsync(IBehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        throw new SagaStateMachineException("This activity requires a body with the event, but no body was specified.");
    }

    /// <summary>Executes the nested activity when the event body and remaining behavior are compatible.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    /// <exception cref="SagaStateMachineException">The event body or remaining behavior is incompatible with <typeparamref name="TMessage"/>.</exception>
    public Task ExecuteAsync<T>(IBehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context is not IBehaviorContext<TSaga, TMessage> dataContext)
            throw new SagaStateMachineException("Expected Type " + typeof(TMessage).Name + " but was " + GetMessageTypeName(context.Message));

        if (next is not IBehavior<TSaga, TMessage> dataNext)
            throw new SagaStateMachineException("The next behavior was not a valid type");

        return _activity.ExecuteAsync(dataContext, dataNext);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the activity.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
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
    /// <exception cref="SagaStateMachineException">The event body or remaining behavior is incompatible with <typeparamref name="TMessage"/>.</exception>
    public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context is not IBehaviorExceptionContext<TSaga, TMessage, TException> dataContext)
            throw new SagaStateMachineException("Expected Type " + typeof(TMessage).Name + " but was " + GetMessageTypeName(context.Message));

        if (next is not IBehavior<TSaga, TMessage> dataNext)
            throw new SagaStateMachineException("The next behavior was not a valid type");

        return _activity.FaultedAsync(dataContext, dataNext);
    }

    static string GetMessageTypeName(object? message) => message?.GetType().Name ?? "null";
}
