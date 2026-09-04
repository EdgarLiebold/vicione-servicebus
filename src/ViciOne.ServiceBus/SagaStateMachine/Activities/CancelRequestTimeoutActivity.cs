using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a cancel request timeout activity implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TRequest">The t request type.</typeparam>
/// <typeparam name="TResponse">The t response type.</typeparam>
public class CancelRequestTimeoutActivity<TSaga, TMessage, TRequest, TResponse> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TRequest : class
    where TResponse : class
    where TMessage : class
{
    readonly bool _completed;
    readonly Request<TSaga, TRequest, TResponse> _request;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="completed">The completed value.</param>
    public CancelRequestTimeoutActivity(Request<TSaga, TRequest, TResponse> request, bool completed)
    {
        _request = request;
        _completed = completed;
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
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("cancelRequest");
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        Guid? requestId = _request.GetRequestId(context.Saga);
        if (requestId.HasValue && _request.Settings.Timeout > TimeSpan.Zero)
        {
            if (context.TryGetPayload(out MessageSchedulerContext? schedulerContext))
            {
                await schedulerContext.CancelScheduledSendAsync(context.ReceiveContext.InputAddress, requestId.Value, context.CancellationToken)
                    .ConfigureAwait(false);
            }
            else
                throw new ConfigurationException("A scheduler was not available to cancel the scheduled request timeout");
        }

        if (_request.Settings.ClearRequestIdOnFaulted || _completed)
            _request.SetRequestId(context.Saga, null);

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
