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
        await context.PublishAsync<RequestCompleted>(new
        {
            context.Saga.CorrelationId,
            Timestamp = TimeProvider.System.GetUtcNow(),
            PayloadType = MessageTypeCache<TMessage>.MessageTypeNames,
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageFactory">The message factory value.</param>
    public RequestCompletedActivity(AsyncEventMessageFactory<TSaga, TMessage, TResponse> messageFactory)
    {
        _messageFactory = messageFactory;
    }

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
        await context.PublishAsync<RequestCompleted>(new
        {
            context.Saga.CorrelationId,
            Timestamp = TimeProvider.System.GetUtcNow(),
            PayloadType = MessageTypeCache<TResponse>.MessageTypeNames,
            Payload = _messageFactory(context)
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
