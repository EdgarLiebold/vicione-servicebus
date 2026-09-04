using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Extracts the CorrelationId from the message where there is a one-to-one correlation
/// identifier in the message (such as CorrelationId) and sets it in the header for use
/// by the saga repository.
/// </summary>
/// <typeparam name="TMessage">The message type</typeparam>
public class CorrelationIdMessageFilter<TMessage> :
    IFilter<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly Func<ConsumeContext<TMessage>, Guid> _getCorrelationId;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="getCorrelationId">The get correlation id value.</param>
    public CorrelationIdMessageFilter(Func<ConsumeContext<TMessage>, Guid> getCorrelationId)
    {
        if (getCorrelationId == null)
            throw new ArgumentNullException(nameof(getCorrelationId));

        _getCorrelationId = getCorrelationId;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("correlationId");
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        var correlationId = _getCorrelationId(context);

        var proxy = new CorrelationIdConsumeContextProxy<TMessage>(context, correlationId);

        return next.SendAsync(proxy);
    }
}
