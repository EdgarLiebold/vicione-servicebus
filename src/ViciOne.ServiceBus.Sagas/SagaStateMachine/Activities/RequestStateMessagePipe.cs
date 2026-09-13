using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Components;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Applies the original request metadata and serialized response payload to a send context.</summary>
internal sealed class RequestStateMessagePipe :
    IPipe<SendContext>
{
    readonly IBehaviorContext<RequestState> _context;
    readonly ForwardedRequestOutcome _outcome;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="outcome">The request outcome being forwarded.</param>
    public RequestStateMessagePipe(IBehaviorContext<RequestState> context, ForwardedRequestOutcome outcome)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(SendContext context)
    {
        context.DestinationAddress = _context.Saga.ResponseAddress;
        context.SourceAddress = _context.Saga.SagaAddress;
        context.FaultAddress = _context.Saga.FaultAddress;
        context.RequestId = _context.Saga.CorrelationId;

        if (_context.Saga.ConversationId.HasValue)
            context.ConversationId = _context.Saga.ConversationId;

        if (_context.Saga.ExpirationTime.HasValue)
        {
            var timeToLive = _context.Saga.ExpirationTime.Value - _context.GetTimeProvider().GetUtcNow().UtcDateTime;
            context.TimeToLive = timeToLive > TimeSpan.Zero ? timeToLive : TimeSpan.FromSeconds(1);
        }

        context.Serializer = _context.SerializerContext.GetMessageSerializer(_outcome.Payload, _outcome.PayloadTypes);

        return Task.CompletedTask;
    }
}
