using System;

namespace ViciOne.ServiceBus.Context;

/// <summary>A consumer instance merged with a message consume context.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class CorrelationIdConsumeContextProxy<TMessage> :
    ConsumeContextProxy<TMessage>
    where TMessage : class
{
    readonly Guid _correlationId;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="correlationId">The correlation id.</param>
    public CorrelationIdConsumeContextProxy(ConsumeContext<TMessage> context, Guid correlationId)
        : base(context)
    {
        _correlationId = correlationId;
    }

    /// <summary>Gets the correlation id.</summary>
    public override Guid? CorrelationId => _correlationId;
}
