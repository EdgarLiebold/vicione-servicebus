using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a copy context pipe implementation.
/// </summary>
public class CopyContextPipe :
    IPipe<SendContext>
{
    readonly Action<ConsumeContext, SendContext>? _callback;
    readonly ConsumeContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="callback">The callback value.</param>
    public CopyContextPipe(ConsumeContext context, Action<ConsumeContext, SendContext>? callback = null)
    {
        _context = context;
        _callback = callback;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(SendContext context)
    {
        context.MessageId = _context.MessageId;
        context.RequestId = _context.RequestId;
        context.CorrelationId = _context.CorrelationId;
        context.ConversationId = _context.ConversationId;
        context.InitiatorId = _context.InitiatorId;
        context.SourceAddress = _context.SourceAddress;
        context.ResponseAddress = _context.ResponseAddress;
        context.FaultAddress = _context.FaultAddress;

        TimeProvider timeProvider = _context.GetTimeProvider();
        context.SetTimeProvider(timeProvider);

        if (_context.ExpirationTime.HasValue)
            context.TimeToLive = _context.ExpirationTime.Value.ToUniversalTime() - timeProvider.GetUtcNow().UtcDateTime;

        foreach (KeyValuePair<string, object> header in _context.Headers.GetAll())
        {
            switch (header.Key)
            {
                case MessageHeaders.RedeliveryCount:
                case MessageHeaders.SchedulingTokenId:
                    continue;
            }

            context.Headers.Set(header.Key, header.Value, false);
        }

        _callback?.Invoke(_context, context);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("copyContext");
    }
}
