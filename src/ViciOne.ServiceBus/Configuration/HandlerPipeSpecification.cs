using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds a handler delegate as the terminal filter for a message pipeline.</summary>
/// <typeparam name="TMessage">The message contract handled by the delegate.</typeparam>
public sealed class HandlerPipeSpecification<TMessage> :
    IPipeSpecification<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly MessageHandler<TMessage> _handler;

    /// <summary>Creates a terminal pipeline specification for a message-handler delegate.</summary>
    /// <param name="handler">The delegate invoked by the terminal filter.</param>
    public HandlerPipeSpecification(MessageHandler<TMessage> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    void IPipeSpecification<ConsumeContext<TMessage>>.Apply(IPipeBuilder<ConsumeContext<TMessage>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFilter(new HandlerMessageFilter<TMessage>(_handler));
    }

    IEnumerable<ValidationResult> ISpecification.Validate() => [];
}
