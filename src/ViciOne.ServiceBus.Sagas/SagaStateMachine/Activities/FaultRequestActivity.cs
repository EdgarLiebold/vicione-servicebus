using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Components;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Forwards a fault payload to the original response address.</summary>
public class FaultRequestActivity :
    IStateMachineActivity<RequestState, IRequestFaulted>
{
    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateScope("faultRequest");
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync(IBehaviorContext<RequestState, IRequestFaulted> context, IBehavior<RequestState, IRequestFaulted> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var outcome = new ForwardedRequestOutcome(context.Message.Payload, context.Message.PayloadType);
        IPipe<SendContext> pipe = new RequestStateMessagePipe(context, outcome);

        var endpoint = await context.GetSendEndpointAsync(context.Saga.ResponseAddress, context.CancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(outcome, pipe, context.CancellationToken).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<RequestState, IRequestFaulted, TException> context,
        IBehavior<RequestState, IRequestFaulted> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        return next.FaultedAsync(context);
    }
}
