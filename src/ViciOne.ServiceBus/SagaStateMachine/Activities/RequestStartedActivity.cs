using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Publishes the <see cref="RequestStarted" /> event, used by the request state machine to track
/// pending requests for a saga instance.
/// </summary>
/// <typeparam name="TSaga"></typeparam>
/// <typeparam name="TMessage"></typeparam>
public class RequestStartedActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("requestStarted");
    }

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        await context.PublishAsync<RequestStarted>(new
        {
            context.Saga.CorrelationId,
            context.RequestId,
            context.ResponseAddress,
            context.FaultAddress,
            context.ExpirationTime,
            PayloadType = MessageTypeCache<TMessage>.MessageTypeNames.ToArray(),
            Payload = context.Message
        }, context.CancellationToken).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }
}
