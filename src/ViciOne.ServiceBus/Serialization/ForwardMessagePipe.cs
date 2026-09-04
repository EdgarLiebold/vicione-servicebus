using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a forward message pipe implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ForwardMessagePipe<TMessage> :
    IPipe<SendContext<TMessage>>,
    ISendPipe
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;
    readonly IPipe<SendContext<TMessage>>? _pipe = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="pipe">The pipe value.</param>
    public ForwardMessagePipe(ConsumeContext<TMessage> context, IPipe<SendContext<TMessage>>? pipe = default)
    {
        _context = context;
        _pipe = pipe;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        _pipe?.Probe(context);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(SendContext<TMessage> context)
    {
        context.MessageId = _context.MessageId;
        context.RequestId = _context.RequestId;
        context.ConversationId = _context.ConversationId;
        context.CorrelationId = _context.CorrelationId;
        context.InitiatorId = _context.InitiatorId;
        context.SourceAddress = _context.SourceAddress;
        context.ResponseAddress = _context.ResponseAddress;
        context.FaultAddress = _context.FaultAddress;

        TimeProvider timeProvider = _context.GetTimeProvider();
        context.SetTimeProvider(timeProvider);

        if (_context.ExpirationTime.HasValue)
            context.TimeToLive = _context.ExpirationTime.Value.ToUniversalTime() - timeProvider.GetUtcNow();

        foreach (KeyValuePair<string, object> header in _context.Headers.GetAll())
            context.Headers.Set(header.Key, header.Value);

        if (_pipe != null && _pipe.IsNotEmpty())
            await _pipe.SendAsync(context).ConfigureAwait(false);

        if (ForwardingExpiration.MarkIfExpired(context, _context.ExpirationTime, timeProvider))
            return;

        var forwarderAddress = _context.Advanced().ReceiveContext.InputAddress ?? _context.DestinationAddress;
        if (forwarderAddress != null && forwarderAddress != context.DestinationAddress)
            context.Headers.Set(MessageHeaders.ForwarderAddress, forwarderAddress.ToString());

        if (_context.Advanced().SerializerContext != null)
            context.Serializer = _context.Advanced().SerializerContext.GetMessageSerializer();
        else
            context.Serializer = new CopyBodySerializer(_context.Advanced().ReceiveContext.ContentType, _context.Advanced().ReceiveContext.Body);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
        where T : class
    {
        return _pipe is ISendContextPipe sendContextPipe
            ? sendContextPipe.SendAsync(context, cancellationToken: cancellationToken)
            : Task.CompletedTask;
    }
}
