using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Components;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a request state message pipe implementation.
/// </summary>
public class RequestStateMessagePipe :
    IPipe<SendContext>
{
    readonly BehaviorContext<RequestState> _context;
    readonly object _message;
    readonly string[] _messageType;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    public RequestStateMessagePipe(BehaviorContext<RequestState> context, object message, string[] messageType)
    {
        _context = context;

        _message = message;
        _messageType = messageType;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
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
