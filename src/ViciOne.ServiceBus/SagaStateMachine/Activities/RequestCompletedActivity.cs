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
public class RequestCompletedActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    public void Probe(ProbeContext context)
    {
        context.CreateScope("requestStarted");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public async Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        await context.PublishAsync<RequestCompleted>(new
        {
            context.Saga.CorrelationId,
            InVar.Timestamp,
            PayloadType = MessageTypeCache<TMessage>.MessageTypeNames,
            Payload = context.Message
        }, context.CancellationToken).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }
}


/// <summary>
/// Publishes the <see cref="RequestCompleted" /> event, used by the request state machine to track
/// pending requests for a saga instance.
/// </summary>
/// <typeparam name="TSaga"></typeparam>
/// <typeparam name="TMessage"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public class RequestCompletedActivity<TSaga, TMessage, TResponse> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
    where TResponse : class
{
    readonly AsyncEventMessageFactory<TSaga, TMessage, TResponse> _messageFactory;

    public RequestCompletedActivity(AsyncEventMessageFactory<TSaga, TMessage, TResponse> messageFactory)
    {
        _messageFactory = messageFactory;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("requestStarted");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public async Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        await context.PublishAsync<RequestCompleted>(new
        {
            context.Saga.CorrelationId,
            InVar.Timestamp,
            PayloadType = MessageTypeCache<TResponse>.MessageTypeNames,
            Payload = _messageFactory(context)
        }, context.CancellationToken).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }
}
