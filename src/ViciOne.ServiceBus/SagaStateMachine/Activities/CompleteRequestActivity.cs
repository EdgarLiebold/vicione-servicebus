using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Components;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.SagaStateMachine;

public class CompleteRequestActivity :
    IStateMachineActivity<RequestState, RequestCompleted>
{
    public void Probe(ProbeContext context)
    {
        context.CreateScope("completeRequest");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

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
