using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Components;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a fault request activity implementation.
/// </summary>
public class FaultRequestActivity :
    IStateMachineActivity<RequestState, RequestFaulted>
{
    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("faultRequest");
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
    public async Task ExecuteAsync(BehaviorContext<RequestState, RequestFaulted> context, IBehavior<RequestState, RequestFaulted> next)
    {
        if (!context.Saga.ExpirationTime.HasValue || context.Saga.ExpirationTime.Value > context.GetTimeProvider().GetUtcNow().UtcDateTime)
        {
            IPipe<SendContext> pipe = new RequestStateMessagePipe(context, context.Message.Payload, context.Message.PayloadType);

            var endpoint = await context.GetSendEndpointAsync(context.Saga.ResponseAddress).ConfigureAwait(false);

            var dummyMessage = new FaultedEvent();

            await endpoint.SendAsync(dummyMessage, pipe, context.CancellationToken).ConfigureAwait(false);
        }

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<RequestState, RequestFaulted, TException> context,
        IBehavior<RequestState, RequestFaulted> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }


    class FaultedEvent
    {
    }
}
