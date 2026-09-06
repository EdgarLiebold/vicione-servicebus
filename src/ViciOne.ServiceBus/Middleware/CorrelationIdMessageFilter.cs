using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Extracts the CorrelationId from the message where there is a one-to-one correlation
/// identifier in the message (such as CorrelationId) and sets it in the header for use
/// by the saga repository.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public class CorrelationIdMessageFilter<TMessage> :
    IFilter<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly Func<ConsumeContext<TMessage>, Guid> _getCorrelationId;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="getCorrelationId">The get correlation id.</param>
    public CorrelationIdMessageFilter(Func<ConsumeContext<TMessage>, Guid> getCorrelationId)
    {
        if (getCorrelationId == null)
            throw new ArgumentNullException(nameof(getCorrelationId));

        _getCorrelationId = getCorrelationId;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("correlationId");
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        var correlationId = _getCorrelationId(context);

        var proxy = new CorrelationIdConsumeContextProxy<TMessage>(context, correlationId);

        return next.SendAsync(proxy);
    }
}
