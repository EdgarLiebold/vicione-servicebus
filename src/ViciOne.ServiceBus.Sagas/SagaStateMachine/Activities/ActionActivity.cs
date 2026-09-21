using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Invokes a synchronous action before continuing an untyped saga behavior.</summary>
/// <typeparam name="TSaga">The saga instance supplied to the action.</typeparam>
public class ActionActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    readonly Action<IBehaviorContext<TSaga>> _action;

    /// <summary>Creates an activity around a synchronous saga-context action.</summary>
    /// <param name="action">The action invoked before continuing the behavior.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public ActionActivity(Action<IBehaviorContext<TSaga>> action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    /// <summary>Visits this action in the state-machine graph.</summary>
    /// <param name="visitor">The visitor inspecting the graph.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visitor"/> is <see langword="null"/>.</exception>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    /// <summary>Creates a <c>then</c> scope in the diagnostic probe.</summary>
    /// <param name="context">The diagnostic probe to extend.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateScope("then");
    }

    /// <summary>Runs the action and then invokes the next untyped saga behavior.</summary>
    /// <param name="context">The saga behavior context passed to the action.</param>
    /// <param name="next">The behavior invoked after the action returns.</param>
    /// <returns>The task returned by the next behavior.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is <see langword="null"/>.</exception>
    public Task ExecuteAsync(IBehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        _action(context);

        return next.ExecuteAsync(context);
    }

    /// <summary>Runs the action and then invokes the next message-specific saga behavior.</summary>
    /// <typeparam name="TData">The message type carried through the saga action and subsequent behavior.</typeparam>
    /// <param name="context">The saga and message context passed to the action.</param>
    /// <param name="next">The behavior invoked after the action returns.</param>
    /// <returns>The task returned by the next behavior.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is <see langword="null"/>.</exception>
    public Task ExecuteAsync<TData>(IBehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        _action(context);

        return next.ExecuteAsync(context);
    }

    /// <summary>Forwards an untyped saga fault without invoking the success action.</summary>
    /// <typeparam name="TException">The exception type carried by the fault context.</typeparam>
    /// <param name="context">The saga and exception context to forward.</param>
    /// <param name="next">The behavior that handles the fault.</param>
    /// <returns>The task returned by the next fault behavior.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is <see langword="null"/>.</exception>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.FaultedAsync(context);
    }

    /// <summary>Forwards a message-specific saga fault without invoking the success action.</summary>
    /// <typeparam name="T">The message contract associated with the fault.</typeparam>
    /// <typeparam name="TException">The exception reported by the operation.</typeparam>
    /// <param name="context">The saga, message, and exception context to forward.</param>
    /// <param name="next">The behavior that handles the fault.</param>
    /// <returns>The task returned by the next fault behavior.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is <see langword="null"/>.</exception>
    public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TSaga, T, TException> context,
        IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.FaultedAsync(context);
    }
}


/// <summary>Invokes a synchronous action before continuing a message-specific saga behavior.</summary>
/// <typeparam name="TSaga">The saga instance supplied to the action.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ActionActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
{
    readonly Action<IBehaviorContext<TSaga, TMessage>> _action;

    /// <summary>Creates an activity around a synchronous saga-message action.</summary>
    /// <param name="action">The action invoked before continuing the behavior.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public ActionActivity(Action<IBehaviorContext<TSaga, TMessage>> action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    /// <summary>Visits this action in the state-machine graph.</summary>
    /// <param name="visitor">The visitor inspecting the graph.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visitor"/> is <see langword="null"/>.</exception>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    /// <summary>Creates a <c>then</c> scope in the diagnostic probe.</summary>
    /// <param name="context">The diagnostic probe to extend.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateScope("then");
    }

    /// <summary>Runs the message-specific action and then invokes the next saga behavior.</summary>
    /// <param name="context">The saga and message context passed to the action.</param>
    /// <param name="next">The behavior invoked after the action returns.</param>
    /// <returns>The task returned by the next behavior.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is <see langword="null"/>.</exception>
    public Task ExecuteAsync(IBehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        _action(context);

        return next.ExecuteAsync(context);
    }

    /// <summary>Forwards a message-specific saga fault without invoking the success action.</summary>
    /// <typeparam name="TException">The exception type carried by the fault context.</typeparam>
    /// <param name="context">The saga, message, and exception context to forward.</param>
    /// <param name="next">The behavior that handles the fault.</param>
    /// <returns>The task returned by the next fault behavior.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is <see langword="null"/>.</exception>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.FaultedAsync(context);
    }
}
