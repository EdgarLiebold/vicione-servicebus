using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the cancel request timeout activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class CancelRequestTimeoutActivity<TSaga, TMessage, TRequest, TResponse> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, ISagaStateMachineInstance
    where TRequest : class
    where TResponse : class
    where TMessage : class
{
    readonly bool _completed;
    readonly IRequest<TSaga, TRequest, TResponse> _request;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="request">The request.</param>
    /// <param name="completed">The completed.</param>
    public CancelRequestTimeoutActivity(IRequest<TSaga, TRequest, TResponse> request, bool completed)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _completed = completed;
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateScope("cancelRequest");
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync(IBehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        context.CancellationToken.ThrowIfCancellationRequested();

        Guid? requestId = _request.GetRequestId(context.Saga);
        if (requestId.HasValue && _request.Settings.Timeout > TimeSpan.Zero)
        {
            if (context.TryGetPayload(out MessageSchedulerContext? schedulerContext))
            {
                await schedulerContext.CancelScheduledSendAsync(context.ReceiveContext.InputAddress, requestId.Value, context.CancellationToken)
                    .ConfigureAwait(false);
            }
            else
                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Saga", "unknown", "A scheduler was not available to cancel the scheduled request timeout", "Correct the named configuration before starting the host"));
        }

        if (_request.Settings.ClearRequestIdOnFaulted || _completed)
            _request.SetRequestId(context.Saga, null);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        return next.FaultedAsync(context);
    }
}
