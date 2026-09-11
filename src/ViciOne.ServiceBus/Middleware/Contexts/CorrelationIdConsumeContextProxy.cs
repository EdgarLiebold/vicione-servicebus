using System;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Overrides the correlation identifier of a typed consume context without changing its message or other metadata.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
internal sealed class CorrelationIdConsumeContextProxy<TMessage> :
    ConsumeContextProxy<TMessage>
    where TMessage : class
{
    readonly Guid _correlationId;

    /// <summary>Creates a consume-context view with an explicit correlation identifier.</summary>
    /// <param name="context">The message consume context to wrap.</param>
    /// <param name="correlationId">The correlation identifier exposed by the proxy.</param>
    public CorrelationIdConsumeContextProxy(ConsumeContext<TMessage> context, Guid correlationId)
        : base(context)
    {
        _correlationId = correlationId;
    }

    /// <inheritdoc />
    public override Guid? CorrelationId => _correlationId;
}
