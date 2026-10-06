using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Executes the pipeline for copy context.</summary>
public class CopyContextPipe :
    IPipe<SendContext>
{
    readonly Action<ConsumeContext, SendContext>? _callback;
    readonly ConsumeContext _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    public CopyContextPipe(ConsumeContext context, Action<ConsumeContext, SendContext>? callback = null)
    {
        _context = context;
        _callback = callback;
    }

    /// <summary>Copies message metadata and eligible headers to the send context, then invokes the optional callback.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("copyContext");
    }
}
