using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Components;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.SagaStateMachine;

public class FaultRequestActivity :
    IStateMachineActivity<RequestState, RequestFaulted>
{
    public void Probe(ProbeContext context)
    {
        context.CreateScope("faultRequest");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

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
