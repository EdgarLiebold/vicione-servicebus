using System;

namespace ViciOne.ServiceBus.Context;

/// <summary>
/// A consumer instance merged with a message consume context
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public class CorrelationIdConsumeContextProxy<TMessage> :
    ConsumeContextProxy<TMessage>
    where TMessage : class
{
    readonly Guid _correlationId;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="correlationId">The correlation id value.</param>
    public CorrelationIdConsumeContextProxy(ConsumeContext<TMessage> context, Guid correlationId)
        : base(context)
    {
        _correlationId = correlationId;
    }

    /// <summary>
    /// Gets the correlation id value.
    /// </summary>
    public override Guid? CorrelationId => _correlationId;
}
