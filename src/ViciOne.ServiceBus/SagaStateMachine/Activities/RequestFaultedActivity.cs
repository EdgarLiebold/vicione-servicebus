using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Publishes the <see cref="RequestCompleted" /> event, used by the request state machine to track
/// pending requests for a saga instance.
/// </summary>
/// <typeparam name="TSaga"></typeparam>
/// <typeparam name="TMessage"></typeparam>
/// <typeparam name="TRequest"></typeparam>
public class RequestFaultedActivity<TSaga, TMessage, TRequest> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
    where TRequest : class
{
    public void Probe(ProbeContext context)
    {
        context.CreateScope("requestFaulted");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public async Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        var payload = context.Message as Fault
            ?? throw new InvalidOperationException($"The message type {TypeCache<TMessage>.ShortName} must implement {nameof(Fault)}.");

        await context.PublishAsync<RequestFaulted>(new
        {
            context.Saga.CorrelationId,
            PayloadType = MessageTypeCache<Fault<TRequest>>.MessageTypeNames,
            Payload = new
            {
                payload.FaultId,
                payload.FaultedMessageId,
                payload.Timestamp,
                payload.Host,
                payload.Exceptions
            }
        }, context.CancellationToken).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }
}
