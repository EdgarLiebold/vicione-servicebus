using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Components;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the pipeline for request state message.</summary>
public class RequestStateMessagePipe :
    IPipe<SendContext>
{
    readonly BehaviorContext<RequestState> _context;
    readonly object _message;
    readonly string[] _messageType;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    public RequestStateMessagePipe(BehaviorContext<RequestState> context, object message, string[] messageType)
    {
        _context = context;

        _message = message;
        _messageType = messageType;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(SendContext context)
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

        context.Serializer = _context.SerializerContext.GetMessageSerializer(_message, _messageType);
    }
}
