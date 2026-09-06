using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes empty state-machine behavior.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class EmptyBehavior<TSaga> :
    IBehavior<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga> context)
    {
        return Task.CompletedTask;
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context)
        where T : class
    {
        return Task.CompletedTask;
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context)
        where T : class
        where TException : Exception
    {
        return Task.CompletedTask;
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context)
        where TException : Exception
    {
        return Task.CompletedTask;
    }
}


/// <summary>Executes empty state-machine behavior.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class EmptyBehavior<TSaga, TMessage> :
    IBehavior<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context)
    {
        return Task.CompletedTask;
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context)
        where TException : Exception
    {
        return Task.CompletedTask;
    }
}
