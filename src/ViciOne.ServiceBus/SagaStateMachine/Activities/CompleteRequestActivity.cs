using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Components;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the complete request activity.</summary>
public class CompleteRequestActivity :
    IStateMachineActivity<RequestState, RequestCompleted>
{
    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("completeRequest");
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync(BehaviorContext<RequestState, RequestCompleted> context, IBehavior<RequestState, RequestCompleted> next)
    {
        if (!context.Saga.ExpirationTime.HasValue || context.Saga.ExpirationTime.Value > context.GetTimeProvider().GetUtcNow().UtcDateTime)
        {
            IPipe<SendContext> pipe = new RequestStateMessagePipe(context, context.Message.Payload, context.Message.PayloadType);

            var endpoint = await context.GetSendEndpointAsync(context.Saga.ResponseAddress).ConfigureAwait(false);

            var dummyMessage = new CompletedEvent();

            await endpoint.SendAsync(dummyMessage, pipe, context.CancellationToken).ConfigureAwait(false);
        }

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<RequestState, RequestCompleted, TException> context,
        IBehavior<RequestState, RequestCompleted> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }


    class CompletedEvent
    {
    }
}
